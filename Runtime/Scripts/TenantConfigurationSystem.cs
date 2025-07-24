
using Reflectis.SDK.Core.SystemFramework;
using Reflectis.SDK.Core.Utilities;
using Reflectis.SDK.Http;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

using static HttpSystem;

namespace Reflectis.SDK.TenantConfiguration
{
    [CreateAssetMenu(menuName = "AnotheReality/Systems/TenantConfigurationSystem", fileName = "TenantConfigurationSystem")]
    public class TenantConfigurationSystem : BaseSystem
    {
        #region Inspector variables

        [Header("Tenant configuration")]
        [SerializeField] private string appId;
        [SerializeField] private string appSecret;
        [SerializeField] private string tenantConfigurationApiUrl;
        [SerializeField] private string tenantConfigurationApiVersion;

        [Header("API settings")]
        [SerializeField] private bool allowUntrustedServers;

        #endregion

        #region Private variables

        private HttpSystem httpSystem;

        public Uri apiBaseUrl;
        private string version;

        private HmacCredential credential;

        private TimeSpan serverTimeOffset;

        #endregion

        #region Properties

        public string TenantConfigurationApiUrl { get => tenantConfigurationApiUrl; set => tenantConfigurationApiUrl = value; }
        public string TenantConfigurationApiVersion { get => tenantConfigurationApiVersion; set => tenantConfigurationApiVersion = value; }
        public string AppId { get => appId; set => appId = value; }
        public string AppSecret { get => appSecret; set => appSecret = value; }

        public Tenant TenantConfiguration { get; protected set; }
        public HttpSystem HttpSystem { get => httpSystem; set => httpSystem = value; }

        #endregion

        #region System implementation

        public override async Task Init()
        {
            httpSystem = httpSystem != null ? httpSystem : SM.GetSystem<HttpSystem>();

            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentException("Missing appId", nameof(appId));
            }

            if (string.IsNullOrEmpty(appSecret))
            {
                throw new ArgumentException("Missing appSecret", nameof(appSecret));
            }

            apiBaseUrl = new Uri(tenantConfigurationApiUrl);
            version = tenantConfigurationApiVersion ?? "1";

            if (apiBaseUrl is null)
            {
                throw new ArgumentNullException(nameof(apiBaseUrl));
            }

            credential = new HmacCredential()
            {
                Id = new Guid(appId),
                Secret = appSecret
            };

            this.appId = appId.ToString();

            if (await IsAlive())
            {
                ApiResponse<Tenant> tenantDataReq = await GetTenantData();
                if (tenantDataReq.IsSuccess)
                {
                    TenantConfiguration = tenantDataReq.Content;
                }
            }

            await base.Init();
        }

        #endregion

        #region ApiServer

        public async Task<bool> IsAlive()
        {
            using UnityWebRequest request = BuildRequest(UnityWebRequest.kHttpVerbGET, "health", authentication: EAuthentication.None);
            await request.SendWebRequest();

            bool success = request.result == UnityWebRequest.Result.Success;

            if (success)
            {
                ApiResponse<DateTime?> serverTimeResponse = new(request.responseCode, request.error, request.downloadHandler.text);
                DateTime? serverTime = serverTimeResponse.Content;

                if (serverTime.HasValue)
                {
                    serverTimeOffset = DateTime.UtcNow - serverTime.Value;
                    Debug.Log($"Server time: {serverTime.Value}, client time offset: {serverTimeOffset}");
                }
                else
                {
                    Debug.LogWarning($"Unable to retrieve server time");
                }
            }

            return success;
        }

        public async Task<ApiResponse<ApiServerStatus>> GetApiServerStatus()
        {
            using UnityWebRequest request = BuildRequest(UnityWebRequest.kHttpVerbGET, "apiserver/status", authentication: EAuthentication.Hmac);
            await request.SendWebRequest();

            return new ApiResponse<ApiServerStatus>(request.responseCode, request.error, request.downloadHandler.text);
        }

        #endregion

        #region Manage apps

        public async Task<ApiResponse<object>> GetTenantAvailability()
        {
            using UnityWebRequest request = BuildRequest(UnityWebRequest.kHttpVerbGET, "manage/apps/tenant/available", authentication: EAuthentication.Hmac);
            await request.SendWebRequest();

            return new ApiResponse<object>(request.responseCode, request.error, request.downloadHandler.text);
        }


        public async Task<ApiResponse<Tenant>> GetTenantData()
        {
            using UnityWebRequest request = BuildRequest(UnityWebRequest.kHttpVerbGET, "manage/apps/tenant", authentication: EAuthentication.Hmac);
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
            using UnityWebRequest request = BuildRequest(UnityWebRequest.kHttpVerbPUT, $"/manage/tenants/{id}/configuration", body: config);
            await request.SendWebRequest();

            return new ApiResponse<TenantConfig>(request.responseCode, request.error, request.downloadHandler.text);
        }

        public async Task<ApiResponse<JwtToken>> GetToken()
        {
            using UnityWebRequest request = BuildRequest(UnityWebRequest.kHttpVerbGET, $"/apiserver/token", authentication: EAuthentication.Hmac);
            await request.SendWebRequest();

            return new ApiResponse<JwtToken>(request.responseCode, request.error, request.downloadHandler.text);
        }


        #endregion

        #region HTTP management private methods

        private UnityWebRequest BuildRequest(string method,
                                            string path,
                                            Dictionary<string, string> queryParams = null,
                                            string body = "",
                                            EAuthentication authentication = EAuthentication.BearerAndHmac)
        {
            queryParams ??= new Dictionary<string, string>();
            queryParams.Add("api-version", version);

            Uri apiBaseUrl = new(tenantConfigurationApiUrl);
            if (apiBaseUrl is null)
            {
                throw new ArgumentNullException(nameof(apiBaseUrl));
            }

            (string timestamp, string hmac) = httpSystem.CalculateHmacHeader(credential, DateTime.UtcNow - serverTimeOffset);
            Dictionary<string, string> headers = new()
            {
                { "AppId", appId },
                { "Content-Type", "application/json" },
                { "Timestamp", timestamp }
            };

            if (authentication.HasFlag(EAuthentication.Hmac))
            {
                headers.Add("Hmac", hmac);
            }

            CertificateHandler certificateHandler = allowUntrustedServers ? new AcceptAllCertificates() : default;

            UnityWebRequest request = httpSystem.CreateHttpRequest(
                method,
                $"{apiBaseUrl}{path}",
                ERequestBodyType.RawString,
                body,
                queryParams,
                headers,
                certificateHandler);

            return request;
        }

        #endregion HTTP management private methods
    }
}
