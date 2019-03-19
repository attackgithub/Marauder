#!/bin/bash

rm ./Transports/HttpTransport/HttpTransport-build.cs
cp ./Transports/HttpTransport/HttpTransport.cs ./Transports/HttpTransport/HttpTransport-build.cs

while getopts ":c:" opt
 do
  case $opt in
    c )
      config=$(echo -ne $OPTARG | base64 -d);
      ;;
    \? ) echo "Usage: build.sh -c b64CONFIG"
      ;;
  esac
done

sed -i "s|CONFIG|$config|g" ./Transports/HttpTransport/HttpTransport-build.cs

msbuild /t:Restore ./Transports/HttpTransport/httptransport.csproj /p:RestorePackagesPath=./Transports/HttpTransport/packages
msbuild ./Transports/HttpTransport/httptransport.csproj /p:Configuration=Debug;TargetFrameworkVersion=v3.5
mv ./Transports/HttpTransport/bin/Debug/net35/httptransport.dll ./Transports/HttpTransport/HttpTransport.dll