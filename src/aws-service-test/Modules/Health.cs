using System;
using Nancy;

namespace aws_service_test.Modules
{
    public class Health : NancyModule
    {
        public Health()
        {
            Get("/health", _ => {
                return Response.AsJson(new
                {
                    version = Environment.ExpandEnvironmentVariables("%DOCKERTAG%")
                });
            });
        }
    }
}
