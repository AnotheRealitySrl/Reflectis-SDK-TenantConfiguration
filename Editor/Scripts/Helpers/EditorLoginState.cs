using Newtonsoft.Json;

using Reflectis.SDK.TenantConfiguration;

using System;

using UnityEditor;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    /// <summary>
    /// Stores editor login state using both SessionState (for runtime) and EditorPrefs (for persistence across editor restarts).
    /// On domain reload, restores from EditorPrefs if SessionState is empty.
    /// </summary>
    public static class EditorLoginState
    {
        private const string TOKEN_KEY = "Reflectis_EditorLogin_Token";
        private const string TENANT_KEY = "Reflectis_EditorLogin_Tenant";
        private const string USERNAME_KEY = "Reflectis_EditorLogin_Username";
        private const string IS_TENANT_MANAGER_KEY = "Reflectis_EditorLogin_IsTenantManager";
        private const string LOGGED_IN_APP_KEY = "Reflectis_EditorLogin_App";
        private const string LOGGED_IN_ENV_KEY = "Reflectis_EditorLogin_Env";

        // Helper methods to read/write with EditorPrefs as persistent backing store
        private static string GetString(string key, string defaultValue = "")
        {
            string value = SessionState.GetString(key, "");
            if (string.IsNullOrEmpty(value))
            {
                value = EditorPrefs.GetString(key, defaultValue);
                if (!string.IsNullOrEmpty(value))
                    SessionState.SetString(key, value);
            }
            return value;
        }

        private static void SetString(string key, string value)
        {
            SessionState.SetString(key, value);
            EditorPrefs.SetString(key, value);
        }

        private static bool GetBool(string key, bool defaultValue = false)
        {
            // SessionState doesn't distinguish "not set" from "false", so check EditorPrefs first on cold start
            string marker = SessionState.GetString(key + "_set", "");
            if (string.IsNullOrEmpty(marker))
            {
                bool value = EditorPrefs.GetBool(key, defaultValue);
                SessionState.SetBool(key, value);
                SessionState.SetString(key + "_set", "1");
                return value;
            }
            return SessionState.GetBool(key, defaultValue);
        }

        private static void SetBool(string key, bool value)
        {
            SessionState.SetBool(key, value);
            SessionState.SetString(key + "_set", "1");
            EditorPrefs.SetBool(key, value);
        }

        public static string BearerToken
        {
            get => GetString(TOKEN_KEY);
            private set => SetString(TOKEN_KEY, value);
        }

        public static Tenant CurrentTenant
        {
            get
            {
                string json = GetString(TENANT_KEY);
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
                SetString(TENANT_KEY, value != null ? JsonConvert.SerializeObject(value) : "");
            }
        }

        public static string Username
        {
            get => GetString(USERNAME_KEY);
            private set => SetString(USERNAME_KEY, value ?? "");
        }

        public static bool IsTenantManager
        {
            get => GetBool(IS_TENANT_MANAGER_KEY);
            private set => SetBool(IS_TENANT_MANAGER_KEY, value);
        }

        public static string LoggedInApp
        {
            get => GetString(LOGGED_IN_APP_KEY);
            private set => SetString(LOGGED_IN_APP_KEY, value ?? "");
        }

        public static string LoggedInEnv
        {
            get => GetString(LOGGED_IN_ENV_KEY);
            private set => SetString(LOGGED_IN_ENV_KEY, value ?? "");
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
