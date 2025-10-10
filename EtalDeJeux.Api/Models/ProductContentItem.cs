using System.Text.Json.Serialization;

namespace EtalDeJeux.Api.Models;

public class ProductContentItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("qty")]
    public int? Quantity { get; set; }
}
