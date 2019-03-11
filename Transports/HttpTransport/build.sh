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
mcs -pkg:dotnet -r:/opt/faction/modules/dotnet/Libraries/Faction.Modules.Dotnet.Common.dll /r:./Transports/HttpTransport/packages/htmlagilitypack/1.8.14/lib/Net45/HtmlAgilityPack.dll /r:./Transports/HttpTransport/packages/newtonsoft.json/12.0.1/lib/net35/Newtonsoft.Json.dll -t:library -out:./Transports/HttpTransport/HttpTransport.dll ./Transports/HttpTransport/HttpTransport-build.cs