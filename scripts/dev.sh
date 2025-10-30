#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

pushd "$ROOT_DIR" >/dev/null

echo "Starting Identity on https://localhost:5000" &
dotnet run --project src/Identity/IdentityProvider.csproj &
ID1=$!

echo "Starting WebApi on http://localhost:5080" &
dotnet run --project src/WebApi/WebApi.csproj &
ID2=$!

echo "Starting Gateway on http://localhost:8080" &
dotnet run --project src/Gateway/Gateway.csproj &
ID3=$!

trap "kill $ID1 $ID2 $ID3" SIGINT SIGTERM EXIT

wait
