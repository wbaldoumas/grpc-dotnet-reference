# grpc-dotnet-playground
A place to experiment with and learn gRPC in .NET!

  * [Resources](#resources)
  * [Creating and Configuring a gRPC Service](#creating-and-configuring-a-grpc-service)
    + [Required and Useful NuGet Packages](#required-and-useful-nuget-packages)
    + [Create Protocol Buffers](#create-protocol-buffers)
    + [Service Implementation](#service-implementation)
    + [Add gRPC Services in Startup](#add-grpc-services-in-startup)
      - [Configure Services](#configure-services)
      - [Configure Interceptors](#configure-interceptors)
      - [Configure Logging](#configure-logging)
    + [Versioning gRPC Services](#versioning-grpc-services)
  * [Creating and Configuring a gRPC Client](#creating-and-configuring-a-grpc-client)
    + [Required and Useful NuGet Packages](#required-and-useful-nuget-packages-1)
    + [Create Protocol Buffers](#create-protocol-buffers-1)
    + [Add gRPC Clients in Startup](#add-grpc-clients-in-startup)
      - [Configure gRPC Clients](#configure-grpc-clients)
      - [Configure HttpClient](#configure-httpclient)
      - [Configure Logging](#configure-logging)
      - [Configure Channel and Interceptors](#configure-channel-and-interceptors)
    + [Configuring a gRPC Client Manually](#configuring-a-grpc-client-manually)
    + [Deadlines Cancellation and Call Context Propagation](#deadlines-cancellation-and-call-context-propagation)
    + [Handling Transient Failures](#handling-transient-failures)
  * [Performance Best Practices](#performance-best-practices)
  * [Miscellaneous Links](#miscellaneous-links)

## Resources

- [Google's gRPC Documentation](https://grpc.io/docs/)
- [Google's Protocol Buffer Documentation](https://developers.google.com/protocol-buffers)
- [MSDN's gRPC Documentation](https://docs.microsoft.com/en-us/aspnet/core/grpc/?view=aspnetcore-5.0)
- [grpc-dotnet Code Examples](https://github.com/grpc/grpc-dotnet/tree/master/examples)

## Creating and Configuring a gRPC Service

### Required and Useful NuGet Packages

- [Grpc.AspNetCore](https://www.nuget.org/packages/Grpc.AspNetCore)
- [Google.Protobuf](https://www.nuget.org/packages/Google.Protobuf)
  
### Create Protocol Buffers

Protocol buffers are how you define your gRPC messages and services. Read more on the Protocol Buffer language guide [here](https://developers.google.com/protocol-buffers/docs/proto3).

```protobuf
syntax = "proto3";

option csharp_namespace = "Grpc.Playground.Api";

message GreeterRequest {
  string Name = 1;
}
```

```protobuf
syntax = "proto3";

option csharp_namespace = "Grpc.Playground.Api";

message GreeterResponse {
  string Message = 1;
}
```

```protobuf
syntax = "proto3";

option csharp_namespace = "Grpc.Playground.Api";

import "Grpc/Protos/GreeterRequest.proto";
import "Grpc/Protos/GreeterResponse.proto";

service Greeter {
  rpc Greet (GreeterRequest) returns (GreeterResponse);
  rpc GreetMany (stream GreeterRequest) returns (GreeterResponse);
  rpc GreetManyTimes (GreeterRequest) returns (stream GreeterResponse);
  rpc GreetOnTheFly (stream GreeterRequest) returns (stream GreeterResponse);
}
```

### Service Implementation

Once your messages and services are defined, you can implement your gRPC service:

```csharp
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
```

### Add gRPC Services in Startup

With the service implemented, you can register it in your `Configure` and `ConfigureServices` methods in `Startup`:

```csharp
services.AddGrpc();
```

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapGrpcService<GreeterService>();
});
```

#### Configure Services

gRPC services can optionally be configured during `Startup` as well, with configuration options:

```csharp
services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
    options.MaxReceiveMessageSize = 2 * 1024 * 1024; // 2 MB
    options.MaxSendMessageSize = 5 * 1024 * 1024; // 5 MB
});
```

Read more on service configuration options [here](https://docs.microsoft.com/en-us/aspnet/core/grpc/configuration?view=aspnetcore-5.0#configure-services-options).

#### Configure Interceptors

Service interceptors are similar to middleware in that they can be used to include code that runs before and after your service operations. Here's an example of an interceptor that intercepts unary requests processed by the service:

```csharp
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
}
```

Interceptors can be added to a service within `Startup`:

```csharp
services.AddGrpc().AddServiceOptions<GreeterService>(options =>
{
    options.Interceptors.Add<MetricLoggingInterceptor>();
});
```

For more information on interceptors and their differences from ASP.NET middleware, see the [docs](https://docs.microsoft.com/en-us/aspnet/core/grpc/migration?view=aspnetcore-5.0#grpc-interceptors-vs-middleware).

#### Configure Logging

Since gRPC services are hosted on ASP.NET Core, it uses the ASP.NET Core logging system and can be configured via `appsettings.json`:

```
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Information",
      "Grpc": "Debug"
    }
  }
}
```

For more information and different ways of configuring gRPC service logging, read the docs [here](https://docs.microsoft.com/en-us/aspnet/core/grpc/diagnostics?view=aspnetcore-5.0#grpc-services-logging).

### Versioning gRPC Services

Versioning gRPC services in order to maintain backwards compatability is a complex topic that deserves careful consideration, especially when initially desigining your protocol buffers. Read more on that [here](https://docs.microsoft.com/en-us/aspnet/core/grpc/versioning?view=aspnetcore-5.0).

## Creating and Configuring a gRPC Client

### Required and Useful NuGet Packages

- [Grpc.Net.Client](https://www.nuget.org/packages/Grpc.Net.Client)
- [Grpc.Tools](https://www.nuget.org/packages/Grpc.Tools)
- [Google.Protobuf](https://www.nuget.org/packages/Google.Protobuf)
- [Grpc.Net.ClientFactory](https://www.nuget.org/packages/Grpc.Net.ClientFactory)

### Create Protocol Buffers

gRPC clients should be based on the same protocol buffers that are defined and used by the underlying gRPC service the client will be talking to. See example of prtocol buffer messages and services above.

### Add gRPC Clients in Startup

If you are adding a gRPC client for a service to an ASP.NET Core application, it can be added by leveraging the [Grpc.Net.ClientFactory](https://www.nuget.org/packages/Grpc.Net.ClientFactory), which reuses underlying channels for services when new clients are created. Check out the [documentation](https://docs.microsoft.com/en-us/aspnet/core/grpc/clientfactory?view=aspnetcore-5.0).

#### Configure gRPC Clients

Check out the [docs](https://docs.microsoft.com/en-us/aspnet/core/grpc/clientfactory?view=aspnetcore-5.0#register-grpc-clients) for information and examples of registering your gRPC clients.

```csharp
services.AddGrpcClient<Greeter.GreeterClient>(o =>
{
    o.Address = new Uri("https://localhost:5001");
});
```

#### Configure HttpClient

You can also configure your gRPC client's underlying HttpClient. Read the [docs](https://docs.microsoft.com/en-us/aspnet/core/grpc/clientfactory?view=aspnetcore-5.0#configure-httpclient) for more information and examples.

```csharp
services
    .AddGrpcClient<Greeter.GreeterClient>(o =>
    {
        o.Address = new Uri("https://localhost:5001");
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        // configure handler with keep-alive options
        var handler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            };
            
        return handler;
    });
```

#### Configure Logging

Logging can also be configured for your gRPC client. A gRPC client registered with the client factory and resolved from DI will automatically use the app's configured logging. You can find more information and examples on that in the [docs](https://docs.microsoft.com/en-us/aspnet/core/grpc/diagnostics?view=aspnetcore-5.0#grpc-client-logging) that cover gRPC client logging.

#### Configure Channel and Interceptors

You can also configure channels and interceptors during startup. The docs [here](https://docs.microsoft.com/en-us/aspnet/core/grpc/clientfactory?view=aspnetcore-5.0#configure-channel-and-interceptors) have more information and examples.

```csharp
services
    .AddGrpcClient<Greeter.GreeterClient>(o =>
    {
        o.Address = new Uri("https://localhost:5001");
    })
    .AddInterceptor(() => new LoggingInterceptor())
    .ConfigureChannel(o =>
    {
        o.Credentials = new CustomCredentials();
    });
```

### Configuring a gRPC Client Manually

Configuring a gRPC client manualy (when not within an ASP.NET Core application) is relatively simple. Read the [docs](https://docs.microsoft.com/en-us/aspnet/core/grpc/deadlines-cancellation?view=aspnetcore-5.0) for more information or check out the [code examples](https://github.com/itabaiyu/grpc-dotnet-playground/blob/main/src/Grpc.Playground.App/Grpc/GreeterClientConfigurator.cs) in this repository to see it in action.

### Deadlines Cancellation and Call Context Propagation

Deadlines, cancellation, and call context propagation are all important for standing up a reliable gRPC client and should be configured. See the [docs](https://docs.microsoft.com/en-us/aspnet/core/grpc/deadlines-cancellation?view=aspnetcore-5.0) for more information on this topic and code examples.

### Handling Transient Failures

Transient failures are something that will ultimately happen. These can be handled by gRPC clients by configuring retries and hedging. Read more on how these can be configured [here](https://docs.microsoft.com/en-us/aspnet/core/grpc/retries?view=aspnetcore-5.0) as well as general guidance for retries [here](https://docs.microsoft.com/en-us/azure/architecture/best-practices/transient-faults).

## Performance Best Practices

Read the [documentation](https://docs.microsoft.com/en-us/aspnet/core/grpc/performance?view=aspnetcore-5.0) for performance best practices guidance.

## Miscellaneous Links

- [Call gRPC services with the .NET client](https://docs.microsoft.com/en-us/aspnet/core/grpc/client?view=aspnetcore-5.0)
- [Performance Best Practices](https://docs.microsoft.com/en-us/aspnet/core/grpc/performance?view=aspnetcore-5.0)
- [gRPC for .NET Configuration](https://docs.microsoft.com/en-us/aspnet/core/grpc/configuration?view=aspnetcore-5.0)
- [Logging & Diagnostics](https://docs.microsoft.com/en-us/aspnet/core/grpc/diagnostics?view=aspnetcore-5.0)
- [gRPC Client Factory](https://docs.microsoft.com/en-us/aspnet/core/grpc/clientfactory?view=aspnetcore-5.0)
- [Create gRPC services and methods](https://docs.microsoft.com/en-us/aspnet/core/grpc/services?view=aspnetcore-5.0)
