using Reflectis.SDK.Core.ApiSystem;
using Reflectis.SDK.Core.Utilities;

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
        [SerializeField] private AbstractPlatformSettings platformSettings;
        [SerializeField] private BuildScriptBase buildScript;

        public List<TextAsset> TenantAssets => tenantAssets;
        public List<TextAsset> AdminTenantAssets => adminTenantAssets;

        [CreateProperty] public string SelectedTenant { get; set; }
        [CreateProperty] public string SelectedEnv { get; set; }
        [CreateProperty] public AppConfig SelectedConfig { get; set; } = new();
        [CreateProperty] public bool DoesAdminConfigurationExist => GetCredentials(adminTenantAssets).Exists(x => x.Item1 == SelectedTenant && x.Item2.ContainsKey(SelectedEnv));

        [CreateProperty] public AbstractAppConfigurator ConfigurationScript { get => configurationScript; set => configurationScript = value; }
        [CreateProperty] public AbstractPlatformSettings PlatformSettings { get => platformSettings; set => platformSettings = value; }
        [CreateProperty] public BuildScriptBase BuildScript { get => buildScript; set => buildScript = value; }


        public List<(string, Dictionary<string, AppConfig>)> GetApiConfigs(List<TextAsset> configurationAssets)
        {
            List<(string, Dictionary<string, AppConfig>)> apiConfigs = new();
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
                apiConfigs.Add((configName, mergedConfigs));
            }
            return apiConfigs;
        }

        public List<(string, Dictionary<string, HmacCredential>)> GetCredentials(List<TextAsset> configurationAssets)
        {
            List<(string, Dictionary<string, HmacCredential>)> credentials = new();
            foreach (var config in configurationAssets)
            {
                if (config == null)
                    continue;

                Dictionary<string, HmacCredential> mergedConfigs = new();
                string configName = AssetDatabase.GetAssetPath(config).Split("/")[^1].Split(".")[0];
                var configDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, HmacCredential>>(config.text);
                if (configDict != null)
                {
                    foreach (var kvp in configDict)
                    {
                        mergedConfigs[kvp.Key] = kvp.Value;
                    }
                }
                credentials.Add((configName, mergedConfigs));
            }
            return credentials;
        }
    }
}
