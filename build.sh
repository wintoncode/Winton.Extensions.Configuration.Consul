#! /bin/bash

set -e

# Build
dotnet restore
dotnet build src/Winton.Extensions.Configuration.Consul -c Release -f netstandard2.0 --no-restore
dotnet build test/Winton.Extensions.Configuration.Consul.Test -c Release -f netcoreapp3.0 --no-restore
dotnet build test/Website -c Release -f netcoreapp3.0 --no-restore

# Unit Test
dotnet test -c Release --no-build

# Integration test
if hash docker 2>/dev/null; then
    ./test/Website/IntegrationTests/run.sh
fi
