using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class GameSession
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; set; }

    public int User1 { get; set; }
    public UserData? User1Nav { get; set; }

    public string? Choice1 { get; set; }
    public int GameNr1 { get; set; }
    public int MoneyPoints1 { get; set; }
    public int CoopCoop1 { get; set; }
    public int CoopDeflect1 { get; set; }
    public int DeflectCoop1 { get; set; }
    public int DeflectDeflect1 { get; set; }

    public int User2 { get; set; }
    public UserData? User2Nav { get; set; }

    public string? Choice2 { get; set; }
    public int GameNr2 { get; set; }
    public int MoneyPoints2 { get; set; }
    public int CoopCoop2 { get; set; }
    public int CoopDeflect2 { get; set; }
    public int DeflectCoop2 { get; set; }
    public int DeflectDeflect2 { get; set; }
}