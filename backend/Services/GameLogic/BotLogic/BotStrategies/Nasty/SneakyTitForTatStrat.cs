public class SneakyTitForTatStrat : IBotStrategy
{
    public PlayerChoice GetNextChoice(BotContext context)
    {
        if (context.Round > 0 && context.Round % 5 == 0)
        {
            return PlayerChoice.Deflect;
        }

        return context.LastOpponentChoice ?? PlayerChoice.Coop;
    }
}