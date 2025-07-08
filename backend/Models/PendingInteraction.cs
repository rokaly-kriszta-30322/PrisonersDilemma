using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class PendingInteraction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PendingId { get; set; }

    public int UserId { get; set; }
    public int TargetId { get; set; }

    public PlayerChoice UserChoice { get; set; }
    public PlayerChoice? TargetChoice { get; set; }

    [ForeignKey("UserId")]
    public UserData? User { get; set; }

    [ForeignKey("TargetId")]
    public UserData? Target { get; set; }
}