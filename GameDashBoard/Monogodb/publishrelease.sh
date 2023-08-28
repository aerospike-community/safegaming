#!/bin/bash
# file: build.sh
# Builds the safegaming GameSimulator application in release mode

dotnet publish GameSimulatorAS.csproj -a x64 -o ".\publish\Release\Aerospike" --self-contained true --configuration Release

echo "If successful, Published to './publish/Release/aerospike'"
echo "To run CD to './publish/Release/Aerospike' and execute './GameSimulatorAS -?'"
echo "Log files and configuration files will be located in this folder"


