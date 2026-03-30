using Newtonsoft.Json;

using Reflectis.SDK.Core.ApiSystem;
using Reflectis.SDK.Core.Utilities;
using Reflectis.SDK.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Unity.Properties;

using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    public class TenantSelectionWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;
        [SerializeField] private VisualTreeAsset appVisualTree = default;
        [SerializeField] private VisualTreeAsset envVisualTree = default;
        [SerializeField] private VisualTreeAsset appConfigurationVisualTree = default;

        private AppConfigurationSettings appConfigurationSettings;

        private const string settings_folder_path = "Assets/Editor/TenantConfiguration";
        private const string settings_configuration_path = "TenantConfiguration.asset";

        private Label loginStatusLabel;
        private Button loginButton;
        private Button logoutButton;

        [MenuItem("Reflectis/Show available tenants")]
        public static void ShowExample()
        {
            TenantSelectionWindow wnd = GetWindow<TenantSelectionWindow>();
            wnd.titleContent = new GUIContent("Show available tenants");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Instantiate UXML
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);

            appConfigurationSettings = FindOrCreateAppConfigurationSettings();

            // Show the AppConfigurationSettings SO in an ObjectField
            ObjectField appConfigSettingsField = root.Q<ObjectField>("AppConfigurationSettingsField");
            appConfigSettingsField.objectType = typeof(AppConfigurationSettings);
            appConfigSettingsField.value = appConfigurationSettings;
            appConfigSettingsField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is AppConfigurationSettings newSettings && newSettings != null)
                {
                    Selection.activeObject = newSettings;
                    EditorGUIUtility.PingObject(newSettings);
                }
            });

            VisualElement selectedAppConfigSection = root.Q<VisualElement>("SelectedAppConfig");

            ScrollView scrollView = root.Q<ScrollView>();
            List<Toggle> toggles = new();
            foreach (var app in appConfigurationSettings.GetAppIdentification(appConfigurationSettings.AppAssets))
            {
                VisualElement appElement = appVisualTree.Instantiate();
                appElement.Q<Label>().text = app.Item1;

                GroupBox togglesContainer = appElement.Q<GroupBox>();

                foreach (var envConfig in app.Item2)
                {
                    VisualElement envElement = envVisualTree.Instantiate();

                    Toggle toggle = envElement.Q<Toggle>();
                    toggles.Add(toggle);
                    toggle.dataSource = (app.Item1, envConfig.Key);
                    toggle.text = envConfig.Key;
                    toggle.RegisterCallback<ChangeEvent<bool>>(evt =>
                    {
                        if (evt.newValue)
                        {
                            appConfigurationSettings.SelectedConfig = envConfig.Value;
                            // Needed because when the SelectedConfig changes, the dataSource of the SelectedAppConfig VisualElement needs to be updated
                            selectedAppConfigSection.dataSource = appConfigurationSettings.SelectedConfig;

                            appConfigurationSettings.SelectedEnv = envConfig.Key;
                            appConfigurationSettings.SelectedApp = app.Item1;
                        }
                    });

                    DataBinding toggleBinding = new()
                    {
                        dataSourcePath = new(),
                        bindingMode = BindingMode.ToTarget
                    };
                    toggleBinding.sourceToUiConverters.AddConverter(
                        (ref (string, string) value) => value.Item1 == appConfigurationSettings.SelectedApp && value.Item2 == appConfigurationSettings.SelectedEnv);
                    toggle.SetBinding(nameof(toggle.value), toggleBinding);

                    togglesContainer.Add(envElement);
                }
                scrollView.Add(appElement);
            }

            selectedAppConfigSection.dataSource = appConfigurationSettings.SelectedConfig;

            Label appIdLabel = selectedAppConfigSection.Q<VisualElement>("AppId").Q<Label>("Value");
            appIdLabel.SetBinding(nameof(appIdLabel.text), new DataBinding()
            {
                dataSourcePath = new PropertyPath($"{nameof(AppIdentification.Credential)}.{nameof(HmacCredential.AppId)}"),
                bindingMode = BindingMode.ToTarget
            });

            Label appSecretLabel = selectedAppConfigSection.Q<VisualElement>("AppSecret").Q<Label>("Value");
            appSecretLabel.SetBinding(nameof(appSecretLabel.text), new DataBinding()
            {
                dataSourcePath = new PropertyPath($"{nameof(AppIdentification.Credential)}.{nameof(HmacCredential.AppSecret)}"),
                bindingMode = BindingMode.ToTarget
            });

            Label appConfigurationUrlLabel = selectedAppConfigSection.Q<VisualElement>("ApiBaseUrl").Q<Label>("Value");
            appConfigurationUrlLabel.SetBinding(nameof(appConfigurationUrlLabel.text), new DataBinding()
            {
                dataSourcePath = PropertyPath.FromName(nameof(AppIdentification.ApiBaseUrl)),
                bindingMode = BindingMode.ToTarget
            });

            Label appConfigurationVersionLabel = selectedAppConfigSection.Q<VisualElement>("ApiVersion").Q<Label>("Value");
            appConfigurationVersionLabel.SetBinding(nameof(appConfigurationVersionLabel.text), new DataBinding()
            {
                dataSourcePath = PropertyPath.FromName(nameof(AppIdentification.ApiVersion)),
                bindingMode = BindingMode.ToTarget
            });


            VisualElement buttonsContainer = root.Q<VisualElement>("ButtonsContainer");
            buttonsContainer.dataSource = appConfigurationSettings;

            Button configureTenantButton = buttonsContainer.Q<Button>("ChangeConfigButton");
            configureTenantButton.clicked += async () =>
            {
                await appConfigurationSettings.ConfigurationScript.ConfigureApp(appConfigurationSettings.SelectedConfig);
            };

            Button buildButton = buttonsContainer.Q<Button>("BuildButton");
            DataBinding buildButtonBinding = new()
            {
                dataSourcePath = PropertyPath.FromName(nameof(AppConfigurationSettings.BuildScript)),
                bindingMode = BindingMode.ToTarget
            };
            buildButtonBinding.sourceToUiConverters.AddConverter((ref BuildScriptBase value) => value != null);
            buildButton.SetBinding(nameof(Button.enabledSelf), buildButtonBinding);
            buildButton.clicked += () =>
            {
                appConfigurationSettings.BuildScript.Build(appConfigurationSettings.SelectedEnv, appConfigurationSettings.SelectedConfig);
            };

            Button configureAppButton = buttonsContainer.Q<Button>("ConfigureAppButton");
            configureAppButton.clicked += () =>
            {
                AppConfigurationWindow.ShowWindow();
                GetWindow<AppConfigurationWindow>().ShowAppConfigurationWindow(appConfigurationSettings.SelectedConfig, appConfigurationSettings);
            };

            // Login section
            loginStatusLabel = root.Q<Label>("LoginStatusLabel");

            loginButton = root.Q<Button>("LoginButton");
            loginButton.clicked += OnLoginClicked;

            logoutButton = root.Q<Button>("LogoutButton");
            logoutButton.clicked += OnLogoutClicked;

            UpdateLoginUI();

            EditorLoginState.OnLoginStateChanged += UpdateLoginUI;
        }

        private void OnDestroy()
        {
            EditorLoginState.OnLoginStateChanged -= UpdateLoginUI;
        }

        private void UpdateLoginUI()
        {
            if (loginStatusLabel == null) return;

            bool loggedIn = EditorLoginState.IsLoggedIn;

            if (loggedIn)
            {
                string tenantLabel = EditorLoginState.CurrentTenant?.Label ?? "Unknown";
                string username = EditorLoginState.Username;
                string userPart = !string.IsNullOrEmpty(username) ? $" · {username}" : string.Empty;
                loginStatusLabel.text = $"Logged in ({tenantLabel}{userPart})";
                loginStatusLabel.style.color = new Color(0.2f, 0.8f, 0.2f);
            }
            else
            {
                loginStatusLabel.text = "Not logged in";
                loginStatusLabel.style.color = new Color(0.8f, 0.2f, 0.2f);
            }

            if (loginButton != null)
                loginButton.style.display = loggedIn ? DisplayStyle.None : DisplayStyle.Flex;
            if (logoutButton != null)
                logoutButton.style.display = loggedIn ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnLogoutClicked()
        {
            AzureAuthService.Reset();
            EditorLoginState.Clear();
            Debug.Log("[TenantSelectionWindow] Logged out.");
        }

        private async void OnLoginClicked()
        {
            AppIdentification config = appConfigurationSettings.SelectedConfig;
            if (config == null)
            {
                Debug.LogError("[TenantSelectionWindow] No tenant configuration selected.");
                return;
            }

            try
            {
                loginStatusLabel.text = "Fetching tenant data...";

                // 1. Get tenant data
                ApiResponse<Tenant> tenantResp = await TenantConfigurationApi.GetTenantData(config);
                if (!tenantResp.IsSuccess)
                {
                    Debug.LogError($"[TenantSelectionWindow] Failed to get tenant data: {tenantResp.ReasonPhrase}");
                    loginStatusLabel.text = "Login failed (tenant data)";
                    return;
                }

                Tenant tenant = tenantResp.Content;

                // 2. Get custom config for B2C params
                ApiResponse<Newtonsoft.Json.Linq.JObject> customConfigResp = await TenantConfigurationApi.GetAppCustomConfig(config);
                if (!customConfigResp.IsSuccess)
                {
                    Debug.LogError($"[TenantSelectionWindow] Failed to get app custom config: {customConfigResp.ReasonPhrase}");
                    loginStatusLabel.text = "Login failed (custom config)";
                    return;
                }

                AzureB2CConfig b2cConfig = AzureB2CConfig.FromAppCustomConfig(customConfigResp.Content);
                if (b2cConfig == null)
                {
                    Debug.LogError("[TenantSelectionWindow] Failed to parse Azure B2C config from custom config.");
                    loginStatusLabel.text = "Login failed (B2C config)";
                    return;
                }

                if (string.IsNullOrEmpty(b2cConfig.Tenant) || string.IsNullOrEmpty(b2cConfig.Policy))
                {
                    Debug.LogError($"[TenantSelectionWindow] Invalid B2C config — Tenant: '{b2cConfig.Tenant}', Policy: '{b2cConfig.Policy}'. Check that the app custom config contains an 'azureB2C' object with 'tenant' and 'policy' fields.");
                    loginStatusLabel.text = "Login failed (B2C config invalid)";
                    return;
                }

                // 3. clientId = AppIdentification.Credential.AppId
                string clientId = config.Credential.AppId.ToString();

                // 4. Initialize Azure auth
                loginStatusLabel.text = "Logging in...";
                AzureAuthService.Init(clientId, b2cConfig.Tenant, b2cConfig.Policy, b2cConfig.RedirectUri);

                // 5. Build scopes
                string[] scopes = new[]
                {
                    "openid",
                    "offline_access",
                    $"https://{b2cConfig.Tenant}.onmicrosoft.com/{b2cConfig.ProfileApiId}/access"
                };

                // 6. Interactive login
                (string accessToken, string username) = await AzureAuthService.LoginInteractive(scopes);

                // 7. Get tokens from profile API
                loginStatusLabel.text = "Getting tokens...";
                string tokensJson = await AzureAuthService.GetUserDataAsync(tenant.Config.ProfileApiUrl, accessToken);
                if (string.IsNullOrEmpty(tokensJson))
                {
                    Debug.LogError("[TenantSelectionWindow] Failed to get user tokens.");
                    loginStatusLabel.text = "Login failed (tokens)";
                    return;
                }

                JwtToken[] tokens = JsonConvert.DeserializeObject<JwtToken[]>(tokensJson);

                // 8. Find token matching tenant label
                string apiLabel = tenant.Label;
                JwtToken matchingToken = tokens.FirstOrDefault(t => t.ApiLabel == apiLabel);
                if (matchingToken == null)
                {
                    Debug.LogError($"[TenantSelectionWindow] No token found for API label: {apiLabel}");
                    loginStatusLabel.text = $"Login failed (no token for {apiLabel})";
                    return;
                }

                // 9. Store login state
                EditorLoginState.Set(matchingToken.Bearer, tenant, username);

                Debug.Log($"[TenantSelectionWindow] Login successful for tenant: {tenant.Label}, user: {username}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TenantSelectionWindow] Login error: {ex.Message}");
                loginStatusLabel.text = "Login failed";
            }
        }

        /// <summary>
        /// Resolves the active AppConfigurationSettings with the following priority:
        /// 1. First asset with IsSelected == true (original logic)
        /// 2. First asset with empty TargetPlatform
        /// 3. Create a new asset
        /// </summary>
        private AppConfigurationSettings FindOrCreateAppConfigurationSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(AppConfigurationSettings).Name);
            List<AppConfigurationSettings> allSettings = new();
            foreach (string guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<AppConfigurationSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    allSettings.Add(asset);
            }

            // 1. Original logic: first with IsSelected == true
            AppConfigurationSettings selected = allSettings.FirstOrDefault(x => x.IsSelected);
            if (selected != null)
                return selected;

            // 2. Fallback: first with empty TargetPlatform
            AppConfigurationSettings fallback = allSettings.FirstOrDefault(x => string.IsNullOrEmpty(x.TargetPlatform));
            if (fallback != null)
                return fallback;

            // 3. Create a new one
            EnsureFolderExists(settings_folder_path);
            AppConfigurationSettings newSettings = CreateInstance<AppConfigurationSettings>();
            string assetPath = $"{settings_folder_path}/{settings_configuration_path}";
            AssetDatabase.CreateAsset(newSettings, assetPath);
            AssetDatabase.SaveAssets();
            return newSettings;
        }

        private void EnsureFolderExists(string folderPath)
        {
            string[] folders = folderPath.Split('/');
            string currentPath = "";

            foreach (string folder in folders)
            {
                currentPath = Path.Combine(currentPath, folder);
                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    AssetDatabase.CreateFolder(Path.GetDirectoryName(currentPath), Path.GetFileName(currentPath));
                }
            }
        }
    }

}
