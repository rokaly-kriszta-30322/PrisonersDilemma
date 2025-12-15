using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class UserData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserId { get; set; }

    public string? UserName { get; set; }
    public string? Password { get; set; }

    public string Role { get; set; } = "client";

    public int? MaxTurns { get; set; }
    public int? GameNr { get; set; }

    [ForeignKey("UserId")]
    public GameData? GameData { get; set; }
    
    [ForeignKey("UserId")]
    public BotStrategy? BotStrategy { get; set; }
}