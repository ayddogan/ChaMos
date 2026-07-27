using Microsoft.EntityFrameworkCore;
using ToDoListApi.Data;
using ToDoListApi.Models;

namespace ToDoListApi.Repositories
{
    public class EfTodoRepository : ITodoRepository
    {
        private readonly AppDbContext _context;

        public EfTodoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(List<TodoItem> Items, int TotalCount)> GetAllAsync(int page, int pageSize, int? ownerId, bool isAdmin)
        {
            var query = _context.TodoItems.AsQueryable();

            if (!isAdmin && ownerId.HasValue)
            {
                query = query.Where(t => t.OwnerId == ownerId.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
            .OrderByDescending(t => t.IsUrgent)
            .ThenBy(t => t.DueDate == null)
            .ThenBy(t => t.DueDate)
            .ThenBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
            return (items, totalCount);
        }

       public async Task<List<TodoItem>> GetPendingRemindersAsync(int ownerId)
        {
            var now = DateTime.UtcNow;

            var candidates = await _context.TodoItems
                .Where(t =>
                    t.OwnerId == ownerId &&
                    !t.IsCompleted &&
                    t.ReminderMinutesBefore.HasValue &&
                    (t.StartDate.HasValue || t.DueDate.HasValue))
                .ToListAsync();

            return candidates
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
        }

        public async Task<TodoItem?> GetByIdAsync(int id)
        {
            return await _context.TodoItems.FindAsync(id);
        }

        public async Task AddAsync(TodoItem todo)
        {
            await _context.TodoItems.AddAsync(todo);
        }

        public void Remove(TodoItem todo)
        {
            _context.TodoItems.Remove(todo);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}