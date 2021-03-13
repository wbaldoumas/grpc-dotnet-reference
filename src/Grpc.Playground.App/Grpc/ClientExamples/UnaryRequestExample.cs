using System;
using System.Threading.Tasks;
using Grpc.Playground.Api;

namespace Grpc.Playground.App.Grpc.ClientExamples
{
    public class UnaryRequestExample : IClientExample
    {
        /// <summary>
        ///     Example of a simple RPC which sends a request to the server and receives a
        ///     response back.
        /// </summary>
        /// <param name="client">The gRPC client to use</param>
        public async Task Run(Greeter.GreeterClient client)
        {
            var response = await client.GreetAsync(new GreeterRequest { Name = "Will" });
            Console.WriteLine($"{response.Message}\n");
        }
    }
}