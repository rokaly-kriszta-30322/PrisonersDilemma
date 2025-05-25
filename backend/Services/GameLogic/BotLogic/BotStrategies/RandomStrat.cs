public class RandomStrat : IBotStrategy
{
    private static readonly Random _rand = new();

    public PlayerChoice GetNextChoice(BotContext context)
    {
        return _rand.Next(2) == 0 ? PlayerChoice.Coop : PlayerChoice.Deflect;
    }
}