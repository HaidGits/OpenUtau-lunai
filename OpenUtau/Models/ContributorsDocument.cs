using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OpenUtau.App.Models {
    public class ContributorEntry {
        [JsonProperty("login")]
        public string Login { get; set; } = string.Empty;

        [JsonProperty("contributions")]
        public int Contributions { get; set; }

        [JsonProperty("profileUrl")]
        public string ProfileUrl { get; set; } = string.Empty;
    }

    public class ContributorsDocument {
        [JsonProperty("generatedAt")]
        public DateTime? GeneratedAt { get; set; }

        [JsonProperty("repository")]
        public string Repository { get; set; } = string.Empty;

        [JsonProperty("contributors")]
        public List<ContributorEntry> Contributors { get; set; } = new();

        [JsonIgnore]
        public bool FetchedLive { get; set; }
    }
}
