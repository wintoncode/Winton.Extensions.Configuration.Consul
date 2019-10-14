#! /bin/bash

curl -v -X PUT --header "Content-Type:application/json" -d @appsettings.json http://consul:8500/v1/kv/appsettings.json
