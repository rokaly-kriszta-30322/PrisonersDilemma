using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class BotStrategy
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int BotId { get; set; }
    public bool Start { get; set; }
    public string? Strategy { get; set; }
    public int MoneyLimit { get; set; }
    
    [ForeignKey("UserId")]
    public int UserId { get; set; }
    public UserData? UserData { get; set; }
}