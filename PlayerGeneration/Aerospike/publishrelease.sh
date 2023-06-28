#!/bin/bash
# file: build.sh
# Builds the safegaming PlayerGenerator application in release mode


dotnet nuget add source "https://www.myget.org/F/andersenacs/auth/d8b6b853-2786-4f25-b04c-afa47f7c0586/api/v2" --name andersenasc > /dev/null 2>&1
dotnet publish PlayerGeneration.csproj -a x64 -o ".\publish\Release\Aerospike" --self-contained true --configuration Release

echo "If successful, Published to './publish/Release/aerospike'"
echo "To run CD to './publish/Release/Aerospike' and execute './PlayerGeneration -?'"
echo "Log files and configuration files will be located in this folder"


