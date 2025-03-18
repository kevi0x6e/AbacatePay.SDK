using System.Text.Json.Serialization;

namespace AbacatePay.SDK.Models.Response.AbacatePayApi;

public record ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
        
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}