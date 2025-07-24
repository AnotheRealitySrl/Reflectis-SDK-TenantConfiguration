using System.Collections.Generic;

using Unity.Properties;

using UnityEditor;

using UnityEngine;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    [CreateAssetMenu(fileName = "TenantConfigurationSettings", menuName = "Reflectis/SDK-TenantConfiguration/TenantConfigurationSettings")]
    public class TenantConfigurationSettings : ScriptableObject
    {
        [SerializeField] private List<TextAsset> tenantAssets = new();
        [SerializeField] private List<TextAsset> adminTenantAssets = new();

        [SerializeField] private AbstractAppConfigurator configurationScript;
        [SerializeField] private BuildScriptBase buildScript;

        public List<TextAsset> TenantAssets => tenantAssets;
        public List<TextAsset> AdminTenantAssets => adminTenantAssets;

        [CreateProperty] public string SelectedTenant { get; set; }
        [CreateProperty] public string SelectedEnv { get; set; }
        [CreateProperty] public AppConfig SelectedConfig { get; set; } = new();
        [CreateProperty] public bool DoesAdminConfigurationExist => GetTenantConfigurations(adminTenantAssets).Exists(x => x.Item1 == SelectedTenant && x.Item2.ContainsKey(SelectedEnv));

        [CreateProperty] public AbstractAppConfigurator ConfigurationScript { get => configurationScript; set => configurationScript = value; }
        [CreateProperty] public BuildScriptBase BuildScript { get => buildScript; set => buildScript = value; }


        public List<(string, Dictionary<string, AppConfig>)> GetTenantConfigurations(List<TextAsset> configurationAssets)
        {
            List<(string, Dictionary<string, AppConfig>)> tenantConfigurations = new();
            foreach (var config in configurationAssets)
            {
                if (config == null)
                    continue;

                Dictionary<string, AppConfig> mergedConfigs = new();
                string configName = AssetDatabase.GetAssetPath(config).Split("/")[^1].Split(".")[0];
                var configDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, AppConfig>>(config.text);
                if (configDict != null)
                {
                    foreach (var kvp in configDict)
                    {
                        mergedConfigs[kvp.Key] = kvp.Value;
                    }
                }
                tenantConfigurations.Add((configName, mergedConfigs));
            }
            return tenantConfigurations;
        }
    }
}
