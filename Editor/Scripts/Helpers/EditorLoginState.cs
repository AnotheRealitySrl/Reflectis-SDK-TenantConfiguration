using Newtonsoft.Json;

using Reflectis.SDK.TenantConfiguration;

using System;

using UnityEditor;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    /// <summary>
    /// Stores editor login state using SessionState.
    /// Persists between domain reloads, resets when editor closes.
    /// </summary>
    public static class EditorLoginState
    {
        private const string TOKEN_KEY = "Reflectis_EditorLogin_Token";
        private const string TENANT_KEY = "Reflectis_EditorLogin_Tenant";
        private const string USERNAME_KEY = "Reflectis_EditorLogin_Username";

        public static string BearerToken
        {
            get => SessionState.GetString(TOKEN_KEY, "");
            private set => SessionState.SetString(TOKEN_KEY, value);
        }

        public static Tenant CurrentTenant
        {
            get
            {
                string json = SessionState.GetString(TENANT_KEY, "");
                if (string.IsNullOrEmpty(json)) return null;
                try
                {
                    return JsonConvert.DeserializeObject<Tenant>(json);
                }
                catch
                {
                    return null;
                }
            }
            private set
            {
                SessionState.SetString(TENANT_KEY, value != null ? JsonConvert.SerializeObject(value) : "");
            }
        }

        public static string Username
        {
            get => SessionState.GetString(USERNAME_KEY, "");
            private set => SessionState.SetString(USERNAME_KEY, value ?? "");
        }

        public static bool IsLoggedIn => !string.IsNullOrEmpty(BearerToken);

        public static event Action OnLoginStateChanged;

        public static void Set(string token, Tenant tenant, string username)
        {
            BearerToken = token;
            CurrentTenant = tenant;
            Username = username;
            OnLoginStateChanged?.Invoke();
        }

        public static void Clear()
        {
            BearerToken = "";
            CurrentTenant = null;
            Username = "";
            OnLoginStateChanged?.Invoke();
        }
    }
}
