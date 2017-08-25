#! /bin/bash

pushd $( dirname $0 )
dotnet publish --configuration Release
docker-compose rm -f
docker-compose -f docker-compose.yml up --build --force-recreate
popd
