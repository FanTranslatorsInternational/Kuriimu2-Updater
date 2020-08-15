using Newtonsoft.Json;

namespace update.Models
{
    class Manifest
    {
        [JsonProperty("source_type")]
        public string SourceType { get; set; }

        [JsonProperty("build_number")]
        public string BuildNumber { get; set; }

        [JsonProperty("application_name")]
        public string ApplicationName { get; set; }

        [JsonProperty("update_source")]
        public string UpdateSource { get; set; }

        [JsonProperty("public_key")]
        public string PublicKey { get; set; }
    }
}
