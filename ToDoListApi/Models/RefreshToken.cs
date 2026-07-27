namespace ToDoListApi.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; private set; } = string.Empty;
        public int UserId { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRevoked { get; private set; } = false;

        public RefreshToken() { }

        public RefreshToken(string token, int userId, DateTime expiresAt)
        {
            Token = token;
            UserId = userId;
            ExpiresAt = expiresAt;
        }

        public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;

        public void Revoke()
        {
            IsRevoked = true;
        }
    }
}