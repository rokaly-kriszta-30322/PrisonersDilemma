public class TwoTitsForTatStrat : IBotStrategy, IResettableBotStrategy
{
    private readonly Dictionary<int, bool> _opponentDeflectedOnce = new();

    public PlayerChoice GetNextChoice(BotContext context)
    {
        var opponentId = context.OpponentId;
        var lastChoice = context.LastOpponentChoice;

        if (lastChoice == PlayerChoice.Deflect)
        {
            if (!_opponentDeflectedOnce.TryGetValue(opponentId, out var deflectedBefore) || !deflectedBefore)
            {
                _opponentDeflectedOnce[opponentId] = true;
                return PlayerChoice.Coop;
            }
            else
            {
                return PlayerChoice.Deflect;
            }
        }

        _opponentDeflectedOnce[opponentId] = false;
        return PlayerChoice.Coop;
    }

    public void ResetAllOpponentMemory()
    {
        Console.WriteLine("clearing in sample");
        _opponentDeflectedOnce.Clear();
    }
}
