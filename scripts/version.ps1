#!/usr/bin/env pwsh
#
# Derive the SCALUS build version from the git ref + the checked-in version source,
# and publish it to the pipeline as build variables. Used by the Windows CI job
# (scripts/version.sh is the identical logic for the Linux/macOS jobs).
#
# Single source of truth: <VersionPrefix> in Directory.Build.props. This script never
# invents a base version - it only decides the 4th component and the prerelease flag:
#
#   Tagged release build  (ref = refs/tags/vX.Y.Z)
#       -> Version    = X.Y.Z            (clean 3-part semver)
#       -> IsPrerelease = false
#       The tag MUST match <VersionPrefix> exactly, or the build FAILS (drift guard):
#       a release can never ship mislabeled relative to the tree it was built from.
#
#   Trunk / PR / manual build (any non-tag ref)
#       -> Version    = X.Y.Z.<BuildCounter>  (incrementing product version)
#       -> IsPrerelease = true
#
# Package versions are ALWAYS purely numeric (WiX/fpm reject semver pre-release
# suffixes); "prerelease" is conveyed downstream by the IsPrerelease flag + the
# GitHub release marking, not by a "-pre" suffix in the version string. Azure's
# pipeline counter is used instead of Build.BuildId so the fourth component
# remains valid for CLR assembly and Windows file versions.
#
# Emits these pipeline variables (via ##vso logging commands):
#   Version        - the full version to pass to publish (-Version / -p:Version)
#   IsPrerelease   - "true" | "false"
#   IsTagBuild     - "true" | "false"
#   ReleaseTag     - the tag (e.g. v2.0.0) on a tag build, else empty
#
# Usage: scripts/version.ps1 [-SourceBranch <ref>] [-BuildCounter <n>]
#   SourceBranch defaults from BUILD_SOURCEBRANCH. BuildCounter is passed from
#   the pipeline's counter expression.
[CmdletBinding()]
param(
    [string]$SourceBranch = $env:BUILD_SOURCEBRANCH,
    [string]$BuildCounter = $env:BUILDCOUNTER
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = (Resolve-Path (Join-Path $scriptDir "..")).Path
$propsFile = Join-Path $rootDir "Directory.Build.props"

# 1. Read the checked-in base version (the single source of truth).
$match = Select-String -Path $propsFile -Pattern '<VersionPrefix>([^<]+)</VersionPrefix>' | Select-Object -First 1
if (-not $match) { throw "Could not read <VersionPrefix> from $propsFile" }
$base = $match.Matches[0].Groups[1].Value.Trim()
if ($base -notmatch '^\d+\.\d+\.\d+$') {
    throw "VersionPrefix '$base' in Directory.Build.props must be a 3-part version (X.Y.Z)."
}

# 2. Decide the build kind from the ref.
$isTagBuild = $false
$isPrerelease = $true
$releaseTag = ""
$version = ""

if ($SourceBranch -like "refs/tags/*") {
    $tag = $SourceBranch.Substring("refs/tags/".Length)
    if ($tag -notmatch '^v\d+\.\d+\.\d+$') {
        throw "Release tag '$tag' must be of the form vX.Y.Z (e.g. v2.0.0)."
    }
    $tagVersion = $tag.Substring(1)

    # Drift guard: a release tag must match the checked-in base exactly.
    if ($tagVersion -ne $base) {
        throw "Release tag '$tag' (=$tagVersion) does not match <VersionPrefix> '$base' in Directory.Build.props. " +
              "Bump VersionPrefix to $tagVersion in the same commit you tag, then re-tag."
    }

    $isTagBuild = $true
    $isPrerelease = $false
    $releaseTag = $tag
    $version = $base
}
else {
    # Trunk / PR / manual: incrementing 4-part numeric, marked prerelease.
    if ([string]::IsNullOrWhiteSpace($BuildCounter)) { $BuildCounter = "0" }
    if ($BuildCounter -notmatch '^\d+$' -or [int64]$BuildCounter -gt 65535) {
        throw "BuildCounter '$BuildCounter' must be an integer from 0 through 65535."
    }

    $version = "$base.$BuildCounter"
}

# 3. Report + publish as pipeline variables.
Write-Host "SourceBranch : $SourceBranch"
Write-Host "BuildCounter : $BuildCounter"
Write-Host "VersionPrefix: $base"
Write-Host "Version      : $version"
Write-Host "IsTagBuild   : $isTagBuild"
Write-Host "IsPrerelease : $isPrerelease"
Write-Host "ReleaseTag   : $releaseTag"

Write-Host "##vso[task.setvariable variable=Version]$version"
Write-Host "##vso[task.setvariable variable=IsPrerelease]$($isPrerelease.ToString().ToLowerInvariant())"
Write-Host "##vso[task.setvariable variable=IsTagBuild]$($isTagBuild.ToString().ToLowerInvariant())"
Write-Host "##vso[task.setvariable variable=ReleaseTag]$releaseTag"

# Also surface it as the build number for easy identification in the DevOps UI.
Write-Host "##vso[build.updatebuildnumber]$version"
