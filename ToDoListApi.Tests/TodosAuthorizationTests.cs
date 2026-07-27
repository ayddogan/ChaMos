using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ToDoListApi.Data;
using Xunit;

namespace ToDoListApi.Tests
{
    public class TodosAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public TodosAuthorizationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        // Yardımcı metod: kullanıcı kaydet, giriş yap, token döndür.
        // İstenirse veritabanına doğrudan erişip Admin yapar (DBeaver'da elle yaptığımızın kod karşılığı).
        private async Task<string> RegisterAndLoginAsync(string email, string password, bool makeAdmin = false)
        {
            await _client.PostAsJsonAsync("/api/auth/register", new { email, password });

            if (makeAdmin)
            {
                using var scope = _factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = await db.Users.FirstAsync(u => u.Email == email.ToLowerInvariant());
                user.PromoteToAdmin();
                await db.SaveChangesAsync();
            }

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);

            return loginResult!.Token;
        }

        [Fact]
        public async Task GetTodos_TokenYok_UnauthorizedDondurmeli()
        {
            // Arrange: hiçbir Authorization header'ı yok (varsayılan durum)

            // Act
            var response = await _client.GetAsync("/api/todos");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTodo_AdminRolu_BasariylaSilmeli()
        {
            // Arrange
            var adminToken = await RegisterAndLoginAsync("admin1@test.com", "Sifre123!", makeAdmin: true);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var createResponse = await _client.PostAsJsonAsync("/api/todos", new { title = "Silinecek görev" });
            var created = await createResponse.Content.ReadFromJsonAsync<TodoResponseDto>(JsonOptions);

            // Act
            var deleteResponse = await _client.DeleteAsync($"/api/todos/{created!.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteTodo_UserRolu_ForbiddenDondurmeli()
        {
            // Arrange: bir Admin görevi oluştursun
            var adminToken = await RegisterAndLoginAsync("admin2@test.com", "Sifre123!", makeAdmin: true);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var createResponse = await _client.PostAsJsonAsync("/api/todos", new { title = "Bir görev" });
            var created = await createResponse.Content.ReadFromJsonAsync<TodoResponseDto>(JsonOptions);

            // Sıradan bir User bu görevi silmeyi denesin
            var userToken = await RegisterAndLoginAsync("user1@test.com", "Sifre123!");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            // Act
            var deleteResponse = await _client.DeleteAsync($"/api/todos/{created!.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
        }

        [Fact]
        public async Task UpdateTodo_BaskasininGorevi_ForbiddenDondurmeli()
        {
            // Arrange: owner kendi görevini oluştursun
            var ownerToken = await RegisterAndLoginAsync("owner1@test.com", "Sifre123!");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
            var createResponse = await _client.PostAsJsonAsync("/api/todos", new { title = "Sahipli görev" });
            var created = await createResponse.Content.ReadFromJsonAsync<TodoResponseDto>(JsonOptions);

            // Başka bir User bu görevi güncellemeyi denesin
            var otherToken = await RegisterAndLoginAsync("other1@test.com", "Sifre123!");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

            // Act
            var updateResponse = await _client.PutAsJsonAsync(
                $"/api/todos/{created!.Id}", new { isCompleted = true });

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
        }

        private class LoginResponseDto
        {
            public string Token { get; set; } = string.Empty;
        }

        private class TodoResponseDto
        {
            public int Id { get; set; }
        }
    }
}