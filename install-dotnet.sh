#!/usr/bin/env bash
repoFolder="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd $repoFolder

koreBuildZip="https://github.com/aspnet/KoreBuild/archive/dev.zip"
if [ ! -z $KOREBUILD_ZIP ]; then
    koreBuildZip=$KOREBUILD_ZIP
fi

buildFolder=".build"
buildFile="$buildFolder/KoreBuild.sh"
dotnetInstallFile="$buildFolder/dotnet/dotnet-install.sh"

if test ! -d $buildFolder; then
    echo "Downloading KoreBuild from $koreBuildZip"
    
    tempFolder="/tmp/KoreBuild-$(uuidgen)"    
    mkdir $tempFolder
    
    localZipFile="$tempFolder/korebuild.zip"
    
    retries=6
    until (wget -O $localZipFile $koreBuildZip 2>/dev/null || curl -o $localZipFile --location $koreBuildZip 2>/dev/null)
    do
        echo "Failed to download '$koreBuildZip'"
        if [ "$retries" -le 0 ]; then
            exit 1
        fi
        retries=$((retries - 1))
        echo "Waiting 10 seconds before retrying. Retries left: $retries"
        sleep 10s
    done
    
    unzip -q -d $tempFolder $localZipFile
  
    mkdir $buildFolder
    cp -r $tempFolder/**/build/** $buildFolder
    
    chmod +x $buildFile
    
    # Cleanup
    if test ! -d $tempFolder; then
        rm -rf $tempFolder
    fi
fi

# We just use KoreBuild to install the dotnet cli
$dotnetInstallFile

# Then we do a custom build pipeline that include versioning
dotnet restore
cd src/AspNetCore.Configuration.Consul
dotnet gitversion
cd ../../
dotnet build src/*/project.json test/*/project.json --configuration Release
dotnet test --no-build --configuration Release -f netcoreapp1.0 test/*/project.json
dotnet pack --no-build src/*/project.json --configuration Release
nuget push src/AspNetCore.Configuration.Consul/bin/Release/*.nupkg -ApiKey 40a11e65-c7f8-47cb-bfb9-c86ccd5e8234 -NonInteractive -Verbosity Detailed