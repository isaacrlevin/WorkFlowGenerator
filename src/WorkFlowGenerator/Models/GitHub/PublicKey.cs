using Newtonsoft.Json;

namespace WorkFlowGenerator.Models.GitHub;

public class PublicKey
{
    [JsonProperty("key_id")]
    public string Key_Id { get; set; }
    [JsonProperty("key")]
    public string Key { get; set; }
}

public class CreateSecretRequest
{ 
    public string key_id { get; set; }

    public byte[] encrypted_value { get; set; }

}
