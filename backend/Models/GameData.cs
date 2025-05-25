using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class GameData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int GDataId { get; set; }

    public int MoneyPoints { get; set; }

    public int CoopCoop { get; set; }
    public int CoopDeflect { get; set; }
    public int DeflectCoop { get; set; }
    public int DeflectDeflect { get; set; }

    [ForeignKey("UserId")]
    public int UserId { get; set; }
    public UserData? UserData { get; set; }

    public GameData(int userId)
    {
        UserId = userId;
        MoneyPoints = 100;
        CoopCoop = 8;
        CoopDeflect = 20;
        DeflectCoop = -12;
        DeflectDeflect = -5;
    }
}