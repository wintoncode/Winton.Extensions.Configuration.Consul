set -e

while getopts v option
do
    case "${option}"
    in
            v) VERSION_AND_PUBLISH="true";;
    esac
done

# Restore packages
dotnet restore

# Set version numbers
if [[ $VERSION_AND_PUBLISH ]]; then
    echo "Versioning..."
    (cd src/Winton.Extensions.Configuration.Consul && dotnet gitversion)
else
    echo "WARN: Skipping versioning."
fi

# Build
dotnet build src/Winton.Extensions.Configuration.Consul/project.json test/Winton.Extensions.Configuration.Consul.Test/project.json --configuration Release

# Unit Test
dotnet test --no-build --configuration Release -f netcoreapp1.0 test/Winton.Extensions.Configuration.Consul.Test/project.json

# Integration test
if hash docker 2>/dev/null; then
    ./test/Website/IntegrationTests/run.sh
fi

# Package
dotnet pack --no-build src/*/project.json --configuration Release