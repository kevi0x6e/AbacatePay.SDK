using System.Net;
using System.Text;
using System.Text.Json;
using AbacatePay.SDK.Models.Response.AbacatePayApi;
using AbacatePay.SDK.Services;
using Moq;
using Moq.Protected;

namespace AbacatePay.SDK.Tests.Services
{
    public class BaseServiceTests
    {
        private class TestService : BaseService
        {
            public TestService(HttpClient httpClient) : base(httpClient) { }

            public Task<(bool Success, T? Data, ErrorModelResponse? Error)> TestPostAsync<T>(string urlPath, object data, CancellationToken cancellationToken = default)
            {
                return PostAsync<T>(urlPath, data, cancellationToken);
            }

            public Task<(bool Success, T? Data, ErrorModelResponse? Error)> TestGetAsync<T>(string urlPath, CancellationToken cancellationToken = default)
            {
                return GetAsync<T>(urlPath, cancellationToken);
            }
        }

        private class TestModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new TestService(null!));
            Assert.Equal("httpClient", exception.ParamName);
        }

        [Fact]
        public void SetBaseUrl_WithValidUrl_SetsBaseUrl()
        {
            // Arrange
            var originalUrl = "https://api.abacatepay.com/v1";
            var newUrl = "https://test.api.abacatepay.com/v1";
            
            // Act
            BaseService.SetBaseUrl(newUrl);
            
            // Assert 
            var handlerMock = new Mock<HttpMessageHandler>();
            SetupMockHandler(handlerMock, HttpStatusCode.OK, 
                new ApiResponse<TestModel> { Data = new TestModel { Id = 1, Name = "Test" } });
            
            var httpClient = new HttpClient(handlerMock.Object);
            var service = new TestService(httpClient);
            
            // Act
            var result = service.TestGetAsync<TestModel>("test").Result;
            
            // Assert 
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.ToString().StartsWith(newUrl)),
                ItExpr.IsAny<CancellationToken>()
            );

            BaseService.SetBaseUrl(originalUrl);
        }

        [Fact]
        public void SetBaseUrl_WithTrailingSlash_RemovesTrailingSlash()
        {
            // Arrange
            var originalUrl = "https://api.abacatepay.com/v1";
            var newUrl = "https://test.api.abacatepay.com/v1/";
            var expectedUrl = "https://test.api.abacatepay.com/v1";
            
            // Act
            BaseService.SetBaseUrl(newUrl);
            
            // Assert through a request
            var handlerMock = new Mock<HttpMessageHandler>();
            SetupMockHandler(handlerMock, HttpStatusCode.OK, 
                new ApiResponse<TestModel> { Data = new TestModel { Id = 1, Name = "Test" } });
            
            var httpClient = new HttpClient(handlerMock.Object);
            var service = new TestService(httpClient);
            
            var result = service.TestGetAsync<TestModel>("test").Result;
            
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.ToString().StartsWith(expectedUrl)),
                ItExpr.IsAny<CancellationToken>()
            );

            BaseService.SetBaseUrl(originalUrl);
        }
        
        [Fact]
        public async Task GetAsync_SuccessfulResponse_ReturnsDeserializedData()
        {
            // Arrange
            var expectedModel = new TestModel { Id = 1, Name = "Test" };
            var handlerMock = new Mock<HttpMessageHandler>();
            SetupMockHandler(handlerMock, HttpStatusCode.OK, new ApiResponse<TestModel> { Data = expectedModel });
            
            var httpClient = new HttpClient(handlerMock.Object);
            var service = new TestService(httpClient);
            
            // Act
            var result = await service.TestGetAsync<TestModel>("test");
            
            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(expectedModel.Id, result.Data.Id);
            Assert.Equal(expectedModel.Name, result.Data.Name);
            Assert.Null(result.Error);
        }
        
        [Fact]
        public async Task GetAsync_ApiErrorResponse_ReturnsErrorModel()
        {
            // Arrange
            var errorMessage = "Something went wrong";
            var handlerMock = new Mock<HttpMessageHandler>();
            SetupMockHandler(handlerMock, HttpStatusCode.OK, new ApiResponse<TestModel> { Error = errorMessage });
            
            var httpClient = new HttpClient(handlerMock.Object);
            var service = new TestService(httpClient);
            
            // Act
            var result = await service.TestGetAsync<TestModel>("test");
            
            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.NotNull(result.Error);
            Assert.Equal(errorMessage, result.Error.Message);
        }

        [Fact]
        public async Task GetAsync_HttpErrorResponse_ReturnsErrorModel()
        {
            // Arrange
            var errorResponse = new ErrorModelResponse("API Error");
            var handlerMock = new Mock<HttpMessageHandler>();
            SetupMockHandler(handlerMock, HttpStatusCode.BadRequest, errorResponse);
            
            var httpClient = new HttpClient(handlerMock.Object);
            var service = new TestService(httpClient);
            
            // Act
            var result = await service.TestGetAsync<TestModel>("test");
            
            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.NotNull(result.Error);
            Assert.Equal("API Error", result.Error.Message);
        }

        [Fact]
        public async Task GetAsync_DeserializationFails_ReturnsErrorModel()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Invalid JSON", Encoding.UTF8, "application/json")
                });
            
            var httpClient = new HttpClient(handlerMock.Object);
            var service = new TestService(httpClient);
            
            // Act
            var result = await service.TestGetAsync<TestModel>("test");
            
            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.NotNull(result.Error);
            Assert.Contains("Error parsing API response", result.Error.Message);
        }

        [Fact]
        public async Task PostAsync_SuccessfulResponse_ReturnsDeserializedData()
        {
            // Arrange
            var testPayload = new { Name = "Test" };
            var expectedModel = new TestModel { Id = 1, Name = "Test" };
            var handlerMock = new Mock<HttpMessageHandler>();
            SetupMockHandler(handlerMock, HttpStatusCode.OK, new ApiResponse<TestModel> { Data = expectedModel });
            
            var httpClient = new HttpClient(handlerMock.Object);
            var service = new TestService(httpClient);
            
            // Act
            var result = await service.TestPostAsync<TestModel>("test", testPayload);
            
            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(expectedModel.Id, result.Data.Id);
            Assert.Equal(expectedModel.Name, result.Data.Name);
            Assert.Null(result.Error);
            
            // Verify 
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.Content!.Headers.ContentType!.MediaType == "application/json"),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task PostAsync_HttpErrorWithInvalidErrorModel_CreatesGenericErrorModel()
        {
            // Arrange
            var testPayload = new { Name = "Test" };
            var handlerMock = new Mock<HttpMessageHandler>();
            
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Not a valid error model", Encoding.UTF8, "application/json")
                });
            
            var httpClient = new HttpClient(handlerMock.Object);
            var service = new TestService(httpClient);
            
            // Act
            var result = await service.TestPostAsync<TestModel>("test", testPayload);
            
            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.NotNull(result.Error);
            Assert.Contains("API request failed with status BadRequest", result.Error.Message);
        }

        [Fact]
        public async Task PostAsync_NullResponse_ReturnsError()
        {
            // Arrange
            var testPayload = new { Name = "Test" };
            var handlerMock = new Mock<HttpMessageHandler>();
            
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                });
            
            var httpClient = new HttpClient(handlerMock.Object);
            var service = new TestService(httpClient);
            
            // Act
            var result = await service.TestPostAsync<TestModel>("test", testPayload);
            
            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.NotNull(result.Error);
            Assert.Equal("Failed to deserialize response", result.Error.Message);
        }

        [Fact]
        public void CreateFullUrl_WithLeadingSlash_RemovesLeadingSlash()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            SetupMockHandler(handlerMock, HttpStatusCode.OK, 
                new ApiResponse<TestModel> { Data = new TestModel { Id = 1, Name = "Test" } });
            
            var httpClient = new HttpClient(handlerMock.Object);
            var service = new TestService(httpClient);
            
            // Act 
            var result = service.TestGetAsync<TestModel>("/test").Result;
            
            // Assert 
            var baseUrl = "https://api.abacatepay.com/v1";
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.ToString() == $"{baseUrl}/test"),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact] 
        public void CreateFullUrl_WithoutLeadingSlash_AddsCorrectly()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            SetupMockHandler(handlerMock, HttpStatusCode.OK, 
                new ApiResponse<TestModel> { Data = new TestModel { Id = 1, Name = "Test" } });
            
            var httpClient = new HttpClient(handlerMock.Object);
            var service = new TestService(httpClient);
            
            // Act 
            var result = service.TestGetAsync<TestModel>("test").Result;
            
            // Assert 
            var baseUrl = "https://api.abacatepay.com/v1";
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.ToString() == $"{baseUrl}/test"),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        private void SetupMockHandler<T>(Mock<HttpMessageHandler> handlerMock, HttpStatusCode statusCode, T responseContent)
        {
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(
                        JsonSerializer.Serialize(responseContent, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                        }),
                        Encoding.UTF8,
                        "application/json"
                    )
                });
        }
    }
}