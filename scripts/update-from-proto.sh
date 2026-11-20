#!/bin/bash
# Regenerate the SDK from the output of the "Generate SDKs" workflow in https://github.com/1080network/proto.
#
# Usage: scripts/update-from-proto.sh <version> <generated-dir>
#   <version>        release tag the code was generated from: vX.Y.Z or vX.Y.Z-preN
#   <generated-dir>  the downloaded workflow artifact (the directory containing csharp/)
#
# Download the generated code for a release with:
#   gh run download <run-id> -R 1080network/proto -n generated-sdks-<version>
set -euo pipefail

VERSION=${1:?usage: $0 <version> <generated-dir>}
GEN=${2:?usage: $0 <version> <generated-dir>}
ROOT=$(cd "$(dirname "$0")/.." && pwd)

if [[ ! "$VERSION" =~ ^v([0-9]+\.[0-9]+\.[0-9]+(-pre[0-9]+)?)$ ]]; then
    echo "version '$VERSION' must look like v1.2.3 or v1.2.3-pre0" >&2
    exit 1
fi
NUGET_VERSION=${BASH_REMATCH[1]}   # NuGet versions drop the leading v
if [[ ! -d "$GEN/csharp/Mica/Issuer" ]]; then
    echo "'$GEN' does not look like generated output (expected csharp/Mica/Issuer)" >&2
    exit 1
fi

echo "Laying out $VERSION ($NUGET_VERSION) from $GEN"
# Generated code only: hand-written code (MicaChannels.cs, tests, examples) is left alone.
rm -rf "$ROOT/src/Mica.Common/Generated" "$ROOT/src/Mica.Issuer/Generated" "$ROOT/src/Mica.Acceptor/Generated"
mkdir -p "$ROOT/src/Mica.Common/Generated/Mica" "$ROOT/src/Mica.Issuer/Generated/Mica" "$ROOT/src/Mica.Acceptor/Generated/Mica"
# Common: shared messages, plus validate.proto which the messages import
cp -r "$GEN/csharp/Micashared" "$GEN/csharp/Validate" "$ROOT/src/Mica.Common/Generated/"
cp -r "$GEN/csharp/Mica/Common" "$GEN/csharp/Mica/Member" "$ROOT/src/Mica.Common/Generated/Mica/"
cp -r "$GEN/csharp/Mica/Issuer" "$ROOT/src/Mica.Issuer/Generated/Mica/"
cp -r "$GEN/csharp/Mica/Acceptor" "$ROOT/src/Mica.Acceptor/Generated/Mica/"

sed -i.bak -E "s|<Version>.*</Version>|<Version>$NUGET_VERSION</Version>|" "$ROOT/Directory.Build.props" && rm "$ROOT/Directory.Build.props.bak"

cd "$ROOT"
dotnet build -c Release
dotnet test -c Release --no-build
dotnet pack -c Release --no-build -o "$ROOT/artifacts"
echo "Done: packages in artifacts/ ($NUGET_VERSION)."
