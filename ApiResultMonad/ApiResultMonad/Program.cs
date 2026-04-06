using System.Net;
using ApiResultMonad.Lib;
using Microsoft.Extensions.DependencyInjection;

namespace ApiResultMonad;

file record Todo(int UserId, int Id, string Title, bool Completed);

internal class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddHttpClient("default", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var httpClient = factory.CreateClient("default");

        const string url = "https://jsonplaceholder.typicode.com/todos/4";
        Console.WriteLine($"Fetching: {url}");

        var result = await httpClient.GetJsonAsync<Todo>(url);

        //Note , Completed is a property of the record Todo here and not to be mistaken for 
        //anything to do with the http request itself not being completed
        var summary = await result
            .MapAsync(async todo => todo with { Title = todo.Title.ToUpperInvariant() })
            .ContinueWith(t => t.Result.Bind(todo =>
                todo.Completed ? ApiResult.Ok($"Done: {todo.Title} . Raw data: {todo}") : ApiResult.HttpFail<string>(HttpStatusCode.UnprocessableEntity, "Not completed")));


        Console.WriteLine(summary.Value switch
        {
            Success<string> s => s.Data,
            HttpError h => $"Error {h.StatusCode}: {h.Message}",
            TransportError t => $"Transport: {t.Exception.Message}",
            _ => "?"
        });
    }
}
