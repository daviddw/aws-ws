using System;
using System.Collections.Generic;
using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.SQS;
using Amazon.SQS.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace aws_lambda_test
{
    public class Function
    {
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest input, ILambdaContext context)
        {
            context.Logger.LogLine(">FunctionHandler\n");

            var config = new AmazonSQSConfig();
            config.ServiceURL = Environment.ExpandEnvironmentVariables("%SQSENDPOINT%");

            var client = new AmazonSQSClient(config);

            context.Logger.LogLine("AmazonSQSClient created\n");

            var request = new SendMessageRequest
            {
                QueueUrl = Environment.ExpandEnvironmentVariables("%QUEUENAME%"),
                MessageBody = input.Body,
            };

            client.SendMessageAsync(request).Wait();

            context.Logger.LogLine("SendMessageAsync completed\n");

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
