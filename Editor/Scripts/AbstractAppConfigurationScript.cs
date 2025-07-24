using System.Threading.Tasks;

using UnityEngine;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    public abstract class AbstractAppConfigurationScript : ScriptableObject
    {
        [SerializeField] protected TenantConfigurationSystem tenantConfigurationSystem;

        public abstract Task ConfigureApp(AppConfig device);
    }
}

