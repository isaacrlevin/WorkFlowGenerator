using Newtonsoft.Json;
using System;

namespace WorkFlowGenerator.Models.GitHub
{

    public class SecretRoot
    {
        [JsonProperty("total_count")]
        public int TotalCount { get; set; }
        [JsonProperty("secrets")]
        public Secret[] Secrets { get; set; }
    }

    public class Secret
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

}
