public static class UserMapper
{
    public static UserData ToUserData(UserRequest dto, string hashedPassword)
    {
        return new UserData
        {
            UserName = dto.UserName,
            Password = hashedPassword,
            Role = "client",
            GameNr = 0,
            MaxTurns = 0
        };
    }

    public static UserResponse ToUserResponse(UserData user)
    {
        return new UserResponse
        {
            UserName = user.UserName,
            Role = user.Role,
            MaxTurns = user.MaxTurns,
            GameNr = user.GameNr
        };
    }
}
