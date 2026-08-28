using Newtonsoft.Json.Linq;

using System;

using UnityEngine;

namespace Virtuademy.SDK.TenantConfiguration
{
    /// <summary>
    /// Azure B2C authentication configuration extracted from app custom config.
    /// </summary>
    [Serializable, Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.Fields)]
    public class AzureB2CConfig
    {
        // No port = MSAL picks a random available port (RFC 8252 loopback).
        // This avoids the HttpListener URL ACL reservation that http://localhost:PORT/ requires on Windows.
        // Register "http://localhost" (no port) in Azure B2C → Authentication → Mobile and desktop applications.
        private const string DefaultRedirectUri = "http://localhost";

        [SerializeField] private string externalIdTenant;
        [SerializeField] private string policy;
        [SerializeField] private string profileApiId;
        [SerializeField] private string redirectUri;

        public string Tenant { get => externalIdTenant; set => externalIdTenant = value; }
        public string Policy { get => policy; set => policy = value; }
        public string ProfileApiId { get => profileApiId; set => profileApiId = value; }

        /// <summary>
        /// The redirect URI registered in Azure for the editor/desktop client.
        /// Falls back to <c>http://localhost</c> if not specified in the config.
        /// </summary>
        public string RedirectUri => string.IsNullOrEmpty(redirectUri) ? DefaultRedirectUri : redirectUri;

        /// <summary>
        /// True when the policy field equals "EntraID" (case-insensitive), indicating Microsoft Entra ID authentication.
        /// </summary>
        public bool IsEntraId => string.Equals(policy, "EntraID", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// True when the policy field starts with "B2C_" (case-insensitive), indicating Azure B2C authentication.
        /// </summary>
        public bool IsB2C => policy != null && policy.StartsWith("B2C_", StringComparison.OrdinalIgnoreCase);

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
                externalIdTenant = b2cToken["tenant"]?.ToString(),
                policy = b2cToken["policy"]?.ToString(),
                profileApiId = b2cToken["profileApiId"]?.ToString(),
                redirectUri = b2cToken["redirectUri"]?.ToString()
            };
        }
    }
}
