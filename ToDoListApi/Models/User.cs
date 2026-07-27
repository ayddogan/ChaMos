namespace ToDoListApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public string Role { get; private set; } = "User";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User() { }

        public User(string email, string passwordHash, string role = "User")
        {
            SetEmail(email);
            PasswordHash = passwordHash;
            Role = role;
        }

        public void SetPasswordHash(string newHash)
        {
            PasswordHash = newHash;
        }
        public void PromoteToAdmin()
        {
         Role = "Admin";
        }   

        private void SetEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("E-posta boş olamaz.");
            }

            Email = email.Trim().ToLowerInvariant();
        }
    }
}