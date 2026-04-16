
using Newtonsoft.Json.Linq;

using Reflectis.SDK.Core.ApiSystem;
using Reflectis.SDK.Http;

using System.Threading.Tasks;

using UnityEngine;

using static Reflectis.SDK.Core.Authentication.IAuthenticationSystem;

namespace Reflectis.SDK.TenantConfiguration
{
    /// <summary>
    /// SM-compatible wrapper for TenantConfigurationApi.
    /// Kept for backward compatibility with the SM.GetSystem pattern.
    /// New code should use TenantConfigurationApi directly.
    /// </summary>
    [CreateAssetMenu(menuName = "AnotheReality/Systems/TenantConfigurationSystem", fileName = "TenantConfigurationSystem")]
    public class TenantConfigurationSystem : ApiSystemBase<TenantConfigurationApi, TenantConfigurationData>
    {
        [SerializeField] private bool getTenantDataOnInit = true;

        #region Properties

        public Tenant TenantConfiguration => TenantConfigurationApi.TenantConfiguration;

        public JObject AppConfig => TenantConfigurationApi.AppConfig;

        public AppIdentification AppIdentification => apiConfig;

        #endregion

        #region System implementation

        public override async Task Init()
        {
            await base.Init();

            if (getTenantDataOnInit)
            {
                ApiResponse<Tenant> tenantDataReq = await TenantConfigurationApi.GetTenantData(apiConfig);
                if (!tenantDataReq.IsSuccess)
                {
                    Debug.LogError($"[{name}]: Failed to get tenant data: {tenantDataReq.ReasonPhrase}");
                }

                ApiResponse<JObject> appCustomConfigReq = await TenantConfigurationApi.GetAppCustomConfig(apiConfig);
                if (!appCustomConfigReq.IsSuccess)
                {
                    Debug.LogError($"[{name}]: Failed to get app data: {appCustomConfigReq.ReasonPhrase}");
                }
            }
        }

        #endregion

        #region Legacy API (delegates to static class)

        public async Task<ApiResponse<object>> GetTenantAvailability()
        {
            return await TenantConfigurationApi.GetTenantAvailability(apiConfig);
        }

        public async Task<ApiResponse<Tenant>> GetTenantData()
        {
            return await TenantConfigurationApi.GetTenantData(apiConfig);
        }

        public async Task<ApiResponse<JObject>> GetAppCustomConfig()
        {
            return await TenantConfigurationApi.GetAppCustomConfig(apiConfig);
        }

        public async Task<ApiResponse> UpdateAppCustomConfig(string config)
        {
            return await TenantConfigurationApi.UpdateAppCustomConfig(apiConfig, config);
        }

        #endregion
    }
}
