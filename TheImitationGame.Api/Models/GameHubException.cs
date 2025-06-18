using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace TheImitationGame.Api.Models
{
    public class GameHubException(GameHubErrorCode code)
        : HubException(JsonSerializer.Serialize(
            new {
                code = code.ToString(),
                message = GameHubErrorMessage.GetMessage(code)
            }
        ))
    { }
}