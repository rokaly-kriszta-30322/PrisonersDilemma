public interface IBotStrategy
{
    PlayerChoice GetNextChoice(BotContext context);
}

public interface IResettableBotStrategy
{
    void ResetAllOpponentMemory();
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
            "tester" => new TesterStrat(),
            "sample" => new TwoTitsForTatStrat(),
            "prober" => new ProberStrat(),
            "grudge" => new FriedmanStrat(),
            _ => throw new ArgumentException($"Unknown strategy: {strategyName}")
        };
    }
}