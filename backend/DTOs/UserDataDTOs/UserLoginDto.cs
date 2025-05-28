public class UserLoginDto
{
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string? Role { get; set; }
    public GameDataLoginDto? GameData { get; set; }
}