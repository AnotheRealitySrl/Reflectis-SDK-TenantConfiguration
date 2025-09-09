using Reflectis.SDK.Core.ApiSystem;
using Reflectis.SDK.Core.Utilities;
using Reflectis.SDK.TenantConfiguration;
using Reflectis.SDK.TenantConfiguration.Editor;

using System.Collections.Generic;
using System.IO;
using System.Linq;

using Unity.Properties;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace Reflectis.SDK.AppConfiguration.Editor
{
    public class TenantSelectionWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;
        [SerializeField] private VisualTreeAsset appVisualTree = default;
        [SerializeField] private VisualTreeAsset envVisualTree = default;
        [SerializeField] private VisualTreeAsset appConfigurationVisualTree = default;

        [SerializeField] private TenantConfigurationSystem tenantConfigurationSystem = default;

        private AppConfigurationSettings appVisualizationSettings;

        private const string settings_folder_path = "Assets/Editor/AppConfiguration";
        private const string settings_configuration_path = "AppConfigurationSettings.asset";


        [MenuItem("Reflectis/SDK/AppConfiguration/Show available apps")]
        public static void ShowExample()
        {
            TenantSelectionWindow wnd = GetWindow<TenantSelectionWindow>();
            wnd.titleContent = new GUIContent("Show available apps");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Instantiate UXML
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);

            string appVisualizationSettingsAssetGuid = AssetDatabase.FindAssets("t:" + typeof(AppConfigurationSettings).Name).ToList().FirstOrDefault();
            appVisualizationSettings = AssetDatabase.LoadAssetAtPath<AppConfigurationSettings>(AssetDatabase.GUIDToAssetPath(appVisualizationSettingsAssetGuid));

            if (appVisualizationSettings == null)
            {
                EnsureFolderExists(settings_folder_path);

                appVisualizationSettings = CreateInstance<AppConfigurationSettings>();
                string settingsAssetPath = $"{settings_folder_path}/{settings_configuration_path}";
                AssetDatabase.CreateAsset(appVisualizationSettings, settingsAssetPath);
                AssetDatabase.SaveAssets();
            }


            if (!appVisualizationSettings)
            {
                appVisualizationSettings = CreateInstance<AppConfigurationSettings>();
                AssetDatabase.CreateAsset(appVisualizationSettings, "Assets/AppVisualizationSettings.asset");
            }


            VisualElement selectedAppConfigSection = root.Q<VisualElement>("SelectedAppConfig");

            ScrollView scrollView = root.Q<ScrollView>();
            List<Toggle> toggles = new();
            foreach (var app in appVisualizationSettings.GetAppIdentification(appVisualizationSettings.AppAssets))
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
                            Debug.Log($"Selected App: {app.Item1}, Env: {envConfig.Key}");
                            appVisualizationSettings.SelectedConfig = envConfig.Value;
                            // Needed because when the SelectedConfig changes, the dataSource of the SelectedAppConfig VisualElement needs to be updated
                            selectedAppConfigSection.dataSource = appVisualizationSettings.SelectedConfig;

                            appVisualizationSettings.SelectedEnv = envConfig.Key;
                            appVisualizationSettings.SelectedApp = app.Item1;
                        }
                    });

                    DataBinding toggleBinding = new()
                    {
                        dataSourcePath = new(),
                        bindingMode = BindingMode.ToTarget
                    };
                    toggleBinding.sourceToUiConverters.AddConverter(
                        (ref (string, string) value) => value.Item1 == appVisualizationSettings.SelectedApp && value.Item2 == appVisualizationSettings.SelectedEnv);
                    toggle.SetBinding(nameof(toggle.value), toggleBinding);

                    togglesContainer.Add(envElement);
                }
                scrollView.Add(appElement);
            }

            selectedAppConfigSection.dataSource = appVisualizationSettings.SelectedConfig;

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

            buttonsContainer.dataSource = appVisualizationSettings;
            Button configureAppButton = buttonsContainer.Q<Button>("ChangeConfigButton");
            DataBinding configureAppButtonBinding = new()
            {
                dataSourcePath = PropertyPath.FromName(nameof(AppConfigurationSettings.ConfigurationScript)),
                bindingMode = BindingMode.ToTarget
            };
            configureAppButtonBinding.sourceToUiConverters.AddConverter((ref AbstractAppConfigurator value) => value != null);
            configureAppButton.SetBinding(nameof(Button.enabledSelf), configureAppButtonBinding);
            configureAppButton.clicked += () =>
            {
                appVisualizationSettings.ConfigurationScript.ConfigureApp(appVisualizationSettings.SelectedConfig);
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
                appVisualizationSettings.BuildScript.Build(appVisualizationSettings.SelectedEnv, appVisualizationSettings.SelectedConfig);
            };

            Button configureAppButton2 = buttonsContainer.Q<Button>("ConfigureAppButton");

            configureAppButton2.clicked += () =>
            {
                AppConfigurationWindow.ShowWindow();
                GetWindow<AppConfigurationWindow>().ShowAppConfigurationWindow(appVisualizationSettings.SelectedConfig);
            };

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
