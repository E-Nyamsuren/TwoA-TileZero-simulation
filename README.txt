TwoA implementation with simulations of AI players in the TileZero game.

Authors: Enkhbold Nyamsuren, Wim van der Vegt
Organization: OUNL
For any questions contact Enkhbold Nyamsuren via Enkhbold.Nyamsuren [AT] ou [DOT] nl

The folder "data" contains the data presented in the study.
There are "S[simulation number]" subfolders. The order of folders follows the order of simulations as presented in the study.
For example, the folder "S1" contains data from the simulation 1 described in the study.
The file "datafile[block number].txt" contains gameplay records from a single block.
The file "ratingfile[block number].txt" contains final ratings of the AI opponents at the end of a block.
The file "aiDifficulty.txt" contains results of simulations where AI opponents played against each other (without TwoA) to establish their win rates.

"analysisScript.R" is R script for analyzing data in the folder "data". 
"commonCode.R" is misc code imported by "analysisScript.R". 
Before running the "analysisScript.R", make sure to set absolute paths within the header of the script code.

The folder "executable" contains "TestApp.exe" that re-runs all the simulations presented in the study.
WERNING: it may require two or more hours to complete the simulations depending on the computer specs.

The folder "TwoAV1.1-simul" contains Visual Studio C# source code. 
The source code includes TwoAs' impemetation, TileZero's implementation, and console program to run the simulations.
The start-up project is "TestApp". The main method is located inside "Program.cs".
The folder "AssetManager" contains source code for the RAGE architecture used by TwoA. It is necessary to compile the source code.
