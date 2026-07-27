using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ToDoListApi.Data;

namespace ToDoListApi.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        // Bu isim SADECE BİR KERE üretiliyor (factory oluşturulduğunda),
        // tüm istekler bu SABİT ismi kullanacak.
        private readonly string _dbName = "TestDb_" + Guid.NewGuid();

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptorsToRemove = services
                    .Where(d =>
                        d.ServiceType.FullName != null &&
                        d.ServiceType.FullName.Contains("AppDbContext"))
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });
            });
        }
    }
}