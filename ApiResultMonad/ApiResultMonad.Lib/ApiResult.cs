namespace ApiResultMonad.Lib
{
    public record Success<T>(T Value);
    public record HttpError(int StatusCode, string Message);
    public record TransportLevelError(string Message, Exception exception);

    public record Cat(string meow);

    public readonly union Pet(Cat);

    
}
