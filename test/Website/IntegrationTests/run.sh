workDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
dotnet publish $workDir/.. --configuration Release
(cd $workDir && docker-compose rm -f)
docker-compose -f $workDir/docker-compose.yml -p ci up --build --force-recreate -d
exitCode=$(docker wait ci_test_1)
docker logs -f ci_test_1
(cd $workDir && docker-compose stop -t 1)
exit $exitCode