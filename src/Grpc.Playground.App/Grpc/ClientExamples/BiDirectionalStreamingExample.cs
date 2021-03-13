using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Playground.Api;

namespace Grpc.Playground.App.Grpc.ClientExamples
{
    public class BiDirectionalStreamingExample : IClientExample
    {
        /// <summary>
        ///     Example of a RPC which streams request from the client and receives a stream
        ///     back from the server, processing requests on the fly.
        /// </summary>
        /// <param name="client">The gRPC client to use</param>
        public async Task Run(Greeter.GreeterClient client)
        {
            using var call = client.GreetOnTheFly();

            Console.WriteLine("Starting background task to receive messages.\n");

            var readTask = Task.Run(async () =>
            {
                await foreach (var response in call.ResponseStream.ReadAllAsync())
                {
                    Console.WriteLine(response.Message);
                }
            });

            Console.WriteLine("Starting to send messages.");
            Console.WriteLine("Type a name to echo then press enter.");

            while (true)
            {
                var name = Console.ReadLine();

                if (string.IsNullOrEmpty(name))
                {
                    break;
                }

                await call.RequestStream.WriteAsync(new GreeterRequest
                {
                    Name = name
                });
            }

            Console.WriteLine("Disconnecting...");

            await call.RequestStream.CompleteAsync();
            await readTask;
        }
    }
}