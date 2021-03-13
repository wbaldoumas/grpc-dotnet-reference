using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;

namespace Grpc.Playground.Api.Grpc.Services
{
    public class GreeterService : Greeter.GreeterBase
    {
        public override async Task<GreeterResponse> Greet(GreeterRequest request, ServerCallContext context) =>
            await Task.FromResult(new GreeterResponse { Message = $"Hello {request.Name}!" });

        public override async Task<GreeterResponse> GreetMany(
            IAsyncStreamReader<GreeterRequest> requestStream,
            ServerCallContext context)
        {
            var requests = new List<GreeterRequest>();

            while (await requestStream.MoveNext())
            {
                requests.Add(requestStream.Current);
            }

            return new GreeterResponse
            {
                Message = $"Hello {string.Join(", ", requests.Select(r => r.Name))}"
            };
        }

        public override async Task GreetManyTimes(
            GreeterRequest request,
            IServerStreamWriter<GreeterResponse> responseStream,
            ServerCallContext context)
        {
            for (var i = 0; i < 5; i++)
            {
                await responseStream.WriteAsync(new GreeterResponse
                {
                    Message = $"Hello {request.Name} {i}!"
                });
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        public override async Task GreetOnTheFly(
            IAsyncStreamReader<GreeterRequest> requestStream,
            IServerStreamWriter<GreeterResponse> responseStream,
            ServerCallContext context)
        {
            await foreach (var request in requestStream.ReadAllAsync())
            {
                await responseStream.WriteAsync(
                    new GreeterResponse
                    {
                        Message = $"Hello {request.Name}!"
                    }
                );
            }
        }
    }
}