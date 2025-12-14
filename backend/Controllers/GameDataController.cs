using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("[controller]")]
[Authorize]
public class GameDataController : Controller
{
    private readonly MyDbContext _myDbContext;
        private readonly GameOver _gameOver;
    public GameDataController(GameOver gameOver, MyDbContext myDbContext)
    {
        _myDbContext = myDbContext;
        _gameOver = gameOver;
    }

    [HttpGet("data")]
    public async Task<IActionResult> GetUpdatedData()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("User ID not found in token.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            return BadRequest("Invalid user ID.");

        var gameData = await _myDbContext.game_data.FirstOrDefaultAsync(gm => gm.UserId == userId);

        if (gameData == null)
            return NotFound("User data not found.");

        return Ok(new
        {
            moneyPoints = gameData.MoneyPoints,
            coopCoop = gameData.CoopCoop,
            coopDeflect = gameData.CoopDeflect,
            deflectCoop = gameData.DeflectCoop,
            deflectDeflect = gameData.DeflectDeflect
        });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> ResetGData()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
            {
                return Unauthorized("Invalid token. User ID not found.");
            }

            int userId = int.Parse(userIdClaim);

            await _gameOver.ResetToStart(userId);

            return Ok(new { message = "Success." });
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during reset: " + ex.Message);
            return StatusCode(500, "Error occurred during reset.");
        }
    }

    [HttpGet("GetAllGData")]
    public async Task<IActionResult> GetAllGData()
    {
        var g_data = await _myDbContext.game_data.ToListAsync();

        return Ok(g_data);
    }

    [HttpGet("GetGData/{id}")]
    public async Task<IActionResult> GetGData(int id)
    {
        var g_data = await _myDbContext.game_data.FirstOrDefaultAsync(gm => gm.GDataId == id);

        if (g_data == null)
        {
            return NotFound("Game data not found.");
        }
        return Ok(g_data);
    }

}