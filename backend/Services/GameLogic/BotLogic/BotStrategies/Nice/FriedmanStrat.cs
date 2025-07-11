public class FriedmanStrat : IBotStrategy, IResettableBotStrategy
{
    private readonly HashSet<int> _defectedOpponents = new();

    public PlayerChoice GetNextChoice(BotContext context)
    {
        if (context.LastOpponentChoice == PlayerChoice.Deflect)
        {
            _defectedOpponents.Add(context.OpponentId);
        }

        if (_defectedOpponents.Contains(context.OpponentId))
        {
            return PlayerChoice.Deflect;
        }

        return PlayerChoice.Coop;
    }

    public void ResetAllOpponentMemory()
    {
        _defectedOpponents.Clear();
    }
}