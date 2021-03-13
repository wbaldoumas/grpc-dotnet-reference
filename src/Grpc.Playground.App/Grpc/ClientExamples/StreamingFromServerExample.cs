using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Playground.Api;

namespace Grpc.Playground.App.Grpc.ClientExamples
{
    public class StreamingFromServerExample : IClientExample
    {
        /// <summary>
        ///     Example of a RPC which streams request from the server to be received by the client.
        /// </summary>
        /// <param name="client">The gRPC client to use</param>
        public async Task Run(Greeter.GreeterClient client)
        {
            using var call = client.GreetManyTimes(new GreeterRequest { Name = "Will" });

            while (await call.ResponseStream.MoveNext(CancellationToken.None))
            {
                Console.WriteLine(call.ResponseStream.Current.Message);
            }

            Console.WriteLine();
        }
    }
}