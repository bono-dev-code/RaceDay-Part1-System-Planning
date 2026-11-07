using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Services;

public interface IRaceDayApiClient
{
    Task<ApiResult<T>> GetAsync<T>(string path);
    Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, bool captureSession = false);
    Task<ApiResult<T>> PutAsync<T>(string path, object body);
    Task<ApiResult<bool>> DeleteAsync(string path);
    Task<ApiResult<T>> UploadAsync<T>(string path, IFormFile file);
}
