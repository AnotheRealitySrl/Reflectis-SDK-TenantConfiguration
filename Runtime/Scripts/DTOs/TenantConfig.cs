using System;

using Unity.Properties;

using UnityEngine;

namespace Reflectis.SDK.TenantConfiguration
{
    [Serializable, Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.Fields)]
    public class TenantConfig
    {
        [SerializeField] private string applicationUrl;
        [SerializeField] private string profileApiUrl;
        [SerializeField] private string profileApiVersion;
        [SerializeField] private string applicationApiUrl;
        [SerializeField] private string applicationApiVersion;
        [SerializeField] private string realtimeApiUrl;
        [SerializeField] private string realtimeApiVersion;

        public string ProfileApiUrl => profileApiUrl;
        public string ProfileApiVersion => profileApiVersion;
        [CreateProperty] public string ApplicationUrl => applicationUrl;
        public string ApplicationApiUrl => applicationApiUrl;
        public string ApplicationApiVersion => applicationApiVersion;
        public string RealtimeApiUrl => realtimeApiUrl;
        public string RealtimeApiVersion => realtimeApiVersion;

    }
}

