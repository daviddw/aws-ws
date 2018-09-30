using System;
using System.Collections.Generic;
using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace aws_lambda_test
{
    public class Function
    {
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest input, ILambdaContext context)
        {
            context.Logger.LogLine(">FunctionHandler\n");

            var config = new AmazonSimpleNotificationServiceConfig();
            config.ServiceURL = Environment.ExpandEnvironmentVariables("%SNSENDPOINT%");

            var client = new AmazonSimpleNotificationServiceClient(config);

            context.Logger.LogLine("AmazonSNSClient created\n");

            var request = new PublishRequest
            {
                TopicArn = Environment.ExpandEnvironmentVariables("%TOPICARN%"),
                Message = input.Body,
            };

            client.PublishAsync(request).Wait();

            context.Logger.LogLine("PublishAsync completed\n");

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = input.Body,
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } },
            };

            context.Logger.LogLine("<FunctionHandler\n");

            return response;
        }
    }
}
