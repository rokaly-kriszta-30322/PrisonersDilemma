using System.ComponentModel.DataAnnotations;

public class UserDataDto
{
    [Key]
    public int UserId { get; set; }

    public string? UserName { get; set; }
    public string? Password { get; set; }

    public string? Role { get; set; }

    public int MaxTurns { get; set; }
    public int GameNr { get; set; }
}