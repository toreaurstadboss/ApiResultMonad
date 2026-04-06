using System.Net;
using ApiResultMonad.Lib;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ApiResultMonad.Tests
{

    //file scoped record only visible in this file
    file record Todo(int UserId, int Id, string Title, bool Completed);

    public class ApiResultMonadIntegrationTests
    {
        private HttpClient _httpClient = null!;
        FakeHttpMessageHandler _fakeHandler = null!;

        [TearDown]
        public void Cleanup(){
            _httpClient.Dispose();
            _fakeHandler.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            _fakeHandler = new FakeHttpMessageHandler();

            var services = new ServiceCollection();
            services.AddHttpClient("default", client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(10);
            }).ConfigurePrimaryHttpMessageHandler(() => _fakeHandler);

            var provider = services.BuildServiceProvider();
            _httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("default");
        }

        [Test]
        public async Task GetJsonAsync_WhenValidCamelCaseJson_ReturnsSuccessWithDeserializedData()
        {
            const string json = """{"userId":1,"id":1,"title":"delectus aut autem","completed":false}""";
            _fakeHandler.SetResponse(HttpStatusCode.OK, json);

            var result = await _httpClient.GetJsonAsync<Todo>("https://example.com/todos/1");

            var success = result.Value.Should().BeOfType<Success<Todo>>().Which;
            success.Data.UserId.Should().Be(1);
            success.Data.Id.Should().Be(1);
            success.Data.Title.Should().Be("delectus aut autem");
            success.Data.Completed.Should().BeFalse();
        }

        [Test]
        public async Task GetJsonAsync_WhenValidPascalCaseJson_ReturnsSuccessWithDeserializedData()
        {
            const string json = """{"UserId":2,"Id":2,"Title":"test","Completed":true}""";
            _fakeHandler.SetResponse(HttpStatusCode.OK, json);

            var result = await _httpClient.GetJsonAsync<Todo>("https://example.com/todos/2");

            result.Value.Should().BeOfType<Success<Todo>>()
                .Which.Data.Id.Should().Be(2);
        }

        [Test]
        [TestCase(HttpStatusCode.NotFound, 404)]
        [TestCase(HttpStatusCode.Unauthorized, 401)]
        [TestCase(HttpStatusCode.InternalServerError, 500)]
        public async Task GetJsonAsync_WhenHttpErrorResponse_ReturnsHttpError(HttpStatusCode statusCode, int expectedCode)
        {
            _fakeHandler.SetResponse(statusCode, "error");

            var result = await _httpClient.GetJsonAsync<Todo>("https://example.com/todos/1");

            result.Value.Should().BeOfType<HttpError>()
                .Which.StatusCode.Should().Be(expectedCode);
        }

        [Test]
        public async Task GetJsonAsync_WhenTransportFails_ReturnsTransportError()
        {
            _fakeHandler.SetException(new HttpRequestException("Connection refused"));

            var result = await _httpClient.GetJsonAsync<Todo>("https://example.com/todos/1");

            result.Value.Should().BeOfType<TransportError>()
                .Which.Exception.Should().BeOfType<HttpRequestException>();
        }

        [Test]
        public async Task GetJsonAsync_WhenResponseBodyIsEmpty_ReturnsTransportError()
        {
            _fakeHandler.SetResponse(HttpStatusCode.OK, "");

            var result = await _httpClient.GetJsonAsync<Todo>("https://example.com/todos/1");

            result.Value.Should().BeOfType<TransportError>();
        }
        
    }
}
