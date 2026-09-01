using System;
using System.Collections.Generic;

using UnityEngine;

namespace Virtuademy.SDK.TenantConfiguration
{
    /// <summary>
    /// One API the platform reports this app may reach, as returned by
    /// <c>GET /manage/apps/api-endpoints</c> (HMAC, pre-login). See ADR 0024 in the
    /// meta-repo.
    /// </summary>
    /// <remarks>
    /// Resolution keys off <see cref="Type"/> — the canonical platform type — and not
    /// off <see cref="Label"/>: labels are tenant-scoped and change when a deployment is
    /// rebranded, types do not.
    /// </remarks>
    [Serializable]
    public class ApiEndpoint
    {
        [SerializeField] private string apiId;
        [SerializeField] private string label;
        [SerializeField] private string type;
        [SerializeField] private List<string> baseUrls;

        public string ApiId => apiId;
        public string Label => label;
        public string Type => type;

        /// <summary>
        /// Base URLs, already ordered by the server so the most useful one to a client
        /// comes first.
        /// </summary>
        public IReadOnlyList<string> BaseUrls => baseUrls;
    }
}
