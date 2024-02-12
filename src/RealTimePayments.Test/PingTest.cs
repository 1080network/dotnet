using System.Security.Cryptography.X509Certificates;
using Grpc.Core;
using Grpc.Net.Client;
using Mica.Partner.Administration.V1;
using Mica.Partner.Service.V1;
using Micashared.Common.Ping.V1;

namespace RealTimePayments.Test;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Ping()
    {
        var trustCertPath = "<certpath>/quickstart-lj5bgu/you_to_network/mica_trust.pem";
        var certificatePath = "<certpath>/quickstart-lj5bgu/you_to_network/you_to_network.pem";
        var keyPath = "<certpath>/quickstart-lj5bgu/you_to_network/you_to_network.key";
        
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(new X509Certificate2(trustCertPath));
        handler.ClientCertificates.Add(X509Certificate2.CreateFromPemFile(certificatePath, keyPath));
        handler.ServerCertificateCustomValidationCallback =   
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;  
        var channel = GrpcChannel.ForAddress("https://localhost:13400", new GrpcChannelOptions
        {
            HttpHandler = handler
        });
        var client = new PartnerToMicaService.PartnerToMicaServiceClient(channel);

        var response = await client.PingAsync(
            new PingRequest());

        Console.WriteLine(response.Status);
        Assert.That(response.Status, Is.EqualTo(PingResponse.Types.Status.Success));
    }
}