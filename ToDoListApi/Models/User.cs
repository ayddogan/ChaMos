namespace ToDoListApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public string Role { get; private set; } = "User";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsEmailVerified { get; private set; } = false;
        public string? EmailVerificationToken { get; private set; }
        public DateTime? EmailVerificationTokenExpiresAt { get; private set; }

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
        public void SetEmailVerificationToken(string token, DateTime expiresAt)
        {
            EmailVerificationToken = token;
            EmailVerificationTokenExpiresAt = expiresAt;
        }

        public void MarkEmailAsVerified()
        {
            IsEmailVerified = true;
            EmailVerificationToken = null;
            EmailVerificationTokenExpiresAt = null;
        }
    }
}