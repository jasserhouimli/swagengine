using System.Text.Json.Serialization;

public record Document(int Id, string Url, string Text);

public class CrawlRequest
{
    [JsonPropertyName("seeds")]
    public string[]? Seeds { get; set; }
}
