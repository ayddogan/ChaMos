namespace ToDoListApi.Models
{
    public class TodoItem
    {
        public int Id { get; set; }
        public string Title { get; private set; } = string.Empty;
        public bool IsCompleted { get; private set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; private set; }
        public int OwnerId { get; private set; }
        public User? Owner { get; private set;}
        public DateTime? StartDate {get; private set; }
        public DateTime? DueDate {get; private set; }
        public bool ReminderSent {get; private set;} =false;
        public bool IsUrgent { get; private set; } = false;
        public string? Description { get; private set; }
        public int? ReminderMinutesBefore { get; private set; }
        public string ReminderBasedOn { get; private set; } = "Due";
        public string? Location { get; private set; }

        public TodoItem() { }

        public TodoItem(string title, int ownerId)
        {
            SetTitle(title);
            OwnerId = ownerId;
        }

        public void Rename(string newTitle, string? updatedBy = null)
        {
            SetTitle(newTitle);
            Touch(updatedBy);
        }

        public void MarkCompleted(bool completed, string? updatedBy = null)
        {
            IsCompleted = completed;
            Touch(updatedBy);
        }
        public void SetUrgent(bool isUrgent)
        {
            IsUrgent = isUrgent;
        }

        public void SetDescription(string? description, string? updatedBy = null)
        {
            Description = description;
            Touch(updatedBy);
        }

        public void SetReminderMinutesBefore(int? minutes, string? basedOn = null)
        {
            ReminderMinutesBefore = minutes;

            if (basedOn == "Start" || basedOn == "Due")
            {
                ReminderBasedOn = basedOn;
            }
        }

        public void SetLocation(string? location, string? updatedBy = null)
        {
            Location = location;
            Touch(updatedBy);
        }

       public void ScheduleDates(DateTime? startDate, DateTime? dueDate, string? updatedBy = null)
        {
            var normalizedStartDate = NormalizeToUtc(startDate);
            var normalizedDueDate = NormalizeToUtc(dueDate);

            if (normalizedStartDate.HasValue && normalizedDueDate.HasValue && normalizedStartDate.Value > normalizedDueDate.Value)
            {
                throw new ArgumentException("Başlangıç tarihi, bitiş tarihinden sonra olamaz.");
            }

            StartDate = normalizedStartDate;
            DueDate = normalizedDueDate;
            ReminderSent = false;
            Touch(updatedBy);
        }

        private static DateTime? NormalizeToUtc(DateTime? date)
        {
            if (!date.HasValue)
            {
                return null;
            }

            return date.Value.Kind switch
            {
                DateTimeKind.Utc => date.Value,
                DateTimeKind.Local => date.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(date.Value, DateTimeKind.Utc)
            };
        }

        public void MarkReminderSent()
        {
            ReminderSent = true;
        }
        private void Touch(string? updatedBy)
        {
            UpdatedAt = DateTime.UtcNow;
            if (updatedBy != null)
            {
                UpdatedBy = updatedBy;
            }
        }

        private void SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Başlık boş olamaz.");
            }

            Title = title.Trim();
        }
    }
}