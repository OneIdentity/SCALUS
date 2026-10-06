#!/usr/bin/env bash
#
# Publish the SCALUS payload for one runtime (macOS / Linux).
#
# Produces, under Publish/<config>/<rid>/:
#   scalus            - the NativeAOT command-line launcher (OS protocol hot path)
#   ui/scalus-ui      - the self-contained Photino configuration GUI + webview
#   examples/         - example templates + the SCALUS.json seed + readme
#
# Downstream packagers (scripts/Osx/package.sh, scripts/Linux/package.sh)
# consume this directory. This replaces the old Cake "Publish"/"PublishUi" tasks.
#
# Usage: scripts/publish.sh --runtime osx-x64 [--configuration Release] [--version 2.0.0]
#
# When --version is omitted it defaults to <VersionPrefix> from Directory.Build.props
# (the single checked-in version source), so local publishes match a plain build.
set -euo pipefail

configuration="Release"
version=""
runtime=""

while (( "$#" )); do
    case "$1" in
        --runtime)        runtime="$2";        shift 2 ;;
        --configuration)  configuration="$2";  shift 2 ;;
        --version)        version="$2";        shift 2 ;;
        *) echo "Unknown argument: $1" >&2; exit 1 ;;
    esac
done

if [ -z "$runtime" ]; then
    echo "Error: --runtime is required (e.g. osx-x64, osx-arm64, linux-x64, linux-arm64)" >&2
    exit 1
fi

scriptdir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
rootdir="$(cd "$scriptdir/.." && pwd)"
cd "$rootdir"

if [ -z "$version" ]; then
    version="$(sed -n 's/.*<VersionPrefix>\([^<]*\)<\/VersionPrefix>.*/\1/p' "$rootdir/Directory.Build.props" | head -n1 | tr -d '[:space:]')"
    if [ -z "$version" ]; then
        echo "Error: could not read <VersionPrefix> from Directory.Build.props" >&2
        exit 1
    fi
    echo "==> No --version supplied; using VersionPrefix '$version' from Directory.Build.props"
fi

publishdir="$rootdir/Publish/$configuration/$runtime"
uidir="$publishdir/ui"
exdir="$publishdir/examples"
publish_properties=("-p:Version=$version")

if [ "$runtime" = "linux-arm64" ]; then
    case "$(uname -m)" in
        aarch64|arm64)
            ;;
        *)
            arm64_objcopy="$(command -v aarch64-linux-gnu-objcopy || true)"
            if [ -z "$arm64_objcopy" ]; then
                echo "Error: linux-arm64 cross-publishing requires aarch64-linux-gnu-objcopy (install binutils-aarch64-linux-gnu)." >&2
                exit 1
            fi

            # NativeAOT otherwise selects the host objcopy, which cannot split or
            # strip debug symbols from the generated ARM64 ELF binary.
            publish_properties+=("-p:ObjCopyName=$arm64_objcopy")
            ;;
    esac
fi

echo "==> Publishing SCALUS $version for $runtime ($configuration)"
rm -rf "$publishdir"
mkdir -p "$publishdir"

# 1. NativeAOT command-line launcher. PublishAot=true in the csproj already
#    yields a single native binary, so no PublishSingleFile is needed.
echo "==> Publishing CLI (scalus)"
dotnet publish "src/Cli/Scalus.Cli.csproj" \
    --configuration "$configuration" \
    --runtime "$runtime" \
    --self-contained true \
    "${publish_properties[@]}" \
    --output "$publishdir"

# 2. Photino configuration GUI. Self-contained but NOT single-file/AOT: the
#    apphost needs its native webview .dylib/.so, wwwroot and runtime loose.
echo "==> Publishing UI (scalus-ui)"
dotnet publish "src/Scalus.Ui/Scalus.Ui.csproj" \
    --configuration "$configuration" \
    --runtime "$runtime" \
    --self-contained true \
    -p:Version="$version" \
    --output "$uidir"

# 3. Example templates + seed + readme.
echo "==> Staging examples"
mkdir -p "$exdir"
cp -R scripts/examples/. "$exdir/"
cp src/SCALUS.json "$exdir/SCALUS.json"
cp scripts/readme.txt "$exdir/readme.txt"
sed -i.bak "s/SCALUSVERSION/$version/g" "$exdir/readme.txt" && rm -f "$exdir/readme.txt.bak"

echo "==> Publish complete: $publishdir"
ls -la "$publishdir"
