using ApiResultMonad.Lib;
using Microsoft.Extensions.DependencyInjection;

namespace ApiResultMonad;

public record Todo(int UserId, int Id, string Title, bool Completed);

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

        const string url = "https://jsonplaceholder.typicode.com/todos/1";
        Console.WriteLine($"Fetching: {url}");

        var result = await httpClient.GetJsonAsync<Todo>(url);

        var output = result.Value switch
        {
            Success<Todo> s  => $"OK => Id: {s.Data.Id}, Title: \"{s.Data.Title}\", Completed: {s.Data.Completed}",
            HttpError h      => $"HTTP error {h.StatusCode}: {h.Message}",
            TransportError t => $"Transport error: {t.Exception.Message}",
            _                => "Unknown result"
        };

        Console.WriteLine(output);
    }
}
