using System.Net;

namespace ApiResultMonad.Lib
{

    /// <summary>
    /// Represents a succeeded API result that returns a <see cref="Value"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="Value"></param>
    public record Success<T>(T Value);

    /// <summary>
    /// Represents a HTTP error with <see cref="StatusCode"/> and <paramref name="Message"/>
    /// </summary>
    /// <param name="StatusCode"></param>
    /// <param name="Message"></param>
    public record HttpError(int StatusCode, string Message);

    /// <summary>
    /// Represents a generic error other than HttpError (sockets, I/O, other..)
    /// </summary>
    /// <param name="Exception"></param>
    public record TransportError(Exception Exception);

    public readonly union ApiResult<T>(
        Success<T> success,
        HttpError httpError,
        TransportError transportLevelError
    )
    {
        //body (logic) for the ApiResult<T> generic union type
        //handle the Map and Bind method

        /// <summary>
        /// Maps successful value(s) mapped possible multiple, errors are propagated unchanged through the 'chain' (mapping)
        /// Synchronous mapping.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="f"></param>
        /// <returns></returns>
        public ApiResult<TResult> Map<TResult>(Func<T,TResult> f) => Value switch {
            Success<T> s => new Success<TResult>(f(s.Value)),
            HttpError h => new HttpError(h.StatusCode, h.Message),
            TransportError t => new TransportError(t.Exception),
            _ => new HttpError((int)HttpStatusCode.InternalServerError, "Unhandled error")
        };

        /// <summary>
        /// Maps successful value(s) mapped possible multiple, errors are propagated unchanged through the 'chain' (mapping)
        /// Asynchronous mapping.
        /// </summary>
        public async Task<ApiResult<TResult>> MapAsync<TResult>(Func<T,Task<TResult>> f) => Value switch
        {
            Success<T> s => new Success<TResult>(await f(s.Value).ConfigureAwait(false)),
            HttpError h => new HttpError(h.StatusCode, h.Message),
            TransportError t => new TransportError(t.Exception),
            _ => new HttpError((int)HttpStatusCode.InternalServerError, "Unhandled error")
        };

        /// <summary>
        /// Bind (flatmap) for sequencing operations.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="f"></param>
        /// <returns></returns>
        public ApiResult<TResult> Bind<TResult>(Func<T, ApiResult<TResult>> f) => Value switch {
            Success<T> s => f(s.Value),
            HttpError h => new HttpError(h.StatusCode, h.Message),
            TransportError t => new TransportError(t.Exception),
            _ => new HttpError((int)HttpStatusCode.InternalServerError, "Unhandled error")
        };

        /// <summary>
        /// Bind (flatmap) for sequencing operations.
        /// Async version.
        /// </summary>
        public async Task<ApiResult<TResult>> BindAsync<TResult>(Func<T,Task<ApiResult<TResult>>> f) => Value switch
        {
            Success<T> s => await f(s.Value).ConfigureAwait(false),
            HttpError h => new HttpError(h.StatusCode, h.Message),
            TransportError t => new TransportError(t.Exception),
            _ => new HttpError((int)HttpStatusCode.InternalServerError, "Unhandled error")
        }; 

    }

    public static class ApiResult
    {
        public static ApiResult<T> Ok<T>(T result) => new Success<T>(result);
        public static ApiResult<T> HttpFail<T>(HttpStatusCode statusCode, string message) => new HttpError((int)statusCode, message);
        public static ApiResult<T> TransportFail<T>(Exception exception) => new TransportError(exception); 
    }
    
}
