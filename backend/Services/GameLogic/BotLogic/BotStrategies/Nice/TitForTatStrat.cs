public class TitForTatStrat : IBotStrategy
{
    public PlayerChoice GetNextChoice(BotContext context)
    {
        return context.LastOpponentChoice ?? PlayerChoice.Coop;
    }
}