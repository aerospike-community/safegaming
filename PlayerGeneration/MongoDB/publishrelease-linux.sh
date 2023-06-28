#!/bin/bash
# file: build.sh
# Builds the safegaming PlayerGenerator application in release mode


dotnet nuget add source "https://www.myget.org/F/andersenacs/auth/d8b6b853-2786-4f25-b04c-afa47f7c0586/api/v2" --name andersenasc  > /dev/null 2>&1
dotnet publish PlayerGenerationMG.csproj -r linux-x64 -o ".\publish\Release\MongoDB\linux" --self-contained true --configuration Release

echo "If successful, Published to './publish/Release/MongoDB/linux'"
echo "To run CD to './publish/Release/MongoDB/linux' and execute './PlayerGenerationMG -?'"
echo "Log files and configuration files will be located in this folder"


