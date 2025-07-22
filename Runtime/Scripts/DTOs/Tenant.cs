using Newtonsoft.Json.Linq;

using System;

using UnityEngine;

namespace Reflectis.SDK.TenantConfiguration
{
    public enum Env
    {
        Sandbox,
        PreProduction,
        Production
    }

    public enum TenantStatus
    {
        Enabled,
        Disabled
    }

    [Serializable, Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.Fields)]
    public class Tenant
    {
        [SerializeField] private int id;
        // TODO: fix enum conversion
        //[Newtonsoft.Json.JsonConverter(typeof(EnumFlagsConverter<Env>))]
        [SerializeField] private string env;
        [SerializeField] private string label;
        [SerializeField] private string note;
        [SerializeField] private TenantStatus status;
        [SerializeField] private JObject config;

        public int Id => id;
        public Env Env => (Env)Enum.Parse(typeof(Env), env);
        public string Label => label;
        public string Note => note;
        public TenantStatus Status => status;
        public JObject Config => config;
    }

}

