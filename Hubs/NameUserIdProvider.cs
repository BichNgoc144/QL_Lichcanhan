using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace QL_Lichcanhan.Hubs
{
    public class NameUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // Lấy userId từ claim "NameIdentifier"
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
