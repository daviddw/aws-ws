using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nancy.Owin;
using aws_service_test.WebSockets;

namespace aws_service_test
{
    public class Startup
    {
        private const uint kWebSocketKeepAliveIntervale = 30;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
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
