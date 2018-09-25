using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace aws_service_test.Connections
{
    public interface IConnectionManager : IDisposable
    {
        Task Connect(IChannel channel);
        Task Send(string message);
    }

    public class ConnectionManager : IConnectionManager
    {
        private readonly string kHelloMessage = JsonConvert.SerializeObject(new { type = "HELLO" });
        private readonly string kGoodbyeMessage = JsonConvert.SerializeObject(new { type = "GOODBYE" });

        private readonly List<IConnection> connections;

        public ConnectionManager()
        {
            this.connections = new List<IConnection>();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Task.Run(async () =>
                {
                    foreach (var c in this.connections)
                    {
                        await c.Send(kGoodbyeMessage);
                        await c.Close();
                    }

                    this.connections.Clear();
                }).Wait();
            }
        }

        public async Task Connect(IChannel channel)
        {
            var connection = new Connection(channel);
            this.connections.Add(connection);
            await connection.Send(kHelloMessage);
        }

        public async Task Send(string message)
        {
            await Task.WhenAll(connections.Select(c => c.Send(message)));
        }
    }
}
