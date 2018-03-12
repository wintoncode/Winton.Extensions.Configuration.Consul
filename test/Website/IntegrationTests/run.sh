#! /bin/bash

set -e

pushd $( dirname $0 )

dotnet publish ../ --configuration Release
docker-compose rm -f
docker-compose -f docker-compose.yml -p ci up --build --force-recreate -d
exitCode=$(docker wait ci_test_1)
docker logs -f ci_test_1
docker-compose stop -t 1

popd

exit $exitCode
