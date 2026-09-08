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
    /// <remarks>
    /// <c>MemberSerialization.Fields</c> is load-bearing, and its absence was a silent bug until
    /// 2026-09-08. The data lives in private fields behind read-only properties, and Newtonsoft's
    /// default only writes public properties and fields — it cannot assign a read-only property and
    /// ignores a private field, so every instance deserialized with every field null. The
    /// consequence was invisible rather than loud: <see cref="TenantConfigurationSystem"/> matches
    /// on <see cref="Type"/>, which was always null, so no endpoint ever matched, the resolver
    /// registered a table that could never answer, and every API silently fell back to the base URL
    /// serialized into its own asset — which is the documented degradation, so nothing complained.
    /// Its siblings <c>Tenant</c> and <c>TenantConfig</c> carry the attribute; this one did not.
    /// </remarks>
    [Serializable, Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.Fields)]
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
