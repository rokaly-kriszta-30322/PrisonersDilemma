public class AlwaysDeflectStrat : IBotStrategy
{
    public PlayerChoice GetNextChoice(BotContext context)
    {
        return PlayerChoice.Deflect;
    }
}