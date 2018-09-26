using System;
using System.Threading.Tasks;
using aws_service_test.Connections;

namespace aws_service_test.Handlers
{
    public interface ISqsMessageHandler
    {
        Task Process(string message);
    }

    public class SqsMessageHandler : ISqsMessageHandler
    {
        private readonly IConnectionManager manager;

        public SqsMessageHandler(IConnectionManager manager)
        {
            this.manager = manager;
        }

        public async Task Process(string message)
        {
            await this.manager.Send(message);
        }
    }
}
