\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\* Deprecated \*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*

This version has been deprecated. Please see the readme located in the safegaming folder.

\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\* Deprecated \*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*\*

The application name is "PlayerGeneration". There are two versions. They are:

-   ./PlayerGeneration which is the Aerospike version
-   ./PlayerGenerationMG which is the MongoDB version

The application used command line arguments and application configuration files. To obtain the list of all command line arguments, pass “-?” To the application. Command line arguments will always override the application config file values.

There are three application configuration files. They are:

-   appsettings.json – This contains all common configuration between the different versions.
-   appsettingsAerospike.json – This contains configuration specifically for Aerospike.  
    Note when connecting to an external Cluster (public IP), the "DBUseExternalIPAddresses" setting must be true.
-   appsettingsMG.json -- This contains configuration specifically for MongoDB

Installation:

1.  Install .Net framework version 7.0 SDK  
    https://learn.microsoft.com/en-us/dotnet/fundamentals/
2.  CD into the appropriate folder (i.e., Aerospike or MongoDB)
3.  Execute the build script.
    1.  “publishrelease.sh” to build a platform neutral version.
    2.  “publishrelease-linux.sh” to build for the Linux platform.
4.  To Run CD into the runtime folder (will be displayed at the end of the build script, step 3):
    1.  MacOS and Linux:  
        ./PlayerGeneration
    2.  Windows:  
        PlayerGeneration.exe
