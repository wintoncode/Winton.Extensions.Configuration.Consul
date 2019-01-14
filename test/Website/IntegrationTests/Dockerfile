FROM ubuntu:trusty

RUN apt-get update && apt-get install -yq curl && apt-get clean

WORKDIR /app

ADD wait-for-it/wait-for-it.sh /app/
ADD appsettings.json /app/
ADD set-config.sh /app/
ADD test.sh /app/
ADD tests /app/tests
