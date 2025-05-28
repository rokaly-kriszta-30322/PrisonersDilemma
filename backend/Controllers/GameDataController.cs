using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("[controller]")]
public class GameDataController : Controller
{
    private readonly MyDbContext _myDbContext;
    private readonly GameLogic _gameLogic;
    private readonly ActiveUsers _activeUsers;
        private readonly GameOver _gameOver;
    public GameDataController(GameOver gameOver, ActiveUsers activeUsers, MyDbContext myDbContext, GameLogic gameLogic)
    {
        _myDbContext = myDbContext;
        _gameLogic = gameLogic;
        _activeUsers = activeUsers;
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

    [HttpGet("GetAllGData")] //
    public async Task<IActionResult> GetAllGData()
    {
        var g_data = await _myDbContext.game_data.ToListAsync();

        return Ok(g_data);
    }

    [HttpGet("GetGData/{id}")] //
    public async Task<IActionResult> GetGData(int id)
    {
        var g_data = await _myDbContext.game_data.FirstOrDefaultAsync(gm => gm.GDataId == id);

        if (g_data == null)
        {
            return NotFound("Game data not found.");
        }
        return Ok(g_data);
    }

    [HttpPost("AddGData")] //on user creation
    public async Task<IActionResult> AddGData([FromBody] GameData userRequest)
    {
        await _myDbContext.AddAsync(userRequest);
        await _myDbContext.SaveChangesAsync();

        return Ok(userRequest);
    }

    [HttpDelete("DeleteGData/{id}")] // when accounts deletes
    public async Task<IActionResult> DeleteGData(int id)
    {
        var g_data = await _myDbContext.game_data.FindAsync(id);
        if (g_data == null)
        {
            return NotFound("Game data not found.");
        }

        _myDbContext.game_data.Remove(g_data);
        await _myDbContext.SaveChangesAsync();

        return Ok("Game data deleted successfully.");
    }

    [HttpPut("UpdateGData/{id}")] //end of every game
    public async Task<IActionResult> UpdateGData(int id, [FromBody] GameData userRequest)
    {

        var existingGData = await _myDbContext.game_data.FindAsync(id);

        if (existingGData == null)
        {
            return NotFound("Game data not found.");
        }

        existingGData.MoneyPoints = userRequest.MoneyPoints;
        existingGData.CoopCoop = userRequest.CoopCoop;
        existingGData.CoopDeflect = userRequest.CoopDeflect;
        existingGData.DeflectCoop = userRequest.DeflectCoop;
        existingGData.DeflectDeflect = userRequest.DeflectDeflect;

        await _myDbContext.SaveChangesAsync();

        return Ok("Game data updated successfully.");
    }

    [HttpPut("UpdateMoney/{id}")] //market
    public async Task<IActionResult> UpdateMoney(int id, [FromBody] GameData userRequest)
    {

        var existingGData = await _myDbContext.game_data.FindAsync(id);

        if (existingGData == null)
        {
            return NotFound("Game data not found.");
        }

        existingGData.MoneyPoints = userRequest.MoneyPoints;

        await _myDbContext.SaveChangesAsync();

        return Ok("Game data updated successfully.");
    }

    [HttpPut("UpdateCC/{id}")] //market
    public async Task<IActionResult> UpdateCC(int id, [FromBody] GameData userRequest)
    {

        var existingGData = await _myDbContext.game_data.FindAsync(id);

        if (existingGData == null)
        {
            return NotFound("Game data not found.");
        }

        existingGData.CoopCoop = userRequest.CoopCoop;

        await _myDbContext.SaveChangesAsync();

        return Ok("Game data updated successfully.");
    }

    [HttpPut("UpdateCD/{id}")] //market
    public async Task<IActionResult> UpdateCD(int id, [FromBody] GameData userRequest)
    {

        var existingGData = await _myDbContext.game_data.FindAsync(id);

        if (existingGData == null)
        {
            return NotFound("Game data not found.");
        }

        existingGData.CoopDeflect = userRequest.CoopDeflect;

        await _myDbContext.SaveChangesAsync();

        return Ok("Game data updated successfully.");
    }

    [HttpPut("UpdateDC/{id}")] //market
    public async Task<IActionResult> UpdateDC(int id, [FromBody] GameData userRequest)
    {

        var existingGData = await _myDbContext.game_data.FindAsync(id);

        if (existingGData == null)
        {
            return NotFound("Game data not found.");
        }

        existingGData.DeflectCoop = userRequest.DeflectCoop;

        await _myDbContext.SaveChangesAsync();

        return Ok("Game data updated successfully.");
    }

    [HttpPut("UpdateDD/{id}")] //market
    public async Task<IActionResult> UpdateDD(int id, [FromBody] GameData userRequest)
    {

        var existingGData = await _myDbContext.game_data.FindAsync(id);

        if (existingGData == null)
        {
            return NotFound("Game data not found.");
        }

        existingGData.DeflectDeflect = userRequest.DeflectDeflect;

        await _myDbContext.SaveChangesAsync();

        return Ok("Game data updated successfully.");
    }

}