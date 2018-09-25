using System;
using System.Threading.Tasks;

namespace aws_service_test.Connections
{
    public interface IChannel
    {
        Task Send(string message);
        Task Close();
    }

    public interface IConnection
    {
        Guid SessionId { get; }
        Task Send(string message);
        Task Close();
    }

    public class Connection : IConnection
    {
        private readonly IChannel channel;

        public Connection(IChannel channel)
        {
            SessionId = Guid.NewGuid();
            this.channel = channel;
        }

        public Guid SessionId
        {
            get;
            private set;
        }

        public async Task Send(string message)
        {
            await this.channel.Send(message);
        }

        public async Task Close()
        {   
            await this.channel.Close();
        }
    }
}
