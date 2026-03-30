using Microsoft.Identity.Client;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using UnityEngine;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    /// <summary>
    /// Azure B2C authentication service using MSAL.
    /// Handles interactive login and token acquisition.
    /// </summary>
    public static class AzureAuthService
    {
        private static IPublicClientApplication _pca;
        private static string _currentClientId;
        private static string _currentTenant;
        private static string _currentPolicy;

        private static string _currentRedirectUri;

        /// <summary>
        /// Initialize the MSAL client. Re-initializes if parameters change.
        /// </summary>
        /// <param name="redirectUri">
        /// The redirect URI registered in Azure B2C for this client ID (e.g. "http://localhost:10717/").
        /// Must exactly match one of the URIs registered under "Mobile and desktop applications" in the Azure portal.
        /// </param>
        public static void Init(string clientId, string tenant, string policy, string redirectUri)
        {
            // Re-initialize if parameters changed
            if (_pca != null
                && _currentClientId == clientId
                && _currentTenant == tenant
                && _currentPolicy == policy
                && _currentRedirectUri == redirectUri)
            {
                return;
            }

            _currentClientId = clientId;
            _currentTenant = tenant;
            _currentPolicy = policy;
            _currentRedirectUri = redirectUri;

            string authority = $"https://{tenant}.b2clogin.com/tfp/{tenant}.onmicrosoft.com/{policy}";

            Debug.Log($"[AzureAuthService] Initializing MSAL — authority: {authority}, redirectUri: {redirectUri}, clientId: {clientId}");

            _pca = PublicClientApplicationBuilder
                .Create(clientId)
                .WithB2CAuthority(authority)
                .WithRedirectUri(redirectUri)
                .Build();
        }

        /// <summary>
        /// Resets the MSAL client. Call before Init() to force re-initialization.
        /// </summary>
        public static void Reset()
        {
            _pca = null;
            _currentClientId = null;
            _currentTenant = null;
            _currentPolicy = null;
            _currentRedirectUri = null;
        }

        /// <summary>
        /// Acquires an access token, using silent acquisition if possible, falling back to interactive login.
        /// </summary>
        /// <returns>A tuple of (accessToken, username) where username is the display name from the ID token,
        /// falling back to the account username (typically email).</returns>
        public static async Task<(string AccessToken, string Username)> LoginInteractive(string[] scopes)
        {
            if (_pca == null)
            {
                throw new InvalidOperationException("AzureAuthService not initialized. Call Init() first.");
            }

            AuthenticationResult result = null;
            try
            {
                try
                {
                    var accounts = await _pca.GetAccountsAsync();
                    result = await _pca.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                                      .ExecuteAsync();
                    Debug.Log("Token ottenuto in modo silenzioso (refresh riuscito)");
                }
                catch (MsalUiRequiredException)
                {
                    result = await _pca.AcquireTokenInteractive(scopes)
                        .WithUseEmbeddedWebView(false)
                        .WithExtraQueryParameters(new Dictionary<string, string>
                        {
                            { "nonce", Guid.NewGuid().ToString() }
                        })
                        .ExecuteAsync();
                    Debug.Log("Login interattivo completato");
                }
            }
            catch (MsalException ex)
            {
                Debug.LogError($"MSAL Error: {ex.Message}");
                throw;
            }

            if (result.ExpiresOn <= DateTimeOffset.Now.AddMinutes(5))
            {
                Debug.Log("Token quasi scaduto, rinnovo in corso...");
                result = await _pca.AcquireTokenSilent(scopes, result.Account).ExecuteAsync();
                Debug.Log("Token rinnovato automaticamente!");
            }

            // Prefer the "name" claim from the ID token, fall back to account username (email)
            string username = result.ClaimsPrincipal?.FindFirst("name")?.Value
                           ?? result.ClaimsPrincipal?.FindFirst("given_name")?.Value
                           ?? result.Account.Username;

            return (result.AccessToken, username);
        }

        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Calls the profile API to get JWT tokens for all API labels.
        /// </summary>
        public static async Task<string> GetUserDataAsync(string apiBaseUrl, string accessToken)
        {
            // Ensure the base URL always has a trailing slash so the relative path appends correctly
            if (!apiBaseUrl.EndsWith("/"))
                apiBaseUrl += "/";

            string apiUrl = $"{apiBaseUrl}my/tokens";
            Debug.Log($"[AzureAuthService] Calling tokens endpoint: {apiUrl}");

            // Use a per-request HttpRequestMessage to avoid mutating shared DefaultRequestHeaders
            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (HttpRequestException ex)
            {
                string inner = ex.InnerException != null ? $" | Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}" : string.Empty;
                Debug.LogError($"[AzureAuthService] Network error calling {apiUrl}: {ex.Message}{inner}");
                throw;
            }

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                Debug.LogError($"[AzureAuthService] API Error {(int)response.StatusCode} ({response.ReasonPhrase}) from {apiUrl}: {body}");
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            Debug.Log($"[AzureAuthService] Tokens response: {json}");
            return json;
        }
    }
}
