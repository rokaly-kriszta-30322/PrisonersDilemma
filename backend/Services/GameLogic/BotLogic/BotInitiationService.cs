using Microsoft.EntityFrameworkCore;

public class BotInitiationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BotInitiationService> _logger;

    public BotInitiationService(IServiceProvider serviceProvider, ILogger<BotInitiationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var activeUsers = scope.ServiceProvider.GetRequiredService<ActiveUsers>();
            var gameLogic = scope.ServiceProvider.GetRequiredService<GameLogic>();

            try
            {
                var activeIds = activeUsers.GetActiveUserIds();

                var bots = await db.user_data
                    .Include(u => u.GameData)
                    .Where(u => u.Role == "bot" && activeIds.Contains(u.UserId) && u.GameData!.MoneyPoints > 0)
                    .ToListAsync();

                foreach (var bot in bots)
                {
                    if (!activeUsers.IsUserActive(bot.UserId)) continue;
                    if (activeUsers.IsUserBusy(bot.UserId)) continue;

                    if (!activeUsers.IsBotActiveMode(bot.UserId)) continue;

                    var fresh = await db.game_data.FirstOrDefaultAsync(g => g.UserId == bot.UserId);
                    if (fresh == null || fresh.MoneyPoints <= 0) continue;

                    activeUsers.SetUserBusy(bot.UserId);

                    try
                    {

                        var hasPending = await db.pending_interactions
                            .AnyAsync(p => p.UserId == bot.UserId || p.TargetId == bot.UserId);
                        if (hasPending) continue;

                        var candidates = await db.user_data
                            .Include(u => u.GameData)
                            .Where(u => u.UserId != bot.UserId &&
                                activeIds.Contains(u.UserId) &&
                                u.GameData!.MoneyPoints > 0)
                            .ToListAsync();

                        candidates = candidates
                            .Where(u => !activeUsers.IsUserBusy(u.UserId))
                            .ToList();

                        if (!candidates.Any()) continue;

                        UserData target;
                        if (activeUsers.IsBotChaosMode(bot.UserId))
                        {
                            var random = new Random();
                            target = candidates[random.Next(candidates.Count)];
                        }
                        else
                        {
                            var orderedIds = candidates.OrderBy(c => c.UserId).Select(c => c.UserId).ToList();
                            var nextTargetId = activeUsers.GetNextOpponentInOrder(bot.UserId, orderedIds);
                            target = candidates.First(c => c.UserId == nextTargetId);
                        }

                        activeUsers.SetUserBusy(target.UserId);

                        try
                        {

                            var targetPending = await db.pending_interactions
                                .AnyAsync(p => p.UserId == target.UserId || p.TargetId == target.UserId);
                            if (targetPending) continue;

                            var freshBotData = await db.game_data.FirstOrDefaultAsync(g => g.UserId == bot.UserId);
                            var freshTargetData = await db.game_data.FirstOrDefaultAsync(g => g.UserId == target.UserId);

                            if (freshBotData == null || freshBotData.MoneyPoints <= 0 ||
                                freshTargetData == null || freshTargetData.MoneyPoints <= 0 ||
                                !activeUsers.IsUserActive(bot.UserId) ||
                                !activeUsers.IsUserActive(target.UserId))
                            {
                                _logger.LogWarning($"Blocked bot {bot.UserId} from initiating due to stale state.");
                                continue;
                            }

                            await gameLogic.InitiateBackup(bot, target);

                            _logger.LogInformation($"Bot {bot.UserName} initiated with {target.UserName}");

                        }
                        finally
                        {
                            activeUsers.SetUserFree(target.UserId);
                        }

                    }
                    finally
                    {
                        activeUsers.SetUserFree(bot.UserId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bot initiation");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}