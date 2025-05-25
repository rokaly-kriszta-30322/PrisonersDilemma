public class HandlerRequest
{
    public GameData playerData { get; set; }
    public PlayerChoice playerChoice { get; set; }
    public PlayerChoice? targetChoice { get; set; }

    public HandlerRequest(GameData playerData, PlayerChoice playerChoice, PlayerChoice targetChoice)
    {
        this.playerData = playerData;
        this.playerChoice = playerChoice;
        this.targetChoice = targetChoice;
    }
}