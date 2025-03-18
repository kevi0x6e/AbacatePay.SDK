using AbacatePay.SDK.Exceptions;
using AbacatePay.SDK.Models.Request;
using AbacatePay.SDK.Models.Response.Billing;
using AbacatePay.SDK.Models.Response.Customer;
using AbacatePay.SDK.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using AbacatePay.SDK.Models.Response;
using AbacatePay.SDK.Models.Response.AbacatePayApi;

namespace AbacatePay.SDK.Tests.unit.Services
{
    public class BillingServiceTests
    {
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly HttpClient _httpClient;
        private readonly BillingService _billingService;
        private readonly JsonSerializerOptions _jsonOptions;

        public BillingServiceTests()
        {
            _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _httpClient = new HttpClient(_handlerMock.Object)
            {
                BaseAddress = new Uri("https://api.abacatepay.com/")
            };
            _billingService = new BillingService(_httpClient);
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        [Fact]
        public async Task CreateBillingAsync_WhenSuccessful_ReturnsData()
        {
            // Arrange
            var request = new BillingRequest(
                Methods: new List<string> { "PIX" },
                Products: new List<ProductRequest>
                {
                    new ProductRequest(
                        ExternalId: "prod-001",
                        Name: "Produto Teste",
                        Description: "Descrição do produto",
                        Quantity: 2,
                        Price: 5000 
                    )
                },
                ReturnUrl: new Uri("https://minhaloja.com/retorno"),
                CompletionUrl: new Uri("https://minhaloja.com/conclusao"),
                CustomerId: "cus_123456",
                Customer: new CustomerRequest(
                    Name: "Joe Due",
                    Cellphone: "+559999999999",
                    Email: "joedue@abacatepay.com",
                    TaxId: "11122233344"
                ),
                Frequency: "ONE_TIME"
            );

            var responseData = new BillingResponse(
                Metadata: new MetadataResponse(
                    Fee: 2.50m,
                    ReturnUrl: new Uri("https://minhaloja.com/retorno"),
                    CompletionUrl: new Uri("https://minhaloja.com/conclusao")
                ),
                Product: new List<ProductResponse>
                {
                    new ProductResponse(
                        Id: "prod_abcdef",
                        ExternalId: "prod-001",
                        Quantity: 2
                    )
                },
                Amount: 100.00m,
                Status: "pending",
                DevMode: false,
                Methods: new List<string> { "PIX" },
                Frequency: "ONE_TIME",
                AllowCoupons: false,
                Coupons: new List<string>(),
                Url: new Uri("https://pay.abacatepay.com/billing/123456"),
                Customer: new BillingCustomerResponse(
                    Metadata: new CustomerMetadataResponse("Joe Due","+559999999999","joedue@abacatepay.com","11122233344s")
                ),
                Id: "billing_123456"
            );
            
            var responseObject = new ApiResponse<BillingResponse>
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
            var result = await _billingService.CreateBillingAsync(request);
            
            var resultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("billing_123456", result.Id);
            Assert.Equal(100.00m, result.Amount);
            Assert.Equal("pending", result.Status);
            Assert.NotNull(result.Product);
            Assert.Single(result.Product);
            Assert.Equal("prod_abcdef", result.Product[0].Id);

            _handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("billing/create")),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task CreateBillingAsync_WhenApiReturnsError_ThrowsException()
        {
            // Arrange
            var request = new BillingRequest(
                Methods: new List<string> { "PIX" },
                Products: new List<ProductRequest>
                {
                    new ProductRequest(
                        ExternalId: "prod-001",
                        Name: "Produto Teste",
                        Description: "Descrição do produto",
                        Quantity: 1,
                        Price: 5000
                    )
                },
                ReturnUrl: new Uri("https://minhaloja.com/retorno"),
                CompletionUrl: new Uri("https://minhaloja.com/conclusao"),
                CustomerId: "cus_123456",
                Customer: new CustomerRequest(
                    Name: "Joe",
                    Cellphone: "1299999999",
                    Email: "joedue@abacatepay.com",
                    TaxId: "11122233344"
                ),
                Frequency: "ONE_TIME"
            );

            var responseObject = new ApiResponse<BillingResponse>
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
                async () => await _billingService.CreateBillingAsync(request));

            Assert.Equal("Unknown error occurred", exception.Message);
        }
        
        [Fact]
        public async Task GetBillingsAsync_WhenSuccessful_ReturnsListOfBillings()
        {
            // Arrange
            var responseData = new List<BillingResponse>
            {
                new BillingResponse(
                    Metadata: new MetadataResponse(
                        Fee: 2.50m,
                        ReturnUrl: new Uri("https://minhaloja.com/retorno"),
                        CompletionUrl: new Uri("https://minhaloja.com/conclusao")
                    ),
                    Product: new List<ProductResponse>
                    {
                        new ProductResponse(
                            Id: "prod_abc123",
                            ExternalId: "prod-001",
                            Quantity: 1
                        )
                    },
                    Amount: 50.00m,
                    Status: "paid",
                    DevMode: false,
                    Methods: new List<string> { "PIX" },
                    Frequency: "ONE_TIME",
                    AllowCoupons: false,
                    Coupons: new List<string>(),
                    Url: new Uri("https://pay.abacatepay.com/billing/123456"),
                    Customer: new BillingCustomerResponse(
                        Metadata: new CustomerMetadataResponse("Joe","+559999999999","joeduen@abacatepay.com","11122233344s")
                    ),
                    Id: "billing_123456"
                ),
                new BillingResponse(
                    Metadata: new MetadataResponse(
                        Fee: 3.00m,
                        ReturnUrl: new Uri("https://minhaloja.com/retorno"),
                        CompletionUrl: new Uri("https://minhaloja.com/conclusao")
                    ),
                    Product: new List<ProductResponse>
                    {
                        new ProductResponse(
                            Id: "prod_def456",
                            ExternalId: "prod-002",
                            Quantity: 2
                        )
                    },
                    Amount: 100.00m,
                    Status: "pending",
                    DevMode: false,
                    Methods: new List<string> { "PIX" },
                    Frequency: "ONE_TIME",
                    AllowCoupons: false,
                    Coupons: new List<string>(),
                    Url: new Uri("https://pay.abacatepay.com/billing/789012"),
                    Customer: new BillingCustomerResponse(
                        Metadata: new CustomerMetadataResponse("Joe","+559999999999","joeduen@abacatepay.com","11122233344s")
                    ),
                    Id: "billing_789012"
                )
            };
            
            var responseObject = new ApiResponse<List<BillingResponse>>
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
            var result = await _billingService.GetBillingsAsync();
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("billing_123456", result[0].Id);
            Assert.Equal("billing_789012", result[1].Id);
            Assert.Equal(50.00m, result[0].Amount);
            Assert.Equal(100.00m, result[1].Amount);
            
            _handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains("billing/list")),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task GetBillingsAsync_WhenApiReturnsError_ThrowsException()
        {
            // Arrange
            var responseObject = new ApiResponse<List<BillingResponse>>
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
                async () => await _billingService.GetBillingsAsync());

            Assert.Equal("Unknown error occurred", exception.Message);
        }

        [Fact]
        public async Task GetBillingsAsync_WhenApiReturnsNullData_ReturnsEmptyList()
        {
            // Arrange
            var responseObject = new ApiResponse<List<BillingResponse>>
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
            var result = await _billingService.GetBillingsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}