
using Reflectis.SDK.Core.ApiSystem;
using Reflectis.SDK.Core.Utilities;
using Reflectis.SDK.Http;

using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

using static Reflectis.SDK.Core.Authentication.IAuthenticationSystem;

namespace Reflectis.SDK.TenantConfiguration
{
    [CreateAssetMenu(menuName = "AnotheReality/Systems/TenantConfigurationSystem", fileName = "TenantConfigurationSystem")]
    public class TenantConfigurationSystem : ApiSystemBase
    {
        #region Inspector variables

        [Header("API settings")]
        [SerializeField] private bool allowUntrustedServers;

        #endregion

        #region Properties

        public Tenant TenantConfiguration { get; protected set; }

        public AppConfig AppConfig => appConfig;

        #endregion

        #region System implementation

        public override async Task Init()
        {
            await base.Init();

            ApiResponse<Tenant> tenantDataReq = await GetTenantData();
            if (tenantDataReq.IsSuccess)
            {
                TenantConfiguration = tenantDataReq.Content;
            }
        }

        public async Task Init(AppConfig config, HttpSystem httpSystem)
        {
            this.httpSystem = httpSystem;

            await Init(config);
        }

        #endregion

        #region Manage apps

        public async Task<ApiResponse<object>> GetTenantAvailability()
        {
            using UnityWebRequest request = await BuildRequest(UnityWebRequest.kHttpVerbGET, "manage/apps/tenant/available", authentication: EAuthentication.Hmac);
            await request.SendWebRequest();

            return new ApiResponse<object>(request.responseCode, request.error, request.downloadHandler.text);
        }


        public async Task<ApiResponse<Tenant>> GetTenantData()
        {
            using UnityWebRequest request = await BuildRequest(UnityWebRequest.kHttpVerbGET, "manage/apps/tenant", authentication: EAuthentication.Hmac);
            await request.SendWebRequest();

            ApiResponse<Tenant> tenantDataRes = new(request.responseCode, request.error, request.downloadHandler.text);

            if (tenantDataRes.IsSuccess)
            {
                TenantConfiguration = tenantDataRes.Content;
            }
            else
            {
                Debug.LogError($"Error retrieving tenant configuration: {tenantDataRes.StatusCode} {tenantDataRes.ReasonPhrase}");
            }

            return tenantDataRes;
        }

        public async Task<ApiResponse<TenantConfig>> UpdateTenantConfig(int id, string config)
        {
            using UnityWebRequest request = await BuildRequest(UnityWebRequest.kHttpVerbPUT, $"/manage/tenants/{id}/configuration", body: config);
            await request.SendWebRequest();

            return new ApiResponse<TenantConfig>(request.responseCode, request.error, request.downloadHandler.text);
        }

        public async Task<ApiResponse<JwtToken>> GetToken()
        {
            using UnityWebRequest request = await BuildRequest(UnityWebRequest.kHttpVerbGET, $"/apiserver/token", authentication: EAuthentication.Hmac);
            await request.SendWebRequest();

            return new ApiResponse<JwtToken>(request.responseCode, request.error, request.downloadHandler.text);
        }


        #endregion
    }
}
