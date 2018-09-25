using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using aws_service_test.Connections;

namespace aws_service_test.WebSockets
{
    public class WebSocketChannel : IChannel
    {
        private readonly WebSocket socket;

        public WebSocketChannel(WebSocket socket)
        {
            this.socket = socket;
        }

        public async Task Close()
        {
            try
            {
                await this.socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None)
                          .ConfigureAwait(false);
            }
            catch(WebSocketException) { }
        }

        public async Task Send(string message)
        {
            var encoded = Encoding.UTF8.GetBytes(message);

            try
            {
                await this.socket.SendAsync(new ArraySegment<byte>(encoded), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (WebSocketException)
            {
                await Close();
            }
        }
    }
}
