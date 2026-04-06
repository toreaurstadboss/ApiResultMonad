using System.Net;
using System.Text;

namespace ApiResultMonad.Tests
{
    /// <summary>
    /// Fake Http message handler for easier testing of Http not depending on an actual network
    /// </summary>
    sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private HttpResponseMessage _response = new(HttpStatusCode.OK);
        private Exception? _exception;

        public void SetResponse(HttpStatusCode statusCode, string content)
        {
            _exception = null;
            _response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };
        }

        public void SetException(Exception exception) => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_exception is not null)
                throw _exception;
            return Task.FromResult(_response);
        }
    }
}
