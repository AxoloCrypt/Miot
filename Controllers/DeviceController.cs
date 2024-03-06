using Microsoft.AspNetCore.Mvc;
using Miot.Handlers;

namespace Miot.Controllers;

public class DeviceController(WebSocketHandler handler) : ControllerBase
{
    [Route("/devices")]
    public async Task SetDevicesConnection()
    {
        await handler.HandleDeviceWebSocketConnectionAsync(HttpContext);
    }

    [Route("/devices/data")]
    public async Task SetClientsConnection()
    {
        await handler.HandleClientWebSocketConnectionAsync(HttpContext);
    }
    
}