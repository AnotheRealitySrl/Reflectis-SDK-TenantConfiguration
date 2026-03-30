using Newtonsoft.Json.Linq;

using Reflectis.SDK.Core.ApiSystem;

using System;

using UnityEngine;

namespace Reflectis.SDK.TenantConfiguration
{
    [CreateAssetMenu(menuName = "Reflectis/ApiData/TenantConfigurationData", fileName = "TenantConfigurationData")]
    public class TenantConfigurationData : ApiDataBase
    {
        [SerializeField] private bool getTenantDataOnInit = true;

        public bool GetTenantDataOnInit => getTenantDataOnInit;

        // Runtime state populated by TenantConfigurationApi
        [NonSerialized] public Tenant tenantConfiguration;
        [NonSerialized] public JObject appConfig;
    }
}
