using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Playground.Api;

namespace Grpc.Playground.App.Grpc.ClientExamples
{
    public class StreamingFromClientExample : IClientExample
    {
        /// <summary>
        ///     Example of a RPC which streams request from the client to be handled by the
        ///     server. Once complete, the client indicates it is done writing and reads the
        ///     response from the server.
        /// </summary>
        /// <param name="client">The gRPC client to use</param>
        public async Task Run(Greeter.GreeterClient client)
        {
            var requests = new List<GreeterRequest>
            {
                new() { Name = "Will" },
                new() { Name = "Carolyn" },
                new() { Name = "Levi" },
                new() { Name = "Angela" }
            };

            using var call = client.GreetMany();

            foreach (var request in requests)
            {
                await call.RequestStream.WriteAsync(request);
            }

            // let the server know you're done writing requests
            await call.RequestStream.CompleteAsync();

            var response = await call;

            Console.WriteLine($"{response.Message}\n");
        }
    }
}