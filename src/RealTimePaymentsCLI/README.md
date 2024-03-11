﻿// See https://aka.ms/new-console-template for more information

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Grpc.Net.Client;
using Mica.Partner.Partner.V1;
using Mica.Partner.Service.V1;

namespace RealTimePaymentsCLI;

internal abstract class RealTimePaymentsCli
{
    // Add intermediate and root cert to /usr/share/ca-certificates/extra on Ubuntu
    // Run sudo dpkg-reconfigure ca-certificates 
    // This is needed for the dotnet mtls client to include the intermediate and root cert in the initial request. 
    public static int Main2(string[] args)
    {
        if (args.Length < 3) {
            Console.WriteLine("Usage: ./executable pfx [pfx password] <addr>.");
            return 1;
        }
        var handler = new HttpClientHandler();
        
        // A pfx file is just a file that contains a cert and a key protected by a password, useful for Windows. 
        // openssl pkcs12 -export -out externalclient.pfx -inkey externalclient.key -in externalclient.crt
        var clientPfxCert = new X509Certificate2(args[0], args[1]);
        handler.ClientCertificates.Add(clientPfxCert);
        handler.ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true;
        
        // We can also just read in pem files.
        /*
        var certificatePath = "/home/user/proj/dotnet/foo.crt";
        var keyPath = "/home/user/proj/dotnet/foo.key";
        handler.ClientCertificates.Add(X509Certificate2.CreateFromPemFile(certificatePath, keyPath));
        */
        var channel = GrpcChannel.ForAddress(args[2], new GrpcChannelOptions
        {
            HttpHandler = handler
        });
        var client = new PartnerToMicaService.PartnerToMicaServiceClient(channel);

        var response = client.GetPartner(new GetPartnerRequest());

        Console.WriteLine(response);
        return 0;
    }
      public static int Main(string[] args)
    {
        var handler = new SocketsHttpHandler();
        
        // A pfx file is just a file that contains a cert and a key protected by a password, useful for Windows. 
        // openssl pkcs12 -export -out externalclient.pfx -inkey externalclient.key -in externalclient.crt
        var clientCert = X509Certificate2.CreateFromPemFile("/home/user/proj/certs/hron3n.crt", "/home/user/proj/certs/hron3n.key");
        var intermediateCert = X509Certificate2.CreateFromPem(File.ReadAllText("/home/user/proj/certs/intermediate.crt"));
        var rootCert = X509Certificate2.CreateFromPem(File.ReadAllText("/etc/ssl/mica-certs/vault_rootca.crt"));

        var trust = SslCertificateTrust.CreateForX509Collection(new X509Certificate2Collection() { rootCert }, false);
        handler.SslOptions.ClientCertificateContext = SslStreamCertificateContext.Create(clientCert, 
            new X509Certificate2Collection() { intermediateCert }, false, trust);
        
        var channel = GrpcChannel.ForAddress("https://api.hron3n.members.mica.io:443", new GrpcChannelOptions
        {
            HttpClient = new HttpClient(handler),
        });
        var client = new PartnerToMicaService.PartnerToMicaServiceClient(channel);

        var response = client.GetPartner(new GetPartnerRequest());

        Console.WriteLine(response);
        return 0;
    }
}