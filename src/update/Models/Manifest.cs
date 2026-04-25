using System.Text.Json.Serialization;

namespace update.Models
{
    /// <summary>
    /// A class holding manifest information.
    /// </summary>
    internal class Manifest
    {
        /// <summary>
        /// The source type of the update.
        /// </summary>
        [JsonPropertyName("source_type")]
        public required string SourceType { get; set; }

        /// <summary>
        /// The current version monicker of the update.
        /// </summary>
        [JsonPropertyName("version")]
        public required string Version { get; set; }

        /// <summary>
        /// The current build number of the update.
        /// </summary>
        [JsonPropertyName("build_number")]
        public required string BuildNumber { get; set; }

        /// <summary>
        /// The name of the executable to start after the update.
        /// </summary>
        [JsonPropertyName("application_name")]
        public required string ApplicationName { get; set; }
    }

    [JsonSerializable(typeof(Manifest))]
    internal partial class ManifestJsonContext : JsonSerializerContext
    {
    }
}
