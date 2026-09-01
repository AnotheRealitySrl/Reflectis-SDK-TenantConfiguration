
using Newtonsoft.Json.Linq;

using Virtuademy.SDK.Core.ApiSystem;
using Virtuademy.SDK.Http;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;
using static Virtuademy.SDK.Core.Authentication.IAuthenticationSystem;

namespace Virtuademy.SDK.TenantConfiguration
{
    /// <summary>
    /// SM-compatible wrapper for TenantConfigurationApi.
    /// Kept for backward compatibility with the SM.GetSystem pattern.
    /// New code should use TenantConfigurationApi directly.
    /// </summary>
    [CreateAssetMenu(menuName = "AnotheReality/Systems/TenantConfigurationSystem", fileName = "TenantConfigurationSystem")]
    public class TenantConfigurationSystem : ApiSystemBase, IApiEndpointResolver
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
        private List<ApiEndpoint> apiEndpoints = new();
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

        #region IApiEndpointResolver

        /// <summary>
        /// Base URL the platform reports for <paramref name="apiType"/>, matched on the
        /// canonical type rather than the tenant-scoped label. Returns false when
        /// discovery has not answered or does not cover that type, which leaves the
        /// caller on the base URL serialized into its own configuration.
        /// </summary>
        public bool TryGetBaseUrl(string apiType, out string baseUrl)
        {
            baseUrl = null;

            if (string.IsNullOrEmpty(apiType) || apiEndpoints == null)
            {
                return false;
            }

            ApiEndpoint match = apiEndpoints.FirstOrDefault(
                e => string.Equals(e.Type, apiType, StringComparison.OrdinalIgnoreCase));

            baseUrl = match?.BaseUrls?.FirstOrDefault();

            return !string.IsNullOrEmpty(baseUrl);
        }

        #endregion

        #region System implementation

        public override async Task Init()
        {
            await base.Init();

            if (getTenantDataOnInit)
            {
                // Fire the 3 HMAC fetches in parallel: they are independent server-side
                // (3 distinct endpoints, no ordering requirement) and serialising them
                // widens the race window with consumers that call
                // GetEffectiveSupportedLanguages too eagerly (e.g. AppManager cascade
                // at boot). With WhenAll, the boot finishes when the slowest of the
                // three returns instead of summing three latencies.
                //
                // serverTimeOffset is passed explicitly: these are static calls, so
                // they bypass ApiSystemBase.BuildRequest and would otherwise sign with
                // the raw device clock. base.Init() has just measured the offset off
                // GET /apiserver/info (getApiInfo), so it is available here.
                Task<ApiResponse<Tenant>> tenantDataTask = GetTenantData(apiConfig, serverTimeOffset);
                Task<ApiResponse<JObject>> appCustomConfigTask = GetAppCustomConfig(apiConfig, serverTimeOffset);
                Task<ApiResponse<TenantPublicConfig>> tenantPublicTask = GetTenantPublicConfig(apiConfig, serverTimeOffset);
                Task<ApiResponse<List<ApiEndpoint>>> apiEndpointsTask = GetApiEndpoints(apiConfig, serverTimeOffset);

                await Task.WhenAll(tenantDataTask, appCustomConfigTask, tenantPublicTask, apiEndpointsTask);

                if (tenantDataTask.Result.IsSuccess)
                    TenantConfiguration = tenantDataTask.Result.Content;
                else
                    Debug.LogError($"[{name}]: Failed to get tenant data: {tenantDataTask.Result.ReasonPhrase}");

                if (appCustomConfigTask.Result.IsSuccess)
                    AppConfig = appCustomConfigTask.Result.Content;
                else
                    Debug.LogError($"[{name}]: Failed to get app data: {appCustomConfigTask.Result.ReasonPhrase}");

                if (tenantPublicTask.Result.IsSuccess)
                    PublicConfig = tenantPublicTask.Result.Content;
                else
                    Debug.LogError($"[{name}]: Failed to get tenant public config: {tenantPublicTask.Result.ReasonPhrase}");

                // Endpoint discovery (ADR 0024). Registering makes this system the resolver
                // the other API systems consult for their base URL. A failure here is not
                // fatal on purpose: no registration means every system keeps the base URL
                // serialized into its own configuration, which is the behaviour that
                // predates discovery.
                if (apiEndpointsTask.Result.IsSuccess)
                {
                    apiEndpoints = apiEndpointsTask.Result.Content ?? new List<ApiEndpoint>();
                    ApiEndpointResolver.Current = this;
                }
                else
                {
                    Debug.LogWarning($"[{name}]: Failed to get API endpoints: {apiEndpointsTask.Result.ReasonPhrase}. " +
                                     "API systems will use the base URLs serialized in the build.");
                }
            }
        }

        /// <summary>
        /// Awaits until <see cref="PublicConfig"/> is populated (or the timeout fires).
        /// Use in consumers that call <see cref="GetEffectiveSupportedLanguages"/> from
        /// code paths that may run before <see cref="Init"/> finishes — typically
        /// `AppManager.GetUserPreferences` at boot and any UI init that races with the
        /// auth pipeline. Pair with the parallelised fetches above: the parallelism
        /// shrinks the window, this wait covers the residual epsilon and any future
        /// flow that bypasses the boot sequence (e.g. runtime tenant switch).
        /// </summary>
        /// <param name="timeoutMs">
        /// Defaults to 10s. If <see cref="PublicConfig"/> stays null past the timeout
        /// (server unreachable, persistent 5xx, …), the method returns false and the
        /// caller proceeds with the documented fail-secure behaviour
        /// (effective list = <c>["en"]</c>, switcher hidden).
        /// </param>
        /// <returns>true if PublicConfig became available within the timeout.</returns>
        public async Task<bool> WaitForPublicConfigAsync(int timeoutMs = 10000)
        {
            if (publicConfig != null) return true;
            float deadline = Time.realtimeSinceStartup + (timeoutMs / 1000f);
            while (publicConfig == null && Time.realtimeSinceStartup < deadline)
                await Task.Yield();
            return publicConfig != null;
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
            return await GetTenantAvailability(apiConfig, serverTimeOffset);
        }

        #endregion

        #region Static API access (for editor/standalone use without SM)

        // Every method here is HMAC-signed, and the signature carries a timestamp the
        // server checks against its own UtcNow: HmacAuthenticationHandler
        // (SPACS-Identity) answers 401 as soon as the two are more than
        // HmacReplayAttackDelaySeconds apart — 15s by default, and no API of ours
        // overrides it. Being static, these calls do not go through
        // ApiSystemBase.BuildRequest and therefore do not pick up the measured
        // serverTimeOffset on their own, so callers that have one must pass it:
        // otherwise a device whose clock is off by more than 15s (kiosks and
        // interactive whiteboards with no NTP are the usual case) fails the whole
        // tenant-configuration bootstrap, AppConfig stays null and AppManager blocks
        // access with MetaverseOffline.
        //
        // The parameter is optional so the editor tooling (TenantSelectionWindow,
        // AppConfigurationWindow, AppConfiguratorScriptBase) keeps compiling
        // unchanged: it runs on a developer machine, where the local clock is the
        // right thing to sign with.

        public static async Task<ApiResponse<object>> GetTenantAvailability(AppIdentification apiConfig, TimeSpan? serverTimeOffset = null)
        {
            using UnityWebRequest request = ApiHelper.BuildRequest(
                UnityWebRequest.kHttpVerbGET, "manage/apps/tenant/available", apiConfig,
                authentication: EAuthentication.Hmac,
                serverTimeOffset: serverTimeOffset);
            await request.SendWebRequest();

            return new ApiResponse<object>(request.responseCode, request.error, request.downloadHandler.text);
        }

        public static async Task<ApiResponse<Tenant>> GetTenantData(AppIdentification apiConfig, TimeSpan? serverTimeOffset = null)
        {
            using UnityWebRequest request = ApiHelper.BuildRequest(
                UnityWebRequest.kHttpVerbGET, "manage/apps/tenant", apiConfig,
                authentication: EAuthentication.Hmac,
                serverTimeOffset: serverTimeOffset);
            await request.SendWebRequest();

            return new ApiResponse<Tenant>(request.responseCode, request.error, request.downloadHandler.text);
        }

        public static async Task<ApiResponse<JObject>> GetAppCustomConfig(AppIdentification apiConfig, TimeSpan? serverTimeOffset = null)
        {
            using UnityWebRequest request = ApiHelper.BuildRequest(
                UnityWebRequest.kHttpVerbGET, "manage/apps/config/custom", apiConfig,
                authentication: EAuthentication.Hmac,
                serverTimeOffset: serverTimeOffset);
            await request.SendWebRequest();

            return new ApiResponse<JObject>(request.responseCode, request.error, request.downloadHandler.text);
        }

        /// <summary>
        /// Whitelisted public projection of <c>tenant.ctn_config</c>. Pre-login
        /// HMAC endpoint readable by every registered app — see
        /// <c>docs/localization.md</c> in the meta-repo for the contract.
        /// </summary>
        public static async Task<ApiResponse<TenantPublicConfig>> GetTenantPublicConfig(AppIdentification apiConfig, TimeSpan? serverTimeOffset = null)
        {
            using UnityWebRequest request = ApiHelper.BuildRequest(
                UnityWebRequest.kHttpVerbGET, "manage/apps/tenant/config-public", apiConfig,
                authentication: EAuthentication.Hmac,
                serverTimeOffset: serverTimeOffset);
            await request.SendWebRequest();

            return new ApiResponse<TenantPublicConfig>(request.responseCode, request.error, request.downloadHandler.text);
        }

        /// <summary>
        /// Every API this app holds an Enabled grant to, with the base URLs that reach
        /// it. Pre-login HMAC endpoint — see ADR 0024 in the meta-repo.
        /// </summary>
        public static async Task<ApiResponse<List<ApiEndpoint>>> GetApiEndpoints(AppIdentification apiConfig, TimeSpan? serverTimeOffset = null)
        {
            using UnityWebRequest request = ApiHelper.BuildRequest(
                UnityWebRequest.kHttpVerbGET, "manage/apps/api-endpoints", apiConfig,
                authentication: EAuthentication.Hmac,
                serverTimeOffset: serverTimeOffset);
            await request.SendWebRequest();

            return new ApiResponse<List<ApiEndpoint>>(request.responseCode, request.error, request.downloadHandler.text);
        }

        public static async Task<ApiResponse> UpdateAppCustomConfig(AppIdentification apiConfig, string config, TimeSpan? serverTimeOffset = null)
        {
            using UnityWebRequest request = ApiHelper.BuildRequest(
                UnityWebRequest.kHttpVerbPUT, "manage/apps/config/custom", apiConfig,
                body: config,
                authentication: EAuthentication.Hmac,
                serverTimeOffset: serverTimeOffset);
            await request.SendWebRequest();

            return new ApiResponse(request.responseCode, request.error, request.downloadHandler.text);
        }

        #endregion
    }
}
