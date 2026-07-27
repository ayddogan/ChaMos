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
    public class TodosIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public TodosIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        // Yardımcı metod: kullanıcı kaydet, gerekirse Admin yap, giriş yap, token al
        private async Task AuthenticateAsync(string email, string password, bool makeAdmin = false)
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

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", loginResult!.Token);
        }

        [Fact]
        public async Task Update_OlmayanKayit_NotFoundDondurmeli()
        {
            // Arrange
            await AuthenticateAsync("update-test@test.com", "Sifre123!");
            var guncellemeVerisi = new { isCompleted = true };

            // Act
            var response = await _client.PutAsJsonAsync("/api/todos/99999", guncellemeVerisi);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_VarOlanKayit_GercektenSilinmeli()
        {
            // Arrange: Admin olarak giriş yapıyoruz (delete sadece Admin'e açık)
            await AuthenticateAsync("delete-test@test.com", "Sifre123!", makeAdmin: true);

            var yeniGorev = new { title = "Silinecek görev" };
            var createResponse = await _client.PostAsJsonAsync("/api/todos", yeniGorev);
            var olusturulanGorev = await createResponse.Content.ReadFromJsonAsync<TodoItemResponse>(JsonOptions);

            // Act
            var deleteResponse = await _client.DeleteAsync($"/api/todos/{olusturulanGorev!.Id}");

            // Assert 1: silme isteği başarılı mı?
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Assert 2: gerçekten silinmiş mi?
            var getResponse = await _client.GetAsync($"/api/todos/{olusturulanGorev.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        private class LoginResponseDto
        {
            public string Token { get; set; } = string.Empty;
        }

        private class TodoItemResponse
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
        }
    }
}