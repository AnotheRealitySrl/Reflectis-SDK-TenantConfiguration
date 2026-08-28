using System;

using UnityEngine;

namespace Virtuademy.SDK.TenantConfiguration
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
        [SerializeField] private string label;
        [SerializeField] private string note;
        [SerializeField] private TenantStatus status;
        [SerializeField] private Env env;
        [SerializeField] private TenantConfig config;

        public int Id => id;
        public string Label => label;
        public string Note => note;
        public TenantStatus Status => status;
        public Env Env => env;
        public TenantConfig Config => config;
    }

}

