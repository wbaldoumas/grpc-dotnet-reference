using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Grpc.Playground.Api.Grpc.Interceptors
{
    public class MetricLoggingInterceptor : Interceptor
    {
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var sw = Stopwatch.StartNew();
            var baseReturn = await base.UnaryServerHandler(request, context, continuation);
            var elapsed = sw.ElapsedMilliseconds;

            LogElapsed(context, elapsed);

            return baseReturn;
        }

        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
            TRequest request,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var sw = Stopwatch.StartNew();
            await base.ServerStreamingServerHandler(request, responseStream, context, continuation);
            var elapsed = sw.ElapsedMilliseconds;

            LogElapsed(context, elapsed);
        }

        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            ServerCallContext context,
            ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var sw = Stopwatch.StartNew();
            var baseReturn = await base.ClientStreamingServerHandler(requestStream, context, continuation);
            var elapsed = sw.ElapsedMilliseconds;

            LogElapsed(context, elapsed);

            return baseReturn;
        }

        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var sw = Stopwatch.StartNew();
            await base.DuplexStreamingServerHandler(requestStream, responseStream, context, continuation);
            var elapsed = sw.ElapsedMilliseconds;

            LogElapsed(context, elapsed);
        }

        private static void LogElapsed(ServerCallContext context, long elapsedMs) =>
            Console.WriteLine($"{context.Method} - elapsed ms: {elapsedMs}");
    }
}