namespace ToDoListApi.Dtos
{
    public class UpdateTodoDto
    {
        public int? Id { get; set; }
        public string? Title { get; set; }
        public bool? IsCompleted { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public bool? IsUrgent { get; set; }
        public string? Description { get; set; }
        public bool DescriptionProvided { get; set; } = false;
        public bool DatesProvided { get; set; } = false;
        public int? ReminderMinutesBefore { get; set; }
        public bool ReminderMinutesProvided { get; set; } = false;
        public string? ReminderBasedOn { get; set; }
        public string? Location { get; set; }
        public bool LocationProvided { get; set; } = false;
    }
}