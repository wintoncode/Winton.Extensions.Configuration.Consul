#! /bin/bash

if curl -X GET --header "Accept:application/json" http://website:5000/config/string | grep -q 'Hello'; then
  echo `basename "$0"` "passed!"
  exit 0
else
  echo `basename "$0"` "failed!"
  exit 1
fi
