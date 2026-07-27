using ToDoListApi.Models;

namespace ToDoListApi.Repositories
{
    public interface ITodoRepository
    {
        Task<(List<TodoItem> Items, int TotalCount)> GetAllAsync(int page, int pageSize, int? ownerId, bool isAdmin);
        Task<TodoItem?> GetByIdAsync(int id);
        Task AddAsync(TodoItem todo);
        void Remove(TodoItem todo);
        Task SaveChangesAsync();
        Task<List<TodoItem>> GetPendingRemindersAsync(int ownerId);
    }
}