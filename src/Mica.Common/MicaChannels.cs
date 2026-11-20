using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Grpc.Net.Client;

namespace Mica.Sdk;

/// <summary>
/// PEM material for a mutual TLS connection to Mica.
/// </summary>
/// <param name="CertificateAuthorities">Root certificate(s) used to verify Mica's server certificate.</param>
/// <param name="Certificate">The client certificate (chain) issued by Mica.</param>
/// <param name="Key">The private key for <paramref name="Certificate"/>.</param>
public sealed record MtlsCredentials(string CertificateAuthorities, string Certificate, string Key)
{
    /// <summary>Reads the credentials from PEM files on disk.</summary>
    public static MtlsCredentials FromFiles(string certificateAuthoritiesFile, string certificateFile, string keyFile) =>
        new(File.ReadAllText(certificateAuthoritiesFile), File.ReadAllText(certificateFile), File.ReadAllText(keyFile));
}

/// <summary>Builds gRPC channels to the Mica network. Every connection to Mica uses mutual TLS.</summary>
public static class MicaChannels
{
    /// <summary>
    /// Opens a channel to <paramref name="address"/> (for example <c>https://api.example.mica.io:443</c>) that presents
    /// the client certificate and verifies the server against the given certificate authorities.
    /// The caller owns the channel and should dispose it when done.
    /// </summary>
    public static GrpcChannel Create(string address, MtlsCredentials credentials)
    {
        // Round-trip through PKCS#12 so the private key is usable by the TLS stack on every platform
        // (a key loaded straight from PEM is ephemeral, which Windows' TLS implementation rejects).
        using var fromPem = X509Certificate2.CreateFromPem(credentials.Certificate, credentials.Key);
        var clientCertificate = new X509Certificate2(fromPem.Export(X509ContentType.Pkcs12));

        var authorities = new X509Certificate2Collection();
        authorities.ImportFromPem(credentials.CertificateAuthorities);
        if (authorities.Count == 0)
            throw new ArgumentException("No certificates found in the certificate authorities PEM.", nameof(credentials));

        // The channel is bound to one address, so check the server certificate against that host name ourselves.
        // (The platform's name-mismatch flag can't be relied on once the trust anchor is replaced: on macOS a chain
        // the system doesn't trust is reported as a name mismatch as well.)
        var host = new Uri(address).Host;

        var handler = new SocketsHttpHandler();
        handler.SslOptions.ClientCertificates = new X509CertificateCollection { clientCertificate };
        handler.SslOptions.RemoteCertificateValidationCallback = (_, serverCertificate, _, _) =>
            serverCertificate is not null
            && new X509Certificate2(serverCertificate).MatchesHostname(host)
            && TrustedBy(authorities, serverCertificate);

        return GrpcChannel.ForAddress(address, new GrpcChannelOptions { HttpHandler = handler, DisposeHttpClient = true });
    }

    private static bool TrustedBy(X509Certificate2Collection authorities, X509Certificate serverCertificate)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.AddRange(authorities);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        return chain.Build(new X509Certificate2(serverCertificate));
    }
}
