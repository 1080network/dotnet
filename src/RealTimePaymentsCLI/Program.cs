// See https://aka.ms/new-console-template for more information
using Grpc.Net.Client;
using Mica.Partner.Partner.V1;
using Mica.Partner.Service.V1;
using Micashared.Common.Ping.V1;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

internal abstract class RealTimePaymentsCli
{

    public static int Main(string[] args)
    {
         if (args.Length < 5) {
            Console.WriteLine("Usage: ./executable [rooca.pem] [intermediate.pem] [client.pfx] [pfx password] <addr>.");
            return 1;
        }

        var handler = new SocketsHttpHandler();

        var rootCert = X509Certificate2.CreateFromPem(File.ReadAllText(args[0]));
        rootCert = new X509Certificate2(rootCert.Export(X509ContentType.Pfx));

        var intermediateCert = X509Certificate2.CreateFromPem(File.ReadAllText(args[1]));
        intermediateCert = new X509Certificate2(intermediateCert.Export(X509ContentType.Pfx));

        // A pfx file is just a file that contains a cert and a key protected by a password, useful for Windows. 
        // openssl pkcs12 -export -out externalclient.pfx -inkey externalclient.key -in externalclient.crt
        var clientCert = new X509Certificate2(args[2], args[3]);
        //clientCert = new X509Certificate2(clientCert.Export(X509ContentType.Pfx));
 
        var trust = SslCertificateTrust.CreateForX509Collection(new X509Certificate2Collection() { rootCert }, false);
        handler.SslOptions.ClientCertificateContext = SslStreamCertificateContext.Create(clientCert, new X509Certificate2Collection() { intermediateCert }, false, trust);

        var channel = GrpcChannel.ForAddress(args[4], new GrpcChannelOptions
        {
            HttpClient = new HttpClient(handler),
        });
        var client = new PartnerToMicaService.PartnerToMicaServiceClient(channel);

        var response = client.Ping(new PingRequest());

        Console.WriteLine(response);
        return 0;
    }

}