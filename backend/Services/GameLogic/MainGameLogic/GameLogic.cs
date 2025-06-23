using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class GameLogic
{
    private readonly MyDbContext _myDbContext;
    private readonly MatrixHandler _matrixHandler;
    private readonly IBotStrategyManager _botStrategyManager;
    private readonly GameOver _gameOver;
    private readonly IHubContext<GameHub> _hub;

    public GameLogic(IBotStrategyManager botStrategyManager, GameOver gameOver, MatrixHandler matrixHandler, MyDbContext myDbContext, IHubContext<GameHub> hubContext)
    {
        _myDbContext = myDbContext;
        _matrixHandler = matrixHandler;
        _botStrategyManager = botStrategyManager;
        _gameOver = gameOver;
        _hub = hubContext;
    }

    public async Task GetUserIdAsync(GameSessionRequest request)
    {
        var user = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserName == request.UserName1);
        var target = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserName == request.UserName2);

        if (user == null || target == null)
        {
            throw new Exception("Could not find user or target ID.");
        }

        if (request.Choice1 == "Coop" || request.Choice1 == "Deflect")
        {
            var initiatorChoice = Enum.Parse<PlayerChoice>(request.Choice1);
            await HandleTradeInitiationAsync(user.UserId, target.UserId, initiatorChoice);
        }
        else if (request.Choice1 == "Buy")
        {
            await HandleBuyAsync(user.UserId);
        }
    }

    public async Task HandleBuyAsync(int? playerId)
    {
        var playerData = await _myDbContext.game_data.FirstOrDefaultAsync(gm => gm.UserId == playerId);
        var userData = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserId == playerId);

        if (playerData == null)
            throw new Exception("GameData not found for player.");

        playerData.MoneyPoints -= 500;
        playerData.CoopCoop += 1;
        playerData.CoopDeflect += 1;
        playerData.DeflectCoop += 1;
        playerData.DeflectDeflect += 1;

        var gameSession = new GameSession
        {
            User1 = playerId!.Value,
            Choice1 = "Buy",
            GameNr1 = userData!.GameNr,
            MoneyPoints1 = playerData.MoneyPoints + 500,
            CoopCoop1 = playerData.CoopCoop - 1,
            CoopDeflect1 = playerData.CoopDeflect - 1,
            DeflectCoop1 = playerData.DeflectCoop - 1,
            DeflectDeflect1 = playerData.DeflectDeflect - 1,

            User2 = playerId.Value,
            Choice2 = "Buy",
            GameNr2 = userData.GameNr,
            MoneyPoints2 = playerData.MoneyPoints,
            CoopCoop2 = playerData.CoopCoop,
            CoopDeflect2 = playerData.CoopDeflect,
            DeflectCoop2 = playerData.DeflectCoop,
            DeflectDeflect2 = playerData.DeflectDeflect,
        };

        _myDbContext.game_session.Add(gameSession);
        await _myDbContext.SaveChangesAsync();
    }

    public async Task HandleTradeInitiationAsync(int playerId, int targetId, PlayerChoice playerChoice)
    {
        var pending = new PendingInteraction
        {
            UserId = playerId,
            TargetId = targetId,
            UserChoice = playerChoice,
            TargetChoice = null
        };

        Console.WriteLine("HandleTradeInitiationAsync called with IDs: " + playerId + targetId);

        _myDbContext.pending_interactions.Add(pending);
        await _myDbContext.SaveChangesAsync();

        var targetUser = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserId == targetId);
        var playerUser = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserId == playerId);

        if (targetUser?.Role == "bot")
        {
            Console.WriteLine("will enter bot response as targetId is bot");
            await RespondAsBotAsync(pending, playerId, targetId);
        }
    }

    public async Task HandleTradeResponseAsync(int pendingId, PlayerChoice targetChoice)
    {
        Console.WriteLine("should enter HandleTradeResponseAsync it" + pendingId);
        var pending = await _myDbContext.pending_interactions.FirstOrDefaultAsync(p => p.PendingId == pendingId);
        if (pending == null || pending.TargetChoice != null)
            throw new Exception("Invalid pending interaction.");

        pending.TargetChoice = targetChoice;
        await _myDbContext.SaveChangesAsync();

        await HandleTradeAsync(pending);
    }

    public async Task HandleTradeAsync(PendingInteraction pending)
    {
        Console.WriteLine("should enter HandleTradeAsync it" + pending.PendingId);
        var playerData = await _myDbContext.game_data.FirstOrDefaultAsync(gm => gm.UserId == pending.UserId);
        var targetData = await _myDbContext.game_data.FirstOrDefaultAsync(gm => gm.UserId == pending.TargetId);

        var targetUser = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserId == pending.TargetId);
        var playerUser = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserId == pending.UserId);

        if (playerData == null || targetData == null)
            throw new Exception("GameData not found for one or both players.");

        Console.WriteLine("should start saving soon" + pending.PendingId + " " + pending.UserId + " " + pending.TargetId);
        Console.WriteLine("gamedatas" + playerData.UserId + " " + targetData.UserId);

        var interaction1 = new HandlerRequest
        (
            playerData,
            pending.UserChoice,
            pending.TargetChoice!.Value
        );

        var interaction2 = new HandlerRequest
        (
            targetData,
            pending.TargetChoice.Value,
            pending.UserChoice
        );

        Console.WriteLine("interactions" + playerData + " " +
            pending.UserChoice + " " +
            pending.TargetChoice!.Value + " " + targetData + " " +
            pending.TargetChoice.Value + " " + pending.UserChoice);

        int playerMoney = _matrixHandler.Outcome(interaction1);
        int targetMoney = _matrixHandler.Outcome(interaction2);

        playerData.MoneyPoints += playerMoney;
        targetData.MoneyPoints += targetMoney;

        var userData = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserId == pending.UserId);
        var targetUserData = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserId == pending.TargetId);

        Console.WriteLine("interactions" + pending.UserId + " " + pending.TargetId);

        var gameSession = new GameSession
        {
            User1 = pending.UserId,
            Choice1 = pending.UserChoice.ToString(),
            GameNr1 = userData!.GameNr,
            MoneyPoints1 = playerData.MoneyPoints,
            CoopCoop1 = playerData.CoopCoop,
            CoopDeflect1 = playerData.CoopDeflect,
            DeflectCoop1 = playerData.DeflectCoop,
            DeflectDeflect1 = playerData.DeflectDeflect,

            User2 = pending.TargetId,
            Choice2 = pending.TargetChoice.ToString(),
            GameNr2 = targetUserData!.GameNr,
            MoneyPoints2 = targetData.MoneyPoints,
            CoopCoop2 = targetData.CoopCoop,
            CoopDeflect2 = targetData.CoopDeflect,
            DeflectCoop2 = targetData.DeflectCoop,
            DeflectDeflect2 = targetData.DeflectDeflect
        };
        if (playerUser!.Role == "bot")
        {
            var bot = await _myDbContext.bot_strat.FirstOrDefaultAsync(b => b.UserId == playerUser.UserId);
            if (bot!.MoneyLimit < playerData.MoneyPoints && bot.MoneyLimit != 0) await HandleBuyAsync(playerUser.UserId);
        }
        else if (targetUser!.Role == "bot")
        {
            var bot = await _myDbContext.bot_strat.FirstOrDefaultAsync(b => b.UserId == targetUser.UserId);
            if (bot!.MoneyLimit < targetData.MoneyPoints && bot.MoneyLimit != 0) await HandleBuyAsync(targetUser.UserId);
        }

        var removeIds = new List<int>();
        if (playerData.MoneyPoints <= 0)
            removeIds.Add(playerData.UserId);
        if (targetData.MoneyPoints <= 0)
            removeIds.Add(targetData.UserId);

        foreach (var id in removeIds)
        {
            await _gameOver.NoMoneyAsync(id);
        }

        _myDbContext.game_session.Add(gameSession);
        _myDbContext.pending_interactions.Remove(pending);
        await _myDbContext.SaveChangesAsync();

        Console.WriteLine("should be saved and inserted");
    }

    public async Task InitiateBackup(UserData bot, UserData target)
    {
        Console.WriteLine($"InitiateBackup called for bot {bot.UserId} target {target.UserId}");

        var lastInteraction = await _myDbContext.game_session
        .Where(gs =>
            (gs.User1 == bot.UserId && gs.User2 == target.UserId && gs.GameNr1 == bot.GameNr) ||
            (gs.User2 == bot.UserId && gs.User1 == target.UserId && gs.GameNr2 == bot.GameNr))
        .OrderByDescending(gs => gs.ID)
        .FirstOrDefaultAsync();
        Console.WriteLine("lastinteraction + gamenr" + lastInteraction + " " + bot.GameNr);

        PlayerChoice choice;
        var strat = await _myDbContext.bot_strat.FirstOrDefaultAsync(b => b.UserId == bot.UserId);

        if (lastInteraction == null)
        {
            choice = strat!.Start ? PlayerChoice.Coop : PlayerChoice.Deflect;
            Console.WriteLine("choice from inside null last" + choice);

        }
        else
        {
            PlayerChoice? lastOpponentChoice = null;

            if (lastInteraction.User1 == target.UserId)
                lastOpponentChoice = Enum.Parse<PlayerChoice>(lastInteraction.Choice1!);
            else if (lastInteraction.User2 == target.UserId)
                lastOpponentChoice = Enum.Parse<PlayerChoice>(lastInteraction.Choice2!);

            var round = await _myDbContext.game_session.CountAsync(gs =>
                (gs.User1 == bot.UserId && gs.User2 == target.UserId && gs.GameNr1 == bot.GameNr && gs.GameNr2 == target.GameNr) ||
                (gs.User2 == bot.UserId && gs.User1 == target.UserId && gs.GameNr2 == bot.GameNr && gs.GameNr1 == target.GameNr));

            Console.WriteLine("context" + lastOpponentChoice + " " + round);

            var context = new BotContext
            {
                OpponentId = target.UserId,
                LastOpponentChoice = lastOpponentChoice,
                Round = round
            };

            var strategy = _botStrategyManager.GetOrCreateStrategy(bot.UserId, strat!.Strategy!);
            choice = strategy.GetNextChoice(context);

            Console.WriteLine("choice from inside NOTnull last" + choice);
        }

        await HandleTradeInitiationAsync(bot.UserId, target.UserId, choice);
    }
    
    public async Task RespondAsBotAsync(PendingInteraction pending, int playerId, int targetId)
    {
        Console.WriteLine("entered it");
        var bot = await _myDbContext.bot_strat.FirstOrDefaultAsync(b => b.UserId == targetId);
        var botNr = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserId == targetId);
        var playerNr = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserId == playerId);

        var lastInteraction = await _myDbContext.game_session
            .Where(gs =>
                (gs.User1 == playerId && gs.User2 == targetId && gs.GameNr2 == botNr!.GameNr) ||
                (gs.User2 == playerId && gs.User1 == targetId && gs.GameNr1 == botNr!.GameNr))
            .OrderByDescending(gs => gs.ID)
            .FirstOrDefaultAsync();

        PlayerChoice? lastOpponentChoice = null;
        PlayerChoice botResponse;

        if (lastInteraction == null)
        {
            botResponse = bot!.Start ? PlayerChoice.Coop : PlayerChoice.Deflect;
            Console.WriteLine("choice from inside null last" + botResponse);
            await HandleTradeResponseAsync(pending.PendingId, botResponse);
        }
        else if (lastInteraction != null)
        {
            if (lastInteraction.User1 == playerId)
                lastOpponentChoice = Enum.Parse<PlayerChoice>(lastInteraction.Choice1!);
            else if (lastInteraction.User2 == playerId)
                lastOpponentChoice = Enum.Parse<PlayerChoice>(lastInteraction.Choice2!);

            var context = new BotContext
            {
                OpponentId = playerId,
                LastOpponentChoice = lastOpponentChoice,
                Round = await _myDbContext.game_session.CountAsync(gs =>
                    (gs.User1 == targetId && gs.User2 == playerId && gs.GameNr1 == botNr!.GameNr && gs.GameNr2 == playerNr!.GameNr) ||
                    (gs.User2 == targetId && gs.User1 == playerId && gs.GameNr2 == botNr!.GameNr && gs.GameNr1 == playerNr!.GameNr))
            };

            var strategy = _botStrategyManager.GetOrCreateStrategy(botNr!.UserId, bot!.Strategy!);
            botResponse = strategy.GetNextChoice(context);
            await HandleTradeResponseAsync(pending.PendingId, botResponse);
        }

        Console.WriteLine("should exit it" + pending.PendingId);
    }

}