namespace LoanManagement.Entities.Models;

// Sisteme giriş yapan kullanıcıları temsil eder (JWT Authentication için).
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = "User"; // "Admin" veya "User"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
