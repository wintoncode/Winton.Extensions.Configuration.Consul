./set-config.sh
for TEST in ./tests/*
	do
		if [ -f $TEST -a -x $TEST ]
		then
			$TEST || exit 1
		fi
done
