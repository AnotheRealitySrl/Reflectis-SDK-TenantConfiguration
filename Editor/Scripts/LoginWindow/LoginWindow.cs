using Newtonsoft.Json;

using Reflectis.SDK.Core.ApiSystem;
using Reflectis.SDK.Core.Utilities;
using Reflectis.SDK.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    public class LoginWindow : EditorWindow
    {
        private const string uxml_path = "Packages/com.anotherealitysrl.reflectis-sdk-tenantconfiguration/Editor/Scripts/LoginWindow/LoginWindow.uxml";
        private const string app_item_uxml_path = "Packages/com.anotherealitysrl.reflectis-sdk-tenantconfiguration/Editor/Scripts/TenantSelectionWindow/TenantSelectionWindow_Item.uxml";
        private const string env_item_uxml_path = "Packages/com.anotherealitysrl.reflectis-sdk-tenantconfiguration/Editor/Scripts/TenantSelectionWindow/TenantSelectionWindow_Env.uxml";

        private AppConfigurationSettings appConfigurationSettings;
        private AppIdentification selectedConfig;
        private string selectedApp;
        private string selectedEnv;

        private Label loginStatusLabel;
        private Label tenantMismatchLabel;
        private Button loginButton;
        private Button logoutButton;

        private const string settings_folder_path = "Assets/Editor/TenantConfiguration";
        private const string settings_configuration_path = "TenantConfiguration.asset";

        [MenuItem("Reflectis/Login")]
        public static void ShowWindow()
        {
            LoginWindow wnd = GetWindow<LoginWindow>();
            wnd.titleContent = new GUIContent("Reflectis Login");
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxml_path);
            var appVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(app_item_uxml_path);
            var envVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(env_item_uxml_path);

            if (visualTreeAsset == null)
            {
                Debug.LogError($"[LoginWindow] Could not load UXML at {uxml_path}");
                return;
            }

            VisualElement labelFromUXML = visualTreeAsset.Instantiate();
            root.Add(labelFromUXML);

            appConfigurationSettings = FindOrCreateAppConfigurationSettings();

            // Populate tenant/env list
            ScrollView scrollView = root.Q<ScrollView>("AppScrollView");
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
                    toggle.text = envConfig.Key;

                    string capturedApp = app.Item1;
                    string capturedEnv = envConfig.Key;
                    AppIdentification capturedConfig = envConfig.Value;

                    toggle.RegisterCallback<ChangeEvent<bool>>(evt =>
                    {
                        if (evt.newValue)
                        {
                            selectedConfig = capturedConfig;
                            selectedApp = capturedApp;
                            selectedEnv = capturedEnv;

                            // Uncheck other toggles
                            foreach (var t in toggles)
                            {
                                if (t != toggle)
                                    t.SetValueWithoutNotify(false);
                            }

                            UpdateMismatchWarning();
                        }
                    });

                    // Pre-select if matches current appConfigurationSettings selection
                    if (app.Item1 == appConfigurationSettings.SelectedApp && envConfig.Key == appConfigurationSettings.SelectedEnv)
                    {
                        toggle.SetValueWithoutNotify(true);
                        selectedConfig = envConfig.Value;
                        selectedApp = app.Item1;
                        selectedEnv = envConfig.Key;
                    }

                    togglesContainer.Add(envElement);
                }
                scrollView.Add(appElement);
            }

            // Login section
            tenantMismatchLabel = root.Q<Label>("TenantMismatchLabel");
            loginStatusLabel = root.Q<Label>("LoginStatusLabel");
            loginButton = root.Q<Button>("LoginButton");
            logoutButton = root.Q<Button>("LogoutButton");

            loginButton.clicked += OnLoginClicked;
            logoutButton.clicked += OnLogoutClicked;

            UpdateLoginUI();
            UpdateMismatchWarning();
            EditorLoginState.OnLoginStateChanged += OnLoginStateChanged;
        }

        private void OnDestroy()
        {
            EditorLoginState.OnLoginStateChanged -= OnLoginStateChanged;
        }

        private void OnLoginStateChanged()
        {
            UpdateLoginUI();
            UpdateMismatchWarning();
        }

        private void UpdateMismatchWarning()
        {
            if (tenantMismatchLabel == null) return;

            if (EditorLoginState.IsLoggedIn
                && !string.IsNullOrEmpty(selectedApp)
                && !EditorLoginState.IsLoggedInto(selectedApp, selectedEnv))
            {
                tenantMismatchLabel.text = $"Warning: you are logged into {EditorLoginState.LoggedInApp}/{EditorLoginState.LoggedInEnv}. Logging in here will switch your session.";
                tenantMismatchLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                tenantMismatchLabel.style.display = DisplayStyle.None;
            }
        }

        private void UpdateLoginUI()
        {
            if (loginStatusLabel == null) return;

            bool loggedIn = EditorLoginState.IsLoggedIn;

            if (loggedIn)
            {
                string tenantLabel = EditorLoginState.CurrentTenant?.Label ?? "Unknown";
                string username = EditorLoginState.Username;
                string userPart = !string.IsNullOrEmpty(username) ? $" - {username}" : string.Empty;
                string rolePart = EditorLoginState.IsTenantManager ? " [TenantManager]" : "";
                loginStatusLabel.text = $"Logged in: {tenantLabel}{userPart}{rolePart}";
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
            Debug.Log("[LoginWindow] Logged out.");
        }

        private async void OnLoginClicked()
        {
            if (selectedConfig == null)
            {
                Debug.LogError("[LoginWindow] No tenant configuration selected.");
                return;
            }

            try
            {
                loginStatusLabel.text = "Fetching tenant data...";

                // 1. Get tenant data
                ApiResponse<Tenant> tenantResp = await TenantConfigurationApi.GetTenantData(selectedConfig);
                if (!tenantResp.IsSuccess)
                {
                    Debug.LogError($"[LoginWindow] Failed to get tenant data: {tenantResp.ReasonPhrase}");
                    loginStatusLabel.text = "Login failed (tenant data)";
                    return;
                }

                Tenant tenant = tenantResp.Content;
                Debug.Log($"[LoginWindow] Tenant: {tenant.Label}, ApplicationApiUrl: {tenant.Config.ApplicationApiUrl}, ProfileApiUrl: {tenant.Config.ProfileApiUrl}");

                // 2. Get custom config for B2C params
                ApiResponse<Newtonsoft.Json.Linq.JObject> customConfigResp = await TenantConfigurationApi.GetAppCustomConfig(selectedConfig);
                if (!customConfigResp.IsSuccess)
                {
                    Debug.LogError($"[LoginWindow] Failed to get app custom config: {customConfigResp.ReasonPhrase}");
                    loginStatusLabel.text = "Login failed (custom config)";
                    return;
                }

                AzureB2CConfig b2cConfig = AzureB2CConfig.FromAppCustomConfig(customConfigResp.Content);
                if (b2cConfig == null)
                {
                    Debug.LogError("[LoginWindow] Failed to parse Azure B2C config from custom config.");
                    loginStatusLabel.text = "Login failed (B2C config)";
                    return;
                }

                if (string.IsNullOrEmpty(b2cConfig.Tenant) || string.IsNullOrEmpty(b2cConfig.Policy))
                {
                    Debug.LogError($"[LoginWindow] Invalid B2C config — Tenant: '{b2cConfig.Tenant}', Policy: '{b2cConfig.Policy}'.");
                    loginStatusLabel.text = "Login failed (B2C config invalid)";
                    return;
                }

                // 3. clientId = AppIdentification.Credential.AppId
                string clientId = selectedConfig.Credential.AppId.ToString();

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
                    Debug.LogError("[LoginWindow] Failed to get user tokens.");
                    loginStatusLabel.text = "Login failed (tokens)";
                    return;
                }

                JwtToken[] tokens = JsonConvert.DeserializeObject<JwtToken[]>(tokensJson);

                // 8. Find token matching tenant label
                string apiLabel = tenant.Label;
                JwtToken matchingToken = tokens.FirstOrDefault(t => t.ApiLabel == apiLabel);
                if (matchingToken == null)
                {
                    Debug.LogError($"[LoginWindow] No token found for API label: {apiLabel}");
                    loginStatusLabel.text = $"Login failed (no token for {apiLabel})";
                    return;
                }

                // 9. Check if user is TenantManager
                bool isTenantManager = false;
                try
                {
                    string applicationApiUrl = tenant.Config.ApplicationApiUrl;
                    if (!string.IsNullOrEmpty(applicationApiUrl))
                    {
                        using var httpClient = new HttpClient();
                        using var permRequest = new HttpRequestMessage(HttpMethod.Get, $"{applicationApiUrl}/tenants/app/Unity/permissions/my?api-version=2");
                        permRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", matchingToken.Bearer);
                        var permResponse = await httpClient.SendAsync(permRequest);
                        isTenantManager = permResponse.IsSuccessStatusCode;
                    }
                }
                catch (Exception permEx)
                {
                    Debug.LogWarning($"[LoginWindow] Could not check TenantManager role: {permEx.Message}");
                }

                // 10. Configure API systems for the selected tenant (same as "Switch tenant")
                loginStatusLabel.text = "Configuring API systems...";
                Debug.Log($"[LoginWindow] Tenant config URLs — ProfileApi: {tenant.Config.ProfileApiUrl}, ApplicationApi: {tenant.Config.ApplicationApiUrl}, ApplicationUrl: {tenant.Config.ApplicationUrl}, RealtimeApi: {tenant.Config.RealtimeApiUrl}, AiApi: {tenant.Config.AIApiUrl}");
                Debug.Log($"[LoginWindow] Selected config — ApiBaseUrl: {selectedConfig.ApiBaseUrl}, AppId: {selectedConfig.Credential.AppId}");
                if (appConfigurationSettings.ConfigurationScript != null)
                {
                    await appConfigurationSettings.ConfigurationScript.ConfigureApp(selectedConfig);
                    Debug.Log("[LoginWindow] API systems configured for selected tenant.");
                }

                // 11. Store login state
                EditorLoginState.Set(matchingToken.Bearer, tenant, username, isTenantManager, selectedApp, selectedEnv);

                Debug.Log($"[LoginWindow] Login successful for tenant: {tenant.Label}, user: {username}, isTenantManager: {isTenantManager}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoginWindow] Login error: {ex.Message}");
                loginStatusLabel.text = "Login failed";
            }
        }

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

            AppConfigurationSettings selected = allSettings.FirstOrDefault(x => x.IsSelected);
            if (selected != null)
                return selected;

            AppConfigurationSettings fallback = allSettings.FirstOrDefault(x => string.IsNullOrEmpty(x.TargetPlatform));
            if (fallback != null)
                return fallback;

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
                    AssetDatabase.CreateFolder(Path.GetDirectoryName(currentPath), Path.GetFileName(currentPath));
            }
        }
    }
}
