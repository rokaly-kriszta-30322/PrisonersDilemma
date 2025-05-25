public class AlwaysCoopStrat : IBotStrategy
{
    public PlayerChoice GetNextChoice(BotContext context)
    {
        return PlayerChoice.Coop;
    }
}