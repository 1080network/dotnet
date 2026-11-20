# Mica SDK for .NET

C# client libraries for the [Mica](https://mica.io) network. Mica's API is [gRPC](https://grpc.io) over mutual TLS;
the messages and clients in this repository are generated from the
[public protos](https://github.com/1080network/proto).

## Which package do I need?

Each role is its own NuGet package, so you only reference the code for the roles you play.

| Package | Use it if you are |
| --- | --- |
| `Mica.Issuer` | an issuer |
| `Mica.Acceptor` | an acceptor |
| `Mica.Common` | everyone: shared messages and the `MicaChannels` helper. Pulled in automatically by the two above |

Targets .NET 8 and runs on later runtimes. Versions match the proto release (`v2.0.0-pre0` is published as `2.0.0-pre0`);
pre-releases need `--prerelease`.

## Installing

The packages are published to the [GitHub Packages NuGet registry](https://github.com/orgs/1080network/packages?ecosystem=nuget).
GitHub requires authentication to read from it, even for public packages: create a personal access token with the
`read:packages` scope and add the source once:

```bash
dotnet nuget add source "https://nuget.pkg.github.com/1080network/index.json" \
    --name mica --username YOUR_GITHUB_USER --password YOUR_TOKEN --store-password-in-clear-text
dotnet add package Mica.Issuer --prerelease    # or Mica.Acceptor
```

## Connecting

Mica uses mutual TLS. You need the root CA, and the client certificate and key Mica issued to you, all as PEM files.
It works on Windows, Linux and macOS.

```csharp
using Mica.Member.Ping.V1;
using Mica.Sdk;

using var channel = MicaChannels.Create(
    "https://api.example.mica.io:443",
    MtlsCredentials.FromFiles("rootca.crt", "client.crt", "client.key"));

var response = await new PingService.PingServiceClient(channel).PingAsync(new PingRequest());
Console.WriteLine(response.Status);
```

The server certificate is verified against the root CA you provide and the host name of the address, and the channel
presents your client certificate. Generated clients are in namespaces such as `Mica.Issuer.Service.V2` and
`Mica.Member.Ping.V1`.

`examples/Ping` is a complete program that connects and calls `Ping`, a good first check that your certificates and
network path work:

```bash
dotnet run --project examples/Ping -- https://api.example.mica.io:443 rootca.crt client.crt client.key
```

## Repository layout

| Path | Contents |
| --- | --- |
| `src/Mica.Common`, `src/Mica.Issuer`, `src/Mica.Acceptor` | one project and package each; `Generated/` inside each is **generated** |
| `src/Mica.Common/MicaChannels.cs` | hand-written mutual TLS helper |
| `tests/` | tests, including a real mutual TLS call to an in-process server |
| `examples/` | runnable examples |
| `scripts/update-from-proto.sh` | regenerates the SDK for a proto release, builds, tests and packs |

## Releasing (maintainers)

1. Run the **Generate SDKs** workflow in [1080network/proto](https://github.com/1080network/proto/actions) for the release tag.
2. Download its output: `gh run download <run-id> -R 1080network/proto -n generated-sdks-<version>`.
3. Run `scripts/update-from-proto.sh <version> <download-dir>`. It lays the generated code into the projects, sets the version, builds, tests and packs.
4. Open a pull request with the changes and merge it.
5. Publish a GitHub release whose tag is the version (for example `v2.0.0-pre0`). The **Publish packages** workflow packs and pushes all three packages to GitHub Packages.
