using System;

using Unity.Properties;

using UnityEngine;

namespace Reflectis.SDK.TenantConfiguration
{
    [Serializable, Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.Fields)]
    public class AppConfig
    {
        [SerializeField] private string appId;
        [SerializeField] private string appSecret;
        [SerializeField] private string tenantConfigurationApiUrl;
        [SerializeField] private string tenantConfigurationApiVersion;

        [CreateProperty] public string AppId => appId;
        [CreateProperty] public string AppSecret => appSecret;
        [CreateProperty] public string TenantConfigurationApiUrl => tenantConfigurationApiUrl;
        [CreateProperty] public string TenantConfigurationApiVersion => tenantConfigurationApiVersion;
    }

}

