using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace aws_service_test.WebSockets
{
    public class WebSocketManagerMiddleware
    {
        private readonly RequestDelegate next;

        public WebSocketManagerMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var socket = await context.WebSockets.AcceptWebSocketAsync();

            var buffer = new byte[1024 * 4];

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(
                  new ArraySegment<byte>(buffer),
                  CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }

            /*channel = new WebSocketChannel(socket, this.log);

            WebSocketChannel channel = null;

            try
            {
                await this.connectionManager.Connect(sessionId, async () =>
                {
                    var socket = await context.WebSockets.AcceptWebSocketAsync();
                    channel = new WebSocketChannel(socket, this.log);

                    return channel;
                });

                await channel.WaitUntilClosed();
            }
            catch (ConnectionException)
            {
                context.Response.StatusCode = 400;
            }*/
        }
    }
}
