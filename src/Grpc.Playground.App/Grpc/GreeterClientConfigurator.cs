using System;
using System.Net.Http;
using System.Threading;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Grpc.Playground.Api;
using Microsoft.Extensions.Logging;

namespace Grpc.Playground.App.Grpc
{
    public static class GreeterClientConfigurator
    {
        /// <summary>
        ///     Configure a gRPC client with a custom HttpHandler that includes a keep-alive
        ///     configuration in order to avoid dropped TCP connections.
        ///     See
        ///     <a
        ///         href="https://docs.microsoft.com/en-us/aspnet/core/grpc/configuration?view=aspnetcore-5.0#configure-client-options">
        ///         client
        ///         configuration docs
        ///     </a>
        ///     for more information.
        /// </summary>
        /// <returns>A new instance of a <see cref="Greeter.GreeterClient" /></returns>
        public static Greeter.GreeterClient Configure()
        {
            // Configure an HttpHandler with keep-alive functionality to avoid dropping idle TCP connections
            var handler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            };

            // Configuration for handling transient failures, with retry policy
            var retryMethodConfig = new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 3,
                    InitialBackoff = TimeSpan.FromMilliseconds(10),
                    MaxBackoff = TimeSpan.FromMilliseconds(100),
                    BackoffMultiplier = 1.5,
                    RetryableStatusCodes = { StatusCode.Unavailable }
                }
            };

            // Configuration for handling transient failures, with hedging policy
            var hedgingMethodConfig = new MethodConfig
            {
                Names = { MethodName.Default },
                HedgingPolicy = new HedgingPolicy
                {
                    MaxAttempts = 3,
                    NonFatalStatusCodes = { StatusCode.Unavailable },
                    HedgingDelay = TimeSpan.FromMilliseconds(10)
                }
            };

            // Configuration for logging. In order to actually expose logging, set log level to "Debug"
            var loggerFactory = LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
                loggingBuilder.SetMinimumLevel(LogLevel.None);
            });

            // Create the channel
            var channel = GrpcChannel.ForAddress(
                "https://localhost:5001",
                new GrpcChannelOptions
                {
                    HttpHandler = handler,
                    LoggerFactory = loggerFactory,
                    ServiceConfig = new ServiceConfig
                    {
                        MethodConfigs = { retryMethodConfig }
                    }
                }
            );

            // Create the client with the configured channel
            var client = new Greeter.GreeterClient(channel);

            return client;
        }
    }
}