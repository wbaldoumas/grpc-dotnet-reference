using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Playground.App.Grpc;
using Grpc.Playground.App.Grpc.ClientExamples;

namespace Grpc.Playground.App
{
    internal static class Program
    {
        private static async Task Main()
        {
            var client = GreeterClientConfigurator.Configure();

            var examples = new List<IClientExample>
            {
                new UnaryRequestExample(),
                new StreamingFromClientExample(),
                new StreamingFromServerExample(),
                new BiDirectionalStreamingExample()
            };

            foreach (var example in examples)
            {
                await example.Run(client);
            }

            Console.ReadLine();
        }
    }
}