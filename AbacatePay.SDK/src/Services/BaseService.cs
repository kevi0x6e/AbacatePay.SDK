using System.Text;
using System.Text.Json;
using AbacatePay.SDK.Models.Response.AbacatePayApi;

namespace AbacatePay.SDK.Services;

public abstract class BaseService
{
    protected readonly HttpClient HttpClient;
    private static string _baseUrl = "https://api.abacatepay.com/v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public BaseService(HttpClient httpClient)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public static void SetBaseUrl(string baseUrl) => _baseUrl = baseUrl.TrimEnd('/');

    protected async Task<(bool Success, T? Data, ErrorModelResponse? Error)> PostAsync<T>(string urlPath, object data, CancellationToken cancellationToken = default)
    {
        var content = new StringContent(JsonSerializer.Serialize(data, JsonOptions), Encoding.UTF8, "application/json");
        return await SendRequest<T>(HttpMethod.Post, urlPath, content, cancellationToken);
    }

    protected async Task<(bool Success, T? Data, ErrorModelResponse? Error)> GetAsync<T>(string urlPath, CancellationToken cancellationToken = default)
    {
        return await SendRequest<T>(HttpMethod.Get, urlPath, null, cancellationToken);
    }

    private async Task<(bool Success, T? Data, ErrorModelResponse? Error)> SendRequest<T>(HttpMethod method, string urlPath, HttpContent? content = null, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(method, CreateFullUrl(urlPath)) { Content = content };
        using var response = await HttpClient.SendAsync(request, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            try
            {
                var errorModel = JsonSerializer.Deserialize<ErrorModelResponse>(responseBody, JsonOptions);
                return (false, default, errorModel);
            }
            catch
            {
                return (false, default, new ErrorModelResponse($"API request failed with status {response.StatusCode}: {responseBody}"));
            }
        }

        try
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(responseBody, JsonOptions);
            
            if (apiResponse == null)
                return (false, default, new ErrorModelResponse("Failed to deserialize response"));
            
            
            if (!string.IsNullOrEmpty(apiResponse.Error))
                return (false, default, new ErrorModelResponse(apiResponse.Error));
            
            
            return (true, apiResponse.Data, null);
        }
        catch (JsonException ex)
        {
            return (false, default, new ErrorModelResponse($"Error parsing API response: {ex.Message}"));
        }
    }

    private static string CreateFullUrl(string urlPath) => $"{_baseUrl}/{urlPath.TrimStart('/')}";
}