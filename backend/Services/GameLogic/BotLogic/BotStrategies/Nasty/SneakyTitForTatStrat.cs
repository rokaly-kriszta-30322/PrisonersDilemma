public class SneakyTitForTatStrat : IBotStrategy, IResettableBotStrategy
{
    private readonly Dictionary<int, OpponentState> _opponentMemory = new();
    public PlayerChoice GetNextChoice(BotContext context)
    {
        if (!_opponentMemory.ContainsKey(context.OpponentId))
        {
            _opponentMemory[context.OpponentId] = new OpponentState();
        }
        var state = _opponentMemory[context.OpponentId];

        state.InteractionCount++;

        if (state.InteractionCount % 5 == 0)
        {
            return PlayerChoice.Deflect;
        }

        return context.LastOpponentChoice ?? PlayerChoice.Coop;
    }

    public void ResetAllOpponentMemory()
    {
        _opponentMemory.Clear();
    }

    private class OpponentState
    {
        public int InteractionCount { get; set; } = 1;
    }
}