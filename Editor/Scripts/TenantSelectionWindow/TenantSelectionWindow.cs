using Reflectis.SDK.Core.ApiSystem;
using Reflectis.SDK.Core.Utilities;

using System.Collections.Generic;
using System.IO;
using System.Linq;

using Unity.Properties;

using UnityEditor;

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


        [MenuItem("Reflectis/SDK/TenantConfiguration/Show available tenants")]
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

            List<string> tenantConfigurationSettingsAssetGuid = AssetDatabase.FindAssets("t:" + typeof(AppConfigurationSettings).Name).ToList();
            List<AppConfigurationSettings> appConfigurationSettingsList = new();
            foreach (var item in tenantConfigurationSettingsAssetGuid)
            {
                appConfigurationSettingsList.Add(AssetDatabase.LoadAssetAtPath<AppConfigurationSettings>(AssetDatabase.GUIDToAssetPath(item)));
            }
            appConfigurationSettings = appConfigurationSettingsList.FirstOrDefault(x => x.IsSelected);


            if (appConfigurationSettings == null)
            {
                EnsureFolderExists(settings_folder_path);

                appConfigurationSettings = CreateInstance<AppConfigurationSettings>();
                string settingsAssetPath = $"{settings_folder_path}/{settings_configuration_path}";
                AssetDatabase.CreateAsset(appConfigurationSettings, settingsAssetPath);
                AssetDatabase.SaveAssets();
            }


            if (!appConfigurationSettings)
            {
                appConfigurationSettings = CreateInstance<AppConfigurationSettings>();
                AssetDatabase.CreateAsset(appConfigurationSettings, "Assets/TenantConfigurationSettings.asset");
            }


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
            //DataBinding configureTenantButtonBinding = new()
            //{
            //    dataSourcePath = PropertyPath.FromName(nameof(AppConfigurationSettings.ConfigurationScript)),
            //    bindingMode = BindingMode.ToTarget
            //};
            //configureTenantButtonBinding.sourceToUiConverters.AddConverter((ref AbstractAppConfigurator value) => value != null);
            //configureTenantButton.SetBinding(nameof(Button.enabledSelf), configureTenantButtonBinding);
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

            //foreach (var button in new List<Button>() { configureTenantButton, configureAppButton })
            //{
            //    DataBinding selectedConfigBinding = new()
            //    {
            //        dataSourcePath = PropertyPath.FromName(nameof(AppConfigurationSettings.SelectedConfig)),
            //        bindingMode = BindingMode.ToTarget
            //    };
            //    selectedConfigBinding.sourceToUiConverters.AddConverter((ref AppConfigurationSettings value) => value != null);
            //    button.SetBinding(nameof(Button.enabledSelf), selectedConfigBinding);
            //}
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
