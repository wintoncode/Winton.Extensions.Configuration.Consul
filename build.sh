#! /bin/bash

set -e

# Restore packages
dotnet restore

# Build
dotnet build --configuration Release

# Unit Test
dotnet test test/Winton.Extensions.Configuration.Consul.Test/ --no-build --configuration Release

# Integration test
if hash docker 2>/dev/null; then
    ./test/Website/IntegrationTests/run.sh
fi
