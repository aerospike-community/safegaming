#!/bin/bash
# file: build.sh
# Builds the safegaming PlayerGenerator application in release mode

dotnet publish GameDashBoardASDBaaS.csproj -r linux-x64 -o ".\publish\Release\Aerospike\linux" --self-contained true --configuration Release

echo "If successful, Published to './publish/Release/aerospike/linux'"
echo "To run CD to './publish/Release/Aerospike/linux' and execute './GameDashBoardASDaaS -?'"
echo "Log files and configuration files will be located in this folder"


