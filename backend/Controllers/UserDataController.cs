using System.Security.Claims;
using System.Threading.Tasks;
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

    public UserDataController(GameLogic gameLogic, MyDbContext myDbContext, IPasswordHasher passwordHasher, AccessTokenGenerator accessToken, ActiveUsers activeUsers)
    { 
        _myDbContext = myDbContext;
        _passwordHasher = passwordHasher;
        _accessToken = accessToken;
        _activeUsers = activeUsers;
        _gameLogic = gameLogic;
        
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
    public IActionResult DeactivateBot(int selectedBotId)
    {
        var bot = _myDbContext.user_data
                .FirstOrDefault(u => u.UserId == selectedBotId && u.Role == "bot");

        _activeUsers.RemoveUser(selectedBotId);
        return Ok("Bot deactivated");
    }

    [HttpPost("bot/activate/{selectedBotId}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ActivateBot(int selectedBotId)
    {
        try
        {
            var bot = _myDbContext.user_data
                .FirstOrDefault(u => u.UserId == selectedBotId && u.Role == "bot");

            if (bot == null)
            {
                return NotFound("Bot not found or not a valid bot.");
            }

            if (_activeUsers.IsUserActive(selectedBotId))
            {
                return BadRequest("Bot is already active.");
            }

            await _gameLogic.ResetToStart(selectedBotId);
            _activeUsers.AddUser(selectedBotId, isBot: true);

            return Ok(new { message = "Bot activated.", userId = selectedBotId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error activating bot: " + ex.Message);
        }
    }

    [HttpGet("GetAllUsers")] //
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

    [Authorize]
    [HttpGet("GetUser")]
    public IActionResult GetUser()
    {
        string username = User.Identity?.Name ?? "Unknown";
        return Ok($"Welcome, {username}");
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

    [Authorize]
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

            await _gameLogic.ResetToStart(userId);

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

    [HttpDelete("DeleteUser/{id}")] //del account
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

    [HttpPut("UpdateUser/{id}")] //
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserData userRequest)
    {

        var existingUser = await _myDbContext.user_data.FindAsync(id);

        if (existingUser == null)
        {
            return NotFound("User not found.");
        }

        existingUser.UserName = userRequest.UserName;
        existingUser.Password = userRequest.Password;
        existingUser.MaxTurns = userRequest.MaxTurns;
        existingUser.GameNr = userRequest.GameNr;

        await _myDbContext.SaveChangesAsync();

        return Ok("User updated successfully.");
    }

    [HttpPut("UpdateUserData/{id}")] //change password or username
    public async Task<IActionResult> UpdateUserData(int id, [FromBody] UserRequest userRequest)
    {

        var existingUser = await _myDbContext.user_data.FindAsync(id);

        if (existingUser == null)
        {
            return NotFound("User not found.");
        }

        existingUser.UserName = userRequest.UserName;
        existingUser.Password = userRequest.Password;

        await _myDbContext.SaveChangesAsync();

        return Ok("User updated successfully.");
    }

    [HttpPut("UpdateGameNr/{id}")] //new game
    public async Task<IActionResult> UpdateMoney(int id, [FromBody] UserData userRequest)
    {

        var existingUser = await _myDbContext.user_data.FindAsync(id);

        if (existingUser == null)
        {
            return NotFound("User not found.");
        }

        existingUser.GameNr = userRequest.GameNr;

        await _myDbContext.SaveChangesAsync();

        return Ok("User updated successfully.");
    }

    [HttpPut("UpdateTurn/{id}")] //new turn
    public async Task<IActionResult> UpdateTurn(int id, [FromBody] UserData userRequest)
    {

        var existingUser = await _myDbContext.user_data.FindAsync(id);

        if (existingUser == null)
        {
            return NotFound("User not found.");
        }

        existingUser.MaxTurns = userRequest.MaxTurns;

        await _myDbContext.SaveChangesAsync();

        return Ok("User updated successfully.");
    }

}