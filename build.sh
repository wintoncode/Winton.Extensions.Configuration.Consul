#! /bin/bash

set -e

# Build
dotnet restore
dotnet build src/Winton.Extensions.Configuration.Consul --no-restore --configuration Release --framework netstandard2.0
dotnet build test/Winton.Extensions.Configuration.Consul.Test --no-restore --configuration Release --framework netcoreapp2.2
dotnet build test/Website --no-restore --configuration Release --framework netcoreapp2.2

# Unit Test
dotnet test test/Winton.Extensions.Configuration.Consul.Test/ --no-build --no-restore --configuration Release

# Integration test
if hash docker 2>/dev/null; then
    ./test/Website/IntegrationTests/run.sh
fi
