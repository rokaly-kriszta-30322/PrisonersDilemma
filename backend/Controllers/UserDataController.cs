using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("[controller]")]
public class UserDataController : Controller
{
    private readonly MyDbContext _myDbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AccessTokenGenerator _accessToken;
    private readonly ActiveUsers _activeUsers;
    private readonly GameLogic _gameLogic;
    private readonly GameOver _gameOver;

    public UserDataController(GameOver gameOver, GameLogic gameLogic, MyDbContext myDbContext, IPasswordHasher passwordHasher, AccessTokenGenerator accessToken, ActiveUsers activeUsers)
    {
        _myDbContext = myDbContext;
        _passwordHasher = passwordHasher;
        _accessToken = accessToken;
        _activeUsers = activeUsers;
        _gameLogic = gameLogic;
        _gameOver = gameOver;

    }

    [HttpGet("users/active")]
    public async Task<IActionResult> GetActivePlayers()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (claim == null || !int.TryParse(claim, out var currentUserId))
        {
            return Unauthorized("User ID claim missing or invalid.");
        }

        var ids = _activeUsers.GetActiveUserIds();

        var users = await _myDbContext.user_data
            .Where(u => ids.Contains(u.UserId))
            .Include(u => u.GameData)
            .Select(u => new {
                u.UserId,
                u.UserName,
                u.Role,
                u.GameData!.MoneyPoints
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("users/set-active")]
    public IActionResult SetActiveStatus([FromBody] bool isActive)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (claim == null || !int.TryParse(claim, out var userId))
            return Unauthorized("Invalid user ID.");

        if (isActive)
            _activeUsers.AddUser(userId, isBot: false);
        else
            _activeUsers.RemoveUser(userId);

        return Ok();
    }

    [HttpGet("bots")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllBots()
    {
        var bots = await _myDbContext.user_data
            .Where(u => u.Role == "bot")
            .Select(b => new {
                b.UserId,
                b.UserName
            })
            .ToListAsync();

        return Ok(bots);
    }

    [HttpPost("bot/deactivate/{selectedBotId}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeactivateBotAsync(int selectedBotId)
    {
        var bot = _myDbContext.user_data
                .FirstOrDefault(u => u.UserId == selectedBotId && u.Role == "bot");
        
        _activeUsers.RemoveUser(selectedBotId);
        await _gameOver.ResetToStart(selectedBotId);
        return Ok("Bot deactivated");
    }

    [HttpPost("bot/activate/{selectedBotId}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ActivateBotAsync(int selectedBotId)
    {
        try
        {
            var bot = _myDbContext.user_data
                .Include(u => u.GameData)
                .FirstOrDefault(u => u.UserId == selectedBotId && u.Role == "bot");

            if (bot == null)
            {
                return NotFound("Bot not found or not a valid bot.");
            }

            if (_activeUsers.IsUserActive(selectedBotId))
            {
                return BadRequest("Bot is already active.");
            }

            if(bot.GameData!.MoneyPoints != 100 || bot.GameData.CoopCoop != 8) await _gameOver.ResetToStart(selectedBotId);
            _activeUsers.AddUser(selectedBotId, isBot: true);

            return Ok(new { message = "Bot activated.", userId = selectedBotId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error activating bot: " + ex.Message);
        }
    }

    [HttpPost("resetTurnsNrs")]
    public async Task<IActionResult> ResetUserTurnsNrsAsync(int userId)
    {
        var user = await _myDbContext.user_data.FindAsync(userId);

        if (user == null)
            return NotFound("User not found.");

        user.MaxTurns = 0;
        user.GameNr = 0;

        await _myDbContext.SaveChangesAsync();

        return Ok("User state reset to 0.");
    }

    [HttpPost("signup")]
    public async Task<IActionResult> AddUser([FromBody] UserRequest userRequest)
    {
        try
        {
            if (string.IsNullOrEmpty(userRequest.UserName) || string.IsNullOrEmpty(userRequest.Password))
            {
                return BadRequest("Username and password are required.");
            }

            if (await _myDbContext.user_data.AnyAsync(u => u.UserName == userRequest.UserName))
            {
                return Conflict("Username is taken.");
            }

            var passwordHash = _passwordHasher.HashPassword(userRequest.Password);
            var user = UserMapper.ToUserData(userRequest, passwordHash);

            await _myDbContext.user_data.AddAsync(user);
            await _myDbContext.SaveChangesAsync();

            var gdata = new GameData(user.UserId);

            await _myDbContext.game_data.AddAsync(gdata);
            await _myDbContext.SaveChangesAsync();

            user.GameData = gdata;
            await _myDbContext.SaveChangesAsync();

            var response = UserMapper.ToUserResponse(user);
            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error while adding user: " + ex.Message);
            return StatusCode(500, "An error occurred while creating the user.");
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
            {
                return Unauthorized("Invalid token. User ID not found.");
            }

            int userId = int.Parse(userIdClaim);

            _activeUsers.RemoveUser(userId);

            await _gameOver.ResetToStart(userId);

            return Ok(new { message = "Logged out successfully." });
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during logout: " + ex.Message);
            return StatusCode(500, "Error occurred during logout.");
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserRequest userRequest)
    {
        try
        {
            if (string.IsNullOrEmpty(userRequest.UserName) || string.IsNullOrEmpty(userRequest.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var user = await _myDbContext.user_data
                .Include(u => u.GameData)
                .FirstOrDefaultAsync(u => u.UserName == userRequest.UserName);

            if (user == null)
            {
                return Unauthorized("User does not exist.");
            }

            if (string.IsNullOrEmpty(user.Password))
            {
                return StatusCode(500, "Stored password is missing. Contact support.");
            }

            bool isCorrectPassword = _passwordHasher.VerifyPassword(userRequest.Password,user.Password);
            Console.WriteLine($"Password match: {isCorrectPassword}");
            if(!isCorrectPassword)
            {
                return Unauthorized("Incorrect password.");
            }

            string accessToken = _accessToken.GenerateToken(user);

            _activeUsers.AddUser(user.UserId, isBot: false);

            var userDto = new UserLoginDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Role = user.Role,
                GameData = new GameDataLoginDto
                {
                    MoneyPoints = user.GameData!.MoneyPoints,
                    CoopCoop = user.GameData.CoopCoop,
                    CoopDeflect = user.GameData.CoopDeflect,
                    DeflectCoop = user.GameData.DeflectCoop,
                    DeflectDeflect = user.GameData.DeflectDeflect
                }
            };

            return Ok(new {
                token = accessToken,
                user = userDto
            });

        }
        catch (Exception ex)
        {
            Console.WriteLine("Error while signing in user: " + ex.Message);
            return StatusCode(500, "Error occured while signing in.");
        }
    }

    [HttpGet("GetAllUsers")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _myDbContext.user_data.ToListAsync();

            if (users == null || !users.Any())
            {
                return NotFound("No users found.");
            }

            return Ok(users);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in GetAllUsers: " + ex.Message);
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpGet("GetUser/{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _myDbContext.user_data.FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            return NotFound("User not found.");
        }
        return Ok(user);
    }

    [HttpPost("AddUser")]
    public async Task<IActionResult> AddSession([FromBody] UserDataDto userRequest)
    {
        var passwordHash = _passwordHasher.HashPassword(userRequest.Password!);
        var userEntity = new UserData
        {
            UserName = userRequest.UserName,
            Password = passwordHash,
            Role = userRequest.Role!,
            MaxTurns = userRequest.MaxTurns,
            GameNr = userRequest.GameNr
        };

        await _myDbContext.user_data.AddAsync(userEntity);
        await _myDbContext.SaveChangesAsync();

        var gdata = new GameData(userEntity.UserId);

        await _myDbContext.game_data.AddAsync(gdata);
        await _myDbContext.SaveChangesAsync();

        userEntity.GameData = gdata;
        await _myDbContext.SaveChangesAsync();

        return Ok("User added");
    }

    [HttpPut("UpdateUser/{userId}")]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] UserDataDto userRequest)
    {
        var user = await _myDbContext.user_data.FindAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        user.UserName = userRequest.UserName;
        if (!string.IsNullOrEmpty(userRequest.Password))
        {
            user.Password = _passwordHasher.HashPassword(userRequest.Password);
        }
        user.Role = userRequest.Role!;
        user.MaxTurns = userRequest.MaxTurns;
        user.GameNr = userRequest.GameNr;

        await _myDbContext.SaveChangesAsync();

        return Ok("User updated");
    }

    [HttpDelete("DeleteUser/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try {
            var user = await _myDbContext.user_data.FindAsync(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            _myDbContext.user_data.Remove(user);
            await _myDbContext.SaveChangesAsync();

            return Ok("User deleted successfully.");
        }
        catch (Exception ex){
            Console.WriteLine("Error while adding user: " + ex.Message);
            return StatusCode(500, "An error occurred while creating the user.");
        }
    }

}