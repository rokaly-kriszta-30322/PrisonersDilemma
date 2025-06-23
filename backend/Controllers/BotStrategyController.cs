using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("[controller]")]
public class BotStrategyController : Controller
{
    private readonly MyDbContext _myDbContext;
    private readonly ActiveUsers _activeUsers;
    private readonly GameLogic _gameLogic;

    public BotStrategyController(GameLogic gameLogic, MyDbContext myDbContext, ActiveUsers activeUsers)
    {
        _myDbContext = myDbContext;
        _activeUsers = activeUsers;
        _gameLogic = gameLogic;
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

    [HttpPut("UpdateMoneyLimit")]
    public async Task<IActionResult> UpdateBotMoneyLimit([FromBody] MoneyLimitDto dto)
    {
        var strategy = await _myDbContext.bot_strat.FirstOrDefaultAsync(b => b.UserId == dto.UserId);

        if (strategy == null)
            return NotFound("Bot strategy not found.");

        strategy.MoneyLimit = dto.MoneyLimit;
        await _myDbContext.SaveChangesAsync();

        return Ok("Money limit updated.");
    }

    [HttpDelete("DeleteBotStrategy/{botId}")]
    public async Task<IActionResult> DeleteBotStrategy(int botId)
    {
        var strategy = await _myDbContext.bot_strat.FirstOrDefaultAsync(b => b.UserId == botId);

        if (strategy == null)
            return NotFound("Bot strategy not found.");

        _myDbContext.bot_strat.Remove(strategy);
        await _myDbContext.SaveChangesAsync();

        return Ok("Bot strategy deleted.");
    }

}