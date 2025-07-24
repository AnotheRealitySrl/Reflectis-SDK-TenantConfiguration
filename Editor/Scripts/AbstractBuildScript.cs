using UnityEngine;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    public abstract class AbstractBuildScript : ScriptableObject
    {
        [SerializeField] protected TenantConfigurationSystem tenantConfigurationSystem;

        public abstract void Build(params object[] buildParams);
    }
}
