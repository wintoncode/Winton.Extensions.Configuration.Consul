workDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
dotnet publish $workDir --configuration Release
(cd $workDir && docker-compose rm -f)
docker-compose -f $workDir/docker-compose.yml up --build --force-recreate
