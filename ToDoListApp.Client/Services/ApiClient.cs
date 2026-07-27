using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace ToDoListApp.Client.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;

        public ApiClient(HttpClient http, IJSRuntime js)
        {
            _http = http;
            _js = js;
        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await SendWithRefreshAsync(() => _http.GetAsync(url));
        }

        public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T data)
        {
            return await SendWithRefreshAsync(() => _http.PostAsJsonAsync(url, data));
        }

        public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string url, T data)
        {
            return await SendWithRefreshAsync(() => _http.PutAsJsonAsync(url, data));
        }

        public async Task<HttpResponseMessage> DeleteAsync(string url)
        {
            return await SendWithRefreshAsync(() => _http.DeleteAsync(url));
        }

        private async Task<HttpResponseMessage> SendWithRefreshAsync(Func<Task<HttpResponseMessage>> sendRequest)
        {
            var response = await sendRequest();

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }

            var refreshed = await TryRefreshTokenAsync();

            if (!refreshed)
            {
                return response;
            }

            // Token yenilendi, isteği bir kere daha deniyoruz
            return await sendRequest();
        }

        private async Task<bool> TryRefreshTokenAsync()
        {
            var refreshToken = await _js.InvokeAsync<string?>("localStorageHelper.getItem", "refreshToken");

            if (string.IsNullOrEmpty(refreshToken))
            {
                return false;
            }

            var refreshResponse = await _http.PostAsJsonAsync("api/auth/refresh", new { refreshToken });

            if (!refreshResponse.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await refreshResponse.Content.ReadFromJsonAsync<RefreshResult>();

            if (result == null)
            {
                return false;
            }

            await _js.InvokeVoidAsync("localStorageHelper.setItem", "accessToken", result.AccessToken);
            await _js.InvokeVoidAsync("localStorageHelper.setItem", "refreshToken", result.RefreshToken);

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

            return true;
        }

        private class RefreshResult
        {
            public string AccessToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
        }
    }
}