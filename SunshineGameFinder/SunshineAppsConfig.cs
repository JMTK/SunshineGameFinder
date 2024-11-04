using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunshineGameFinder
{
    public class SunshineApp
    {
        public string name { get; set; }

        [JsonProperty("image-path")]
        public string imagepath { get; set; }
        public List<string> detached { get; set; }
        public string output { get; set; }
        public string cmd { get; set; }

        [JsonProperty("working-dir")]
        public string workingdir { get; set; }

        [JsonProperty("exclude-global-prep-cmd")]
        public bool excludeglobalprepcmd { get; set; }
        public bool elevated { get; set; }
        [JsonProperty("auto-detach")]
        public bool autodetach { get; set; }
        [JsonProperty("wait-all")]
        public bool waitall { get; set; }

        [JsonProperty("exit-timeout")]
        public int exittimeout { get; set; } = 5;
    }

    public class Env
    {
        public string PATH { get; set; }
    }

    public class SunshineConfig
    {
        public SunshineConfig()
        {

        }
        public SunshineConfig(Env env, List<SunshineApp> apps)
        {
            this.env = env;
            this.apps = apps;
        }

        public Env env { get; set; }
        public List<SunshineApp> apps { get; set; }
    }
}
