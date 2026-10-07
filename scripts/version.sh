#!/usr/bin/env bash
#
# Derive the SCALUS build version from the git ref + the checked-in version source,
# and publish it to the pipeline as build variables. Used by the Linux/macOS CI jobs
# (scripts/version.ps1 is the identical logic for the Windows job).
#
# See scripts/version.ps1 for the full contract. In short:
#   Tagged release build (ref = refs/tags/vX.Y.Z):
#       Version = X.Y.Z, IsPrerelease=false; the tag MUST match <VersionPrefix> or the
#       build FAILS (drift guard).
#   Trunk / PR / manual build (any non-tag ref):
#       Version = X.Y.Z.<BuildCounter>, IsPrerelease=true (incrementing product version).
#
# Emits pipeline variables: Version, IsPrerelease, IsTagBuild, ReleaseTag.
#
# Usage: scripts/version.sh [--source-branch <ref>] [--build-counter <n>]
#   SourceBranch defaults from BUILD_SOURCEBRANCH. BuildCounter is passed from
#   the pipeline's counter expression.
set -euo pipefail

source_branch="${BUILD_SOURCEBRANCH:-}"
build_counter="${BUILDCOUNTER:-}"

while (( "$#" )); do
    case "$1" in
        --source-branch) source_branch="$2"; shift 2 ;;
        --build-counter) build_counter="$2"; shift 2 ;;
        *) echo "Unknown argument: $1" >&2; exit 1 ;;
    esac
done

scriptdir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
rootdir="$(cd "$scriptdir/.." && pwd)"
propsfile="$rootdir/Directory.Build.props"

# 1. Read the checked-in base version (the single source of truth).
base="$(sed -n 's/.*<VersionPrefix>\([^<]*\)<\/VersionPrefix>.*/\1/p' "$propsfile" | head -n1 | tr -d '[:space:]')"
if [ -z "$base" ]; then
    echo "Error: could not read <VersionPrefix> from $propsfile" >&2
    exit 1
fi
if ! [[ "$base" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Error: VersionPrefix '$base' must be a 3-part version (X.Y.Z)." >&2
    exit 1
fi

# 2. Decide the build kind from the ref.
is_tag_build="false"
is_prerelease="true"
release_tag=""
version=""

if [[ "$source_branch" == refs/tags/* ]]; then
    tag="${source_branch#refs/tags/}"
    if ! [[ "$tag" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
        echo "Error: release tag '$tag' must be of the form vX.Y.Z (e.g. v2.0.0)." >&2
        exit 1
    fi
    tag_version="${tag#v}"

    # Drift guard: a release tag must match the checked-in base exactly.
    if [ "$tag_version" != "$base" ]; then
        echo "Error: release tag '$tag' (=$tag_version) does not match <VersionPrefix> '$base' in Directory.Build.props." >&2
        echo "       Bump VersionPrefix to $tag_version in the same commit you tag, then re-tag." >&2
        exit 1
    fi

    is_tag_build="true"
    is_prerelease="false"
    release_tag="$tag"
    version="$base"
else
    # Trunk / PR / manual: incrementing 4-part numeric, marked prerelease.
    [ -z "$build_counter" ] && build_counter="0"
    if ! [[ "$build_counter" =~ ^[0-9]+$ ]] || (( 10#$build_counter > 65535 )); then
        echo "Error: BuildCounter '$build_counter' must be an integer from 0 through 65535." >&2
        exit 1
    fi

    version="$base.$build_counter"
fi

# 3. Report + publish as pipeline variables.
echo "SourceBranch : $source_branch"
echo "BuildCounter : $build_counter"
echo "VersionPrefix: $base"
echo "Version      : $version"
echo "IsTagBuild   : $is_tag_build"
echo "IsPrerelease : $is_prerelease"
echo "ReleaseTag   : $release_tag"

echo "##vso[task.setvariable variable=Version]$version"
echo "##vso[task.setvariable variable=IsPrerelease]$is_prerelease"
echo "##vso[task.setvariable variable=IsTagBuild]$is_tag_build"
echo "##vso[task.setvariable variable=ReleaseTag]$release_tag"

# Also surface it as the build number for easy identification in the DevOps UI.
echo "##vso[build.updatebuildnumber]$version"
