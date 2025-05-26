public class TesterStrat : IBotStrategy
{
    public PlayerChoice GetNextChoice(BotContext context)
    {
        return PlayerChoice.Coop;
    }
}