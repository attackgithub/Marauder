#!/bin/bash

while getopts ":n:p:t:i:j:e:f:d:" opt
 do
  case $opt in
    n ) 
      echo "got n:"$OPTARG
      payload_name=$OPTARG
      ;;
    p )
      echo "got p:"$OPTARG
      password=$OPTARG
      ;;
    t )
      echo "got t:"$OPTARG
      transport=$OPTARG
      ;;
    i )
      echo "got i:"$OPTARG
      interval=$OPTARG
      ;;
    j )
      echo "got j:"$OPTARG
      jitter=$OPTARG
      ;;
    e )
      echo "got d:"$OPTARG
      expiration_date=$OPTARG
      ;;
    f )
      echo "got f:"$OPTARG
      framework=$OPTARG
      ;;
    d )
      echo "got d:"$OPTARG
      debug="True"
      ;;
    \? ) echo "Usage: build.sh -n PAYLOADNAME -p PASSWORD -t BASE64_TRANSPORT -d (switch. enable debugging)"
      ;;
  esac
done
echo -e "{\"PayloadName\":\""$payload_name"\", \"Password\":\""$password"\", \"Transport\":\""$transport"\", \"BeaconInterval\":\""$interval"\", \"Jitter\":\""$jitter"\", \"ExpirationDate\":\""$expiration_date\"", \"Debug\":\""$debug\""}" > ./settings.json

if [ "$debug" = "True" ]; then
  configuration=Debug
else
  configuration=Release
fi
nuget restore
msbuild Marauder.csproj /p:TrimUnusedDependencies=true /t:Build /p:Configuration=$configuration /p:TargetFramework=$framework