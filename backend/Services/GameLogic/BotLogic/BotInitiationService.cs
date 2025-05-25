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
                    .Where(u => u.Role == "bot" && activeIds.Contains(u.UserId))
                    .ToListAsync();

                foreach (var bot in bots)
                {
                    var hasPending = await db.pending_interactions
                        .AnyAsync(p => p.UserId == bot.UserId || p.TargetId == bot.UserId);
                    if (hasPending) continue;

                    var candidates = await db.user_data
                        .Where(u => u.UserId != bot.UserId && activeIds.Contains(u.UserId))
                        .ToListAsync();

                    if (!candidates.Any()) continue;

                    var random = new Random();
                    var target = candidates[random.Next(candidates.Count)];

                    var targetPending = await db.pending_interactions
                        .AnyAsync(p => p.UserId == target.UserId || p.TargetId == target.UserId);
                    if (targetPending) continue;

                    var strategy = await db.bot_strat.FirstOrDefaultAsync(b => b.UserId == bot.UserId);
                    if (strategy == null) continue;

                    var choice = strategy.Start ? PlayerChoice.Coop : PlayerChoice.Deflect;

                    await gameLogic.HandleTradeInitiationAsync(bot.UserId, target.UserId, choice);

                    _logger.LogInformation($"Bot {bot.UserName} initiated with {target.UserName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bot initiation");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}