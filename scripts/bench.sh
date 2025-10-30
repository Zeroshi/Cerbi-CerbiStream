#!/usr/bin/env bash
set -euo pipefail

# Run BenchmarkDotNet suites quickly with joiner; keep artifacts out of repo
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
P="${ROOT_DIR}/BenchmarkSuite1/BenchmarkSuite1.csproj"

if [ ! -f "$P" ]; then
 echo "Benchmark project not found: $P" >&2
 exit1
fi

dotnet build "$P" -c Release >/dev/null
# Run only net8.0 runtime as the project targets net8.0; adjust if multi-targeted later
exec dotnet run --project "$P" -c Release -- --join --runtimes net8.0
