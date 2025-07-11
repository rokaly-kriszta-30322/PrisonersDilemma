public class ProberStrat : IBotStrategy, IResettableBotStrategy
{
    private readonly Dictionary<int, OpponentState> _opponentMemory = new();

    public PlayerChoice GetNextChoice(BotContext context)
    {
        if (!_opponentMemory.ContainsKey(context.OpponentId))
            _opponentMemory[context.OpponentId] = new OpponentState();

        var state = _opponentMemory[context.OpponentId];

        if (context.Round == 1)
        {
            return PlayerChoice.Coop;
        }

        if (context.Round == 2)
        {
            if (context.LastOpponentChoice == PlayerChoice.Deflect)
                state.PlayTitForTat = true;
            else
                state.PlayTitForTat = false;

            return PlayerChoice.Coop;
        }

        if (state.PlayTitForTat == true)
        {
            var choice = context.LastOpponentChoice ?? PlayerChoice.Coop;
            return choice;
        }
        else
        {
            return PlayerChoice.Deflect;
        }
    }

    private class OpponentState
    {
        public bool? PlayTitForTat { get; set; } = null;
    }

    public void ResetAllOpponentMemory()
    {
        _opponentMemory.Clear();
    }

}
