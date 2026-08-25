using UnityEditor;

namespace Reflectis.SDK.TenantConfiguration.Editor
{
    /// <summary>
    /// Which Application API the editor tooling talks to.
    ///
    /// Normally the logged-in tenant's, but an author testing changes that are not deployed yet
    /// needs to point the whole toolchain elsewhere. Without one place to say so, that ends up as
    /// a hardcoded URL in whichever file is being debugged — and the other call sites keep hitting
    /// the tenant, so half the flow talks to one API and half to the other. That is exactly how
    /// the environment DLL import spent an afternoon uploading to one host and importing from
    /// another.
    ///
    /// The override lives in EditorPrefs, so it survives domain reloads and editor restarts:
    /// clear it when you are done, or you will keep publishing somewhere else without noticing.
    /// It only redirects HTTP calls — file uploads go over SFTP to whatever host the API's own
    /// configuration hands out, which is not affected by this.
    /// </summary>
    public static class EditorApiEndpoint
    {
        private const string OVERRIDE_KEY = "Reflectis_EditorLogin_ApiUrlOverride";

        /// <summary>Base URL to use instead of the tenant's, or empty for the tenant's.</summary>
        public static string Override
        {
            get => EditorPrefs.GetString(OVERRIDE_KEY, string.Empty);
            set => EditorPrefs.SetString(OVERRIDE_KEY, (value ?? string.Empty).Trim().TrimEnd('/'));
        }

        /// <summary>True when the tooling is NOT talking to the logged-in tenant.</summary>
        public static bool IsOverridden => !string.IsNullOrEmpty(Override);

        /// <summary>
        /// The Application API base URL, override first. Null when there is no override and no
        /// tenant is logged in — callers already treat that as "cannot reach the platform".
        /// </summary>
        public static string ApplicationApiUrl
        {
            get
            {
                string over = Override;
                return !string.IsNullOrEmpty(over)
                    ? over
                    : EditorLoginState.CurrentTenant?.Config?.ApplicationApiUrl;
            }
        }
    }
}
