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
        private const string IS_TENANT_MANAGER_KEY = "Reflectis_EditorLogin_IsTenantManager";
        private const string LOGGED_IN_APP_KEY = "Reflectis_EditorLogin_App";
        private const string LOGGED_IN_ENV_KEY = "Reflectis_EditorLogin_Env";

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

        public static bool IsTenantManager
        {
            get => SessionState.GetBool(IS_TENANT_MANAGER_KEY, false);
            private set => SessionState.SetBool(IS_TENANT_MANAGER_KEY, value);
        }

        public static string LoggedInApp
        {
            get => SessionState.GetString(LOGGED_IN_APP_KEY, "");
            private set => SessionState.SetString(LOGGED_IN_APP_KEY, value ?? "");
        }

        public static string LoggedInEnv
        {
            get => SessionState.GetString(LOGGED_IN_ENV_KEY, "");
            private set => SessionState.SetString(LOGGED_IN_ENV_KEY, value ?? "");
        }

        public static bool IsLoggedIn => !string.IsNullOrEmpty(BearerToken);

        /// <summary>
        /// Checks whether the given app/env pair matches the currently logged-in tenant.
        /// </summary>
        public static bool IsLoggedInto(string app, string env)
        {
            return IsLoggedIn
                && !string.IsNullOrEmpty(LoggedInApp)
                && LoggedInApp == app
                && LoggedInEnv == env;
        }

        public static event Action OnLoginStateChanged;

        public static void Set(string token, Tenant tenant, string username, bool isTenantManager = false, string app = null, string env = null)
        {
            BearerToken = token;
            CurrentTenant = tenant;
            Username = username;
            IsTenantManager = isTenantManager;
            LoggedInApp = app;
            LoggedInEnv = env;
            OnLoginStateChanged?.Invoke();
        }

        public static void Clear()
        {
            BearerToken = "";
            CurrentTenant = null;
            Username = "";
            IsTenantManager = false;
            LoggedInApp = "";
            LoggedInEnv = "";
            OnLoginStateChanged?.Invoke();
        }
    }
}
