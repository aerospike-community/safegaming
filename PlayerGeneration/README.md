This zip file contains three folders that is assocated with the target platform. They are completely self-contained and no additional packages need to be installed to execute the application. 
The application name is "PlayerGeneration". 

The corresponding application configuration file is "appsettings.json". The application does not take any arguments.  
Changes to behavior and connection need to be made within the application configuration file.
Note when connecting to an external Cluster (public IP), the "DBUseExternalIPAddresses" setting must be true.

Installation:

1) Unzip the PlayerGenration.zip file into a folder.
2) CD into this folder.
3) To Run:
    MacOS and Linux: ./PlayerGeneration
    Windows: PlayerGeneration.exe