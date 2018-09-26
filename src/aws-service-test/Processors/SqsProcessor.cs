using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using aws_service_test.Handlers;

namespace aws_service_test.Processors
{
    public class SqsProcessor : IDisposable
    {
        private readonly IAmazonSQS client;
        private string queueUrl;
        private readonly ISqsMessageHandler handler;
        private readonly CancellationTokenSource tokenSource;

        private Task receiveTask;

        public SqsProcessor(IAmazonSQS client, string queueUrl, ISqsMessageHandler handler)
        {
            this.client = client;
            this.queueUrl = queueUrl;
            this.handler = handler;
            this.tokenSource = new CancellationTokenSource();
            this.receiveTask = Task.Factory.StartNew(() => { }, this.tokenSource.Token);
            StartReceive();
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
                this.tokenSource.Cancel();

                try
                {
                    this.receiveTask.Wait();
                }
                catch (AggregateException) { }
            }
        }

        private void StartReceive()
        {
            if (!tokenSource.IsCancellationRequested)
            {
                this.receiveTask = this.receiveTask.ContinueWith(task =>
                {
                    task.Wait();
                    Receive().Wait();
                    StartReceive();
                });
            }
        }

        private async Task Receive()
        {
            var request = new ReceiveMessageRequest
            {
                MaxNumberOfMessages = 10,
                MessageAttributeNames = new List<string> { "All" },
                QueueUrl = this.queueUrl
            };

            try
            {
                var response = await this.client.ReceiveMessageAsync(request, this.tokenSource.Token);

                foreach (var m in response.Messages)
                {
                    Receive(m);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error receiving SQS message\n{e}");
            }
        }

        private void Receive(Message message)
        {
            if (!this.tokenSource.Token.IsCancellationRequested)
            {
                this.handler.Process(message.Body).Wait(this.tokenSource.Token);

                this.client.DeleteMessageAsync(this.queueUrl, message.ReceiptHandle, this.tokenSource.Token).Wait(this.tokenSource.Token);
            }
        }
    }
}
