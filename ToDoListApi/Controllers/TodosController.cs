using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToDoListApi.Dtos;
using ToDoListApi.Exceptions;
using ToDoListApi.Models;
using ToDoListApi.Repositories;

namespace ToDoListApi.Controllers
{
    [ApiController]
    [Route("api/todos")]
    [Authorize]
    public class TodosController : ControllerBase
    {
        private readonly ITodoRepository _repository;

        public TodosController(ITodoRepository repository)
        {
            _repository = repository;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Token içinde kullanıcı id'si bulunamadı.");

            return int.Parse(idClaim.Value);
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        [HttpGet]
        public async Task<ActionResult<PagedResultDto<TodoItem>>> GetTodos(int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var currentUserId = GetCurrentUserId();
            var (items, totalCount) = await _repository.GetAllAsync(page, pageSize, currentUserId, IsAdmin());

            var result = new PagedResultDto<TodoItem>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TodoItem>> GetTodo(int id)
        {
            var todo = await _repository.GetByIdAsync(id);

            if (todo == null)
            {
                throw new NotFoundException($"{id} id'li görev bulunamadı.");
            }

            return todo;
        }

        [HttpGet("reminders")]
        public async Task<ActionResult<List<TodoItem>>> GetPendingReminders()
        {
            var currentUserId = GetCurrentUserId();
            var reminders = await _repository.GetPendingRemindersAsync(currentUserId);
            return Ok(reminders);
        }

       [HttpPost]
        public async Task<ActionResult<TodoItem>> CreateTodo(CreateTodoDto dto)
        {
            var currentUserId = GetCurrentUserId();
            var todo = new TodoItem(dto.Title, currentUserId);

            if (dto.StartDate.HasValue || dto.DueDate.HasValue)
            {
                todo.ScheduleDates(dto.StartDate, dto.DueDate);
            }

            if (dto.IsUrgent)
            {
                todo.SetUrgent(true);
            }

            if (dto.Description != null)
            {
                todo.SetDescription(dto.Description);
            }
            if (dto.Location != null)
            {
                todo.SetLocation(dto.Location);
            }
            if (dto.ReminderMinutesBefore.HasValue)
            {
                todo.SetReminderMinutesBefore(dto.ReminderMinutesBefore.Value, dto.ReminderBasedOn);
            }

            await _repository.AddAsync(todo);
            await _repository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTodo), new { id = todo.Id }, todo);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTodo(int id, UpdateTodoDto dto)
        {
            if (dto.Id.HasValue && dto.Id.Value != id)
            {
                throw new ArgumentException("URL'deki id ile gönderilen id uyuşmuyor.");
            }

            var todo = await _repository.GetByIdAsync(id);

            if (todo == null)
            {
                throw new NotFoundException($"{id} id'li görev bulunamadı.");
            }

            var currentUserId = GetCurrentUserId();
            if (!IsAdmin() && todo.OwnerId != currentUserId)
            {
                throw new ForbiddenException("Bu görevi güncelleme yetkiniz yok.");
            }

            if (dto.Title != null)
            {
                todo.Rename(dto.Title, dto.UpdatedBy);
            }

            if (dto.IsCompleted.HasValue)
            {
                todo.MarkCompleted(dto.IsCompleted.Value, dto.UpdatedBy);
            }

            if (dto.DatesProvided)
            {
                todo.ScheduleDates(dto.StartDate, dto.DueDate, dto.UpdatedBy);
            }
            if (dto.IsUrgent.HasValue)
            {
                todo.SetUrgent(dto.IsUrgent.Value);
            }
            if (dto.DescriptionProvided)
            {
                todo.SetDescription(dto.Description, dto.UpdatedBy);
            }
            if (dto.LocationProvided)
            {
                todo.SetLocation(dto.Location, dto.UpdatedBy);
            }
            if (dto.ReminderMinutesProvided)
            {
                todo.SetReminderMinutesBefore(dto.ReminderMinutesBefore, dto.ReminderBasedOn);
            }
            await _repository.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodo(int id)
        {
            var todo = await _repository.GetByIdAsync(id);

            if (todo == null)
            {
                throw new NotFoundException($"{id} id'li görev bulunamadı.");
            }

            var currentUserId = GetCurrentUserId();
            if (!IsAdmin() && todo.OwnerId != currentUserId)
            {
                throw new ForbiddenException("Bu görevi silme yetkiniz yok.");
            }

            _repository.Remove(todo);
            await _repository.SaveChangesAsync();

            return NoContent();
        }
    }
}