using Microsoft.EntityFrameworkCore;
using ToDoListApi.Data;

namespace ToDoListApi.BackgroundServices
{
    public class ReminderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReminderBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);
        private readonly TimeSpan _reminderWindow = TimeSpan.FromHours(1);

        public ReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Hatırlatma servisi başlatıldı. Kontrol aralığı: {Interval}", _checkInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hatırlatma kontrolü sırasında beklenmedik bir hata oluştu.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CheckAndSendRemindersAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;
            
           var candidates = await db.TodoItems
                .Include(t => t.Owner)
                .Where(t =>
                    !t.IsCompleted &&
                    !t.ReminderSent &&
                    t.ReminderMinutesBefore.HasValue &&
                    (t.StartDate.HasValue || t.DueDate.HasValue))
                .ToListAsync();

            var todosNeedingReminder = candidates
                .Where(t =>
                {
                    var baseDate = t.ReminderBasedOn == "Start" ? t.StartDate : t.DueDate;

                    if (!baseDate.HasValue || baseDate.Value <= now)
                    {
                        return false;
                    }

                    var reminderTime = baseDate.Value.AddMinutes(-t.ReminderMinutesBefore!.Value);
                    return reminderTime <= now;
                })
                .ToList();

            if (todosNeedingReminder.Count == 0)
            {
                return;
            }

            foreach (var todo in todosNeedingReminder)
            {
                _logger.LogWarning(
                    "HATIRLATMA: Görev '{Title}' (Id: {TodoId}) için son tarih yaklaşıyor. " +
                    "Kullanıcı: {UserEmail} | Son tarih: {DueDate} | {Minutes} dakika önce hatırlatma istenmişti.",
                    todo.Title,
                    todo.Id,
                    todo.Owner?.Email ?? "bilinmiyor",
                    todo.DueDate,
                    todo.ReminderMinutesBefore
                );

                todo.MarkReminderSent();
            }

            await db.SaveChangesAsync();

            _logger.LogInformation("{Count} görev için hatırlatma gönderildi.", todosNeedingReminder.Count);
        }
    }
}