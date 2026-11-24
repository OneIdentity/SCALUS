#!/usr/bin/env bash
#
# Assemble the Linux release artifacts from a published payload.
#
# Produces, under Output/<config>/<rid>/:
#   scalus-<version>-<rid>.tar.gz    - portable tarball + setup.sh (manual install)
#   scalus_<version>-1_<debarch>.deb - Debian/Ubuntu package  (if fpm is available)
#   scalus-<version>-1.<rpmarch>.rpm - RHEL/Fedora/SUSE package (if fpm is available)
#
# The .deb/.rpm install the FHS-native layout:
#   /opt/scalus/{scalus, ui/scalus-ui, examples/}   - the app payload
#   /usr/bin/scalus        -> /opt/scalus/scalus       (symlink; CLI launcher)
#   /usr/bin/scalus-ui     -> /opt/scalus/ui/scalus-ui (symlink; config GUI)
#   /usr/share/applications/scalus-ui.desktop         - app-menu launcher (opens the GUI)
#   /usr/share/icons/hicolor/{256x256,scalable}/apps/scalus.{png,svg}
# Protocol registration stays a per-user runtime action (`scalus register`), which
# writes its own scalus.desktop + xdg-mime associations — the package does not
# pre-register handlers.
#
# Usage: scripts/Linux/package.sh --runtime linux-x64 [--configuration Release] [--version 2.0.0]
# When --version is omitted it defaults to <VersionPrefix> from Directory.Build.props.
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
    echo "Error: --runtime is required (e.g. linux-x64, linux-arm64)" >&2
    exit 1
fi

scriptdir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
rootdir="$(cd "$scriptdir/../.." && pwd)"
cd "$rootdir"

if [ -z "$version" ]; then
    version="$(sed -n 's/.*<VersionPrefix>\([^<]*\)<\/VersionPrefix>.*/\1/p' "$rootdir/Directory.Build.props" | head -n1 | tr -d '[:space:]')"
    if [ -z "$version" ]; then
        echo "Error: could not read <VersionPrefix> from Directory.Build.props" >&2
        exit 1
    fi
fi

publishdir="$rootdir/Publish/$configuration/$runtime"
outputdir="$rootdir/Output/$configuration/$runtime"
assetsdir="$scriptdir/assets"

if [ ! -f "$publishdir/scalus" ]; then
    echo "Error: $publishdir/scalus not found. Run scripts/publish.sh first." >&2
    exit 1
fi
if [ ! -x "$publishdir/ui/scalus-ui" ]; then
    echo "Error: $publishdir/ui/scalus-ui not found. Run scripts/publish.sh first." >&2
    exit 1
fi

mkdir -p "$outputdir"

# ---------------------------------------------------------------------------
# 1. Portable tarball (+ setup.sh) — kept alongside the native packages, mirrors
#    the macOS artifacts (both a .pkg and a .tar.gz ship).
# ---------------------------------------------------------------------------
echo "==> Packaging Linux tarball for $runtime"
cp "$scriptdir/setup.sh" "$publishdir/setup.sh"
chmod +x "$publishdir/setup.sh"

tarball="$outputdir/scalus-$version-$runtime.tar.gz"
rm -f "$tarball"
# Ship runtime files only — exclude NativeAOT/managed debug symbols (*.dbg, *.pdb),
# which are large (the AOT scalus.dbg alone is ~15 MB) and not needed to run.
tar --exclude='*.dbg' --exclude='*.pdb' -czf "$tarball" -C "$publishdir" .
echo "==> Built $tarball"
ls -la "$tarball"

# ---------------------------------------------------------------------------
# 2. Native .deb / .rpm via fpm (skipped with a warning if fpm isn't installed
#    so the tarball path still works on a bare box).
# ---------------------------------------------------------------------------
if ! command -v fpm >/dev/null 2>&1; then
    echo "==> WARNING: fpm not found on PATH; skipping .deb/.rpm."
    echo "    Install with: sudo gem install --no-document fpm   (needs ruby + build tools)"
    exit 0
fi

# Map the .NET RID to Debian and RPM architecture names.
case "$runtime" in
    linux-x64)   debarch="amd64"; rpmarch="x86_64"  ;;
    linux-arm64) debarch="arm64"; rpmarch="aarch64" ;;
    *) echo "Error: unsupported runtime '$runtime' for native packaging" >&2; exit 1 ;;
esac

# Stage the FHS tree that both packages are built from.
stage="$outputdir/stage"
rm -rf "$stage"
optdir="$stage/opt/scalus"
mkdir -p "$optdir" \
         "$stage/usr/bin" \
         "$stage/usr/share/applications" \
         "$stage/usr/share/icons/hicolor/256x256/apps" \
         "$stage/usr/share/icons/hicolor/scalable/apps"

# App payload (same internal layout as Windows/macOS/tarball: scalus + ui/ + examples/).
cp -a "$publishdir/scalus"    "$optdir/scalus"
cp -a "$publishdir/ui"        "$optdir/ui"
cp -a "$publishdir/examples"  "$optdir/examples"
chmod 0755 "$optdir/scalus" "$optdir/ui/scalus-ui"

# Drop debug symbols from the installable payload (kept in Publish/ for triage).
find "$optdir" -type f \( -name '*.dbg' -o -name '*.pdb' \) -delete

# Entry-point symlinks onto PATH (absolute targets so they resolve post-install).
ln -sf /opt/scalus/scalus       "$stage/usr/bin/scalus"
ln -sf /opt/scalus/ui/scalus-ui "$stage/usr/bin/scalus-ui"

# Desktop menu launcher + icons.
install -m 0644 "$assetsdir/scalus-ui.desktop" "$stage/usr/share/applications/scalus-ui.desktop"
install -m 0644 "$assetsdir/scalus.png"        "$stage/usr/share/icons/hicolor/256x256/apps/scalus.png"
install -m 0644 "$assetsdir/scalus.svg"        "$stage/usr/share/icons/hicolor/scalable/apps/scalus.svg"

description="SCALUS - Session Client Application Launch Uri System.
Registers OS handlers for session URI schemes (rdp, ssh, telnet) and launches
the configured native client. Includes a Photino desktop UI for configuration."

# Common fpm arguments shared by both targets.
common_args=(
    -s dir
    -n scalus
    -v "$version"
    --iteration 1
    --license "Apache-2.0"
    --vendor "One Identity"
    --maintainer "One Identity <support@oneidentity.com>"
    --url "https://github.com/petrsnd/SCALUS"
    --description "$description"
    --after-install "$assetsdir/after-install.sh"
    --after-remove  "$assetsdir/after-remove.sh"
    --force
    -C "$stage"
)

echo "==> Building .deb ($debarch)"
deb="$outputdir/scalus_${version}-1_${debarch}.deb"
rm -f "$deb"
fpm "${common_args[@]}" \
    -t deb -a "$debarch" -p "$deb" \
    --depends "libwebkit2gtk-4.1-0" \
    --depends "libgtk-3-0" \
    --depends "libnotify4" \
    opt usr
echo "==> Built $deb"

echo "==> Building .rpm ($rpmarch)"
rpm="$outputdir/scalus-${version}-1.${rpmarch}.rpm"
rm -f "$rpm"
fpm "${common_args[@]}" \
    -t rpm -a "$rpmarch" -p "$rpm" \
    --depends "webkit2gtk4.1" \
    --depends "gtk3" \
    --depends "libnotify" \
    opt usr
echo "==> Built $rpm"

rm -rf "$stage"
echo "==> Linux artifacts:"
ls -la "$outputdir"/scalus*.tar.gz "$outputdir"/scalus*.deb "$outputdir"/scalus*.rpm 2>/dev/null || true
