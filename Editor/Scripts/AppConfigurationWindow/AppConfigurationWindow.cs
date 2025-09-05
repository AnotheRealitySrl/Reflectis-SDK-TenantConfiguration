using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reflectis.SDK.Core.ApiSystem;
using Reflectis.SDK.Core.Utilities;

using System;
using System.Collections.Generic;

using Unity.Properties;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    // Wrapper class to make configuration items editable via data binding
    [System.Serializable]
    public class EditableConfigItem
    {
        [CreateProperty] public string Key { get; set; }
        [CreateProperty] public object Value { get; set; }

        public EditableConfigItem(string key, object value)
        {
            Key = key;
            Value = value;
        }
    }

    public class AppConfigurationWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;

        [SerializeField] private VisualTreeAsset configurationItemTextField = default;
        [SerializeField] private VisualTreeAsset configurationItemNumericField = default;
        [SerializeField] private VisualTreeAsset configurationItemCheckBox = default;

        private VisualElement root;

        private List<EditableConfigItem> editableAppConfigurationItems;

        private VisualElement appConfigContainer;

        public static AppConfigurationWindow ShowWindow()
        {
            AppConfigurationWindow wnd = GetWindow<AppConfigurationWindow>();
            wnd.titleContent = new GUIContent("AppConfigurationEditorWindow");

            return wnd;
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            root = rootVisualElement;

            // Instantiate UXML
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);
        }

        public async void ShowAppConfigurationWindow(AppIdentification app)
        {
            HttpSystem httpSystem = CreateInstance<HttpSystem>();
            TenantConfigurationSystem tenantConfigurationSystemAdmin = CreateInstance<TenantConfigurationSystem>();

            AppIdentification appConfig = new(app.Credential, app.ApiBaseUrl, app.ApiVersion);
            await tenantConfigurationSystemAdmin.Init(appConfig, httpSystem);

            VisualElement credentials = root.Q<VisualElement>("Credentials");
            credentials.Q<VisualElement>(nameof(HmacCredential.AppId)).Q<Label>("Value").text = app.Credential.AppId.ToString();
            credentials.Q<VisualElement>(nameof(HmacCredential.AppSecret)).Q<Label>("Value").text = app.Credential.AppSecret;


            JObject customAppConfig = (await tenantConfigurationSystemAdmin.GetAppCustomConfig()).Content;

            editableAppConfigurationItems = new List<EditableConfigItem>();
            foreach (var el in customAppConfig)
            {
                editableAppConfigurationItems.Add(new EditableConfigItem(el.Key, el.Value));
            }

            appConfigContainer = root.Q<VisualElement>("AppPropertiesContainer");

            // Create UI elements for each editable item
            PopolateContainer(editableAppConfigurationItems, appConfigContainer);

            Button updateAppbutton = root.Q<Button>("UpdateAppConfigurationButton");
            updateAppbutton.clicked += async () =>
            {
                // Convert the editable items back to the original dictionary format
                Dictionary<string, object> updatedConfig = new();
                foreach (var item in editableAppConfigurationItems)
                {
                    updatedConfig[item.Key] = item.Value;
                }
                // Update the tenant configuration
                //await tenantConfigurationSystemAdmin.UpdateTenantConfig(tenant.Id, JsonConvert.SerializeObject(updatedConfig));

                updatedConfig = new();
                foreach (var item in editableAppConfigurationItems)
                {
                    updatedConfig[item.Key] = item.Value;
                }
                await tenantConfigurationSystemAdmin.UpdateAppCustomConfig(JsonConvert.SerializeObject(updatedConfig));
                // Optionally, refresh the UI or show a success message
                Debug.Log($"App configuration updated successfully. New config: {JsonConvert.SerializeObject(updatedConfig)}");
            };
        }

        private void PopolateContainer(IEnumerable<EditableConfigItem> editableConfigItems, VisualElement container)
        {
            foreach (var editableItem in editableConfigItems)
            {
                VisualElement visualElement = null;
                switch (editableItem.Value)
                {
                    case string _:
                        visualElement = CreateTextFieldItem(editableItem);
                        break;
                    case System.Int64 _:
                        visualElement = CreateNumericFieldItem(editableItem);
                        break;
                    case bool _:
                        visualElement = CreateCheckBoxItem(editableItem);
                        break;
                    default:
                        visualElement = CreateTextFieldItem(editableItem);
                        //Debug.LogWarning($"Unsupported type for key '{editableItem.Key}': {editableItem.Value.GetType()}");
                        break;
                }
                container.Add(visualElement);
            }
        }


        private VisualElement CreateTextFieldItem(EditableConfigItem editableItem)
        {
            VisualElement configItem = configurationItemTextField.Instantiate();

            TextField textField = configItem.Q<TextField>();

            // Set the data source to the editable wrapper
            textField.dataSource = editableItem;

            // Bind the label to the Key property
            DataBinding keyBinding = new()
            {
                dataSourcePath = PropertyPath.FromName("Key"),
                bindingMode = BindingMode.TwoWay
            };
            textField.SetBinding(nameof(TextField.label), keyBinding);

            // Bind the value to the Value property
            DataBinding valueBinding = new()
            {
                dataSourcePath = PropertyPath.FromName("Value"),
                bindingMode = BindingMode.TwoWay
            };
            textField.SetBinding(nameof(TextField.value), valueBinding);

            return configItem;
        }

        private VisualElement CreateNumericFieldItem(EditableConfigItem editableItem)
        {
            VisualElement configItem = configurationItemNumericField.Instantiate();

            IntegerField integerField = configItem.Q<IntegerField>();

            // Set the data source to the editable wrapper
            integerField.dataSource = editableItem;

            // Bind the label to the Key property
            DataBinding keyBiding = new()
            {
                dataSourcePath = PropertyPath.FromName("Key"),
                bindingMode = BindingMode.TwoWay
            };
            integerField.SetBinding(nameof(IntegerField.label), keyBiding);

            integerField.value = Convert.ToInt32(editableItem.Value);
            DataBinding valueBinding = new()
            {
                dataSourcePath = PropertyPath.FromName("Value"),
                bindingMode = BindingMode.TwoWay
            };
            //valueBinding.sourceToUiConverters.AddConverter((ref System.Int64 value) => Convert.ToInt32(value));
            integerField.SetBinding(nameof(IntegerField.value), valueBinding);

            return configItem;
        }

        private VisualElement CreateCheckBoxItem(EditableConfigItem editableItem)
        {
            VisualElement configItem = configurationItemCheckBox.Instantiate();

            Toggle toggle = configItem.Q<Toggle>();

            // Set the data source to the editable wrapper
            toggle.dataSource = editableItem;

            // Bind the label to the Key property
            DataBinding keyBinding = new()
            {
                dataSourcePath = PropertyPath.FromName("Key"),
                bindingMode = BindingMode.TwoWay
            };
            toggle.SetBinding(nameof(TextField.label), keyBinding);

            // Bind the value to the Value property
            DataBinding valueBinding = new()
            {
                dataSourcePath = PropertyPath.FromName("Value"),
                bindingMode = BindingMode.TwoWay
            };
            toggle.SetBinding(nameof(TextField.value), valueBinding);

            return configItem;
        }
    }

}
