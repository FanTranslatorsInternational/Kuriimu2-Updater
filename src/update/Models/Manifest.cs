using Newtonsoft.Json;

namespace update.Models
{
    /// <summary>
    /// A class holding manifest information.
    /// </summary>
    class Manifest
    {
        /// <summary>
        /// The source type of the update.
        /// </summary>
        [JsonProperty("source_type")]
        public string SourceType { get; set; }

        /// <summary>
        /// The current build number of the update.
        /// </summary>
        [JsonProperty("build_number")]
        public string BuildNumber { get; set; }

        /// <summary>
        /// The name of the executable to start after the update.
        /// </summary>
        [JsonProperty("application_name")]
        public string ApplicationName { get; set; }
    }
}
