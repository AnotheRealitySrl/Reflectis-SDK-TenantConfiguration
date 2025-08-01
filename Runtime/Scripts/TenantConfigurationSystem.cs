
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
        [SerializeField] private bool getTenantDataOnInit = true;

        #region Properties

        public Tenant TenantConfiguration { get; protected set; }

        public AppConfig AppConfig => apiConfig;

        #endregion

        #region System implementation

        public override async Task Init()
        {
            await base.Init();

            if (getTenantDataOnInit)
            {
                ApiResponse<Tenant> tenantDataReq = await GetTenantData();
                if (tenantDataReq.IsSuccess)
                {
                    TenantConfiguration = tenantDataReq.Content;
                }
                else
                {
                    Debug.LogError($"[{name}]: Failed to get tenant data: {tenantDataReq.ReasonPhrase}");
                }
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

            return new(request.responseCode, request.error, request.downloadHandler.text);
        }

        public async Task<ApiResponse<TenantConfig>> UpdateTenantConfig(int id, string config)
        {
            using UnityWebRequest request = await BuildRequest(UnityWebRequest.kHttpVerbPUT, $"/manage/tenants/{id}/configuration", body: config);
            await request.SendWebRequest();

            return new ApiResponse<TenantConfig>(request.responseCode, request.error, request.downloadHandler.text);
        }


        #endregion
    }
}
