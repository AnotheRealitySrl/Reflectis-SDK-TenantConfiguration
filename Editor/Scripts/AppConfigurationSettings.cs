using Reflectis.SDK.Core.ApiSystem;

using System.Collections.Generic;

using Unity.Properties;

using UnityEditor;

using UnityEngine;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    [CreateAssetMenu(fileName = "AppConfigurationSettings", menuName = "Reflectis/SDK-TenantConfiguration/AppConfigurationSettings")]
    public class AppConfigurationSettings : ScriptableObject
    {
        [SerializeField] private List<TextAsset> appAssets = new();

        [SerializeField] private AbstractAppConfigurator configurationScript;
        [SerializeField] private BuildScriptBase buildScript;

        public List<TextAsset> AppAssets => appAssets;

        [CreateProperty] public string SelectedApp { get; set; }
        [CreateProperty] public string SelectedEnv { get; set; }
        [CreateProperty] public AppIdentification SelectedConfig { get; set; } = new();

        [CreateProperty] public AbstractAppConfigurator ConfigurationScript { get => configurationScript; set => configurationScript = value; }
        [CreateProperty] public BuildScriptBase BuildScript { get => buildScript; set => buildScript = value; }


        public List<(string, Dictionary<string, AppIdentification>)> GetAppIdentification(List<TextAsset> configurationAssets)
        {
            List<(string, Dictionary<string, AppIdentification>)> apiConfigs = new();
            foreach (var config in configurationAssets)
            {
                if (config == null)
                    continue;

                Dictionary<string, AppIdentification> mergedConfigs = new();
                string configName = AssetDatabase.GetAssetPath(config).Split("/")[^1].Split(".")[0];
                var configDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, AppIdentification>>(config.text);
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
    }
}
