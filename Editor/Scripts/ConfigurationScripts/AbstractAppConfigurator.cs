using Reflectis.SDK.Core.ApiSystem;

using System.Threading.Tasks;

using UnityEngine;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    public abstract class AbstractAppConfigurator : ScriptableObject
    {
        [SerializeField] protected TenantConfigurationSystem tenantConfigurationSystem;

        public abstract Task ConfigureApp(AppIdentification device);

        public TenantConfigurationSystem TenantConfigurationSystem => tenantConfigurationSystem;
    }
}

