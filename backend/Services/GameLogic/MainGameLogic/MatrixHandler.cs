public enum PlayerChoice
{
    Coop,
    Deflect
}

public class MatrixHandler
{

    public int Outcome(HandlerRequest interaction)
    {
        return (interaction.targetChoice, interaction.playerChoice) switch
        {
            (PlayerChoice.Coop, PlayerChoice.Coop) => interaction.playerData.CoopCoop,
            (PlayerChoice.Coop, PlayerChoice.Deflect) => interaction.playerData.CoopDeflect,
            (PlayerChoice.Deflect, PlayerChoice.Coop) => interaction.playerData.DeflectCoop,
            (PlayerChoice.Deflect, PlayerChoice.Deflect) => interaction.playerData.DeflectDeflect,
            _ => throw new ArgumentException("Invalid combination")
        };
    }

}
