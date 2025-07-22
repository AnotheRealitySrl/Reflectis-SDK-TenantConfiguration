using Newtonsoft.Json;

using Reflectis.SDK.TenantConfiguration;

using System.Collections.Generic;

using Unity.Properties;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

// Wrapper class to make configuration items editable via data binding
[System.Serializable]
public class EditableConfigItem
{
    [CreateProperty]
    public string Key { get; set; }

    [CreateProperty]
    public object Value { get; set; }

    public EditableConfigItem(string key, object value)
    {
        Key = key;
        Value = value;
    }
}

public class TenantConfigurationEditorWindow : EditorWindow
{
    [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;
    [SerializeField] private VisualTreeAsset configurationItemVisualTreeAsset = default;

    private VisualElement root;

    private Dictionary<string, object> tenantConfiguration;
    private List<EditableConfigItem> editableConfigItems;

    [MenuItem("Reflectis Worlds/Tenant Configuration Window")]
    public static TenantConfigurationEditorWindow ShowWindow()
    {
        TenantConfigurationEditorWindow wnd = GetWindow<TenantConfigurationEditorWindow>();
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

        tenantConfigurationSystemAdmin.AppId = app.AppId;
        tenantConfigurationSystemAdmin.AppSecret = app.AppSecret;
        tenantConfigurationSystemAdmin.TenantConfigurationApiUrl = app.TenantConfigurationApiUrl;
        tenantConfigurationSystemAdmin.TenantConfigurationApiVersion = app.TenantConfigurationApiVersion;
        tenantConfigurationSystemAdmin.HttpSystem = httpSystem;
        _ = tenantConfigurationSystemAdmin.Init();

        VisualElement container = root.Q<VisualElement>("PropertiesContainer");
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
            VisualElement configItem = configurationItemVisualTreeAsset.Instantiate();
            container.Add(configItem);

            TextField textField = configItem.Q<TextField>();

            // Set the data source to the editable wrapper
            textField.dataSource = editableItem;

            // Bind the label to the Key property
            DataBinding textFieldKeyBinding = new()
            {
                dataSourcePath = PropertyPath.FromName("Key"),
                bindingMode = BindingMode.TwoWay
            };
            textField.SetBinding(nameof(TextField.label), textFieldKeyBinding);

            // Bind the value to the Value property
            DataBinding textFieldValueBinding = new()
            {
                dataSourcePath = PropertyPath.FromName("Value"),
                bindingMode = BindingMode.TwoWay
            };
            textField.SetBinding(nameof(TextField.value), textFieldValueBinding);

            // Update the original dictionary when the editable item changes
            textField.RegisterValueChangedCallback(evt =>
            {
                tenantConfiguration[editableItem.Key] = evt.newValue;
            });
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
}
