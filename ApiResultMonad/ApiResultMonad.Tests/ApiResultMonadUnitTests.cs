using System.Data;
using System.Net;
using ApiResultMonad.Lib;
using FluentAssertions;

namespace ApiResultMonad.Tests
{
    public class ApiResultMonadUnitTests
    {
        [SetUp]
        public void Setup()
        {
        }

        // --- Factory helpers ---

        [Test]
        public void OkCreatesSuccess()
        {
            var result = ApiResult.Ok(42);
            result.Value.Should().BeOfType<Success<int>>()
                .Which.Data.Should().Be(42);
        }

        [Test]
        public void HttpFailCreatesHttpError()
        {
            var result = ApiResult.HttpFail<int>(HttpStatusCode.NotFound, "not found");
            result.Value.Should().BeOfType<HttpError>()
                .Which.StatusCode.Should().Be(404);
        }   
        
        [Test]
        public void HttpFailInternalServerErrorStillMapsToHttpError(){
            var result = ApiResult.HttpFail<int>(HttpStatusCode.InternalServerError, "internal server error");
            result.Value.Should().BeOfType<HttpError>()
                .Which.StatusCode.Should().Be(500);
        }       

        [Test]
        public void TransportFailCreatesTransportError()
        {
            var ex = new InvalidOperationException("boom");
            var result = ApiResult.TransportFail<int>(ex);
            result.Value.Should().BeOfType<TransportError>()
                .Which.Exception.Should().BeSameAs(ex);
        }

        // --- Map (sync) ---

        [Test]
        [TestCase(5, 10)]
        [TestCase(0, 0)]
        [TestCase(-3, -6)]
        public void MapTransformsSuccessValue(int input, int expected)
        {
            var result = ApiResult.Ok(input).Map(x => x * 2);
            result.Value.Should().BeOfType<Success<int>>()
                .Which.Data.Should().Be(expected);
        }

        [Test]
        public void MapDivideByZeroThrows()
        {
            var act = () => ApiResult.Ok(0).Map(x => 15 / x);
            act.Should().Throw<DivideByZeroException>();
        }

        [Test]
        public void MapWithValidDivisorReturnsSuccess()
        {
            var result = ApiResult.Ok(3).Map(x => 15 / x);
            result.Value.Should().BeOfType<Success<int>>()
                .Which.Data.Should().Be(5);
        }

        [Test]
        public void MapPropagatesHttpError()
        {
            var result = ApiResult.HttpFail<int>(HttpStatusCode.BadRequest, "bad request")
                .Map(x => x * 2);
            result.Value.Should().BeOfType<HttpError>()
                .Which.StatusCode.Should().Be(400);
        }

        [Test]
        public void MapPropagatesTransportError()
        {
            var ex = new Exception("network failure");
            var result = ApiResult.TransportFail<int>(ex).Map(x => x * 2);
            result.Value.Should().BeOfType<TransportError>()
                .Which.Exception.Should().BeSameAs(ex);
        }

        // --- MapAsync ---

        [Test]
        [TestCase(10, 25)]
        [TestCase(0, 5)]
        public async Task MapAsyncThenBindAsyncTransformsSuccess(int input, int expected)
        {
            var ok = ApiResult.Ok(input);
            var doubled = await ok.MapAsync(async x => x * 2);
            var result = await doubled.BindAsync<int>(async x => ApiResult.Ok(x + 5));
            result.Value.Should().BeOfType<Success<int>>()
                .Which.Data.Should().Be(expected);
        }

        [Test]
        public async Task MapAsyncPropagatesHttpError()
        {
            var result = await ApiResult.HttpFail<int>(HttpStatusCode.Unauthorized, "unauthorized")
                .MapAsync(async x => x * 2);
            result.Value.Should().BeOfType<HttpError>()
                .Which.StatusCode.Should().Be(401);
        }

        [Test]
        public async Task MapAsyncPropagatesTransportError()
        {
            var ex = new Exception("timeout");
            var result = await ApiResult.TransportFail<int>(ex)
                .MapAsync(async x => x * 2);
            result.Value.Should().BeOfType<TransportError>()
                .Which.Exception.Should().BeSameAs(ex);
        }

        // --- Bind (sync) ---

        [Test]
        public void BindChainsSuccessToNextOperation()
        {
            var result = ApiResult.Ok(10).Bind(x => ApiResult.Ok(x.ToString()));
            result.Value.Should().BeOfType<Success<string>>()
                .Which.Data.Should().Be("10");
        }

        [Test]
        public void BindPropagatesHttpError()
        {
            var result = ApiResult.HttpFail<int>(HttpStatusCode.Forbidden, "forbidden")
                .Bind(x => ApiResult.Ok(x * 2));
            result.Value.Should().BeOfType<HttpError>()
                .Which.StatusCode.Should().Be(403);
        }

        [Test]
        public void BindPropagatesTransportError()
        {
            var ex = new Exception("io error");
            var result = ApiResult.TransportFail<int>(ex)
                .Bind(x => ApiResult.Ok(x * 2));
            result.Value.Should().BeOfType<TransportError>()
                .Which.Exception.Should().BeSameAs(ex);
        }

        [Test]
        public void BindReturnsFailureFromBoundFunction()
        {
            var result = ApiResult.Ok(10)
                .Bind<int>(_ => ApiResult.HttpFail<int>(HttpStatusCode.InternalServerError, "error in chain"));
            result.Value.Should().BeOfType<HttpError>()
                .Which.StatusCode.Should().Be(500);
        }

        // --- BindAsync ---

        [Test]
        public async Task BindAsyncChainsSuccessToNextOperation()
        {
            var result = await ApiResult.Ok(7)
                .BindAsync(async x => ApiResult.Ok(x * 3));
            result.Value.Should().BeOfType<Success<int>>()
                .Which.Data.Should().Be(21);
        }

        [Test]
        public async Task BindAsyncPropagatesHttpError()
        {
            var result = await ApiResult.HttpFail<int>(HttpStatusCode.NotFound, "not found")
                .BindAsync(async x => ApiResult.Ok(x * 3));
            result.Value.Should().BeOfType<HttpError>()
                .Which.StatusCode.Should().Be(404);
        }

        [Test]
        public async Task BindAsyncPropagatesTransportError()
        {
            var ex = new Exception("socket closed");
            var result = await ApiResult.TransportFail<int>(ex)
                .BindAsync(async x => ApiResult.Ok(x * 3));
            result.Value.Should().BeOfType<TransportError>()
                .Which.Exception.Should().BeSameAs(ex);
        }
    }
}
