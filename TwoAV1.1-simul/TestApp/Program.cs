#region Header

/*
Copyright 2015 Enkhbold Nyamsuren (http://www.bcogs.net , http://www.bcogs.info/)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

Namespace: TestApp
Filename: Program.cs
*/

// Change history:
// [2016.10.06]
//      - [SC] updated used namespace to TwoA
//      - [SC] renamed variable 'hat' to 'twoA'
// [2016.11.14]
//      - [SC] deleted 'using Swiss';
// [2016.11.29]
//      - [SC] "gameplaylogs.xml" and "TwoAAppSettings.xml" are converted to embedded resources
//      - [SC] updated description of example output for 'UpdateRatings' annd 'TargetScenarioID' methods
//      - [SC] added 'testKnowledgeSpaceGeneration' method with examples of using knowledge space generator
// [2017.01.02]
//      - [SC] added 'TileZero' project
//      - [SC] added 'evaluateTileZeroAIDifficulty' method
//      - [SC] added 'doTileZeroSimulation' method
//      - [SC] added references to external libraries: 
//              'Microsoft.Msagl.dll'
//              'Microsoft.Msagl.Drawing.dll'
//              'Microsoft.Msagl.GraphViewerGdi.dll'
// [2017.01.03]
//      - [SC] added 'testScoreCalculations' method
//

#endregion Header

namespace TestApp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Xml.Linq;
    using System.Diagnostics;

    using System.Windows.Forms;

    // [SC] MSAGL libraries for graph visualization
    using Microsoft.Msagl.GraphViewerGdi;
    using Microsoft.Msagl.Drawing;

    using TwoA;
    using AssetPackage;
    using TileZero;

    class Program
    {

        static int OLD_OLD = 1;
        static int NEW_OLD = 2;
        static int NEW_NEW = 3;

        static void Main (string[] args) {
            // [SC] find win rates of AI opponents playing against each ither
            evaluateTileZeroAIDifficulty();

            // [SC] Simulation 1
            doTileZeroFreqStabilitySimulation(OLD_OLD);
            // [SC] Simulation 2
            doTileZeroFreqStabilitySimulation(NEW_OLD);
            // [SC] Simulation 3
            doTileZeroFreqStabilitySimulation(NEW_NEW);
            
            // [SC] Simulation 4
            doTileZeroSimulation();
            
            Console.ReadKey();
        }

        static void evaluateTileZeroAIDifficulty() {
            Random rnd = new Random();

            // [SC] creating a game object
            Game tzGame = new Game();
            // [SC] disable logging to speed-up simulation
            Cfg.enableLog(false);

            // [SC] the number of games to be played with each AI evolution
            double numberOfMatches = 2000;
            // [SC] the number of tiles to  be played
            int playableTileCount = 54;

            string[] aiOneEvolution = { TileZero.Cfg.VERY_EASY_AI
                                          , TileZero.Cfg.EASY_AI
                                          , TileZero.Cfg.MEDIUM_COLOR_AI
                                          , TileZero.Cfg.MEDIUM_SHAPE_AI
                                          , TileZero.Cfg.HARD_AI
                                          , TileZero.Cfg.VERY_HARD_AI
                                        };


            using (StreamWriter aiDifficultyFile = new StreamWriter(@"aiDifficulty.txt")) {
                string line = ""
                    + "\"" + "ID" + "\""
                    + "\t" + "\"" + "AIOne" + "\""
                    + "\t" + "\"" + "AITwo" + "\""
                    + "\t" + "\"" + "StartPlayerIndex" + "\""
                    + "\t" + "\"" + "AIOneWin" + "\"";
                aiDifficultyFile.WriteLine(line);

                int counter = 1;

                for (int aiOneIndex = 0; aiOneIndex < aiOneEvolution.Length; aiOneIndex++) {
                    for (int aiTwoIndex = aiOneIndex; aiTwoIndex < aiOneEvolution.Length; aiTwoIndex++) {
                        string aiOneID = aiOneEvolution[aiOneIndex];
                        string aiTwoID = aiOneEvolution[aiTwoIndex];

                        int startPlayerIndex = 0;
                        for (int currMatchIndex = 0; currMatchIndex < numberOfMatches; currMatchIndex++) {
                            tzGame.initNewGame(aiOneID, aiTwoID, playableTileCount, startPlayerIndex);
                            tzGame.startNewGame();
                            TileZero.Cfg.clearLog();

                            double correctAnswer = 0;
                            if (tzGame.getPlayerByIndex(0).WinFlag) {
                                correctAnswer = 1;
                            }

                            line = ""
                                + "\"" + counter++ + "\""
                                + "\t" + "\"" + aiOneID + "\""
                                + "\t" + "\"" + aiTwoID + "\""
                                + "\t" + "\"" + (startPlayerIndex+1) + "\""
                                + "\t" + "\"" + correctAnswer + "\"";
                            aiDifficultyFile.WriteLine(line);
                            Console.WriteLine(line);
                        }

                        startPlayerIndex = 1;
                        for (int currMatchIndex = 0; currMatchIndex < numberOfMatches; currMatchIndex++) {
                            tzGame.initNewGame(aiOneID, aiTwoID, playableTileCount, startPlayerIndex);
                            tzGame.startNewGame();
                            TileZero.Cfg.clearLog();

                            double correctAnswer = 0;
                            if (tzGame.getPlayerByIndex(0).WinFlag) {
                                correctAnswer = 1;
                            }

                            line = ""
                                + "\"" + counter++ + "\""
                                + "\t" + "\"" + aiOneID + "\""
                                + "\t" + "\"" + aiTwoID + "\""
                                + "\t" + "\"" + (startPlayerIndex+1) + "\""
                                + "\t" + "\"" + correctAnswer + "\"";
                            aiDifficultyFile.WriteLine(line);
                            Console.WriteLine(line);
                        }
                    }
                }
            }
        }

        static void doTileZeroFreqStabilitySimulation(int equationType) {
            string adaptID = "Game difficulty - Player skill";
            string gameID = "TileZeroFreqTest";
            string playerID = "PresetAI";

            // [SC] do not update xml files
            bool updateDatafiles = false;

            // [SC] to randomly decide which player moves first
            Random rnd = new Random();
            // [SC] creating a game object
            Game tzGame = new Game();
            // [SC] disable logging to speed-up simulation
            Cfg.enableLog(false);

            double numberOfSimulations = 10;
            double numberOfMatches = 1000; // [SC] the number of games to be played with each AI evolution
            int playableTileCount = 54;
            double minRT = 60000;

            double distMean = 0.75;
            double distSD = 0.1;
            double lowerLimit = 0.5;
            double upperLimit = 1.0;

            // [SC] a player is represented by the Very Hard AI
            string[] aiOneEvolution = { TileZero.Cfg.VERY_HARD_AI };

            // [SC] start simulations
            for (int simIndex = 0; simIndex < numberOfSimulations; simIndex++) {
                TwoA twoA = new TwoA(new MyBridge());
                twoA.SetTargetDistribution(distMean, distSD, lowerLimit, upperLimit);
                double avgAccuracy = 0;

                // [SC] start AI player evolution from easiest to hardest
                foreach (string aiOneID in aiOneEvolution) {
                    // [SC] play a number of games before each evolution
                    for (int matchIndex = 0; matchIndex < numberOfMatches; matchIndex++) {
                        // [SC] get a recommended opponent AI from TwoA
                        string aiTwoID = null;

                        if (equationType == OLD_OLD) {
                            aiTwoID = twoA.TargetScenarioIDOld(adaptID, gameID, playerID, false);
                        }
                        else if (equationType == NEW_OLD) {
                            aiTwoID = twoA.TargetScenarioIDOld(adaptID, gameID, playerID, true);
                        }
                        else if (equationType == NEW_NEW) {
                            aiTwoID = twoA.TargetScenarioID(adaptID, gameID, playerID);
                        }

                        Cfg.clearLog();

                        // [SC] start one match simualtion
                        int startPlayerIndex = rnd.Next(2); // [SC] decide whether the player or the opponent moves first
                        tzGame.initNewGame(aiOneID, aiTwoID, playableTileCount, startPlayerIndex);
                        tzGame.startNewGame();

                        // [SC] simulate player's response time
                        SimpleRNG.SetSeedFromRandom();
                        double rt = SimpleRNG.GetNormal(120000, 10000); // [SC] max is 900000
                        if (rt < minRT) { rt = minRT; }

                        // [SC] obtain player's accuracy
                        double correctAnswer = 0;
                        if (tzGame.getPlayerByIndex(0).WinFlag) {
                            correctAnswer = 1;
                        }

                        avgAccuracy += correctAnswer;

                        printMsg("Counter: " + (matchIndex + 1) + "; Start index: " + startPlayerIndex);
                        //printMsg(TileZero.Cfg.getLog());

                        // [SC] update ratings
                        twoA.UpdateRatings(adaptID, gameID, playerID, aiTwoID, rt, correctAnswer, updateDatafiles);
                    }
                }

                printMsg(String.Format("Accurracy for simulation {0}: {1}", simIndex, avgAccuracy / (numberOfMatches * aiOneEvolution.Length)));

                // [SC] creating a file with final ratings
                using (StreamWriter ratingfile = new StreamWriter("S" + equationType + "/ratingfile" + simIndex + ".txt")) {
                    string line = ""
                        + "\"ScenarioID\""
                        + "\t" + "\"Rating\""
                        + "\t" + "\"PlayCount\""
                        + "\t" + "\"Uncertainty\"";

                    ratingfile.WriteLine(line);

                    foreach (string scenarioID in aiOneEvolution) {
                        line = ""
                            + "\"" + scenarioID + "\""
                            + "\t" + "\"" + twoA.ScenarioRating(adaptID, gameID, scenarioID) + "\""
                            + "\t" + "\"" + twoA.ScenarioPlayCount(adaptID, gameID, scenarioID) + "\""
                            + "\t" + "\"" + twoA.ScenarioUncertainty(adaptID, gameID, scenarioID) + "\"";

                        ratingfile.WriteLine(line);
                    }
                }

                // [SC] creating a file with history of gameplays including player and scenario ratings
                using (StreamWriter datafile = new StreamWriter("S" + equationType + "/datafile" + simIndex + ".txt")) {
                    GameplaysData gameplays = twoA.GameplaysData;
                    TwoAAdaptation adaptNode = gameplays.Adaptation.First(p => p.AdaptationID.Equals(adaptID));
                    TwoAGame gameNode = adaptNode.Game.First(p => p.GameID.Equals(gameID));

                    string line = ""
                        + "\"ID\""
                        + "\t" + "\"PlayerID\""
                        + "\t" + "\"ScenarioID\""
                        + "\t" + "\"Timestamp\""
                        + "\t" + "\"RT\""
                        + "\t" + "\"Accuracy\""
                        + "\t" + "\"PlayerRating\""
                        + "\t" + "\"ScenarioRating\"";

                    datafile.WriteLine(line);

                    int id = 1;
                    foreach (TwoAGameplay gp in gameNode.Gameplay) {
                        line = ""
                            + "\"" + id++ + "\""
                            + "\t" + "\"" + gp.PlayerID + "\""
                            + "\t" + "\"" + gp.ScenarioID + "\""
                            + "\t" + "\"" + gp.Timestamp + "\""
                            + "\t" + "\"" + gp.RT + "\""
                            + "\t" + "\"" + gp.Accuracy + "\""
                            + "\t" + "\"" + gp.PlayerRating + "\""
                            + "\t" + "\"" + gp.ScenarioRating + "\"";

                        datafile.WriteLine(line);
                    }
                }
            }
        }

        static void doTileZeroSimulation() {
            string adaptID = "Game difficulty - Player skill";
            string gameID = "TileZeroSimul";
            string playerID = "EvolvingAI";

            // [SC] do not update xml files
            bool updateDatafiles = false;

            // [SC] to randomly decide which player moves first
            Random rnd = new Random();
            // [SC] creating a game object
            Game tzGame = new Game();
            // [SC] disable logging to speed-up simulation
            Cfg.enableLog(false);

            double numberOfSimulations = 10;
            double numberOfMatches = 400; // [SC] the number of games to be played with each AI evolution
            int playableTileCount = 54;
            double minRT = 60000;

            double distMean = 0.75;
            double distSD = 0.1;
            double lowerLimit = 0.5;
            double upperLimit = 1.0;

            // [SC] a player is represented by an AI evolving from Very Easy AI to Very Hard AI
            string[] aiOneEvolution = { TileZero.Cfg.VERY_EASY_AI
                                          , TileZero.Cfg.EASY_AI
                                          , TileZero.Cfg.MEDIUM_COLOR_AI
                                          , TileZero.Cfg.MEDIUM_SHAPE_AI
                                          , TileZero.Cfg.HARD_AI
                                          , TileZero.Cfg.VERY_HARD_AI
                                        };

            // [SC] start simulations
            for (int simIndex = 0; simIndex < numberOfSimulations; simIndex++) {
                TwoA twoA = new TwoA(new MyBridge());
                twoA.SetTargetDistribution(distMean, distSD, lowerLimit, upperLimit);
                double avgAccuracy = 0;

                // [SC] start AI player evolution from easiest to hardest
                foreach (string aiOneID in aiOneEvolution) {
                    // [SC] play a number of games before eacg evolution
                    for (int matchIndex = 0; matchIndex < numberOfMatches; matchIndex++) {
                        // [SC] get a recommended opponent AI from TwoA
                        string aiTwoID = null;

                        aiTwoID = twoA.TargetScenarioID(adaptID, gameID, playerID);

                        Cfg.clearLog();

                        // [SC] start one match simualtion
                        int startPlayerIndex = rnd.Next(2); // [SC] decide whether the player or the opponent moves first
                        tzGame.initNewGame(aiOneID, aiTwoID, playableTileCount, startPlayerIndex);
                        tzGame.startNewGame();

                        // [SC] simulate player's response time
                        SimpleRNG.SetSeedFromRandom();
                        double rt = SimpleRNG.GetNormal(120000, 10000); // [SC] max is 900000
                        if (rt < minRT) { rt = minRT; }

                        // [SC] obtain player's accuracy
                        double correctAnswer = 0;
                        if (tzGame.getPlayerByIndex(0).WinFlag) {
                            correctAnswer = 1;
                        }

                        avgAccuracy += correctAnswer;

                        printMsg("Counter: " + (matchIndex + 1) + "; Start index: " + startPlayerIndex);
                        //printMsg(TileZero.Cfg.getLog());

                        // [SC] update ratings
                        twoA.UpdateRatings(adaptID, gameID, playerID, aiTwoID, rt, correctAnswer, updateDatafiles);
                    }
                }

                printMsg(String.Format("Accurracy for simulation {0}: {1}", simIndex, avgAccuracy / (numberOfMatches * aiOneEvolution.Length)));

                // [SC] creating a file with final ratings
                using (StreamWriter ratingfile = new StreamWriter("S4/ratingfile" + simIndex + ".txt")) {
                    string line = ""
                        + "\"ScenarioID\""
                        + "\t" + "\"Rating\""
                        + "\t" + "\"PlayCount\""
                        + "\t" + "\"Uncertainty\"";

                    ratingfile.WriteLine(line);

                    foreach (string scenarioID in aiOneEvolution) {
                        line = ""
                            + "\"" + scenarioID + "\""
                            + "\t" + "\"" + twoA.ScenarioRating(adaptID, gameID, scenarioID) + "\""
                            + "\t" + "\"" + twoA.ScenarioPlayCount(adaptID, gameID, scenarioID) + "\""
                            + "\t" + "\"" + twoA.ScenarioUncertainty(adaptID, gameID, scenarioID) + "\"";

                        ratingfile.WriteLine(line);
                    }
                }

                // [SC] creating a file with history of gameplays including player and scenario ratings
                using (StreamWriter datafile = new StreamWriter("S4/datafile" + simIndex + ".txt")) {
                    GameplaysData gameplays = twoA.GameplaysData;
                    TwoAAdaptation adaptNode = gameplays.Adaptation.First(p => p.AdaptationID.Equals(adaptID));
                    TwoAGame gameNode = adaptNode.Game.First(p => p.GameID.Equals(gameID));

                    string line = ""
                        + "\"ID\""
                        + "\t" + "\"PlayerID\""
                        + "\t" + "\"ScenarioID\""
                        + "\t" + "\"Timestamp\""
                        + "\t" + "\"RT\""
                        + "\t" + "\"Accuracy\""
                        + "\t" + "\"PlayerRating\""
                        + "\t" + "\"ScenarioRating\"";

                    datafile.WriteLine(line);

                    int id = 1;
                    foreach (TwoAGameplay gp in gameNode.Gameplay) {
                        line = ""
                            + "\"" + id++ + "\""
                            + "\t" + "\"" + gp.PlayerID + "\""
                            + "\t" + "\"" + gp.ScenarioID + "\""
                            + "\t" + "\"" + gp.Timestamp + "\""
                            + "\t" + "\"" + gp.RT + "\""
                            + "\t" + "\"" + gp.Accuracy + "\""
                            + "\t" + "\"" + gp.PlayerRating + "\""
                            + "\t" + "\"" + gp.ScenarioRating + "\"";

                        datafile.WriteLine(line);
                    }
                }
            }
        }

        // [SC] prints a message to both console and debug output window
        static void printMsg(string msg) {
            Console.WriteLine(msg);
            Debug.WriteLine(msg);
        }
    }

    // [SC][2016.11.29] modified
    class MyBridge : IBridge, IDataStorage
    {
        // [SC] "TwoAAppSettings.xml" and "gameplaylogs.xml" are embedded resources
        // [SC] these XML files are for running this test only and contain dummy data
        // [SC] to use the TwoA asset with your game, generate blank XML files with the accompanying widget https://github.com/rageappliedgame/HATWidget
        private const string resourceNS = "TestApp.Resources.";

        public MyBridge() {}

        public bool Exists(string fileId) {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();
            return resourceNames.Contains<string>(resourceNS + fileId);
        }

        public void Save(string fileId, string fileData) {
            // [SC] save is not implemented since the xml files are embedded resources
        }

        public string Load(string fileId) {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceNS + fileId)) {
                using (StreamReader reader = new StreamReader(stream)) {
                    return reader.ReadToEnd();
                }
            }
        }

        public String[] Files() {
            return null;
        }

        public bool Delete(string fileId) {
            return false;
        }
    }
}
