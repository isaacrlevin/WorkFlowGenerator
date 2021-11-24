using Newtonsoft.Json;

namespace WorkFlowGenerator.Models.GitHub;

public class PublicKey
{
    [JsonProperty("key_id")]
    public string KeyId { get; set; }
    [JsonProperty("key")]
    public string Key { get; set; }
}
