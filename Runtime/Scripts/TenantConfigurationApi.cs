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
    /// Static API class for tenant configuration operations.
    /// Can be used without SM/BaseSystem by accessing Data directly.
    /// </summary>
    public class TenantConfigurationApi : ApiBase<TenantConfigurationData>
    {
        public static Tenant TenantConfiguration => Data.tenantConfiguration;
        public static JObject AppConfig => Data.appConfig;
        public static AppIdentification AppIdentification => Data.ApiConfig;

        #region API methods using Data SO

        public static async Task<ApiResponse<Tenant>> GetTenantData()
        {
            return await GetTenantData(Data.ApiConfig);
        }

        public static async Task<ApiResponse<JObject>> GetAppCustomConfig()
        {
            return await GetAppCustomConfig(Data.ApiConfig);
        }

        public static async Task<ApiResponse> UpdateAppCustomConfig(string config)
        {
            return await UpdateAppCustomConfig(Data.ApiConfig, config);
        }

        public static async Task<ApiResponse<object>> GetTenantAvailability()
        {
            return await GetTenantAvailability(Data.ApiConfig);
        }

        #endregion

        #region API methods with explicit config (for editor/standalone use)

        public static async Task<ApiResponse<Tenant>> GetTenantData(AppIdentification config)
        {
            using UnityWebRequest request = ApiHelper.BuildRequest(
                UnityWebRequest.kHttpVerbGET, "manage/apps/tenant", config,
                authentication: EAuthentication.Hmac);
            await request.SendWebRequest();

            var response = new ApiResponse<Tenant>(request.responseCode, request.error, request.downloadHandler.text);

            if (response.IsSuccess && _data != null)
            {
                _data.tenantConfiguration = response.Content;
            }

            return response;
        }

        public static async Task<ApiResponse<JObject>> GetAppCustomConfig(AppIdentification config)
        {
            using UnityWebRequest request = ApiHelper.BuildRequest(
                UnityWebRequest.kHttpVerbGET, "manage/apps/config/custom", config,
                authentication: EAuthentication.Hmac);
            await request.SendWebRequest();

            var response = new ApiResponse<JObject>(request.responseCode, request.error, request.downloadHandler.text);

            if (response.IsSuccess && _data != null)
            {
                _data.appConfig = response.Content;
            }

            return response;
        }

        public static async Task<ApiResponse> UpdateAppCustomConfig(AppIdentification config, string configData)
        {
            using UnityWebRequest request = ApiHelper.BuildRequest(
                UnityWebRequest.kHttpVerbPUT, "manage/apps/config/custom", config,
                body: configData,
                authentication: EAuthentication.Hmac);
            await request.SendWebRequest();

            return new ApiResponse(request.responseCode, request.error, request.downloadHandler.text);
        }

        public static async Task<ApiResponse<object>> GetTenantAvailability(AppIdentification config)
        {
            using UnityWebRequest request = ApiHelper.BuildRequest(
                UnityWebRequest.kHttpVerbGET, "manage/apps/tenant/available", config,
                authentication: EAuthentication.Hmac);
            await request.SendWebRequest();

            return new ApiResponse<object>(request.responseCode, request.error, request.downloadHandler.text);
        }

        #endregion
    }
}
