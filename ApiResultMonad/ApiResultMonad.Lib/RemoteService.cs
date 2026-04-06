using System.Net;
using System.Text.Json;
using ApiResultMonad.Lib;

public static class RemoteServiceExtensions
{

    /// <summary>
    /// Extension method - Retrieves json asynchronously from a target <see cref="url"/>
    /// Uses the <see cref="ApiResult"/> monad using C# 15 union types in .NET 11 that supports
    /// functional programming directly
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="httpClient"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    public static async Task<ApiResult<T>> GetJsonAsync<T>(this HttpClient httpClient, string url)
    {
        try
        {
            using var response = await httpClient.GetAsync(url).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new HttpError((int)response.StatusCode, await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }

            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var val = await JsonSerializer.DeserializeAsync<T>(stream, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }).ConfigureAwait(false);
            return val is not null ? new Success<T>(val) :
                ApiResult.HttpFail<T>(HttpStatusCode.UnprocessableEntity, $"No content or wrong content trying to get content of type: {typeof(T).Name}");            
            
        }
        catch (Exception ex)
        {
            return new TransportError(ex);
        }


    }



}

