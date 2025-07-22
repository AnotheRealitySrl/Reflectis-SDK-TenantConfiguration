using System.Collections.Generic;

using Unity.Properties;

using UnityEditor;

using UnityEngine;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    [CreateAssetMenu(fileName = "TenantVisualizationSettings", menuName = "Reflectis/SDK/Tenant Configuration/TenantVisualizationSettings")]
    public class TenantVisualizationSettings : ScriptableObject
    {
        [SerializeField] private List<TextAsset> tenantAssets = new List<TextAsset>();
        [SerializeField] private List<TextAsset> adminTenantAssets = new List<TextAsset>();

        [SerializeField] private AbstractAppConfigurationScript configurationScript;
        [SerializeField] private AbstractBuildScript buildScript;

        public List<TextAsset> TenantAssets => tenantAssets;
        public List<TextAsset> AdminTenantAssets => adminTenantAssets;

        [CreateProperty] public string SelectedTenant { get; set; }
        [CreateProperty] public string SelectedEnv { get; set; }
        [CreateProperty] public AppConfig SelectedConfig { get; set; } = new();

        public AbstractAppConfigurationScript ConfigurationScript { get => configurationScript; set => configurationScript = value; }
        public AbstractBuildScript BuildScript { get => buildScript; set => buildScript = value; }


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
