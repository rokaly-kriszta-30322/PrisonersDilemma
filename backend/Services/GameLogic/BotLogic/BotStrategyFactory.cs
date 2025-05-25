public interface IBotStrategy
{
    PlayerChoice GetNextChoice(BotContext context);
}

public static class BotStrategyFactory
{
    public static IBotStrategy GetStrategy(string strategyName)
    {
        return strategyName.ToLower() switch
        {
            "random" => new RandomStrat(),
            "coop" => new AlwaysCoopStrat(),
            "deflect" => new AlwaysDeflectStrat(),
            "titfortat" => new TitForTatStrat(),
            "sneaky" => new SneakyTitForTatStrat(),
            _ => throw new ArgumentException($"Unknown strategy: {strategyName}")
        };
    }
}