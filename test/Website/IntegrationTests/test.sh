#! /bin/bash

./set-config.sh

# Sleep for 10 seconds to allow the app to sync the config
sleep 10

for TEST in ./tests/*
	do
		if [ -f $TEST -a -x $TEST ]
		then
			$TEST || exit 1
		fi
done
