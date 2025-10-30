#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

# Run BenchmarkDotNet suites quickly with joiner; keep artifacts out of repo
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$proj = Join-Path $root 'BenchmarkSuite1/BenchmarkSuite1.csproj'

if (-not (Test-Path $proj)) {
 Write-Error "Benchmark project not found: $proj"
 exit1
}

dotnet build $proj -c Release | Out-Null
# Run only net8.0 runtime as the project targets net8.0; adjust if multi-targeted later
& dotnet run --project $proj -c Release -- --join --runtimes net8.0
