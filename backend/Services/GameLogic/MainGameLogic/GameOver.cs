using Microsoft.EntityFrameworkCore;

public class GameOver
{
    private readonly MyDbContext _myDbContext;
    private readonly ActiveUsers _activeUsers;
    private readonly IBotStrategyManager _botStrategyManager;

    public GameOver(IBotStrategyManager botStrategyManager, ActiveUsers activeUsers, MyDbContext myDbContext)
    {
        _myDbContext = myDbContext;
        _activeUsers = activeUsers;
        _botStrategyManager = botStrategyManager;
    }

    public async Task ResetToStart(int userId)
    {
        var user = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserId == userId);
        await RemoveInteractionsForUserAsync(userId);

        var currentGameNr = user!.GameNr;
        var turnCount = await _myDbContext.game_session
        .Where(gs =>
            (gs.User1 == userId && gs.GameNr1 == currentGameNr) ||
            (gs.User2 == userId && gs.GameNr2 == currentGameNr))
        .CountAsync();

        if (turnCount > user.MaxTurns)
        {
            user.MaxTurns = turnCount;
        }
        user!.GameNr += 1;

        var userData = await _myDbContext.game_data.FirstOrDefaultAsync(gm => gm.UserId == userId);

        userData!.MoneyPoints = 100;
        userData.CoopCoop = 8;
        userData.CoopDeflect = 20;
        userData.DeflectCoop = -12;
        userData.DeflectDeflect = -5;

        if (user.Role == "bot")
        {
            var strategyRecord = await _myDbContext.bot_strat.FirstOrDefaultAsync(b => b.UserId == userId);
            if (strategyRecord != null)
            {
                _botStrategyManager.ResetBotMemory(userId);
                _botStrategyManager.RemoveBotStrategy(userId);
            }
        }

        await _myDbContext.SaveChangesAsync();
    }

    public async Task NoMoneyAsync(int userId)
    {
        _activeUsers.RemoveUser(userId);

        var user = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return;

        if (user.Role == "bot")
        {
            await ResetToStart(userId);
        }
    }

    public async Task RemoveInteractionsForUserAsync(int userId)
    {
        var interactions = await _myDbContext.pending_interactions
            .Where(p => p.UserId == userId || p.TargetId == userId)
            .ToListAsync();

        if (interactions.Any())
        {
            _myDbContext.pending_interactions.RemoveRange(interactions);
            await _myDbContext.SaveChangesAsync();
        }
    }

}