using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Services;

public class RaceDayApiClient(HttpClient httpClient, IHttpContextAccessor contextAccessor) : IRaceDayApiClient
{
    private const string ApiCookieKey = "ApiSessionCookie";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<T>> GetAsync<T>(string path)
    {
        return SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, path));
    }

    public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, bool captureSession = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return SendAsync<T>(request, captureSession);
    }

    public Task<ApiResult<T>> PutAsync<T>(string path, object body) =>
        SendAsync<T>(new HttpRequestMessage(HttpMethod.Put, path) { Content = JsonContent.Create(body) });

    public async Task<ApiResult<bool>> DeleteAsync(string path)
    {
        var result = await SendAsync<object>(new HttpRequestMessage(HttpMethod.Delete, path));
        return new ApiResult<bool>
        {
            IsSuccess = result.IsSuccess,
            StatusCode = result.StatusCode,
            Error = result.Error,
            Data = result.IsSuccess
        };
    }

    public async Task<ApiResult<T>> UploadAsync<T>(string path, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        using var form = new MultipartFormDataContent();
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new(file.ContentType);
        form.Add(content, "image", file.FileName);
        return await SendAsync<T>(new HttpRequestMessage(HttpMethod.Post, path) { Content = form });
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request, bool captureSession = false)
    {
        var session = contextAccessor.HttpContext?.Session;
        var cookie = session?.GetString(ApiCookieKey);
        if (!string.IsNullOrWhiteSpace(cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        try
        {
            using var response = await httpClient.SendAsync(request);
            if (captureSession && response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                // Keep the API session cookie so later requests remain authenticated.
                var apiCookie = cookies
                    .Select(x => x.Split(';', 2)[0])
                    .FirstOrDefault(x => x.StartsWith(
                        ".RaceDay.Session=",
                        StringComparison.Ordinal));
                if (apiCookie is not null)
                {
                    session?.SetString(ApiCookieKey, apiCookie);
                }
            }

            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent ||
                    response.Content.Headers.ContentLength == 0)
                {
                    return new ApiResult<T>
                    {
                        IsSuccess = true,
                        StatusCode = (int)response.StatusCode
                    };
                }

                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                return new ApiResult<T>
                {
                    IsSuccess = true,
                    StatusCode = (int)response.StatusCode,
                    Data = data
                };
            }

            var error = await ReadErrorAsync(response);
            if ((int)response.StatusCode == 401)
            {
                session?.Clear();
            }

            return new ApiResult<T>
            {
                StatusCode = (int)response.StatusCode,
                Error = error
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>
            {
                StatusCode = 503,
                Error = "The RaceDay API is unavailable. Please start the API and try again."
            };
        }
        catch (TaskCanceledException)
        {
            return new ApiResult<T>
            {
                StatusCode = 504,
                Error = "The RaceDay API request timed out. Please try again."
            };
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text))
        {
            return $"The request failed with status {(int)response.StatusCode}.";
        }

        try
        {
            using var document = JsonDocument.Parse(text);
            if (document.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? text;
            }

            if (document.RootElement.TryGetProperty("title", out var title))
            {
                return title.GetString() ?? text;
            }
        }
        catch (JsonException)
        {
            // Return the original API response when it is not valid JSON.
        }

        return text;
    }
}
