using Newtonsoft.Json;

using Reflectis.SDK.Core.ApiSystem;

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

    public class TenantConfigurationWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;

        [SerializeField] private VisualTreeAsset configurationItemTextField = default;
        [SerializeField] private VisualTreeAsset configurationItemNumericField = default;
        [SerializeField] private VisualTreeAsset configurationItemCheckBox = default;

        private VisualElement root;

        private Dictionary<string, object> tenantConfiguration;
        private List<EditableConfigItem> editableConfigItems;

        private VisualElement container;

        [MenuItem("Reflectis Worlds/Tenant Configuration Window")]
        public static TenantConfigurationWindow ShowWindow()
        {
            TenantConfigurationWindow wnd = GetWindow<TenantConfigurationWindow>();
            wnd.titleContent = new GUIContent("TenantConfigurationEditorWindow");

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

        public async void ShowTenantConfigurationWindow(AppConfig app)
        {
            HttpSystem httpSystem = CreateInstance<HttpSystem>();
            TenantConfigurationSystem tenantConfigurationSystemAdmin = CreateInstance<TenantConfigurationSystem>();

            AppConfig appConfig = new(app.AppId, app.AppSecret, app.ApiBaseUrl, app.ApiVersion);
            await tenantConfigurationSystemAdmin.Init(appConfig, httpSystem);

            container = root.Q<VisualElement>("PropertiesContainer");
            Tenant tenant = (await tenantConfigurationSystemAdmin.GetTenantData()).Content;

            tenantConfiguration = tenant.Config.ToObject<Dictionary<string, object>>();

            // Create editable wrapper items
            editableConfigItems = new List<EditableConfigItem>();
            foreach (var el in tenantConfiguration)
            {
                editableConfigItems.Add(new EditableConfigItem(el.Key, el.Value));
            }

            // Create UI elements for each editable item
            foreach (var editableItem in editableConfigItems)
            {
                switch (editableItem.Value)
                {
                    case string _:
                        CreateTextFieldItem(editableItem);
                        break;
                    case System.Int64 _:
                        CreateNumericFieldItem(editableItem);
                        break;
                    case bool _:
                        CreateCheckBoxItem(editableItem);
                        break;
                    default:
                        CreateTextFieldItem(editableItem);
                        //Debug.LogWarning($"Unsupported type for key '{editableItem.Key}': {editableItem.Value.GetType()}");
                        break;
                }
            }


            Button updateTenantbutton = root.Q<Button>("UpdateTenantConfigurationButton");
            updateTenantbutton.clicked += async () =>
            {
                // Convert the editable items back to the original dictionary format
                Dictionary<string, object> updatedConfig = new();
                foreach (var item in editableConfigItems)
                {
                    updatedConfig[item.Key] = item.Value;
                }
                // Update the tenant configuration
                //await tenantConfigurationSystemAdmin.UpdateTenantConfig(tenant.Id, JsonConvert.SerializeObject(updatedConfig));

                // Optionally, refresh the UI or show a success message
                Debug.Log($"Tenant {tenant.Id} configuration updated successfully. New config: {JsonConvert.SerializeObject(updatedConfig)}");
            };
        }


        private void CreateTextFieldItem(EditableConfigItem editableItem)
        {
            VisualElement configItem = configurationItemTextField.Instantiate();

            container.Add(configItem);

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

            // Update the original dictionary when the editable item changes
            textField.RegisterValueChangedCallback(evt =>
            {
                tenantConfiguration[editableItem.Key] = evt.newValue;
            });
        }

        private void CreateNumericFieldItem(EditableConfigItem editableItem)
        {
            VisualElement configItem = configurationItemNumericField.Instantiate();

            container.Add(configItem);

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

            // Update the original dictionary when the editable item changes
            integerField.RegisterValueChangedCallback(evt =>
            {
                tenantConfiguration[editableItem.Key] = evt.newValue;
            });
        }

        private void CreateCheckBoxItem(EditableConfigItem editableItem)
        {
            VisualElement configItem = configurationItemCheckBox.Instantiate();

            container.Add(configItem);

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

            // Update the original dictionary when the editable item changes
            toggle.RegisterValueChangedCallback(evt =>
            {
                tenantConfiguration[editableItem.Key] = evt.newValue;
            });
        }
    }

}
