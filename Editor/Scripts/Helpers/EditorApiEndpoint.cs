namespace Virtuademy.SDK.TenantConfiguration.Editor
{
    /// <summary>
    /// The Application API the editor tooling talks to: addressables deploy, interpreted-script
    /// verification and environment DLL import all resolve it here.
    ///
    /// One named place on purpose. Reading the tenant inline is a two-line expression, and while
    /// this feature was being built that expression got replaced with a hardcoded localhost in
    /// four separate files during debugging — with the ones nobody remembered still pointing at
    /// the tenant, so half the flow talked to one API and half to the other. A single accessor is
    /// what makes that mismatch impossible to introduce by accident.
    /// </summary>
    public static class EditorApiEndpoint
    {
        /// <summary>
        /// Base URL of the logged-in tenant's Application API, or null when nobody is logged in —
        /// callers already treat that as "cannot reach the platform".
        /// </summary>
        public static string ApplicationApiUrl => EditorLoginState.CurrentTenant?.Config?.ApplicationApiUrl;
    }
}
