using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("[controller]")]
public class PendingInteractionsController : Controller
{
    private readonly MyDbContext _myDbContext;
    private readonly GameLogic _gameLogic;
    public PendingInteractionsController(MyDbContext myDbContext, GameLogic gameLogic)
    { 
        _myDbContext = myDbContext;
        _gameLogic = gameLogic;
    }

    [HttpDelete("DeleteInteraction/{id}")]
    public async Task<IActionResult> DeleteSession(int id)
    {
        var interaction = await _myDbContext.pending_interactions.FindAsync(id);
        if (interaction == null)
        {
            return NotFound("Session not found.");
        }

        _myDbContext.pending_interactions.Remove(interaction);
        await _myDbContext.SaveChangesAsync();

        return Ok("Session deleted successfully.");
    }
}