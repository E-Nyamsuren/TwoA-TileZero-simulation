#region Header

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
// [2016.12.14]
//      - [SC] updated 'calcTargetBetas' to calculate a fuzzy interval consisting of four rating values
// [2016.12.15]
//      - [SC] changed TargetScenarioID method to use a fuzzy interval to decide on the recommended scenario
// [2017.01.03]
//      - [SC] added 'validateCorrectAnswer' method
//      - [SC] modified body of 'calcActualScore' method
//      - [SC] changed 'calcActualScore' method to internal instead of private
//      - [SC] added setter to 'ProvDate' property
//      - [SC] renamed property 'TargetDistributionMean' to 'TargetDistrMean'
// [2017.01.04]
//      - [SC] modified 'TargetDistrMean' property
//      - [SC] added 'TargetLowerLimit' property
//      - [SC] added 'TargetUpperLimit' property
//      - [SC] added 'TargetDistrSD' property
//      - [SC] added 'FiSDMultiplier' property
//      - [SC] modified 'calcTargetBetas' method
// [2017.01.05]
//      - [SC] modified 'DifficultyAdapter(TwoA asset)' contructor
//      - [SC] added 'setDefaultTargetDistribution' method
//      - [SC] added 'setTargetDistribution' method
//      - [SC] modified properties MaxDelay, MaxPlay, and ProvU
//      - [SC] removed exception throws from 'UpdateRatings'
//      - [SC] removed exception throws from 'TargetScenarioID'
//      - [SC] removed exception throws from validateCorrectAnswer, validateResponseTime, validateItemMaxDuration
//  [2017.01.12]
//      - [SC] added 'log(Severity severity, string msg)' and 'log(string msg)' methods
//
//
    
//
// [TODO]: 
//      - transaction style update of player and scenario data; no partial update should be possible; if any value fails to update then all values should fail to update
//      - provU and provDate are not used anywhere yet

#endregion Header

namespace TwoA
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    using AssetPackage;

    internal class DifficultyAdapter
    {
        #region Fields

        private TwoA asset; // [ASSET]

        // [SC]
        private const string ADAPTER_TYPE = "Game difficulty - Player skill";

        private const double DEF_THETA = 0.01; // [SC][2016.01.07]

        private const string TIMESTAMP_FORMAT = "s"; // Sortable DateTime as used in XML serializing : 's' -> 'yyyy-mm-ddThh:mm:ss'

        private const double SCORE_ERROR_CODE = -9999;

        private const double DISTR_LOWER_LIMIT = 0.001;     // [SC] lower limit of any probability value
        private const double DISTR_UPPER_LIMIT = 0.999;     // [SC] upper limit of any probability value

        #endregion Fields

        #region Consts, Fields, Properties

        /// <summary>
        /// Gets the prov theta.
        /// </summary>
        public double ProvTheta {
            get { return DEF_THETA; }
        }

        /// <summary>
        /// Gets the type of the adapter
        /// </summary>
        public string Type {
            get { return ADAPTER_TYPE; }
        }

        /// <summary>
        /// Getter for a code indicating error in calculating score. 
        /// </summary>
        public double ScoreErrorCode {
            get { return SCORE_ERROR_CODE; }
        }

        //////////////////////////////////////////////////////////////////////////////////////
        ////// START: const, fields, and properties for calculating target betasTargetDistributionMean
        #region const, fields, and properties for calculating target betas

        private const double TARGET_DISTR_MEAN = 0.75;      // [SC] default value for 'targetDistrMean' field
        private const double TARGET_DISTR_SD = 0.1;         // [SC] default value for 'targetDistrSD' field
        private const double TARGET_LOWER_LIMIT = 0.50;     // [SC] default value for 'targetLowerLimit' field
        private const double TARGET_UPPER_LIMIT = 1.0;      // [SC] default value for 'targetUpperLimit' field

        private const double FI_SD_MULTIPLIER = 1.0;        // [SC] multipler for SD used to calculate the means of normal distributions used to decide on lower and upper bounds of the supports in a fuzzy interval

        private double targetDistrMean;
        private double targetDistrSD;
        private double targetLowerLimit;
        private double targetUpperLimit;

        private double fiSDMultiplier;

        /// <summary>
        /// Getter for target distribution mean. See 'setTargetDistribution' method for setting a value.
        /// </summary>
        internal double TargetDistrMean {
            get { return targetDistrMean; }
            private set { targetDistrMean = value; }
        }

        /// <summary>
        /// Getter for target distribution standard deviation. See 'setTargetDistribution' method for setting a value.
        /// </summary>
        internal double TargetDistrSD {
            get { return targetDistrSD; }
            private set { targetDistrSD = value; }
        }

        /// <summary>
        /// Getter for target distribution lower limit. See 'setTargetDistribution' method for setting a value.
        /// </summary>
        internal double TargetLowerLimit {
            get { return targetLowerLimit; }
            private set { targetLowerLimit = value; }
        }

        /// <summary>
        /// Getter for target distribution upper limit. See 'setTargetDistribution' method for setting a value.
        /// </summary>
        internal double TargetUpperLimit {
            get { return targetUpperLimit; }
            private set { targetUpperLimit = value; }
        }

        /// <summary>
        /// Getter/setter for a weight used to calculate distribution means for a fuzzy selection algorithm.
        /// </summary>
        internal double FiSDMultiplier {
            get { return fiSDMultiplier; }
            set {
                if (value <= 0) {
                    log(Severity.Warning,
                        String.Format("In FiSDMultiplier: The standard deviation multiplier '{0}' is less than or equal to 0."
                            + "Setting to the default value '{1}'.", value, FI_SD_MULTIPLIER));

                    fiSDMultiplier = FI_SD_MULTIPLIER;
                }
                else {
                    fiSDMultiplier = value;
                }
            }
        }

        /// <summary>
        /// Sets target distribution parameters to their default values.
        /// </summary>
        internal void setDefaultTargetDistribution() {
            setTargetDistribution(TARGET_DISTR_MEAN, TARGET_DISTR_SD, TARGET_LOWER_LIMIT, TARGET_UPPER_LIMIT);
        }

        // [TEST]
        /// <summary>
        /// Sets target distribution parameters to custom values.
        /// </summary>
        /// 
        /// <param name="tDistrMean">   Dstribution mean</param>
        /// <param name="tDistrSD">     Distribution standard deviation</param>
        /// <param name="tLowerLimit">  Distribution lower limit</param>
        /// <param name="tUpperLimit">  Distribution upper limit</param>
        internal void setTargetDistribution(double tDistrMean, double tDistrSD, double tLowerLimit, double tUpperLimit) {
            bool validValuesFlag = true;

            // [SD] setting distribution mean
            if (tDistrMean <= 0 || tDistrMean >= 1) {
                log(Severity.Warning,
                    String.Format("In setTargetDistribution: The target distribution mean '{0}' is not within the open interval (0, 1).", tDistrMean));
                
                validValuesFlag = false;
            }

            // [SC] setting distribution SD
            if (tDistrSD <= 0 || tDistrSD >= 1) {
                log(Severity.Warning,
                    String.Format("In setTargetDistribution: The target distribution standard deviation '{0}' is not within the open interval (0, 1).", tDistrSD));

                validValuesFlag = false;
            }

            // [SC] setting distribution lower limit
            if (tLowerLimit < 0 || tLowerLimit > 1) {
                log(Severity.Warning,
                    String.Format("In setTargetDistribution: The lower limit of distribution '{0}' is not within the closed interval [0, 1].", tLowerLimit));

                validValuesFlag = false;
            }
            else if (tLowerLimit >= tDistrMean) {
                log(Severity.Warning,
                    String.Format("In setTargetDistribution: The lower limit of distribution '{0}' is bigger than or equal to the mean of the distribution '{1}'."
                        , tLowerLimit, tDistrMean));

                validValuesFlag = false;
            }

            // [SC] setting distribution upper limit
            if (tUpperLimit < 0 || tUpperLimit > 1) {
                log(Severity.Warning,
                    String.Format("In setTargetDistribution: The upper limit of distribution '{0}' is not within the closed interval [0, 1].", tUpperLimit));

                validValuesFlag = false;
            }
            else if (tUpperLimit <= tDistrMean) {
                log(Severity.Warning,
                    String.Format("In setTargetDistribution: The upper limit of distribution '{0}' is less than or equal to the mean of the distribution {1}."
                        , tUpperLimit, tDistrMean));

                validValuesFlag = false;
            }

            if (validValuesFlag) {
                TargetDistrMean = tDistrMean;
                TargetDistrSD = tDistrSD;
                TargetLowerLimit = tLowerLimit;
                TargetUpperLimit = tUpperLimit;
            }
            else {
                log(Severity.Warning,
                    String.Format("In setTargetDistribution: Invalid value combination is found. Setting parameters to their default values."));

                TargetDistrMean = TARGET_DISTR_MEAN;
                TargetDistrSD = TARGET_DISTR_SD;
                TargetLowerLimit = TARGET_LOWER_LIMIT;
                TargetUpperLimit = TARGET_UPPER_LIMIT;
            }
        }

        #endregion const, fields, and properties for calculating target betas
        ////// END: const, fields, and properties for calculating target betas
        //////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////
        ////// START: const, fields, and properties for calculating rating uncertainties
        #region const, fields, and properties for calculating rating uncertainties

        private const double DEF_MAX_DELAY = 30;                // [SC] The default value for the max number of days after which player's or item's undertainty reaches the maximum
        private const double DEF_MAX_PLAY = 40;                 // [SC] The default value for the max number of administrations that should result in minimum uncertaint in item's or player's ratings
        private const double DEF_U = 1.0;                       // [SC] The default value for the provisional uncertainty to be assigned to an item or player
        private const string DEF_DATE = "2015-01-01T01:01:01";

        private double maxDelay;        // [SC] set to DEF_MAX_DELAY in the constructor
        private double maxPlay;         // [SC] set to DEF_MAX_PLAY in the constructor
        private double provU;           // [SC] set to DEF_U in the constructor
        private string provDate;        // [SC] set to DEF_DATE in the constructor

        /// <summary>
        /// Gets or sets the maximum delay.
        /// </summary>
        internal double MaxDelay {
            get { return maxDelay; }
            set {
                if (value <= 0) {
                    log(Severity.Warning,
                        String.Format("In MaxDelay: The maximum number of delay days '{0}' should be higher than 0."
                            + "Setting to the default value '{1}'.", value, DEF_MAX_DELAY));

                    maxDelay = DEF_MAX_DELAY;
                }
                else {
                    maxDelay = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum play.
        /// </summary>
        internal double MaxPlay {
            get { return maxPlay; }
            set {
                if (value <= 0) {
                    log(Severity.Warning,
                        String.Format("In MaxPlay: The maximum administration parameter '{0}' should be higher than 0."
                            + "Setting to the default value '{1}'.", value, DEF_MAX_PLAY));

                    maxPlay = DEF_MAX_PLAY;
                }
                else { 
                    maxPlay = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the provisional uncertainty.
        /// </summary>
        internal double ProvU {
            get { return provU; }
            set {
                if (0 > value || value > 1) { // [SC][2016.01.07] "0 < value" => "0 > value"
                    log(Severity.Warning,
                        String.Format("In ProvU: Provisional uncertainty value '{0}' should be between 0 and 1."
                            + "Setting to the default value '{1}'.", value, DEF_U));

                    provU = DEF_U;
                }
                else {
                    provU = value;
                }
            }
        }

        /// <summary>
        /// Gets the provisional play date.
        /// </summary>
        internal string ProvDate {
            get { return provDate; }
            set { provDate = value; } // [SC][2017.01.03]
        }

        #endregion const, fields, and properties for calculating rating uncertainties
        ////// END: const, fields, and properties for calculating rating uncertainties
        //////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////
        ////// START: const, fields, and properties for calculating k factors
        #region const, fields, and properties for calculating k factors

        private const double DEF_K = 0.0075;    // [SC] The default value for the K constant when there is no uncertainty
        private const double DEF_K_UP = 4.0;    // [SC] the default value for the upward uncertainty weight
        private const double DEF_K_DOWN = 0.5;  // [SC] The default value for the downward uncertainty weight

        private double kConst;          // [SC] set to DEF_K in the constructor
        private double kUp;             // [SC] set to DEF_K_UP in the constructor
        private double kDown;           // [SC] set to DEF_K_DOWN in the constructor

        /// <summary>
        /// Getter/setter for the K constant.
        /// </summary>
        private double KConst {
            get { return kConst; }
            set {
                if (value < 0) {
                    log(Severity.Warning,
                        String.Format("In KConst: K constant '{0}' cannot be a negative number."
                            + "Setting to the default value '{1}'.", value, DEF_K));

                    kConst = DEF_K;
                }
                else {
                    kConst = value;
                }
            }
        }

        /// <summary>
        /// Getter/setter for the upward uncertainty weight.
        /// </summary>
        private double KUp {
            get { return kUp; }
            set {
                if (value < 0) {
                    log(Severity.Warning,
                        String.Format("In KUp: The upward uncertianty weight '{0}' cannot be a negative number."
                            + "Setting to the default value '{1}'.", value, DEF_K_UP));

                    kUp = DEF_K_UP;
                }
                else {
                    kUp = value;
                }
            }
        }

        /// <summary>
        /// Getter/setter for the downward uncertainty weight.
        /// </summary>
        private double KDown {
            get { return kDown; }
            set {
                if (value < 0) {
                    log(Severity.Warning,
                        String.Format("In KDown: The downward uncertainty weight '{0}' cannot be a negative number."
                            + "Setting to the default value '{1}'.", value, DEF_K_DOWN));

                    kDown = DEF_K_DOWN;
                }
                else {
                    kDown = value;
                }
            }
        }

        #endregion const, fields, and properties for calculating k factors
        ////// END: properties for calculating k factors
        //////////////////////////////////////////////////////////////////////////////////////

        #endregion Consts, Fields, Properties

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
            this.asset = asset; // [ASSET]

            setDefaultTargetDistribution();

            MaxPlay = DEF_MAX_PLAY;
            MaxDelay = DEF_MAX_DELAY;

            KConst = DEF_K;
            KUp = DEF_K_UP;
            KDown = DEF_K_DOWN;

            ProvU = DEF_U;

            ProvDate = DEF_DATE;
        }

        #endregion Constructors

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
            if (asset == null) {
                log(Severity.Error, "In UpdateRatings: Unable to update ratings. Asset instance is not detected.");
                return;
            }

            if (!(validateCorrectAnswer(correctAnswer) && validateResponseTime(rt))) {
                log(Severity.Error, "In UpdateRatings: Unable to update ratings. Invalid response time and/or accuracy detected.");
                return;
            }

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
                log(Severity.Error, "In UpdateRatings: Unable to update ratings. Player data is missing.");
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
                log(Severity.Error, "In UpdateRatings: Unable to update ratings. Scenario data is missing.");
                return;
            }

            DateTime currDateTime = DateTime.UtcNow;

            // [SC] parsing player data
            double playerLastPlayedDays = (currDateTime - playerLastPlayed).Days;
            if (playerLastPlayedDays > DEF_MAX_DELAY) {
                playerLastPlayedDays = MaxDelay;
            }

            // [SC] parsing scenario data
            double scenarioLastPlayedDays = (currDateTime - scenarioLastPlayed).Days;
            if (scenarioLastPlayedDays > DEF_MAX_DELAY) {
                scenarioLastPlayedDays = MaxDelay;
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
        /// <param name="gameID">   Identifier for the game. </param>
        /// <param name="playerID"> Identifier for the player. </param>
        ///
        /// <returns>
        /// A string.
        /// </returns>
        internal string TargetScenarioID(string gameID, string playerID) { // [SC][2016.03.14] CalculateTargetScenarioID => TargetScenarioID
            if (asset == null) {
                log(Severity.Error, "In TargetScenarioID: Unable to recommend a scenario. Asset instance is not detected.");
                return null;
            }

            // [SC] get player rating.
            // [2016.11.14] modified
            double playerRating = asset.PlayerRating(ADAPTER_TYPE, gameID, playerID); // [ASSET]

            // [SC] get IDs of available scenarios
            List<string> scenarioIDList = asset.AllScenariosIDs(ADAPTER_TYPE, gameID); // [ASSET]
            if (scenarioIDList.Count == 0) {
                log(Severity.Error, 
                    String.Format("In TargetScenarioID: No scenarios found for adaptation '{0}' in game '{1}'.", ADAPTER_TYPE, gameID));
                return null;
            }

            // [SC] calculate min and max possible ratings for candidate scenarios
            double[] ratingFI = calcTargetBetas(playerRating); // [SC][2016.12.14] fuzzy interval for rating

            // [SC] info for the scenarios within the core rating range and with the lowest play count
            List<string> coreScenarioID = new List<string>();
            double coreMinPlayCount = 0;

            // [SC] info for the scenarios within the support rating range and with the lowest play count
            List<string> supportScenarioID = new List<string>();
            double supportMinPlayCount = 0;

            // [SC] info for the closest scenarios outside of the fuzzy interval and the lowest play count
            List<string> outScenarioID = new List<string>();
            double outMinPlayCount = 0;
            double outMinDistance = 0;

            // [SC] iterate through the list of all scenarios
            foreach (string scenarioID in scenarioIDList) {
                if (String.IsNullOrEmpty(scenarioID)) {
                    log(Severity.Error,
                        String.Format("In TargetScenarioID: Null scenario ID found for adaptation '{0}' in game '{1}'.", ADAPTER_TYPE, gameID));
                    return null;
                }

                // [2016.11.14] modified
                double scenarioRating = asset.ScenarioRating(ADAPTER_TYPE, gameID, scenarioID); // [ASSET]
                double scenarioPlayCount = asset.ScenarioPlayCount(ADAPTER_TYPE, gameID, scenarioID); // [ASSET]

                // [SC] the scenario rating is within the core rating range
                if (scenarioRating >= ratingFI[1] && scenarioRating <= ratingFI[2]) {
                    if (coreScenarioID.Count == 0 || scenarioPlayCount < coreMinPlayCount) {
                        coreScenarioID.Clear();
                        coreScenarioID.Add(scenarioID);
                        coreMinPlayCount = scenarioPlayCount;
                    }
                    else if (scenarioPlayCount == coreMinPlayCount) {
                        coreScenarioID.Add(scenarioID);
                    }
                }
                // [SC] the scenario rating is outside of the core rating range but within the support range
                else if (scenarioRating >= ratingFI[0] && scenarioRating <= ratingFI[3]) {
                    if (supportScenarioID.Count == 0 || scenarioPlayCount < supportMinPlayCount) {
                        supportScenarioID.Clear();
                        supportScenarioID.Add(scenarioID);
                        supportMinPlayCount = scenarioPlayCount;
                    }
                    else if (scenarioPlayCount == supportMinPlayCount) {
                        supportScenarioID.Add(scenarioID);
                    }
                }
                // [SC] the scenario rating is outside of the support rating range
                else {
                    double distance = Math.Min(Math.Abs(scenarioRating - ratingFI[1]), Math.Abs(scenarioRating - ratingFI[2]));
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

            if (coreScenarioID.Count() > 0) {
                return coreScenarioID[SimpleRNG.Next(coreScenarioID.Count())];
            }
            else if (supportScenarioID.Count() > 0) {
                return supportScenarioID[SimpleRNG.Next(supportScenarioID.Count())];
            }
            return outScenarioID[SimpleRNG.Next(outScenarioID.Count())];
        }

        /// <summary>
        /// Calculates a fuzzy interval for a target beta.
        /// </summary>
        ///
        /// <param name="theta"> The theta. </param>
        ///
        /// <returns>
        /// A four-element array of ratings (in an ascending order) representing lower and upper bounds of the support and core
        /// </returns>
        private double[] calcTargetBetas(double theta) {
            // [SC] mean of one-sided normal distribution from which to derive the lower bound of the support in a fuzzy interval
            double lower_distr_mean = TargetDistrMean - (FiSDMultiplier * TargetDistrSD);
            if (lower_distr_mean < DISTR_LOWER_LIMIT) {
                lower_distr_mean = DISTR_LOWER_LIMIT;
            }
            // [SC] mean of one-sided normal distribution from which to derive the upper bound of the support in a fuzzy interval
            double upper_distr_mean = TargetDistrMean + (FiSDMultiplier * TargetDistrSD);
            if (upper_distr_mean > DISTR_UPPER_LIMIT) {
                upper_distr_mean = DISTR_UPPER_LIMIT;
            }

            // [SC] the array stores four probabilities (in an ascending order) that represent lower and upper bounds of the support and core 
            double[] randNums = new double[4];

            // [SC] calculating two probabilities as the lower and upper bounds of the core in a fuzzy interval
            double rndNum;
            for (int index = 1; index < 3; index++) {
                while (true) {
                    SimpleRNG.SetSeedFromRandom();
                    rndNum = SimpleRNG.GetNormal(TargetDistrMean, TargetDistrSD);

                    if (rndNum > TargetLowerLimit || rndNum < TargetUpperLimit) {
                        if (rndNum < DISTR_LOWER_LIMIT) {
                            rndNum = DISTR_LOWER_LIMIT;
                        }
                        else if (rndNum > DISTR_UPPER_LIMIT) {
                            rndNum = DISTR_UPPER_LIMIT;
                        }
                        break;
                    }
                }
                randNums[index] = rndNum;
            }
            // [SC] sorting lower and upper bounds of the core in an ascending order
            if (randNums[1] > randNums[2]) {
                double temp = randNums[1];
                randNums[1] = randNums[2];
                randNums[2] = temp;
            }

            // [SC] calculating probability that is the lower bound of the support in a fuzzy interval
            while (true) {
                SimpleRNG.SetSeedFromRandom();
                rndNum = SimpleRNG.GetNormal(lower_distr_mean, TargetDistrSD, true);

                if (rndNum < randNums[1]) {
                    if (rndNum < DISTR_LOWER_LIMIT) {
                        rndNum = DISTR_LOWER_LIMIT;
                    }
                    break;
                }
            }
            randNums[0] = rndNum;

            // [SC] calculating probability that is the upper bound of the support in a fuzzy interval
            while (true) {
                SimpleRNG.SetSeedFromRandom();
                rndNum = SimpleRNG.GetNormal(upper_distr_mean, TargetDistrSD, false);

                if (rndNum > randNums[2]) {
                    if (rndNum > DISTR_UPPER_LIMIT) {
                        rndNum = DISTR_UPPER_LIMIT;
                    }
                    break;
                }
            }
            randNums[3] = rndNum;

            // [SC][2016.10.07] this is the old equation to calculate target beta
            // theta + Math.Log(randomNum / (1 - randomNum));

            // [SC] tralsating probability bounds of a fuzzy interval into a beta values
            double lowerLimitBeta = theta + Math.Log((1 - randNums[3]) / randNums[3]);
            double minBeta = theta + Math.Log((1 - randNums[2]) / randNums[2]); // [SC][2016.10.07] a modified version of the equation from the original data; better suits the data
            double maxBeta = theta + Math.Log((1 - randNums[1]) / randNums[1]);
            double upperLimitBeta = theta + Math.Log((1 - randNums[0]) / randNums[0]);

            return new double[] { lowerLimitBeta, minBeta, maxBeta, upperLimitBeta };
        }

        #endregion functions for calculating matching scenario
        ////// END: functions for calculating matching scenario
        //////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////
        ////// START: OLD functions for calculating matching scenario
        #region OLD functions for calculating matching scenario

        /// <summary>
        /// THIS METHOD IS DEPRECIATED; DO NOT USE IT. FOR RESEARCH USE ONLY.
        /// Calculates expected beta for target scenario. Returns ID of a scenario with beta closest to the target beta.
        /// If two more scenarios match then scenario that was least played is chosen.
        /// </summary>
        ///
        /// <returns>
        /// A string.
        /// </returns>
        internal string TargetScenarioIDOld(string gameID, string playerID, bool useNewEquation) {
            if (asset == null) {
                log(Severity.Error, "In TargetScenarioID: Unable to recommend a scenario. Asset instance is not detected.");
                return null;
            }

            // [SC] get player rating.
            double playerRating = asset.PlayerRating(ADAPTER_TYPE, gameID, playerID);

            // [SC] get IDs of available scenarios
            List<string> scenarioIDList = asset.AllScenariosIDs(ADAPTER_TYPE, gameID);
            if (scenarioIDList.Count == 0) {
                log(Severity.Error,
                    String.Format("In TargetScenarioID: No scenarios found for adaptation '{0}' in game '{1}'.", ADAPTER_TYPE, gameID));
                return null;
            }

            double targetScenarioRating = 0;
            double minDistance = 0;
            string minDistanceScenarioID = null;
            double minPlayCount = 0;

            if (useNewEquation) {
                targetScenarioRating = calcTargetBetaNew(playerRating);
            } else {
                targetScenarioRating = calcTargetBetaOld(playerRating);
            }

            foreach (string scenarioID in scenarioIDList) {
                if (String.IsNullOrEmpty(scenarioID)) {
                    throw new System.ArgumentException("Null scenario ID found for adaptation " + ADAPTER_TYPE + " in game " + gameID);
                }

                double scenarioPlayCount = asset.ScenarioPlayCount(ADAPTER_TYPE, gameID, scenarioID); 
                double scenarioRating = asset.ScenarioRating(ADAPTER_TYPE, gameID, scenarioID);

                double distance = Math.Abs(scenarioRating - targetScenarioRating);
                if (minDistanceScenarioID == null || distance < minDistance) {
                    minDistance = distance;
                    minDistanceScenarioID = scenarioID;
                    minPlayCount = scenarioPlayCount;
                }
                else if (distance == minDistance && scenarioPlayCount < minPlayCount) {
                    minDistance = distance;
                    minDistanceScenarioID = scenarioID;
                    minPlayCount = scenarioPlayCount;
                }
            }

            return minDistanceScenarioID;
        }

        /// <summary>
        /// THIS METHOD IS DEPRECIATED; DO NOT USE IT. FOR RESEARCH USE ONLY.
        /// </summary>
        private double calcTargetBetaOld(double theta) {
            double randomNum;
            SimpleRNG.SetSeedFromSystemTime();
            do {
                randomNum = SimpleRNG.GetNormal(TARGET_DISTR_MEAN, TARGET_DISTR_SD);
            } while (randomNum <= TARGET_LOWER_LIMIT || randomNum >= TARGET_UPPER_LIMIT || randomNum == 1 || randomNum == 0); // [SC][2016.03.14] || randomNum == 1 || randomNum == 0

            return theta + Math.Log(randomNum / (1 - randomNum));
        }

        /// <summary>
        /// THIS METHOD IS DEPRECIATED; DO NOT USE IT. FOR RESEARCH USE ONLY.
        /// </summary>
        private double calcTargetBetaNew(double theta) {
            double randomNum;
            SimpleRNG.SetSeedFromSystemTime();
            do {
                randomNum = SimpleRNG.GetNormal(TARGET_DISTR_MEAN, TARGET_DISTR_SD);
            } while (randomNum <= TARGET_LOWER_LIMIT || randomNum >= TARGET_UPPER_LIMIT || randomNum == 1 || randomNum == 0); // [SC][2016.03.14] || randomNum == 1 || randomNum == 0

            return theta + Math.Log((1 - randomNum) / randomNum);
        }

        #endregion OLD functions for calculating matching scenario
        ////// END: OLD functions for calculating matching scenario
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
        /// <param name="responseTime">    a response time in milliseconds. </param>validateResponseTime
        /// <param name="itemMaxDuration">  maximum duration of time given to a
        ///                                 player to provide an answer. </param>
        ///
        /// <returns>
        /// actual score as a double.
        /// </returns>
        internal double calcActualScore(double correctAnswer, double responseTime, double itemMaxDuration) {
            if (!(validateCorrectAnswer(correctAnswer)
                && validateResponseTime(responseTime)
                && validateItemMaxDuration(itemMaxDuration))) {

                log(Severity.Error
                    , String.Format("In calcActualScore: Cannot calculate score. Invalid parameter detected. Returning error code '{0}'."
                        , ScoreErrorCode));

                return ScoreErrorCode;
            }

            // [SC][2017.01.03]
            if (responseTime > itemMaxDuration) {
                responseTime = itemMaxDuration;

                log(Severity.Warning
                    , String.Format("In calcActualScore: Response time '{0}' exceeds the item's max time duration '{1}'. Setting the response time to item's max duration."
                        , responseTime, itemMaxDuration));
            }

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
            if (ratingDifference == 0) { // [SC][2016.01.07]
                ratingDifference = 0.001;
            }

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
            double newThetaU = currThetaU - (1.0 / MaxPlay) + (currDelayCount / MaxDelay);
            if (newThetaU < 0) {
                newThetaU = 0.0;
            }
            else if (newThetaU > 1) {
                newThetaU = 1.0;
            }
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
            double newBetaU = currBetaU - (1.0 / MaxPlay) + (currDelayCount / MaxDelay);
            if (newBetaU < 0) {
                newBetaU = 0.0;
            }
            else if (newBetaU > 1) {
                newBetaU = 1.0;
            }
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
            return KConst * (1 + (KUp * currThetaU) - (KDown * currBetaU));
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
            return KConst * (1 + (KUp * currBetaU) - (KDown * currThetaU));
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

        /// <summary>
        /// Tests the validity of the value representing correctness of player's answer.
        /// </summary>
        /// 
        /// <param name="correctAnswer"> Player's answer. </param>
        /// 
        /// <returns>True if the value is valid</returns>
        private bool validateCorrectAnswer(double correctAnswer) { // [SC][2017.01.03]
            if (correctAnswer != 0 && correctAnswer != 1) {
                log(Severity.Error
                    , String.Format("In validateCorrectAnswer: Accuracy should be either 0 or 1. Current value is '{0}'.", correctAnswer));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Tests the validity of the value representing the response time.
        /// </summary>
        /// 
        /// <param name="responseTime">Response time in milliseconds</param>
        /// 
        /// <returns>True if the value is valid</returns>
        private bool validateResponseTime(double responseTime) {
            if (responseTime <= 0) {
                log(Severity.Error
                    , String.Format("In validateResponseTime: Response time cannot be 0 or negative. Current value is '{0}'.", responseTime));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Tests the validity of the value representing the max amount of time to respond.
        /// </summary>
        /// 
        /// <param name="itemMaxDuration">Time duration in mulliseconds</param>
        /// 
        /// <returns>True if the value is valid</returns>
        private bool validateItemMaxDuration(double itemMaxDuration) {
            if (itemMaxDuration <= 0) {
                log(Severity.Error
                    , String.Format("In validateItemMaxDuration: Max playable duration cannot be 0 or negative. Current value is '{0}'."
                        , itemMaxDuration));

                return false;
            }

            return true;
        }

        #endregion value tester functions
        ////// END: tester functions
        //////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////
        ////// START: misc methods
        #region misc methods

        /// <summary>
        /// Logs a message by default under a Severity.Information type
        /// </summary>
        /// 
        /// <param name="msg">      A message to be logged</param>
        private void log(string msg) {
            log(Severity.Information, msg);
        }

        /// <summary>
        /// Logs a message using assets's Log method
        /// </summary>
        /// 
        /// <param name="severity"> Message type</param>
        /// <param name="msg">      A message to be logged</param>
        private void log(Severity severity, string msg) {
            if (asset != null) {
                asset.Log(severity, msg);
            }
        }

        #endregion misc methods
        ////// END: misc methods
        //////////////////////////////////////////////////////////////////////////////////////

        #endregion Methods
    }
}