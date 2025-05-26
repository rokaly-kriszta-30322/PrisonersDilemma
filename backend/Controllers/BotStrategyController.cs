using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("[controller]")]
public class BotStrategyController : Controller
{
    private readonly MyDbContext _myDbContext;
    private readonly GameLogic _gameLogic;
    private readonly ActiveUsers _activeUsers;

    public BotStrategyController(MyDbContext myDbContext, GameLogic gameLogic, ActiveUsers activeUsers)
    { 
        _myDbContext = myDbContext;
        _gameLogic = gameLogic;
        _activeUsers = activeUsers;
    }

    [HttpPost("AddBotStrategy")]
    public async Task<IActionResult> AddBotStrategy([FromBody] BotStrategyDto botDto)
    {
        var user = await _myDbContext.user_data.FindAsync(botDto.UserId);

        if (user == null || user.Role != "bot")
            return BadRequest("Bot user not found or user is not a bot.");

        var existingStrategy = await _myDbContext.bot_strat
            .FirstOrDefaultAsync(b => b.UserId == botDto.UserId);

        if (existingStrategy != null)
        {
            existingStrategy.Start = botDto.Start;
            existingStrategy.Strategy = botDto.Strategy;
            existingStrategy.MoneyLimit = botDto.MoneyLimit;
        }
        else
        {
            var botStrategy = new BotStrategy
            {
                UserId = botDto.UserId,
                Start = botDto.Start,
                Strategy = botDto.Strategy,
                MoneyLimit = botDto.MoneyLimit
            };

            await _myDbContext.bot_strat.AddAsync(botStrategy);
        }

        await _myDbContext.SaveChangesAsync();
        return Ok("Bot strategy saved.");
    }

    [HttpPost("interaction/initiate")]
    public async Task<IActionResult> BotInitiate([FromBody] BotInitiation dto)
    {
        var activeIds = _activeUsers.GetActiveUserIds();

        var bot = await _myDbContext.user_data.FirstOrDefaultAsync(u =>
            u.UserId == dto.BotId && u.Role == "bot" && activeIds.Contains(u.UserId));
        if (bot == null)
            return NotFound("Bot not found or inactive");

        var hasPending = await _myDbContext.pending_interactions
            .AnyAsync(p => p.UserId == dto.BotId || p.TargetId == dto.BotId);
        if (hasPending)
            return BadRequest("Bot already has a pending interaction");

        var target = await _myDbContext.user_data.FirstOrDefaultAsync(u =>
            u.UserName == dto.TargetName && activeIds.Contains(u.UserId));
        if (target == null)
            return NotFound("Target user not found or inactive");

        var targetPending = await _myDbContext.pending_interactions
            .AnyAsync(p => p.UserId == target.UserId || p.TargetId == target.UserId);
        if (targetPending)
            return BadRequest("Target already has a pending interaction");

        await _gameLogic.InitiateBackup(bot,target);

        return Ok("Interaction initiated");
    }

    [HttpGet("pending/bot/{botId}")]
    public async Task<IActionResult> GetPendingForBot(int botId)
    {
        var pending = await _myDbContext.pending_interactions.FirstOrDefaultAsync(p => p.UserId == botId || p.TargetId == botId);
        return pending == null ? NoContent() : Ok(pending);
    }

}