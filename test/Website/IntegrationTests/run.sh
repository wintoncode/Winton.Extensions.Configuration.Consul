#! /bin/bash

set -e

pushd $( dirname $0 )

dotnet publish ../ --configuration Release
docker-compose rm -f
docker-compose -p ci up --build --force-recreate -d
docker container ls
exitCode=$(docker wait test)
docker logs -f test
docker-compose stop -t 1

popd

exit $exitCode
