public class TesterStrat : IBotStrategy, IResettableBotStrategy
{
    private readonly Dictionary<int, OpponentState> _memory = new();

    public PlayerChoice GetNextChoice(BotContext context)
    {
        if (!_memory.ContainsKey(context.OpponentId))
        {
            _memory[context.OpponentId] = new OpponentState();
        }

        var state = _memory[context.OpponentId];

        if (context.Round == 1)
        {
            return PlayerChoice.Coop;
        }

        if (context.Round == 2)
        {
            state.FirstOpponentChoice = context.LastOpponentChoice!.Value;
        }

        if (state.FirstOpponentChoice == PlayerChoice.Deflect)
        {
            if (context.Round == 2) return PlayerChoice.Coop;
            Console.WriteLine("Entered Tit-for-Tat route");
            return context.LastOpponentChoice ?? PlayerChoice.Coop;
        }
        else
        {
            Console.WriteLine("Entered alternating route, round: " + context.Round);
            return context.Round % 2 == 0 ? PlayerChoice.Deflect : PlayerChoice.Coop;
        }
    }

    public void ResetAllOpponentMemory()
    {
        Console.WriteLine("Clearing strategy memory");
        _memory.Clear();
    }

    private class OpponentState
    {
        public PlayerChoice? FirstOpponentChoice { get; set; } = null;
    }
}
