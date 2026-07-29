using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ToDoListApi.Services
{
    public class EmailService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<EmailService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = configuration["Resend:ApiKey"]
                ?? throw new InvalidOperationException("Resend:ApiKey ayarı bulunamadı.");
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var payload = new
            {
                from = "ChaMos <noreply@chamos.site>",
                to = new[] { toEmail },
                subject,
                html = htmlBody
            };

            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Email gönderilemedi. Alıcı: {ToEmail}, Hata: {Error}", toEmail, errorContent);
            }
            else
            {
                _logger.LogInformation("Email başarıyla gönderildi. Alıcı: {ToEmail}", toEmail);
            }
        }
    }
}