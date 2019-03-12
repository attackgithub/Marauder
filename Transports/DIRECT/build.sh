#!/bin/bash

nuget restore ./Transports/DIRECT/packages.config -PackagesDirectory ./Transports/DIRECT/packages

rm ./Transports/DIRECT/DIRECT-build.cs
rm ./Transports/DIRECT.dll
cp ./Transports/DIRECT/DIRECT.cs ./Transports/DIRECT/DIRECT-build.cs

while getopts ":u:n:s:" opt
 do
  case $opt in
    u ) 
      echo "got n:"$OPTARG >> ./Transports/DIRECT/output.txt
      url=$(echo -ne $OPTARG | base64 -d);
      echo "converted to: $url" >> ./Transports/DIRECT/output.txt
      ;;
    n ) 
      echo "got n:"$OPTARG >> ./Transports/DIRECT/output.txt
      key_name=$(echo -ne $OPTARG | base64 -d);
      echo "converted to: $key_name" >> ./Transports/DIRECT/output.txt
      ;;
    s )
      echo "got s:"$OPTARG >> ./Transports/DIRECT/output.txt
      secret=$(echo -ne $OPTARG | base64 -d);
      echo "converted to: $secret" >> ./Transports/DIRECT/output.txt
      ;;
    \? ) echo "Usage: build.sh -n KEYNAME -p SECRET"
      ;;
  esac
done

sed -i "s|APIURL|$url|g" ./Transports/DIRECT/DIRECT-build.cs
sed -i "s/KEYNAME/$key_name/g" ./Transports/DIRECT/DIRECT-build.cs
sed -i "s/SECRET/$secret/g" ./Transports/DIRECT/DIRECT-build.cs

mcs -pkg:dotnet -t:library -r:./Transports/DIRECT/packages/Newtonsoft.Json.12.0.1/lib/net35/Newtonsoft.Json.dll -r:./Transports/DIRECT/packages/Faction.Modules.Dotnet.Common.20190309.0.0/lib/net35/Faction.Modules.Dotnet.Common.dll -out:./Transports/DIRECT/DIRECT.dll ./Transports/DIRECT/DIRECT-build.cs