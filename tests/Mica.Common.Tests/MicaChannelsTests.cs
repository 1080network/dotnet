using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Grpc.Core;
using Mica.Member.Ping.V1;
using Mica.Sdk;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Mica.Common.Tests;

/// <summary>Makes real mutual TLS Ping calls to an in-process server, using certificates generated for the test.</summary>
public class MicaChannelsTests
{
    private sealed class Identity
    {
        public X509Certificate2 Certificate { get; }
        public string CertificatePem => Certificate.ExportCertificatePem();
        public string KeyPem { get; }

        public Identity(string name, Identity? ca = null)
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(ca is null, false, 0, true));
            var san = new SubjectAlternativeNameBuilder();
            san.AddIpAddress(IPAddress.Loopback);
            san.AddDnsName("localhost");
            request.CertificateExtensions.Add(san.Build());
            var from = DateTimeOffset.UtcNow.AddMinutes(-1);
            var to = DateTimeOffset.UtcNow.AddHours(ca is null ? 2 : 1); // a certificate cannot outlive its issuer
            var cert = ca is null
                ? request.CreateSelfSigned(from, to)
                : request.Create(ca.Certificate, from, to, RandomNumberGenerator.GetBytes(8));
            // Attach the private key so the certificate can be used for TLS
            Certificate = ca is null ? cert : cert.CopyWithPrivateKey(key);
            KeyPem = key.ExportPkcs8PrivateKeyPem();
        }
    }

    private sealed class PingImpl : PingService.PingServiceBase
    {
        public override Task<PingResponse> Ping(PingRequest request, ServerCallContext context) =>
            Task.FromResult(new PingResponse { Status = PingResponse.Types.Status.Success, ServiceType = "test" });
    }

    private static async Task<(WebApplication App, string Address)> StartServer(Identity ca, Identity server)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddGrpc();
        var serverCertificate = new X509Certificate2(server.Certificate.Export(X509ContentType.Pkcs12));
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0, listen =>
        {
            listen.Protocols = HttpProtocols.Http2;
            listen.UseHttps(https =>
            {
                https.ServerCertificate = serverCertificate;
                https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                https.ClientCertificateValidation = (clientCertificate, _, _) =>
                {
                    using var chain = new System.Security.Cryptography.X509Certificates.X509Chain();
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.Add(ca.Certificate);
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    return chain.Build(clientCertificate);
                };
            });
        }));
        var app = builder.Build();
        app.MapGrpcService<PingImpl>();
        await app.StartAsync();
        var address = app.Urls.First().Replace("[::1]", "127.0.0.1");
        return (app, address);
    }

    [Fact]
    public async Task PingSucceedsOverMutualTlsAndAServerFromAnUnknownAuthorityIsRejected()
    {
        var ca = new Identity("test ca");
        var (app, address) = await StartServer(ca, new Identity("server", ca));
        await using var _ = app;
        var client = new Identity("client", ca);

        using (var channel = MicaChannels.Create(address, new MtlsCredentials(ca.CertificatePem, client.CertificatePem, client.KeyPem)))
        {
            var response = await new PingService.PingServiceClient(channel).PingAsync(new PingRequest(), deadline: DateTime.UtcNow.AddSeconds(10));
            Assert.Equal(PingResponse.Types.Status.Success, response.Status);
        }

        var otherCa = new Identity("other ca");
        using var untrusting = MicaChannels.Create(address, new MtlsCredentials(otherCa.CertificatePem, client.CertificatePem, client.KeyPem));
        await Assert.ThrowsAsync<RpcException>(async () =>
            await new PingService.PingServiceClient(untrusting).PingAsync(new PingRequest(), deadline: DateTime.UtcNow.AddSeconds(10)));
    }

    [Fact]
    public void EmptyAuthoritiesAreRejected()
    {
        var ca = new Identity("test ca");
        var client = new Identity("client", ca);
        Assert.Throws<ArgumentException>(() =>
            MicaChannels.Create("https://127.0.0.1:1", new MtlsCredentials("", client.CertificatePem, client.KeyPem)));
    }
}
