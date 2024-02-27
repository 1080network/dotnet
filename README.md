Example ping request

### C#

```c#
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
```
