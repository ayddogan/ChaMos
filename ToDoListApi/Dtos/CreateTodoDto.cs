namespace ToDoListApi.Dtos
{
    public class CreateTodoDto
    {
        public string Title { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsUrgent { get; set; } = false;
        public string? Description { get; set; }
        public int? ReminderMinutesBefore { get; set; }
        public string? ReminderBasedOn { get; set; }
        public string? Location { get; set; }
    }
}