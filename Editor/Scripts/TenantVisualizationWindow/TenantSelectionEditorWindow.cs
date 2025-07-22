using System.Collections.Generic;
using System.IO;
using System.Linq;

using Unity.Properties;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    public class TenantSelectionEditorWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;
        [SerializeField] private VisualTreeAsset tenantVisualTree = default;
        [SerializeField] private VisualTreeAsset envVisualTree = default;
        [SerializeField] private VisualTreeAsset tenantConfigurationVisualTree = default;

        [SerializeField] private TenantConfigurationSystem tenantConfigurationSystem = default;

        private TenantVisualizationSettings tenantVisualizationSettings;

        private const string settings_folder_path = "Assets/Editor/TenantConfiguration";
        private const string settings_configuration_path = "TenantConfigurationSettings.asset";


        [MenuItem("Reflectis/SDK/Tenant Configuration/Show available tenants")]
        public static void ShowExample()
        {
            TenantSelectionEditorWindow wnd = GetWindow<TenantSelectionEditorWindow>();
            wnd.titleContent = new GUIContent("Show available tenants");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Instantiate UXML
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);

            string tenantVisualizationSettingsAssetGuid = AssetDatabase.FindAssets("t:" + typeof(TenantVisualizationSettings).Name).ToList().FirstOrDefault();
            tenantVisualizationSettings = AssetDatabase.LoadAssetAtPath<TenantVisualizationSettings>(AssetDatabase.GUIDToAssetPath(tenantVisualizationSettingsAssetGuid));

            if (tenantVisualizationSettings == null)
            {
                EnsureFolderExists(settings_folder_path);

                tenantVisualizationSettings = CreateInstance<TenantVisualizationSettings>();
                string settingsAssetPath = $"{settings_folder_path}/{settings_configuration_path}";
                AssetDatabase.CreateAsset(tenantVisualizationSettings, settingsAssetPath);
                AssetDatabase.SaveAssets();
            }


            if (!tenantVisualizationSettings)
            {
                tenantVisualizationSettings = CreateInstance<TenantVisualizationSettings>();
                AssetDatabase.CreateAsset(tenantVisualizationSettings, "Assets/TenantVisualizationSettings.asset");
            }


            VisualElement selectedTenantConfigSection = root.Q<VisualElement>("SelectedTenantConfig");

            ScrollView scrollView = root.Q<ScrollView>();
            List<Toggle> toggles = new();
            foreach (var tenant in tenantVisualizationSettings.GetTenantConfigurations(tenantVisualizationSettings.TenantAssets))
            {
                VisualElement tenantElement = tenantVisualTree.Instantiate();
                tenantElement.Q<Label>().text = tenant.Item1;

                GroupBox togglesContainer = tenantElement.Q<GroupBox>();

                foreach (var envConfig in tenant.Item2)
                {
                    VisualElement envElement = envVisualTree.Instantiate();

                    Toggle toggle = envElement.Q<Toggle>();
                    toggles.Add(toggle);
                    toggle.dataSource = (tenant.Item1, envConfig.Key);
                    toggle.text = envConfig.Key;
                    toggle.RegisterCallbackOnce<ChangeEvent<bool>>(evt =>
                    {
                        if (evt.newValue)
                        {
                            tenantVisualizationSettings.SelectedConfig = envConfig.Value;
                            // Needed because when the SelectedConfig changes, the dataSource of the SelectedTenantConfig VisualElement needs to be updated
                            selectedTenantConfigSection.dataSource = tenantVisualizationSettings.SelectedConfig;

                            tenantVisualizationSettings.SelectedEnv = envConfig.Key;
                            tenantVisualizationSettings.SelectedTenant = tenant.Item1;
                        }
                    });

                    DataBinding toggleBinding = new()
                    {
                        dataSourcePath = new(),
                        bindingMode = BindingMode.ToTarget
                    };
                    toggleBinding.sourceToUiConverters.AddConverter(
                        (ref (string, string) value) => value.Item1 == tenantVisualizationSettings.SelectedTenant && value.Item2 == tenantVisualizationSettings.SelectedEnv);
                    toggle.SetBinding(nameof(toggle.value), toggleBinding);

                    togglesContainer.Add(envElement);
                }
                scrollView.Add(tenantElement);
            }

            selectedTenantConfigSection.dataSource = tenantVisualizationSettings.SelectedConfig;

            Label appIdLabel = selectedTenantConfigSection.Q<Label>("AppId");
            appIdLabel.SetBinding(nameof(appIdLabel.text), new DataBinding()
            {
                dataSourcePath = PropertyPath.FromName(nameof(AppConfig.AppId)),
                bindingMode = BindingMode.ToTarget
            });

            Label appSecretLabel = selectedTenantConfigSection.Q<Label>("AppSecret");
            appSecretLabel.SetBinding(nameof(appSecretLabel.text), new DataBinding()
            {
                dataSourcePath = PropertyPath.FromName(nameof(AppConfig.AppSecret)),
                bindingMode = BindingMode.ToTarget
            });

            Label tenantConfigurationUrlLabel = selectedTenantConfigSection.Q<Label>("TenantConfigurationUrl");
            tenantConfigurationUrlLabel.SetBinding(nameof(tenantConfigurationUrlLabel.text), new DataBinding()
            {
                dataSourcePath = PropertyPath.FromName(nameof(AppConfig.TenantConfigurationApiUrl)),
                bindingMode = BindingMode.ToTarget
            });

            Label tenantConfigurationVersionLabel = selectedTenantConfigSection.Q<Label>("TenantConfigurationVersion");
            tenantConfigurationVersionLabel.SetBinding(nameof(tenantConfigurationVersionLabel.text), new DataBinding()
            {
                dataSourcePath = PropertyPath.FromName(nameof(AppConfig.TenantConfigurationApiVersion)),
                bindingMode = BindingMode.ToTarget
            });


            VisualElement buttonsContainer = root.Q<VisualElement>("ButtonsContainer");

            Button configureAppButton = buttonsContainer.Q<Button>("ConfigureAppButton");
            configureAppButton.clicked += () =>
            {
                tenantVisualizationSettings.ConfigurationScript.ConfigureApp(tenantVisualizationSettings.SelectedConfig);
            };

            Button buildButton = buttonsContainer.Q<Button>("BuildButton");
            buildButton.clicked += () =>
            {
                tenantVisualizationSettings.ConfigurationScript.ConfigureApp(tenantVisualizationSettings.SelectedConfig);
            };

            Button configureTenantButton = buttonsContainer.Q<Button>("ConfigureTenantButton");
            configureTenantButton.clicked += () =>
            {
                TenantConfigurationEditorWindow.ShowWindow();
                GetWindow<TenantConfigurationEditorWindow>().ShowTenantConfigurationWindow(tenantVisualizationSettings.SelectedConfig);
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
