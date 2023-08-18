#!/bin/bash
# file: build.sh
# Builds the safegaming PlayerGenerator application in release mode

dotnet publish PlayerGeneration.csproj -a x64 -o ".\publish\Release\Aerospike" --self-contained true --configuration Release

echo "If successful, Published to './publish/Release/aerospike'"
echo "To run CD to './publish/Release/Aerospike' and execute './PlayerGeneration -?'"
echo "Log files and configuration files will be located in this folder"


