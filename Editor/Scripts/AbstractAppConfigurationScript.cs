using UnityEngine;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    [CreateAssetMenu(fileName = "AppConfigurationScriptBase", menuName = "Reflectis/SDK/Tenant Configuration/AppConfigurationScriptBase")]
    public abstract class AbstractAppConfigurationScript : ScriptableObject
    {
        [SerializeField] private TenantConfigurationSystem tenantConfigurationSystem;

        public abstract void ConfigureApp(AppConfig device);
    }
}

