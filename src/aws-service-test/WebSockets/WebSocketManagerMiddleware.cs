using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using aws_service_test.Connections;
using Microsoft.AspNetCore.Http;

namespace aws_service_test.WebSockets
{
    public class WebSocketManagerMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IConnectionManager manager;

        public WebSocketManagerMiddleware(RequestDelegate next, IConnectionManager manager)
        {
            this.next = next;
            this.manager = manager;
        }

        public async Task Invoke(HttpContext context)
        {
            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var channel = new WebSocketChannel(socket);

            await this.manager.Connect(channel);

            var buffer = new byte[1024 * 4];

            while (socket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }
        }
    }
}
