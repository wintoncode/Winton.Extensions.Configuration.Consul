#! /bin/bash
set -e

# Restore packages
dotnet restore

# Build
dotnet build src/Winton.Extensions.Configuration.Consul --configuration Release --framework netstandard1.3
dotnet build test/Winton.Extensions.Configuration.Consul.Test --configuration Release --framework netcoreapp2.0
dotnet build test/Website --configuration Release --framework netcoreapp1.1

# Unit Test
dotnet test test/Winton.Extensions.Configuration.Consul.Test/ --no-build --configuration Release

# Integration test
if hash docker 2>/dev/null; then
    ./test/Website/IntegrationTests/run.sh
fi
