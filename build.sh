#! /bin/bash

set -e

# Build
dotnet build src/Winton.Extensions.Configuration.Consul --configuration Release --framework netstandard2.0
dotnet build test/Winton.Extensions.Configuration.Consul.Test --configuration Release --framework netcoreapp2.0
dotnet build test/Website --configuration Release --framework netcoreapp2.0

# Unit Test
dotnet test test/Winton.Extensions.Configuration.Consul.Test/ --no-build --no-restore --configuration Release

# Integration test
if hash docker 2>/dev/null; then
    ./test/Website/IntegrationTests/run.sh
fi
