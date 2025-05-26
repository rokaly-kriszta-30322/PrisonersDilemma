public interface IBotStrategyManager
{
    IBotStrategy GetOrCreateStrategy(int botUserId, string strategyName);
    void ResetBotMemory(int botUserId);
}

public class BotStrategyManager : IBotStrategyManager
{
    private readonly Dictionary<int, IBotStrategy> _strategies = new();
    private readonly object _lock = new();

    public IBotStrategy GetOrCreateStrategy(int botUserId, string strategyName)
    {
        lock (_lock)
        {
            if (!_strategies.TryGetValue(botUserId, out var strategy))
            {
                strategy = BotStrategyFactory.GetStrategy(strategyName);
                _strategies[botUserId] = strategy;
            }
            return strategy;
        }
    }

    public void ResetBotMemory(int botUserId)
    {
        lock (_lock)
        {
            if (_strategies.TryGetValue(botUserId, out var strategy) && strategy is IResettableBotStrategy resettable)
            {
                resettable.ResetAllOpponentMemory();
            }
        }
    }

    public void RemoveBotStrategy(int botUserId)
    {
        lock (_lock)
        {
            _strategies.Remove(botUserId);
        }
    }
}
