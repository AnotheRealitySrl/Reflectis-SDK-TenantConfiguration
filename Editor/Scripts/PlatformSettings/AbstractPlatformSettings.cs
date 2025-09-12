using UnityEngine;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    public abstract class AbstractPlatformSettings : ScriptableObject
    {
        public abstract void Configure(object settings = null);
    }
}
