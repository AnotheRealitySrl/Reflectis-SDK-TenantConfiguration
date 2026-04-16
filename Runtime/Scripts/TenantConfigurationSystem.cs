
using Newtonsoft.Json.Linq;

using Reflectis.SDK.Core.ApiSystem;
using Reflectis.SDK.Http;

using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;
using static Reflectis.SDK.Core.Authentication.IAuthenticationSystem;

namespace Reflectis.SDK.TenantConfiguration
{
    /// <summary>
    /// SM-compatible wrapper for TenantConfigurationApi.
    /// Kept for backward compatibility with the SM.GetSystem pattern.
    /// New code should use TenantConfigurationApi directly.
    /// </summary>
    [CreateAssetMenu(menuName = "AnotheReality/Systems/TenantConfigurationSystem", fileName = "TenantConfigurationSystem")]
    public class TenantConfigurationSystem : ApiSystemBase
    {
        #region Inspector info
        [Header("Tenant Configuration API Info")]
        [SerializeField] private bool getTenantDataOnInit = true;
        #endregion

        #region Private stuff
        // Runtime state populated by TenantConfigurationApi
        private Tenant tenantConfiguration;
        private JObject appConfig;
        #endregion

        #region Properties
        public bool GetTenantDataOnInit => getTenantDataOnInit;

        public Tenant TenantConfiguration { get { return tenantConfiguration; } set { tenantConfiguration = value; } }

        public JObject AppConfig { get { return appConfig; } set { appConfig = value; } }

        public AppIdentification AppIdentification => apiConfig;
        #endregion

        #region System implementation

        public override async Task Init()
        {
            await base.Init();

            if (getTenantDataOnInit)
            {
                ApiResponse<Tenant> tenantDataReq = await GetTenantData(apiConfig);
                if (!tenantDataReq.IsSuccess)
                {
                    Debug.LogError($"[{name}]: Failed to get tenant data: {tenantDataReq.ReasonPhrase}");
                }

                ApiResponse<JObject> appCustomConfigReq = await GetAppCustomConfig(apiConfig);
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
            return await GetTenantAvailability(apiConfig);
        }

        #endregion

        #region Static API access (for editor/standalone use without SM)
        
        public static async Task<ApiResponse<object>> GetTenantAvailability(AppIdentification apiConfig)
        {
            using UnityWebRequest request = ApiHelper.BuildRequest(
                UnityWebRequest.kHttpVerbGET, "manage/apps/tenant/available", apiConfig,
                authentication: EAuthentication.Hmac);
            await request.SendWebRequest();

            return new ApiResponse<object>(request.responseCode, request.error, request.downloadHandler.text);
        }

        public static async Task<ApiResponse<Tenant>> GetTenantData(AppIdentification apiConfig)
        {
            using UnityWebRequest request = ApiHelper.BuildRequest(
                UnityWebRequest.kHttpVerbGET, "manage/apps/tenant", apiConfig,
                authentication: EAuthentication.Hmac);
            await request.SendWebRequest();

            return new ApiResponse<Tenant>(request.responseCode, request.error, request.downloadHandler.text);
        }

        public static async Task<ApiResponse<JObject>> GetAppCustomConfig(AppIdentification apiConfig)
        {
            using UnityWebRequest request = ApiHelper.BuildRequest(
                UnityWebRequest.kHttpVerbGET, "manage/apps/config/custom", apiConfig,
                authentication: EAuthentication.Hmac);
            await request.SendWebRequest();

            return new ApiResponse<JObject>(request.responseCode, request.error, request.downloadHandler.text);
        }

        public static async Task<ApiResponse> UpdateAppCustomConfig(AppIdentification apiConfig, string config)
        {
            using UnityWebRequest request = ApiHelper.BuildRequest(
                UnityWebRequest.kHttpVerbPUT, "manage/apps/config/custom", apiConfig,
                body: config,
                authentication: EAuthentication.Hmac);
            await request.SendWebRequest();

            return new ApiResponse(request.responseCode, request.error, request.downloadHandler.text);
        }

        #endregion
    }
}
