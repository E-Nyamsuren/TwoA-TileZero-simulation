﻿#region Header

/*
Copyright 2015 Enkhbold Nyamsuren (http://www.bcogs.net , http://www.bcogs.info/), Wim van der Vegt

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

Namespace: TwoA
Filename: DifficultyAdapter.cs
Description:
    The asset implements ELO-based difficulty to skill adaptation algorithm described in "Klinkenberg, S., Straatemeier, M., & Van der Maas, H. L. J. (2011).
    Computer adaptive practice of maths ability using a new item response model for on the fly ability and difficulty estimation.
    Computers & Education, 57 (2), 1813-1824.".
*/


// Change history:
// [2016.03.14]
//      - [SC] changed calcTargetBeta method
//      - [SC] changed calcExpectedScore method to prevent division by 0
//      - [SC] corrected ProvU property
// [2016.03.15]
//      - [SC] added ADAPTER_TYPE field
//      - [SC] added property for ADAPTER_TYPE field
//      - [SC] replaced all adaptID parameters in TargetScenarioID method with ADAPTER_TYPE field
//      - [SC] replaced all adaptID parameters in UpdateRatings method with ADAPTER_TYPE field
//      - [SC] changed UpdateRatings method
// [2016.03.15]
//      - [SC] added 'updateDatafiles' parameter to the updateDatafiles method's signature
//      - [SC] the design document was updated to reflect changes in the source code. Refer to https://rage.ou.nl/index.php?q=filedepot_download/358/501
// [2016.10.06]
//      - [SC] renamed namespace 'HAT' to 'TwoA'
// [2016.10.07]
//      - [SC] from the constructor, moved instantiation of SimpleRNG to 'TwoA.InitSettings' method
//      - [SC] in calcTargetBeta method, changed equation from 'theta + Math.Log(randomNum / (1 - randomNum))' to 'theta + Math.Log((1 - randomNum) / randomNum)'
// [2016.11.14]
//      - [SC] deleted 'using Swiss';
// [2016.11.29]
//      - [SC] added 'SimpleRNG.SetSeedFromSystemTime()' to 'calcTargetBeta' method;
// [2016.12.07]
//      - [SC] added replaced 'calcTargetBeta' method with 'calcTargetBetas' method that calculates a range for a target beta rather than single beta value
//      - [SC] changed the approach used by TargetScenarioID method to decide on the recommended scenario
    
//
// TODO: 
//      - what if response time is higher than the max allowed duration
//      - transaction style update of player and scenario data; no partial update should be possible; if any value fails to update then all values should fail to update

#endregion Header

namespace TwoA
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    internal class DifficultyAdapter
    {
        #region Fields
        //// [TODO][SC perhaps all getter/setter functions should just assign default values instead of throwing exceptions

        // [SC]
        private const string ADAPTER_TYPE = "Game difficulty - Player skill";

        private const string DEF_DATE = "2015-01-01T01:01:01";

        private const double DEF_K = 0.0075; // [SC] The default value for the K constant when there is no uncertainty
        private const double DEF_K_UP = 4.0; // [SC] the default value for the upward uncertainty weight
        private const double DEF_K_DOWN = 0.5; // [SC] The default value for the downward uncertainty weight

        private const double DEF_MAX_DELAY = 30; // [SC] The default value for the max number of days after which player's or item's undertainty reaches the maximum
        private const double DEF_MAX_PLAY = 40; // [SC] The default value for the max number of administrations that should result in minimum uncertaint in item's or player's ratings

        private const double DEF_THETA = 0.01; // [SC][2016.01.07]

        private const double DEF_U = 1.0; // [SC] The default value for the provisional uncertainty to be assigned to an item or player
        
        private const double TARGET_DISTR_MEAN = 0.75;
        private const double TARGET_DISTR_SD = 0.1;
        private const double TARGET_LOWER_LIMIT = 0.5;
        private const double TARGET_UPPER_LIMIT = 1;

        private const string TIMESTAMP_FORMAT = "s"; // Sortable DateTime as used in XML serializing : 's' -> 'yyyy-mm-ddThh:mm:ss'

        private TwoA asset; // [ASSET]

        //// [TODO][SC] need to setup getter and setters as well as proper value validity checks

        private double maxDelay;        // [SC] set to DEF_MAX_DELAY in the constructor
        private double maxPlay;         // [SC] set to DEF_MAX_PLAY in the constructor

        private double kConst;          // [SC] set to DEF_K in the constructor
        private double kUp;             // [SC] set to DEF_K_UP in the constructor
        private double kDown;           // [SC] set to DEF_K_DOWN in the constructor

        private double provU;           // [SC] set to DEF_U in the constructor

        private string provDate;        // [SC] set to DEF_DATE in the constructor

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the TwoA.DifficultyAdapter class.
        /// 
        /// Assign default values if max play frequency and max non-play delay values
        /// are not provided.
        /// 
        /// Add a reference to the TwoA asset so we can use it.
        /// </summary>
        ///
        /// <remarks>
        /// Alternative for the asset parameter would be to ask the AssetManager for
        /// a reference.
        /// </remarks>
        ///
        /// <param name="asset"> The asset. </param>
        internal DifficultyAdapter(TwoA asset) {
            maxPlay = DEF_MAX_PLAY;
            maxDelay = DEF_MAX_DELAY;

            kConst = DEF_K;
            kUp = DEF_K_UP;
            kDown = DEF_K_DOWN;

            provU = DEF_U;

            provDate = DEF_DATE;

            this.asset = asset; // [ASSET]
        }

        #endregion Constructors

        #region Properties

        //////////////////////////////////////////////////////////////////////////////////////
        ////// START: properties for calculating rating uncertainties
        #region properties for calculating rating uncertainties

        /// <summary>
        /// Gets or sets the maximum delay.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">    Thrown when one or more arguments
        ///                                         have unsupported or illegal values. </exception>
        ///
        /// <value>
        /// The maximum delay.
        /// </value>
        private double MaxDelay {
            get { return maxDelay; }
            set {
                if (value <= 0) throw new System.ArgumentException("The maximum number of delay days should be higher than 0.");
                else maxDelay = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum play.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">    Thrown when one or more arguments
        ///                                         have unsupported or illegal values. </exception>
        ///
        /// <value>
        /// The maximum play.
        /// </value>
        private double MaxPlay {
            get { return maxPlay; }
            set {
                if (value <= 0) throw new System.ArgumentException("The maximum administration parameter should be higher than 0.");
                else maxPlay = value;
            }
        }

        /// <summary>
        /// Gets or sets the provisional uncertainty.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">    Thrown when one or more arguments
        ///                                         have unsupported or illegal values. </exception>
        ///
        /// <value>
        /// The provisional uncertainty.
        /// </value>
        private double ProvU {
            get { return provU; }
            set {
                if (0 > value || value > 1) throw new System.ArgumentException("Provisional uncertainty value should be between 0 and 1"); // [SC][2016.01.07] "0 < value" => "0 > value"
                else provU = value;
            }
        }

        /// <summary>
        /// Gets the provisional play date.
        /// </summary>
        ///
        /// <value>
        /// The provisional play date.
        /// </value>
        private string ProvDate {
            get { return provDate; }
        }

        #endregion properties for calculating rating uncertainties
        ////// END: properties for calculating rating uncertainties
        //////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////
        ////// START: properties for calculating k factors
        #region properties for calculating k factors

        /// <summary>
        /// Getter/setter for the K constant.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">    Thrown when one or more arguments
        ///                                         have unsupported or illegal values. </exception>
        ///
        /// <value>
        /// The k constant.
        /// </value>
        private double KConst {
            get { return kConst; }
            set {
                if (value < 0) throw new System.ArgumentException("K constant cannot be a negative number.");
                else kConst = value;
            }
        }

        /// <summary>
        /// Getter/setter for the upward uncertainty weight.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">    Thrown when one or more arguments
        ///                                         have unsupported or illegal values. </exception>
        ///
        /// <value>
        /// The upward uncertainty weight.
        /// </value>
        private double KUp {
            get { return kUp; }
            set {
                if (value < 0) throw new System.ArgumentException("The upward uncertianty weight cannot be a negative number.");
                else kUp = value;
            }
        }

        /// <summary>
        /// Getter/setter for the downward uncertainty weight.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">    Thrown when one or more arguments
        ///                                         have unsupported or illegal values. </exception>
        ///
        /// <value>
        /// The downward uncertainty weight.
        /// </value>
        private double KDown {
            get { return kDown; }
            set {
                if (value < 0) throw new System.ArgumentException("The downward uncertainty weight cannot be a negative number.");
                else kDown = value;
            }
        }

        #endregion properties for calculating k factors
        ////// END: properties for calculating k factors
        //////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>
        /// Gets the prov theta.
        /// </summary>
        ///
        /// <value>
        /// The prov theta.
        /// </value>
        private double ProvTheta {
            get { return DEF_THETA; }
        }

        /// <summary>
        /// Gets target distribution mean.
        /// </summary>
        ///
        /// <value>
        /// The target distribution mean.
        /// </value>
        internal double TargetDistributionMean { // [SC][2016.01.07] TargetDistributedMean => TargetDistributionMean
            get { return TARGET_DISTR_MEAN; }
        }

        /// <summary>
        /// Gets the type of the adapter
        /// </summary>
        ///
        /// <value>
        /// Adapter type as string.
        /// </value> 
        public string Type {
            get { return ADAPTER_TYPE; }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Updates the ratings.
        /// </summary>
        ///
        /// <param name="gameID">               Identifier for the game. </param>
        /// <param name="playerID">             Identifier for the player. </param>
        /// <param name="scenarioID">           Identifier for the scenario. </param>
        /// <param name="rt">                   The right. </param>
        /// <param name="correctAnswer">        The correct answer. </param>
        /// <param name="updateDatafiles">      Set to true to update adaptation and gameplay logs files. </param>
        internal void UpdateRatings(string gameID, string playerID, string scenarioID, double rt, double correctAnswer, bool updateDatafiles) {

            // [SC] getting player data
            double playerRating;
            double playerPlayCount;
            double playerUncertainty;
            DateTime playerLastPlayed;

            // [2016.11.14] modified
            try {
                playerRating = asset.PlayerRating(ADAPTER_TYPE, gameID, playerID);
                playerPlayCount = asset.PlayerPlayCount(ADAPTER_TYPE, gameID, playerID);
                playerUncertainty = asset.PlayerUncertainty(ADAPTER_TYPE, gameID, playerID);
                playerLastPlayed = asset.PlayerLastPlayed(ADAPTER_TYPE, gameID, playerID);
            }
            catch (ArgumentException) {
                Debug.WriteLine("Cannot calculate new ratings. Player data is missing.");
                return;
            }

            // [SC] getting scenario data
            double scenarioRating;
            double scenarioPlayCount;
            double scenarioUncertainty;
            double scenarioTimeLimit;
            DateTime scenarioLastPlayed;

            // [2016.11.14] modified
            try {
                // [ASSET]
                scenarioRating = asset.ScenarioRating(ADAPTER_TYPE, gameID, scenarioID);
                scenarioPlayCount = asset.ScenarioPlayCount(ADAPTER_TYPE, gameID, scenarioID);
                scenarioUncertainty = asset.ScenarioUncertainty(ADAPTER_TYPE, gameID, scenarioID);
                scenarioTimeLimit = asset.ScenarioTimeLimit(ADAPTER_TYPE, gameID, scenarioID);
                scenarioLastPlayed = asset.ScenarioLastPlayed(ADAPTER_TYPE, gameID, scenarioID);
            }
            catch (ArgumentException) {
                Debug.WriteLine("Cannot calculate new ratings. Scenario data is missing.");
                return;
            }

            DateTime currDateTime = DateTime.UtcNow;

            // [SC] parsing player data
            double playerLastPlayedDays = (currDateTime - playerLastPlayed).Days;
            if (playerLastPlayedDays > DEF_MAX_DELAY) {
                playerLastPlayedDays = maxDelay;
            }

            // [SC] parsing scenario data
            double scenarioLastPlayedDays = (currDateTime - scenarioLastPlayed).Days;
            if (scenarioLastPlayedDays > DEF_MAX_DELAY) {
                scenarioLastPlayedDays = maxDelay;
            }

            // [SC] calculating actual and expected scores
            double actualScore = calcActualScore(correctAnswer, rt, scenarioTimeLimit);
            double expectScore = calcExpectedScore(playerRating, scenarioRating, scenarioTimeLimit);

            // [SC] calculating player and scenario uncertainties
            double playerNewUncertainty = calcThetaUncertainty(playerUncertainty, playerLastPlayedDays);
            double scenarioNewUncertainty = calcBetaUncertainty(scenarioUncertainty, scenarioLastPlayedDays);

            // [SC] calculating player and sceario K factors
            double playerNewKFct = calcThetaKFctr(playerNewUncertainty, scenarioNewUncertainty);
            double scenarioNewKFct = calcBetaKFctr(playerNewUncertainty, scenarioNewUncertainty);

            // [SC] calculating player and scenario ratings
            double playerNewRating = calcTheta(playerRating, playerNewKFct, actualScore, expectScore);
            double scenarioNewRating = calcBeta(scenarioRating, scenarioNewKFct, actualScore, expectScore);

            // [SC] updating player and scenario play counts
            double playerNewPlayCount = playerPlayCount + 1;
            double scenarioNewPlayCount = scenarioPlayCount + 1;

            // [2016.11.14] modified
            // [SC] storing updated player data
            asset.PlayerRating(ADAPTER_TYPE, gameID, playerID, playerNewRating);
            asset.PlayerPlayCount(ADAPTER_TYPE, gameID, playerID, playerNewPlayCount);
            asset.PlayerKFactor(ADAPTER_TYPE, gameID, playerID, playerNewKFct);
            asset.PlayerUncertainty(ADAPTER_TYPE, gameID, playerID, playerNewUncertainty);
            asset.PlayerLastPlayed(ADAPTER_TYPE, gameID, playerID, currDateTime);

            // [2016.11.14] modified
            // [SC] storing updated scenario data
            asset.ScenarioRating(ADAPTER_TYPE, gameID, scenarioID, scenarioNewRating);
            asset.ScenarioPlayCount(ADAPTER_TYPE, gameID, scenarioID, scenarioNewPlayCount);
            asset.ScenarioKFactor(ADAPTER_TYPE, gameID, scenarioID, scenarioNewKFct);
            asset.ScenarioUncertainty(ADAPTER_TYPE, gameID, scenarioID, scenarioNewUncertainty);
            asset.ScenarioLastPlayed(ADAPTER_TYPE, gameID, scenarioID, currDateTime);

            // [SC] save changes to local XML file
            if (updateDatafiles) {
                asset.SaveAdaptationData();
            }

            // [SC] creating game log
            asset.CreateNewRecord(ADAPTER_TYPE, gameID, playerID, scenarioID, rt, correctAnswer, playerNewRating, scenarioNewRating, currDateTime, updateDatafiles); // [ASSET]
        }

        //////////////////////////////////////////////////////////////////////////////////////
        ////// START: functions for calculating matching scenario
        #region functions for calculating matching scenario

        /// <summary>
        /// Calculates expected beta for target scenario. Returns ID of a scenario with beta closest to the target beta.
        /// If two more scenarios match then scenario that was least played is chosen.  
        /// </summary>
        ///
        /// <exception cref="ArgumentException">    Thrown when one or more arguments
        ///                                         have unsupported or illegal values. </exception>
        ///
        /// <param name="gameID">   Identifier for the game. </param>
        /// <param name="playerID"> Identifier for the player. </param>
        ///
        /// <returns>
        /// A string.
        /// </returns>
        internal string TargetScenarioID(string gameID, string playerID) { // [SC][2016.03.14] CalculateTargetScenarioID => TargetScenarioID
            // [SC] get player rating.
            // [2016.11.14] modified
            double playerRating = asset.PlayerRating(ADAPTER_TYPE, gameID, playerID); // [ASSET]

            // [SC] get IDs of available scenarios
            List<string> scenarioIDList = asset.AllScenariosIDs(ADAPTER_TYPE, gameID); // [ASSET]
            if (scenarioIDList.Count == 0) {
                throw new System.ArgumentException("No scenarios found for adaptation " + ADAPTER_TYPE + " in game " + gameID);
            }

            // [SC] calculate min and max possible ratings for candidate scenarios
            double[] minMaxRatings = calcTargetBetas(playerRating); // [SC][2016.12.07] modified

            // [SC] info for the scenarios within the min/max rating range and with the lowest play count
            List<string> inScenarioID = new List<string>();
            double inMinPlayCount = 0;

            // [SC] info for the closest scenarios outside of the min/max rating range and the lowest play count
            List<string> outScenarioID = new List<string>();
            double outMinPlayCount = 0;
            double outMinDistance = 0;

            // [SC] iterate through the list of all scenarios
            foreach (string scenarioID in scenarioIDList) {
                if (String.IsNullOrEmpty(scenarioID)) {
                    throw new System.ArgumentException("Null scenario ID found for adaptation " + ADAPTER_TYPE + " in game " + gameID);
                }

                // [2016.11.14] modified
                double scenarioRating = asset.ScenarioRating(ADAPTER_TYPE, gameID, scenarioID); // [ASSET]
                double scenarioPlayCount = asset.ScenarioPlayCount(ADAPTER_TYPE, gameID, scenarioID); // [ASSET]

                // [SC] the scenario rating is within min/max rating range
                if (scenarioRating >= minMaxRatings[0] && scenarioRating <= minMaxRatings[1]) {
                    if (inScenarioID.Count == 0 || scenarioPlayCount < inMinPlayCount) {
                        inScenarioID.Clear();
                        inScenarioID.Add(scenarioID);
                        inMinPlayCount = scenarioPlayCount;
                    }
                    else if (scenarioPlayCount == inMinPlayCount) {
                        inScenarioID.Add(scenarioID);
                    }
                }
                else { // [SC] the scenario rating is outside of the min/max rating range
                    double distance = Math.Min(Math.Abs(scenarioRating - minMaxRatings[0]), Math.Abs(scenarioRating - minMaxRatings[1]));
                    if (outScenarioID.Count == 0 || distance < outMinDistance) {
                        outScenarioID.Clear();
                        outScenarioID.Add(scenarioID);
                        outMinDistance = distance;
                        outMinPlayCount = scenarioPlayCount;
                    }
                    else if (distance == outMinDistance && scenarioPlayCount < outMinPlayCount) {
                        outScenarioID.Clear();
                        outScenarioID.Add(scenarioID);
                        outMinPlayCount = scenarioPlayCount;
                    }
                    else if (distance == outMinDistance && scenarioPlayCount == outMinPlayCount) {
                        outScenarioID.Add(scenarioID);
                    }
                }
            }

            if (inScenarioID.Count() > 0) {
                return inScenarioID[SimpleRNG.Next(inScenarioID.Count())];
            }

            return outScenarioID[SimpleRNG.Next(outScenarioID.Count())];
        }

        /// <summary>
        /// Calculates a range for a target beta.
        /// </summary>
        ///
        /// <param name="theta"> The theta. </param>
        ///
        /// <returns>
        /// A two-element  array with min and max values of ratings.
        /// </returns>
        private double[] calcTargetBetas(double theta) {
            double[] randNums = new double[2];

            for (int index = 0; index < randNums.Length; index++) {
                double rndNum;
                do {
                    SimpleRNG.SetSeedFromRandom();
                    rndNum = SimpleRNG.GetNormal(TARGET_DISTR_MEAN, TARGET_DISTR_SD);
                } while (rndNum <= TARGET_LOWER_LIMIT || rndNum >= TARGET_UPPER_LIMIT || rndNum == 1 || rndNum == 0); // [SC][2016.03.14] || randomNum == 1 || randomNum == 0
                randNums[index] = rndNum;
            }

            Array.Sort(randNums);

            // [SC][2016.10.07] this is the old equation to calculate target beta
            // theta + Math.Log(randomNum / (1 - randomNum));

            double minBeta = theta + Math.Log((1 - randNums[1]) / randNums[1]); // [SC][2016.10.07] a modified version of the equation from the original data; better suits the data
            double maxBeta = theta + Math.Log((1 - randNums[0]) / randNums[0]);

            return new double[] { minBeta, maxBeta };
        }

        #endregion functions for calculating matching scenario
        ////// END: functions for calculating matching scenario
        //////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////
        ////// START: functions for calculating expected and actual scores
        #region functions for calculating expected and actual scores

        /// <summary>
        /// Calculates actual score given success/failure outcome and response time.
        /// </summary>
        ///
        /// <param name="correctAnswer">   should be either 0, for failure,
        ///                                         or 1 for success. </param>
        /// <param name="responseTime">    a response time in milliseconds. </param>
        /// <param name="itemMaxDuration">  maximum duration of time given to a
        ///                                 player to provide an answer. </param>
        ///
        /// <returns>
        /// actual score as a double.
        /// </returns>
        private double calcActualScore(double correctAnswer, double responseTime, double itemMaxDuration) {
            validateResponseTime(responseTime);
            validateItemMaxDuration(itemMaxDuration);

            double discrParam = getDiscriminationParam(itemMaxDuration);
            return (double)(((2.0 * correctAnswer) - 1.0) * ((discrParam * itemMaxDuration) - (discrParam * responseTime)));
        }

        /// <summary>
        /// Calculates expected score given player's skill rating and item's
        /// difficulty rating.
        /// </summary>
        ///
        /// <param name="playerTheta">     player's skill rating. </param>
        /// <param name="itemBeta">        item's difficulty rating. </param>
        /// <param name="itemMaxDuration">  maximum duration of time given to a
        ///                                 player to provide an answer. </param>
        ///
        /// <returns>
        /// expected score as a double.
        /// </returns>
        private double calcExpectedScore(double playerTheta, double itemBeta, double itemMaxDuration) {
            validateItemMaxDuration(itemMaxDuration);

            double weight = getDiscriminationParam(itemMaxDuration) * itemMaxDuration;

            double ratingDifference = playerTheta - itemBeta; // [SC][2016.01.07]
            if (ratingDifference == 0) ratingDifference = 0.001; // [SC][2016.01.07]

            double expFctr = (double)Math.Exp(2.0 * weight * ratingDifference); // [SC][2016.01.07]

            return (weight * ((expFctr + 1.0) / (expFctr - 1.0))) - (1.0 / ratingDifference); // [SC][2016.01.07]
        }

        /// <summary>
        /// Calculates discrimination parameter a_i necessary to calculate expected
        /// and actual scores.
        /// </summary>
        ///
        /// <param name="itemMaxDuration">  maximum duration of time given to a
        ///                                 player to provide an answer; should be
        ///                                 player. </param>
        ///
        /// <returns>
        /// discrimination parameter a_i as double number.
        /// </returns>
        private double getDiscriminationParam(double itemMaxDuration) {
            return (double)(1.0 / itemMaxDuration);
        }

        #endregion functions for calculating expected and actual scores
        ////// END: functions for calculating expected and actual scores
        //////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////
        ////// START: functions for calculating rating uncertainties
        #region functions for calculating rating uncertainties

        /// <summary>
        /// Calculates a new uncertainty for the theta rating.
        /// </summary>
        ///
        /// <param name="currThetaU">       current uncertainty value for theta
        ///                                 rating. </param>
        /// <param name="currDelayCount">   the current number of consecutive days
        ///                                 the player has not played. </param>
        ///
        /// <returns>
        /// a new uncertainty value for theta rating.
        /// </returns>
        private double calcThetaUncertainty(double currThetaU, double currDelayCount) {
            double newThetaU = currThetaU - (1.0 / maxPlay) + (currDelayCount / maxDelay);
            if (newThetaU < 0) newThetaU = 0.0;
            else if (newThetaU > 1) newThetaU = 1.0;
            return newThetaU;
        }

        /// <summary>
        /// Calculates a new uncertainty for the beta rating.
        /// </summary>
        ///
        /// <param name="currBetaU">        current uncertainty value for the beta
        ///                                 rating. </param>
        /// <param name="currDelayCount">   the current number of consecutive days
        ///                                 the item has not beein played. </param>
        ///
        /// <returns>
        /// a new uncertainty value for the beta rating.
        /// </returns>
        private double calcBetaUncertainty(double currBetaU, double currDelayCount) {
            double newBetaU = currBetaU - (1.0 / maxPlay) + (currDelayCount / maxDelay);
            if (newBetaU < 0) newBetaU = 0.0;
            else if (newBetaU > 1) newBetaU = 1.0;
            return newBetaU;
        }

        #endregion functions for calculating rating uncertainties
        ////// END: functions for calculating rating uncertainties
        //////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////
        ////// START: functions for calculating k factors
        #region functions for calculating k factors

        /// <summary>
        /// Calculates a new K factor for theta rating
        /// </summary>
        ///
        /// <param name="currThetaU">   current uncertainty for the theta rating</param>
        /// <param name="currBetaU">    current uncertainty for the beta rating</param>
        /// 
        /// <returns>a double value of a new K factor for the theta rating</returns>
        private double calcThetaKFctr(double currThetaU, double currBetaU) {
            return kConst * (1 + (kUp * currThetaU) - (kDown * currBetaU));
        }

        /// <summary>
        /// Calculates a new K factor for the beta rating
        /// </summary>
        /// 
        /// <param name="currThetaU">   current uncertainty fot the theta rating</param>
        /// <param name="currBetaU">    current uncertainty for the beta rating</param>
        /// 
        /// <returns>a double value of a new K factor for the beta rating</returns>
        private double calcBetaKFctr(double currThetaU, double currBetaU) {
            return kConst * (1 + (kUp * currBetaU) - (kDown * currThetaU));
        }

        #endregion functions for calculating k factors
        ////// END: functions for calculating k factors
        //////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////
        ////// START: functions for calculating theta and beta ratings
        #region functions for calculating theta and beta ratings

        /// <summary>
        /// Calculates a new theta rating.
        /// </summary>
        ///
        /// <param name="currTheta">   current theta rating. </param>
        /// <param name="thetaKFctr">  K factor for the theta rating. </param>
        /// <param name="actualScore"> actual performance score. </param>
        /// <param name="expectScore"> expected performance score. </param>
        ///
        /// <returns>
        /// a double value for the new theta rating.
        /// </returns>
        private double calcTheta(double currTheta, double thetaKFctr, double actualScore, double expectScore) {
            return currTheta + (thetaKFctr * (actualScore - expectScore));
        }

        /// <summary>
        /// Calculates a new beta rating.
        /// </summary>
        ///
        /// <param name="currBeta">    current beta rating. </param>
        /// <param name="betaKFctr">   K factor for the beta rating. </param>
        /// <param name="actualScore"> actual performance score. </param>
        /// <param name="expectScore"> expected performance score. </param>
        ///
        /// <returns>
        /// a double value for new beta rating.
        /// </returns>
        private double calcBeta(double currBeta, double betaKFctr, double actualScore, double expectScore) {
            return currBeta + (betaKFctr * (expectScore - actualScore));
        }

        #endregion functions for calculating theta and beta ratings
        ////// END: functions for calculating theta and beta ratings
        //////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////
        ////// START: tester functions
        #region value tester functions

        //// [TODO][SC] what to do with expceptions: let the program crash?, or catch them?, or let to propagate to the main method? create a log file?
        /// <summary>
        /// Tests the validity of the value representing the response time.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">    Thrown when one or more arguments
        ///                                         have unsupported or illegal values. </exception>
        ///
        /// <param name="responseTime"> . </param>
        private void validateResponseTime(double responseTime) {
            if (responseTime == 0) throw new System.ArgumentException("Parameter cannot be 0.", "responseTime");
            if (responseTime < 0) throw new System.ArgumentException("Parameter cannot be negative.", "responseTime");
        }

        /// <summary>
        /// Tests the validity of the value representing the max amount of time to
        /// respond.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">    Thrown when one or more arguments
        ///                                         have unsupported or illegal values. </exception>
        ///
        /// <param name="itemMaxDuration"> . </param>
        private void validateItemMaxDuration(double itemMaxDuration) {
            if (itemMaxDuration == 0) throw new System.ArgumentException("Parameter cannot be 0.", "itemMaxDuration");
            if (itemMaxDuration < 0) throw new System.ArgumentException("Parameter cannot be negative.", "itemMaxDuration");
        }

        #endregion value tester functions
        ////// END: tester functions
        //////////////////////////////////////////////////////////////////////////////////////

        #endregion Methods
    }
}