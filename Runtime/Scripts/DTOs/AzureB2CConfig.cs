using Newtonsoft.Json.Linq;

using System;

using UnityEngine;

namespace Reflectis.SDK.TenantConfiguration
{
    /// <summary>
    /// Azure B2C authentication configuration extracted from app custom config.
    /// </summary>
    [Serializable]
    public class AzureB2CConfig
    {
        // No port = MSAL picks a random available port (RFC 8252 loopback).
        // This avoids the HttpListener URL ACL reservation that http://localhost:PORT/ requires on Windows.
        // Register "http://localhost" (no port) in Azure B2C → Authentication → Mobile and desktop applications.
        private const string DefaultRedirectUri = "http://localhost";

        [SerializeField] private string tenant;
        [SerializeField] private string policy;
        [SerializeField] private string profileApiId;
        [SerializeField] private string redirectUri;

        public string Tenant { get => tenant; set => tenant = value; }
        public string Policy { get => policy; set => policy = value; }
        public string ProfileApiId { get => profileApiId; set => profileApiId = value; }

        /// <summary>
        /// The redirect URI registered in Azure B2C for the editor/desktop client.
        /// Falls back to <c>http://localhost:10717/</c> if not specified in the config.
        /// </summary>
        public string RedirectUri => string.IsNullOrEmpty(redirectUri) ? DefaultRedirectUri : redirectUri;

        /// <summary>
        /// Extracts AzureB2CConfig from the app custom config JObject.
        /// Expects a structure like: { "azureB2C": { "tenant": "...", "policy": "...", "profileApiId": "...", "redirectUri": "..." } }
        /// </summary>
        public static AzureB2CConfig FromAppCustomConfig(JObject customConfig)
        {
            if (customConfig == null)
            {
                Debug.LogError("[AzureB2CConfig] Custom config is null");
                return null;
            }

            JToken b2cToken = customConfig["authenticationData"];
            if (b2cToken == null)
            {
                Debug.LogError("[AzureB2CConfig] 'authenticationData' section not found in custom config");
                return null;
            }

            return new AzureB2CConfig
            {
                tenant = b2cToken["tenant"]?.ToString(),
                policy = b2cToken["policy"]?.ToString(),
                profileApiId = b2cToken["profileApiId"]?.ToString(),
                redirectUri = b2cToken["redirectUri"]?.ToString()
            };
        }
    }
}
