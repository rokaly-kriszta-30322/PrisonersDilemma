using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class PendingInteractionsController : Controller
{
    private readonly MyDbContext _myDbContext;
    public PendingInteractionsController(MyDbContext myDbContext)
    { 
        _myDbContext = myDbContext;
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