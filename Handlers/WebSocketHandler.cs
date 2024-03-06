using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Miot.DataStores;

namespace Miot.Handlers;

public class WebSocketHandler
{
    private readonly ConcurrentBag<WebSocket> _webSockets = [];
    private readonly ConcurrentBag<WebSocket> _clientWebSockets = [];
    private readonly IoTDeviceDataStore _ioTDeviceDataStore = new();
    
    public async Task HandleDeviceWebSocketConnectionAsync(HttpContext httpContext)
    {
        if (!httpContext.WebSockets.IsWebSocketRequest)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
        _webSockets.Add(webSocket);
        await ReceiveDataFromDeviceAsync(webSocket);
    }
    
    public async Task HandleClientWebSocketConnectionAsync(HttpContext httpContext)
    {
        if (!httpContext.WebSockets.IsWebSocketRequest)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
        _clientWebSockets.Add(webSocket);
        await InvokeClientWebSocketAsync(webSocket);
    }
    
    private async Task ReceiveDataFromDeviceAsync(WebSocket? webSocket)
    {
        if(webSocket == null)
            return;
        
        byte[] buffer = new byte[1024 * 4];

        WebSocketReceiveResult receivedResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receivedResult.CloseStatus.HasValue)
        {
            var message = Encoding.UTF8.GetString(buffer);
            
            _ioTDeviceDataStore.AddIoTDeviceData(message);
            
            await webSocket.SendAsync(
                new ArraySegment<byte>("Ok"u8.ToArray()),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
            
            receivedResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }
        await webSocket.CloseAsync(
            receivedResult.CloseStatus.Value,
            receivedResult.CloseStatusDescription,
            CancellationToken.None
        );
        _webSockets.TryTake(out webSocket);
    }

    private async Task InvokeClientWebSocketAsync(WebSocket? webSocket)
    {
        if(webSocket == null)
            return;

        byte[] receivedBuffer = new byte[1024 * 4];

        WebSocketReceiveResult receivedResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receivedBuffer), CancellationToken.None);

        if (receivedResult.MessageType != WebSocketMessageType.Text)
            return;
        
        while (!receivedResult.CloseStatus.HasValue)
        {
            IEnumerable<string> iotDevicesData = _ioTDeviceDataStore.GetDevicesData();

            string iotDevicesJson = JsonSerializer.Serialize(iotDevicesData);
        
            ArraySegment<byte> responseBuffer = new(Encoding.UTF8.GetBytes(iotDevicesJson));
        
            await webSocket.SendAsync(responseBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
            receivedResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receivedBuffer), CancellationToken.None);
        }
        
        await webSocket.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            "Closing connection",
            CancellationToken.None
        );
        _clientWebSockets.TryTake(out webSocket);
    }
}