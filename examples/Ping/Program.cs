// Connects to Mica with mutual TLS and calls Ping, a good first check that your certificates and network path work.
//
//   dotnet run --project examples/Ping -- https://api.example.mica.io:443 rootca.crt client.crt client.key
using Mica.Member.Ping.V1;
using Mica.Sdk;

if (args.Length != 4)
{
    Console.Error.WriteLine("usage: Ping <https://host:port> <rootca.crt> <client.crt> <client.key>");
    return 1;
}

using var channel = MicaChannels.Create(args[0], MtlsCredentials.FromFiles(args[1], args[2], args[3]));
var response = await new PingService.PingServiceClient(channel).PingAsync(new PingRequest(), deadline: DateTime.UtcNow.AddSeconds(15));
Console.WriteLine($"status={response.Status} service={response.ServiceType} version={response.BuildVersion}");
return 0;
