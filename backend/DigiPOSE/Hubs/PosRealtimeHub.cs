using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DigiPOSE.Hubs
{
    public class PosRealtimeHub : Hub
    {
        private readonly ILogger<PosRealtimeHub> _logger;

        public PosRealtimeHub(ILogger<PosRealtimeHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinTenantGroup(int tenantId)
        {
            string groupName = $"Tenant_{tenantId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation(">>> [SIGNALR_JOIN]: POS Terminal (ConnId: {ConnId}) joined LAN group [{Group}].", Context.ConnectionId, groupName);
        }

        public async Task LeaveTenantGroup(int tenantId)
        {
            string groupName = $"Tenant_{tenantId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation(">>> [SIGNALR_LEAVE]: POS Terminal (ConnId: {ConnId}) left LAN group [{Group}].", Context.ConnectionId, groupName);
        }

        public async Task JoinAdminTelemetryGroup()
        {
            string groupName = "AdminTelemetryGroup";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation(">>> [CYBER_RADAR_JOIN]: Admin HUD Dashboard (ConnId: {ConnId}) linked to [{Group}] for live telemetry.", Context.ConnectionId, groupName);
        }

        public async Task LeaveAdminTelemetryGroup()
        {
            string groupName = "AdminTelemetryGroup";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation(">>> [CYBER_RADAR_LEAVE]: Admin HUD Dashboard (ConnId: {ConnId}) unlinked from [{Group}].", Context.ConnectionId, groupName);
        }
    }
}
