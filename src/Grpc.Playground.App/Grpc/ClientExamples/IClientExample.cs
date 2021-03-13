using System.Threading.Tasks;
using Grpc.Playground.Api;

namespace Grpc.Playground.App.Grpc.ClientExamples
{
    public interface IClientExample
    {
        Task Run(Greeter.GreeterClient client);
    }
}