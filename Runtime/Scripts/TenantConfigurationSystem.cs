
using Newtonsoft.Json.Linq;

using Reflectis.SDK.Core.ApiSystem;
using Reflectis.SDK.Http;

using System;
using System.Collections.Generic;
using System.Linq;
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
        private TenantPublicConfig publicConfig;
        #endregion

        #region Properties
        public bool GetTenantDataOnInit => getTenantDataOnInit;

        public Tenant TenantConfiguration { get { return tenantConfiguration; } set { tenantConfiguration = value; } }

        public JObject AppConfig { get { return appConfig; } set { appConfig = value; } }

        /// <summary>
        /// Whitelisted public projection of <c>tenant.ctn_config</c> fetched at
        /// boot from <c>GET /manage/apps/tenant/config-public</c> (HMAC).
        /// Source of truth for the runtime language switcher and auto-default.
        /// See <c>docs/localization.md</c> in the meta-repo.
        /// </summary>
        public TenantPublicConfig PublicConfig { get { return publicConfig; } set { publicConfig = value; } }

        public AppIdentification AppIdentification => apiConfig;
        #endregion

        #region System implementation

        public override async Task Init()
        {
            await base.Init();

            if (getTenantDataOnInit)
            {
                ApiResponse<Tenant> tenantDataReq = await GetTenantData(apiConfig);
                if (tenantDataReq.IsSuccess)
                {
                    TenantConfiguration = tenantDataReq.Content;
                }
                else
                {
                    Debug.LogError($"[{name}]: Failed to get tenant data: {tenantDataReq.ReasonPhrase}");
                }

                ApiResponse<JObject> appCustomConfigReq = await GetAppCustomConfig(apiConfig);
                if (appCustomConfigReq.IsSuccess)
                {
                    AppConfig = appCustomConfigReq.Content;
                }
                else
                {
                    Debug.LogError($"[{name}]: Failed to get app data: {appCustomConfigReq.ReasonPhrase}");
                }

                ApiResponse<TenantPublicConfig> tenantPublicReq = await GetTenantPublicConfig(apiConfig);
                if (tenantPublicReq.IsSuccess)
                {
                    PublicConfig = tenantPublicReq.Content;
                }
                else
                {
                    Debug.LogError($"[{name}]: Failed to get tenant public config: {tenantPublicReq.ReasonPhrase}");
                }
            }
        }

        /// <summary>
        /// Compute the effective list of languages the user can pick from:
        /// intersection of <see cref="PublicConfig"/>.SupportedLanguages with
        /// the caller-provided "Virtuademy-known" rosa (typically
        /// <c>I2.Loc.LocalizationManager.GetAllLanguagesCode()</c> on the Unity
        /// side), preserving the operator-declared order. Falls back to a
        /// synthetic <c>["en"]</c> when the intersection is empty so consumers
        /// have a deterministic single-language baseline ("fail secure").
        /// <para>
        /// **No legacy fallback**: if <see cref="PublicConfig"/> is null or
        /// <c>SupportedLanguages</c> is empty, the result is <c>["en"]</c>.
        /// Operators who haven't populated <c>tenant.ctn_config.supportedLanguages</c>
        /// see the fail-loud single-language state, prompting them to fix the
        /// config. See <c>docs/localization.md</c> in the meta-repo.
        /// </para>
        /// </summary>
        /// <param name="knownLanguageCodes">
        /// The language codes the local build can actually render. Pass
        /// <c>I2.Loc.LocalizationManager.GetAllLanguagesCode(allowRegions: true, SkipDisabled: true)</c>
        /// from the Unity application layer. The SDK stays decoupled from
        /// I2Loc by accepting the rosa as a parameter.
        /// </param>
        public List<string> GetEffectiveSupportedLanguages(IEnumerable<string> knownLanguageCodes)
        {
            HashSet<string> known = knownLanguageCodes == null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(knownLanguageCodes, StringComparer.OrdinalIgnoreCase);

            List<string> supported = publicConfig?.SupportedLanguages;
            List<string> effective = supported == null
                ? new List<string>()
                : supported.Where(lang => !string.IsNullOrWhiteSpace(lang) && known.Contains(lang)).ToList();

            return effective.Count > 0 ? effective : new List<string> { "en" };
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

        /// <summary>
        /// Whitelisted public projection of <c>tenant.ctn_config</c>. Pre-login
        /// HMAC endpoint readable by every registered app — see
        /// <c>docs/localization.md</c> in the meta-repo for the contract.
        /// </summary>
        public static async Task<ApiResponse<TenantPublicConfig>> GetTenantPublicConfig(AppIdentification apiConfig)
        {
            using UnityWebRequest request = ApiHelper.BuildRequest(
                UnityWebRequest.kHttpVerbGET, "manage/apps/tenant/config-public", apiConfig,
                authentication: EAuthentication.Hmac);
            await request.SendWebRequest();

            return new ApiResponse<TenantPublicConfig>(request.responseCode, request.error, request.downloadHandler.text);
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
