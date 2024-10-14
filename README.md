# MODSIM-DSS
What is Modsim-DSS?
MODSIM-DSS is a generalized river basin Decision Support System and network flow model developed at Colorado State University designed specifically to meet the growing demands and pressures on river basin managers today.

Rapid growth in population centers and mounting needs for irrigation have dramatically increased the need to expand sources of reliable water supply, while attending to environmental and ecological issues. Publically owned water systems must often deal with severe restrictions from complex legal agreements, contracts, federal regulations, interstate compacts, and pressures from various special interest groups. Optimal coordination of these many facets of river basin systems requires the assistance of computer modeling tools to make rational management decisions.

MODSIM-DSS was designed for this highly complex and constantly evolving river basin management environment. Out-of-the-box functionality to simulate complex river systems is complemented by a structure that allows customization of the MODSIM solution, enabling the user to link MODSIM with other models and create new modules.  For example:

* MODSIM-DSS has been linked with stream-aquifer models for analysis of the conjunctive use of groundwater and surface water resources.

* MODSIM-DSS has also been used with water quality simulation models for assessing the effectiveness of pollution control strategies.

* MODSIM-DSS has been integrated with geographic information systems (GIS) for geo-referencing the model objects and managing spatial data base requirements of river basin management.

MODSIM-DSS is structured as a Decision Support System, with a core module that provides the solver, the model stuctures and utilities to read and write models.  Graphical user interfaces (GUI) can be created with the core module, allowing users to create MODSIM networks using the river basin system topology. Data structures embodied in each model object are controlled by a data base management system. Formatted data files are prepared by the network utilities and a highly efficient network flow optimization model is executed with the user inputs without requiring the user to set up the optimization problem. Results of the network optimization are compiled in output databases which can be presented in useful graphical plots.

# Documentation
Download the user manual from the [MODSIM Documentation](http://modsim.engr.colostate.edu/modsim.php), using the user name and password privided in the web page. 

# MODSIM-DSS Code
The code includes a set of libraries (dlls) that provide the main MODSIM-DSS fucntionality, which include libraries with the model structure (libsim), the model solver (ModsimModel), network utils (NetworkUtils), and the XY file read and write (XYFile).  There are other libraries also included that support the solver, the structure and the custom code editor (windows form). The development testing project, is a command line project, that helps test changes against satisfactory results.  

## How to run a MODSIM-DSS Network
The MODSIM-DSS source code provides a command line project (MainMODSIMRun.csproj) that compiles into an executable that performs a standard MODSIM run for the XY file provided at the first argument of the exe.  

## Testing Performance
A battery of networks is used to test the MODSIM-DSS code performance.  The networks include a set of input and expected output sets (included in the MODSIMTest/TestNetworks folder) that are expected to be replicated when new features or adjustments are incorporated. The test is performed using the MODSIMTest C# project, which uses the current code to run and compare the resutls for all the networks in the battery of networks.

When new features are added to the MODSIM-DSS, test networks (input and output) are expected the be added to the battery of network for testing the functionality. The namimg convention for the files to be added to the battery is to include "XXXX - orig.xy" for the input, and "XXXX - origOUTPUT.sqlite" for the output files.  

* Run the ModsimTests project and look in ModsimTests/TestNetWorks for a folder called "_diffs". If it does not exist after running the test networks that means all the tests passed. A log file of all runs is also available for review at ModsimTests/TestNetworks/TestNetworks.log.

## Release Notes
Version | Notes|
---------|------|
8.6.2   | First version licensed under an open source GPL v2. 
8.6.1   |Version updated to the .NET Framework 4.6.1. Includes multiples minor fixes and improvements, including: - Link layers, - Results graphics enhancements, - GUI forms error handling and sizing fixes, - Fixes dead pool operations issues. Microsoft Access drivers requirements are no longer prompted to the user when the software is installed, since sqlite is the recommended output format.    
8.6.0   |First version release with support for sqlite database output format. |

# Grafical User Interface
The MODSIM-DSS grapical user interface (for Windows) requires commercial licenses that cannot be distributed in GitHub.  Colorado State University provides a compiled vesion of the GUI to the user community free of charge. The interface is compiled with the current version of the source code. However, GUI distributions with previous version are also available.  Please visit: http://modsim.engr.colostate.edu/modsim.php to download the GUI.  

# Credits

This tool is legacy of Dr. John Labadie, a former Colorado State University Professor, who develop the original version of the software and maintained it, improved it and applied it in numerous river basins around the world over his carrier in water resources planning and management.   

## Who do I talk to? ###

Contact:
* Primary -  Enrique Triana (etriana@rti.org)
* Secondary - Timothy gates (Timothy.Gates@ColoState.EDU) 