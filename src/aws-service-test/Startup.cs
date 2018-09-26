using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Nancy.Owin;
using Amazon.SQS;

using aws_service_test.WebSockets;
using aws_service_test.Connections;
using aws_service_test.Processors;
using aws_service_test.Handlers;
using Amazon.SQS.Model;
using System.Threading.Tasks;
using System.Linq;

namespace aws_service_test
{
    public class Startup
    {
        private const string kQueueNamePrefix = "aws-service-test-queue";
        private const uint kWebSocketKeepAliveIntervale = 30;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConnectionManager>(x =>
            {
                return new ConnectionManager();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var lifetime = app.ApplicationServices.GetRequiredService<IApplicationLifetime>();

            IDisposable processor = null;

            lifetime.ApplicationStarted.Register(() =>
            {
                var config = new AmazonSQSConfig();
                config.ServiceURL = Environment.ExpandEnvironmentVariables("%SQSENDPOINT%");

                var client = new AmazonSQSClient(config);

                var request = new ListQueuesRequest();
                request.QueueNamePrefix = kQueueNamePrefix;

                var queues = client.ListQueuesAsync(request).Result;
                var sqsUrl = queues.QueueUrls.FirstOrDefault();

                var manager = app.ApplicationServices.GetService<IConnectionManager>();
                var handler = new SqsMessageHandler(manager);

                processor = new SqsProcessor(client, sqsUrl, handler);
            });


            lifetime.ApplicationStopped.Register(() =>
            {
                processor?.Dispose();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(kWebSocketKeepAliveIntervale)
            });

            app.MapWhen(IsWebSocketRequest, x => x.UseMiddleware<WebSocketManagerMiddleware>());

            app.UseOwin(x => x.UseNancy());
        }

        private bool IsWebSocketRequest(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                return false;
            }

            // any additional checks for a ws connection?

            return true;
        }
    }
}
