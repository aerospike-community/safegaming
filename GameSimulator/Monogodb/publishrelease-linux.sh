#!/bin/bash
# file: build.sh
# Builds the safegaming GameSimulator application in release mode


dotnet publish GameSimulatorMG.csproj -r linux-x64 -o ".\publish\Release\MongoDB\linux" --self-contained true --configuration Release

echo "If successful, Published to './publish/Release/MongoDB/linux'"
echo "To run CD to './publish/Release/MongoDB/linux' and execute './GameSimulatorMG -?'"
echo "Log files and configuration files will be located in this folder"


