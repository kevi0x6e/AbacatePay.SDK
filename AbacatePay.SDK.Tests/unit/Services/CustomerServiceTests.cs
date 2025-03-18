using System.Net;
using System.Text;
using System.Text.Json;
using AbacatePay.SDK.Exceptions;
using AbacatePay.SDK.Models.Request;
using AbacatePay.SDK.Models.Response.AbacatePayApi;
using AbacatePay.SDK.Models.Response.Customer;
using AbacatePay.SDK.Services;
using Moq;
using Moq.Protected;

namespace AbacatePay.SDK.Tests.unit.Services;

public class CustomerServiceTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly CustomerService _customerService;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public CustomerServiceTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.abacatepay.com/")
        };
        _customerService = new CustomerService(_httpClient);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }
    
    [Fact]
    public async Task CreateCustomerAsync_WhenSuccessful_ReturnsData()
    {
        // Arrange
        var request = new CustomerRequest(
            Name: "Joe", 
            Cellphone: "+559999999999",
            Email: "joedue@abacatepay.com",
            TaxId: "11122233344"
            );

        var responseData = new CustomerResponse(
            Id: "client_123456",
            Metadata: new CustomerMetadataResponse(
                Name: "Joe",
                Cellphone: "+559999999999",
                Email: "joedue@abacatepay.com",
                TaxId: "11122233344"
                )
            );
            
        var responseObject = new ApiResponse<CustomerResponse>
        {
            Data = responseData,
            Error = null
        };

        var jsonResponse = JsonSerializer.Serialize(responseObject, _jsonOptions);
            
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
                )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            })
            .Verifiable();

        // Act
        var result = await _customerService.CreateClientAsync(request);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("client_123456", result.Id);
        Assert.Equal("Joe", result.Metadata.Name);
        Assert.Equal("joedue@abacatepay.com", result.Metadata.Email);
        Assert.Equal("+559999999999", result.Metadata.Cellphone);

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.ToString().Contains("customer/create")),
            ItExpr.IsAny<CancellationToken>()
            );
    }
    
    [Fact]
    public async Task CreateCustomerAsync_WhenApiReturnsError_ThrowsException()
    { 
        // Arrange
        var request = new CustomerRequest(
            Name: "Joe", 
            Cellphone: "+559999999999",
            Email: "joedue@abacatepay.com",
            TaxId: "11122233344"
            );
            
        var responseObject = new ApiResponse<CustomerResponse>
        { 
            Data = null,
            Error = "Unknown error occurred"
        };

        var jsonResponse = JsonSerializer.Serialize(responseObject, _jsonOptions);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
                )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

            // Act & Assert
        var exception = await Assert.ThrowsAsync<AbacatePayException>(
            async () => await _customerService.CreateClientAsync(request));

        Assert.Equal("Unknown error occurred", exception.Message);
    }
    
    [Fact]
    public async Task GetCustomersAsync_WhenSuccessful_ReturnsListOfCustomers()
    {
        // Arrange
        var responseData = new List<CustomerResponse>
        {
            new CustomerResponse(
                Id: "client_123456",
                Metadata: new CustomerMetadataResponse(
                    Name: "Joe",
                    Cellphone: "+559999999999",
                    Email: "joedue@abacatepay.com",
                    TaxId: "11122233344")
                ),
            new CustomerResponse(
                Id: "client_1234567",
                Metadata: new CustomerMetadataResponse(
                    Name: "Joe",
                    Cellphone: "+559999999999",
                    Email: "joedue@abacatepay.com",
                    TaxId: "11122233344")
                )
            };
            
            var responseObject = new ApiResponse<List<CustomerResponse>>
            {
                Data = responseData,
                Error = null
            };

            var jsonResponse = JsonSerializer.Serialize(responseObject, _jsonOptions);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _customerService.GetClientsAsync();
            
            var resultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("client_123456", result[0].Id);
            Assert.Equal("client_1234567", result[1].Id);
            
            _handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains("customer/list")),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    
    [Fact]
    public async Task GetCustomersAsync_WhenApiReturnsError_ThrowsException()
    {
        // Arrange
        var responseObject = new ApiResponse<List<CustomerResponse>>
        {
            Data = null,
            Error = "Unknown error occurred"
        };

        var jsonResponse = JsonSerializer.Serialize(responseObject, _jsonOptions);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AbacatePayException>(
            async () => await _customerService.GetClientsAsync());

        Assert.Equal("Unknown error occurred", exception.Message);
    }
    
    [Fact]
    public async Task GetCustomersAsync_WhenApiReturnsNullData_ReturnsEmptyList()
    {
        // Arrange
        var responseObject = new ApiResponse<List<CustomerResponse>>
        {
            Data = null,
            Error = null
        };

        var jsonResponse = JsonSerializer.Serialize(responseObject, _jsonOptions);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _customerService.GetClientsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}