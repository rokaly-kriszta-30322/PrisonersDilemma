using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("[controller]")]
public class GameSessionController : Controller
{
    private readonly MyDbContext _myDbContext;
    private readonly GameLogic _gameLogic;
    public GameSessionController(MyDbContext myDbContext, GameLogic gameLogic)
    { 
        _myDbContext = myDbContext;
        _gameLogic = gameLogic;
    }

    [Authorize]
    [HttpPost("game/interaction")]
    public async Task<IActionResult> SendInteraction([FromBody] GameSessionRequest request)
    {
        var initiator = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserName == request.UserName1);
        if (initiator == null)
            return NotFound("Initiator not found");

        var target = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserName == request.UserName2);
        if (target == null)
            return NotFound("Target not found");
        
        var initiatorBusy = await _myDbContext.pending_interactions
            .AnyAsync(p => p.UserId == initiator.UserId || p.TargetId == initiator.UserId);
        if (initiatorBusy)
            return BadRequest("You already have a pending interaction");

        var targetBusy = await _myDbContext.pending_interactions
            .AnyAsync(p => p.UserId == target.UserId || p.TargetId == target.UserId);
        if (targetBusy)
            return BadRequest("Target user is already in a pending interaction");

        await _gameLogic.GetUserIdAsync(request);
        return Ok("Interaction sent");
    }

    [Authorize]
    [HttpGet("game/pending")]
    public async Task<IActionResult> GetPendingInteraction()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out int userId))
            return Unauthorized("Invalid or missing user claim.");

        var pending = await _myDbContext.pending_interactions
            .FirstOrDefaultAsync(p => p.TargetId == userId && p.TargetChoice == null);

        if (pending == null)
            return NoContent();

        var sender = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserId == pending.UserId);
        if (sender == null)
            return NotFound("Sender not found.");

        return Ok(new {
            pending.PendingId,
            FromUser = sender.UserName,
            pending.UserChoice
        });
    }

    [Authorize]
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out int userId))
            return Unauthorized("Invalid or missing user claim.");

        var user = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            return NotFound("User not found.");

        var sessions = await _myDbContext.game_session
        .Where(gs =>
            (gs.User1 == userId && gs.GameNr1 == user!.GameNr) ||
            (gs.User2 == userId && gs.GameNr2 == user!.GameNr))
        .OrderByDescending(gs => gs.ID)
        .Take(10)
        .ToListAsync();

        var history = new List<object>();

        foreach (var gs in sessions)
        {
            bool isUser1 = gs.User1 == userId;
            int opponentId = isUser1 ? gs.User2 : gs.User1;

            var opponent = await _myDbContext.user_data.FindAsync(opponentId);
            string opponentName = opponent?.UserName ?? "Unknown";

            var entry = new
            {
                OpponentName = opponentName,
                YourChoice = isUser1 ? gs.Choice1 : gs.Choice2,
                OpponentChoice = isUser1 ? gs.Choice2 : gs.Choice1
            };

            history.Add(entry);
        }

        return Ok(history);
    }

    [Authorize]
    [HttpPost("game/respond")]
    public async Task<IActionResult> RespondToInteraction([FromBody] TradeResponse response)
    {
        await _gameLogic.HandleTradeResponseAsync(response.PendingId, response.TargetChoice);
        return Ok("Response submitted");
    }

    [Authorize]
    [HttpPost("game/buy")]
    public async Task<IActionResult> Buy()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out var userId))
        {
            return Unauthorized("User ID claim not found or invalid.");
        }

        try
        {
            await _gameLogic.HandleBuyAsync(userId);
            return Ok("Buy successful.");
        }
        catch (Exception ex)
        {
            return BadRequest($"Buy failed: {ex.Message}");
        }
    }

    [HttpGet("GetAllSessions")]
    public async Task<IActionResult> GetAllSessions()
    {
        var sessions = await _myDbContext.game_session.ToListAsync();
        return Ok(sessions);
    }

    [HttpPost("AddSession")] //one action happened
    public async Task<IActionResult> AddSession([FromBody] GameSession userRequest)
    {
        await _myDbContext.AddAsync(userRequest);
        await _myDbContext.SaveChangesAsync();
        
        return Ok(userRequest);
    }

    [HttpDelete("DeleteSession/{id}")] //nope
    public async Task<IActionResult> DeleteSession(int id)
    {
        var session = await _myDbContext.game_session.FindAsync(id);
        if (session == null)
        {
            return NotFound("Session not found.");
        }

        _myDbContext.game_session.Remove(session);
        await _myDbContext.SaveChangesAsync();

        return Ok("Session deleted successfully.");
    }

    [HttpDelete("DeleteAllSessions")]
    public async Task<IActionResult> DeleteAllSessions()
    {
        var allSessions = _myDbContext.game_session;

        _myDbContext.game_session.RemoveRange(allSessions);
        await _myDbContext.SaveChangesAsync();

        return Ok("All sessions deleted successfully.");
    }

    [HttpPut("UpdateSession/{id}")] //nope
    public async Task<IActionResult> UpdateUser(int id, GameSession userRequest)
    {

        var existingSession = await _myDbContext.game_session.FindAsync(id);

        if (existingSession == null)
        {
            return NotFound("Session not found.");
        }

        existingSession.ID = userRequest.ID;
        existingSession.User1 = userRequest.User1;
        existingSession.Choice1 = userRequest.Choice1;
        existingSession.GameNr1 = userRequest.GameNr1;
        existingSession.MoneyPoints1 = userRequest.MoneyPoints1;
        existingSession.CoopCoop1 = userRequest.CoopCoop1;
        existingSession.CoopDeflect1 = userRequest.CoopDeflect1;
        existingSession.DeflectCoop1 = userRequest.DeflectCoop1;
        existingSession.DeflectDeflect1 = userRequest.DeflectDeflect1;
        existingSession.User2 = userRequest.User2;
        existingSession.Choice2 = userRequest.Choice2;
        existingSession.GameNr2 = userRequest.GameNr2;
        existingSession.MoneyPoints2 = userRequest.MoneyPoints2;
        existingSession.CoopCoop2 = userRequest.CoopCoop2;
        existingSession.CoopDeflect2 = userRequest.CoopDeflect2;
        existingSession.DeflectCoop2 = userRequest.DeflectCoop2;
        existingSession.DeflectDeflect2 = userRequest.DeflectDeflect2;

        await _myDbContext.SaveChangesAsync();

        return Ok("Session updated successfully.");
    }

    [HttpPost("Action")]
    public async Task<IActionResult> SubmitMove([FromBody] GameSessionRequest request)
    {
        await _gameLogic.GetUserIdAsync(request);
        return Ok();
    }

    [HttpPost("ResponseToTrade")]
    public async Task<IActionResult> RespondToTrade([FromBody] TradeResponse response)
    {
        await _gameLogic.HandleTradeResponseAsync(response.PendingId, response.TargetChoice);
        return Ok();
    }

}