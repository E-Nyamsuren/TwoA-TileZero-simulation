################################################################################
###### START: before running the script verify the code in this comment block

## [SC] installing gplots package
if (!("gplots" %in% rownames(installed.packages()))) {
	install.package("gplots")
}
library("gplots")

## [SC] path where R source code is located
sourcepath <- "" 
## [SC] path where data is located
datapath <- "data/"

###### END: before running the script verify the code in this comment block
################################################################################

## [SC] number of blocks
simulationCount <- 10

dataList <- NULL

targetP <- 0.75
startRating <- 1.0

evolutionEqCount <- 5
evolutionEqStep <- 200

evolutionPlayerCount <- 6
evolutionPlayerStep <- 400

## [SC] starting ratings of scenarios
scenarioStartRatings <- c(-0.3685815, 0.2676741, 1.655582, 1.623623, 1.612617, 2.000194)
## [SC] objective difficulty indicators of scenarios
winProportions <- c(0, 0.02, 0.22575, 0.23200, 0.22550, 0.49450)

source(paste0(sourcepath, "commonCode.R"))

scenarioIDVC <- c("Very Easy AI", "Easy AI", "Medium Color AI", "Medium Shape AI", "Hard AI", "Very Hard AI")
simScenarioIDVC <- c("Medium Color AI", "Medium Shape AI", "Hard AI")
scenarioColors <- c("2", "3", "4", "5", "6", "orange")

openSimDatafile <- function(simulationID, simIndex) {
	return(read.table(paste0(datapath, "S", simulationID, "/", "datafile", simIndex, ".txt")
			, header=TRUE, stringsAsFactors=FALSE))
}

openSimRatingfile <- function(simulationID, simIndex) {
	return(read.table(paste0(datapath, "S", simulationID, "/", "ratingfile", simIndex, ".txt")
			, header=TRUE, stringsAsFactors=FALSE))
}

openAIDifficultyFile <- function() {
	return(read.table(paste0(datapath, "aiDifficulty.txt"), header=TRUE, stringsAsFactors=FALSE))
}

loadAllDatafiles <- function() {
	dataList <<- list()

	## [TODO]
	for(simulationID in 1:4) {
		dataDF <- NULL

		for(simIndex in 0:(simulationCount - 1)) {
			simDataDF <- openSimDatafile(simulationID, simIndex)
			simDataDF <- cbind(simDataDF, SimID = simIndex)
			simDataDF <- simDataDF[order(simDataDF$ID),]
			
			## [SC] since not all scenarios are played at all time, filling NAs in scenario ratings for each gameplay point
			simDataDF <- cbind(simDataDF, Scenario1=NA)
			simDataDF <- cbind(simDataDF, Scenario2=NA)
			simDataDF <- cbind(simDataDF, Scenario3=NA)
			simDataDF <- cbind(simDataDF, Scenario4=NA)
			simDataDF <- cbind(simDataDF, Scenario5=NA)
			simDataDF <- cbind(simDataDF, Scenario6=NA)

			simDataDF <- cbind(simDataDF, Scenario1F=NA)
			simDataDF <- cbind(simDataDF, Scenario2F=NA)
			simDataDF <- cbind(simDataDF, Scenario3F=NA)
			simDataDF <- cbind(simDataDF, Scenario4F=NA)
			simDataDF <- cbind(simDataDF, Scenario5F=NA)
			simDataDF <- cbind(simDataDF, Scenario6F=NA)

			scenarioCurrRatings <- scenarioStartRatings
			if (simulationID == 4) scenarioCurrRatings <- rep(startRating, length(scenarioIDVC))
			scenarioCurrFreq <- c(0, 0, 0, 0, 0, 0)

			for(rowIndex in 1:nrow(simDataDF)){
				scenarioID <- simDataDF$ScenarioID[rowIndex]
				scenarioRating <- simDataDF$ScenarioRating[rowIndex]

				for (scenarioIndex in 1:length(scenarioIDVC)) {
					if (scenarioID == scenarioIDVC[scenarioIndex]){
						scenarioCurrRatings[scenarioIndex] <- scenarioRating
						scenarioCurrFreq[scenarioIndex] <- scenarioCurrFreq[scenarioIndex] + 1
					}
					simDataDF[rowIndex, paste0("Scenario", scenarioIndex)] <- scenarioCurrRatings[scenarioIndex]
					simDataDF[rowIndex, paste0("Scenario", scenarioIndex, "F")] <- scenarioCurrFreq[scenarioIndex]
				}
			}

			if(is.null(dataDF)) {
				dataDF <- simDataDF
			} else {
				dataDF <- rbind(dataDF, simDataDF)
			}
		}

		evolutionCount <- evolutionEqCount
		evolutionStep <- evolutionEqStep

		if (simulationID == 4){
			evolutionCount <- evolutionPlayerCount
			evolutionStep <- evolutionPlayerStep
		}

		dataDF <- cbind(dataDF, EvolutionID = NA)
		prevEndPoint <- 0
		endPointVC <- numeric()
		for(evolIndex in 1:evolutionCount){
			currEndPoint <- evolIndex * evolutionStep
			endPointVC <- c(endPointVC, paste0(prevEndPoint+1, "-", currEndPoint))

			dataDF[(dataDF$ID > prevEndPoint & dataDF$ID <= currEndPoint), "EvolutionID"] <- evolIndex
			prevEndPoint <- currEndPoint
		}

		dataList[[simulationID]] <<- dataDF
	}
}

plotFrequencyRating <- function(dataDF, mainTitle){
	subOP <- par(mfrow=c(1,2), oma=c(0,0,2,0))

	indexVC <- c(3, 4, 5)
	
	##################################################################################

	scenariosDF <- dataDF[,c("ID", paste0("Scenario", indexVC, "F"))]
	minFreq <- min(scenariosDF[,paste0("Scenario", indexVC, "F")])
	maxFreq <- max(scenariosDF[,paste0("Scenario", indexVC, "F")])
	newPlot <- TRUE

	## [SC] plotting scenario frequencies
	for(scenarioIndex in indexVC) {
		scenarioDF <- scenariosDF[,c("ID", paste0("Scenario", scenarioIndex, "F"))]
		colnames(scenarioDF)[colnames(scenarioDF) == paste0("Scenario", scenarioIndex, "F")] <- "ScenarioFreq"

		scenarioFreqAvgDF <- aggregate(ScenarioFreq ~ ID, scenarioDF, mean)
		colnames(scenarioFreqAvgDF)[colnames(scenarioFreqAvgDF) == "ScenarioFreq"] <- "Mean" 
		scenarioFreqSEDF <- aggregate(ScenarioFreq ~ ID, scenarioDF, getSE)
		colnames(scenarioFreqSEDF)[colnames(scenarioFreqSEDF) == "ScenarioFreq"] <- "SE"
		scenarioFreqAvgDF <- merge(scenarioFreqAvgDF, scenarioFreqSEDF)
		scenarioFreqAvgDF <- scenarioFreqAvgDF[order(scenarioFreqAvgDF$ID),]

		if (newPlot) {
			plot(x=scenarioFreqAvgDF$ID, y=scenarioFreqAvgDF$Mean
				, type="l", lwd=2
				, col=scenarioColors[scenarioIndex]
				, ylim=c(0, 400)
				, main="Opponent's cumulative frequencies", xlab="Games", ylab="Frequency")
			newPlot <- FALSE
		} else {
			lines(x=scenarioFreqAvgDF$ID, y=scenarioFreqAvgDF$Mean, type="l", lwd=2, col=scenarioColors[scenarioIndex])
		}
		lines(x=scenarioFreqAvgDF$ID, y=scenarioFreqAvgDF$Mean - scenarioFreqAvgDF$SE, type="l", col=scenarioColors[scenarioIndex], lwd=1, lty=3)
		lines(x=scenarioFreqAvgDF$ID, y=scenarioFreqAvgDF$Mean + scenarioFreqAvgDF$SE, type="l", col=scenarioColors[scenarioIndex], lwd=1, lty=3)
	}

	## [SC] plotting legend
	legend("topright", legend=simScenarioIDVC, col=scenarioColors[3:5], lty=1, lwd=2)

	##################################################################################

	freqDistDF <- subset(scenariosDF, ID == max(scenariosDF$ID))
	distancesVC <- numeric()
	for(currRowIndex in 1:nrow(freqDistDF)){
		freqVC <- sort(as.numeric(freqDistDF[currRowIndex, paste0("Scenario", indexVC, "F")]))
		distance <- (freqVC[2] - freqVC[1]) + (freqVC[3] - freqVC[2])
		distancesVC <- c(distancesVC, distance)
	}

	print(paste0("Frequency distance mean: ", mean(distancesVC)))
	print(paste0("Frequency distance SD: ", sd(distancesVC)))
	print(paste0("Min frequency distance: ", min(distancesVC)))
	print(paste0("Max frequency distance: ", max(distancesVC)))

	##################################################################################

	scenariosDF <- dataDF[,c("ID", paste0("Scenario", indexVC))]
	minRating <- min(scenariosDF[,paste0("Scenario", indexVC)])
	maxRating <- max(scenariosDF[,paste0("Scenario", indexVC)])
	newPlot <- TRUE

	## [SC] plotting scenario ratings
	for(scenarioIndex in indexVC) {
		scenarioDF <- scenariosDF[,c("ID", paste0("Scenario", scenarioIndex))]
		colnames(scenarioDF)[colnames(scenarioDF) == paste0("Scenario", scenarioIndex)] <- "ScenarioRating"

		scenarioRatingAvgDF <- aggregate(ScenarioRating ~ ID, scenarioDF, mean)
		colnames(scenarioRatingAvgDF)[colnames(scenarioRatingAvgDF) == "ScenarioRating"] <- "Mean" 
		scenarioRatingSEDF <- aggregate(ScenarioRating ~ ID, scenarioDF, getSE)
		colnames(scenarioRatingSEDF)[colnames(scenarioRatingSEDF) == "ScenarioRating"] <- "SE"
		scenarioRatingAvgDF <- merge(scenarioRatingAvgDF, scenarioRatingSEDF)
		scenarioRatingAvgDF <- scenarioRatingAvgDF[order(scenarioRatingAvgDF$ID),]

		if (newPlot) {
			plot(x=scenarioRatingAvgDF$ID, y=scenarioRatingAvgDF$Mean
				, type="l", lwd=2
				, col=scenarioColors[scenarioIndex]
				, ylim=c(1.2, 1.7)
				, main="Changes in opponents' ratings", xlab="Games", ylab="Ratings")
			newPlot <- FALSE
		} else {
			lines(x=scenarioRatingAvgDF$ID, y=scenarioRatingAvgDF$Mean, type="l", lwd=2, col=scenarioColors[scenarioIndex])
		}
		lines(x=scenarioRatingAvgDF$ID, y=scenarioRatingAvgDF$Mean - scenarioRatingAvgDF$SE, type="l", col=scenarioColors[scenarioIndex], lwd=1, lty=3)
		lines(x=scenarioRatingAvgDF$ID, y=scenarioRatingAvgDF$Mean + scenarioRatingAvgDF$SE, type="l", col=scenarioColors[scenarioIndex], lwd=1, lty=3)
	}

	## [SC] plotting legend
	legend("topright", legend=simScenarioIDVC, col=scenarioColors[3:5], lty=1, lwd=2)

	##################################################################################
	
	title(main=mainTitle, outer=TRUE)

	par(subOP)
}

## [SC]
simOneTwoAccuracyOverIntervals <- function(reload=FALSE) {
	if (reload || length(dataList) == 0) {
		loadAllDatafiles()
	}

	subOP <- par(mfrow=c(1,1))

	for(simulationID in 1:2) {
		dataDF <- dataList[[simulationID]]

		## [SC] calculating accuracies for each evolution step
		accuracyDF <- aggregate(Accuracy ~ SimID + EvolutionID, dataDF, mean)
		
		accuracyAvgDF <- aggregate(Accuracy ~ EvolutionID, accuracyDF, mean)
		colnames(accuracyAvgDF)[colnames(accuracyAvgDF) == "Accuracy"] <- "Mean"
		accuracySeDF <- aggregate(Accuracy ~ EvolutionID, accuracyDF, getSE)
		colnames(accuracySeDF)[colnames(accuracySeDF) == "Accuracy"] <- "SE"
		accuracyAvgDF <- merge(accuracyAvgDF, accuracySeDF)
		accuracyAvgDF <- accuracyAvgDF[order(accuracyAvgDF$EvolutionID),]

		if (simulationID == 1) {
			plotCI(accuracyAvgDF$Mean, uiw=accuracyAvgDF$SE
				, type="b", lwd=2, pch=simulationID
				, ylim=c(0, 1)
				, main=paste0("Win rates in every ", evolutionEqStep, " games")
				, xlab="Game intervals", ylab="Win rate")
			
			## [SC] plotting target success probability
			abline(h=targetP, lty=2, lwd=2, col="gray")

			legend("topright", legend=c("Simulation 1", "Simulation 2"), lty=1, lwd=2, pch=1:2)
		} else {
			plotCI(accuracyAvgDF$Mean, uiw=accuracyAvgDF$SE, add=TRUE
				, type="b", lwd=2, pch=simulationID)
		}

		accuracyOverallDF <- aggregate(Accuracy ~ SimID, dataDF, mean)
		print(paste0("Win rate mean: ", mean(accuracyOverallDF$Accuracy))) ## Averaged from all 10 blocks
		print(paste0("Win rate SE: ", getSE(accuracyOverallDF$Accuracy)))
	}

	par(subOP)
}

## [SC]
simTwoSelectionBias <- function(reload=FALSE){
	if (reload || length(dataList) == 0) {
		loadAllDatafiles()
	}

	plotFrequencyRating(dataList[[2]], "Simulation 2")
}

## [SC]
simThreeSelectionBias <- function(reload=FALSE) {
	if (reload || length(dataList) == 0) {
		loadAllDatafiles()
	}

	plotFrequencyRating(dataList[[3]], "Simulation 3")
}

## [SC]
simFourRatings <- function(reload=FALSE) {
	if (reload || length(dataList) == 0) {
		loadAllDatafiles()
	}

	dataDF <- dataList[[4]]

	subOP <- par(mfrow=c(1, 1))
	
	minRating <- min(dataDF[,c("PlayerRating", "ScenarioRating")])
	maxRating <- max(dataDF[,c("PlayerRating", "ScenarioRating")])

	playerRatingAvgDF <- aggregate(PlayerRating ~ ID, dataDF, mean)
	colnames(playerRatingAvgDF)[colnames(playerRatingAvgDF) == "PlayerRating"] <- "Mean" 
	playerRatingSEDF <- aggregate(PlayerRating ~ ID, dataDF, getSE)
	colnames(playerRatingSEDF)[colnames(playerRatingSEDF) == "PlayerRating"] <- "SE"
	playerRatingAvgDF <- merge(playerRatingAvgDF, playerRatingSEDF)
	playerRatingAvgDF <- playerRatingAvgDF[order(playerRatingAvgDF$ID),]

	## [SC] plotting player rating
	plot(x=playerRatingAvgDF$ID, y=playerRatingAvgDF$Mean, xaxp=c(0, nrow(playerRatingAvgDF), evolutionPlayerCount)
		, type="l", lwd=2, col=1
		, ylim=c(minRating, maxRating)
		, main="Skill and difficulty ratings", xlab="Games", ylab="Ratings")
	lines(x=playerRatingAvgDF$ID, y=playerRatingAvgDF$Mean - playerRatingAvgDF$SE, type="l", col=1, lwd=1, lty=2)
	lines(x=playerRatingAvgDF$ID, y=playerRatingAvgDF$Mean + playerRatingAvgDF$SE, type="l", col=1, lwd=1, lty=2)
		
	## [SC] plotting scenario ratings
	for(scenarioIndex in 1:length(scenarioIDVC)) {
		scenarioDF <- dataDF[,c("ID", paste0("Scenario", scenarioIndex))]
		colnames(scenarioDF)[colnames(scenarioDF) == paste0("Scenario", scenarioIndex)] <- "ScenarioRating"

		scenarioRatingAvgDF <- aggregate(ScenarioRating ~ ID, scenarioDF, mean)
		colnames(scenarioRatingAvgDF)[colnames(scenarioRatingAvgDF) == "ScenarioRating"] <- "Mean" 
		scenarioRatingSEDF <- aggregate(ScenarioRating ~ ID, scenarioDF, getSE)
		colnames(scenarioRatingSEDF)[colnames(scenarioRatingSEDF) == "ScenarioRating"] <- "SE"
		scenarioRatingAvgDF <- merge(scenarioRatingAvgDF, scenarioRatingSEDF)
		scenarioRatingAvgDF <- scenarioRatingAvgDF[order(scenarioRatingAvgDF$ID),]

		lines(x=scenarioRatingAvgDF$ID, y=scenarioRatingAvgDF$Mean, type="l", lwd=2, col=scenarioColors[scenarioIndex])
		lines(x=scenarioRatingAvgDF$ID, y=scenarioRatingAvgDF$Mean - scenarioRatingAvgDF$SE, type="l", col=scenarioColors[scenarioIndex], lwd=1, lty=2)
		lines(x=scenarioRatingAvgDF$ID, y=scenarioRatingAvgDF$Mean + scenarioRatingAvgDF$SE, type="l", col=scenarioColors[scenarioIndex], lwd=1, lty=2)
	}

	## [SC] plotting horizontal dividers for evolution steps
	for(evolIndex in 1:(evolutionPlayerCount-1)){
		abline(v=(evolutionPlayerStep * evolIndex), lty=2, lwd=1, col="gray")
	}

	## [SC] plotting starting rating
	abline(h=startRating, lty=2, lwd=1, col="gray")
		
	## [SC] plotting legend
	legend("topleft", legend=c("PlayerRating", scenarioIDVC), col=c("1", scenarioColors), lty=1, lwd=2)

	par(subOP)
}

## [SC]
simFourFrequencies <- function(reload=FALSE) {
	if (reload || length(dataList) == 0) {
		loadAllDatafiles()
	}

	dataDF <- dataList[[4]]

	freqDF <- cbind(dataDF, Frequency = 1)
	freqDF <- aggregate(Frequency ~ SimID + EvolutionID + ScenarioID, freqDF, sum)
		
	## [SC] setting frequencies to 0 for any scenario that was not played during an evolution
	for(simID in unique(freqDF$SimID)) {
		for (evolID in unique(freqDF$EvolutionID)) {
			subsetDF <- subset(freqDF, SimID == simID & EvolutionID == evolID)
			for(scenarioID in scenarioIDVC) {
				if(!(scenarioID %in% subsetDF$ScenarioID)) {
					freqDF <- rbind(freqDF, data.frame(SimID=simID, EvolutionID=evolID, ScenarioID=scenarioID, Frequency=0))
				}
			}
		}
	}

	freqAvgDF <- aggregate(Frequency ~ EvolutionID + ScenarioID, freqDF, mean)
	colnames(freqAvgDF)[colnames(freqAvgDF) == "Frequency"] <- "Mean"
	freqSeDF <- aggregate(Frequency ~ EvolutionID + ScenarioID, freqDF, getSE)
	colnames(freqSeDF)[colnames(freqSeDF) == "Frequency"] <- "SE"
	freqAvgDF <- merge(freqAvgDF, freqSeDF)

	print(freqAvgDF)
		
	for(scenarioIndex in 1:length(scenarioIDVC)){
		scenarioID <- scenarioIDVC[scenarioIndex]
		scenarioFreqDF <- subset(freqAvgDF, ScenarioID == scenarioID)
		scenarioFreqDF <- scenarioFreqDF[order(scenarioFreqDF$EvolutionID),]
		if (scenarioIndex == 1){
			plotCI(scenarioFreqDF$Mean, uiw=scenarioFreqDF$SE
				, type="b", lwd=2, col=scenarioColors[scenarioIndex], pch=scenarioIndex
				, ylim=c(0, max(freqAvgDF$Mean, na.rm=TRUE))
				, main=paste0("Frequencies of opponents in every ", evolutionPlayerStep, " games")
				, xlab="Intervals of 400 games", ylab="Frequencies")
		} else {
			plotCI(scenarioFreqDF$Mean, uiw=scenarioFreqDF$SE, add=TRUE
				, type="b", lwd=2, col=scenarioColors[scenarioIndex], pch=scenarioIndex)
		}
	}

	legend("topright", legend=scenarioIDVC, col=scenarioColors, lty=1, lwd=2, pch=1:length(scenarioIDVC))
}

## [SC]
simFourFinalRatingCorr <- function() {
	ratingDF <- NULL

	for(simIndex in 0:(simulationCount - 1)) {
		simRatingDF <- openSimRatingfile(4, simIndex)
		simRatingDF <- cbind(simRatingDF, SimID = simIndex)

		if (is.null(ratingDF)) {
			ratingDF <- simRatingDF
		} else {
			ratingDF <- rbind(ratingDF, simRatingDF)
		}
	}

	ratingAvgDF <- aggregate(Rating ~ ScenarioID, ratingDF, mean)
	colnames(ratingAvgDF)[colnames(ratingAvgDF) == "Rating"] <- "Mean"
	ratingSeDF <- aggregate(Rating ~ ScenarioID, ratingDF, getSE)
	colnames(ratingSeDF)[colnames(ratingSeDF) == "Rating"] <- "SE"
	ratingAvgDF <- merge(ratingAvgDF, ratingSeDF)
	ratingAvgDF <- ratingAvgDF[match(scenarioIDVC, ratingAvgDF$ScenarioID),]
	
	print(ratingAvgDF)

	print(cor.test(ratingAvgDF$Mean, winProportions))
}

aiWinRateAnalysis <- function(){
	diffDF <- openAIDifficultyFile()

	matchCount <- nrow(diffDF)/(2*sum(1:6))

	diffOverallDF <- aggregate(AIOneWin ~ AIOne + AITwo, diffDF, sum)
	diffOverallDF$AIOneWin <- diffOverallDF$AIOneWin/(matchCount * 2)

	diffMatrix <- matrix(nrow=length(scenarioIDVC), ncol=length(scenarioIDVC))
	colnames(diffMatrix) <- scenarioIDVC
	rownames(diffMatrix) <- scenarioIDVC

	for(aiOneIndex in 1:length(scenarioIDVC)){
		for(aiTwoIndex in aiOneIndex:length(scenarioIDVC)){
			aiOneID <- scenarioIDVC[aiOneIndex]
			aiTwoID <- scenarioIDVC[aiTwoIndex]

			subsetDF <- subset(diffOverallDF, AIOne==aiOneID & AITwo==aiTwoID)

			diffMatrix[aiOneIndex, aiTwoIndex] <- subsetDF$AIOneWin[1]			
		}
	}

	print(diffMatrix)
}

doDataAnalysis <- function(){
	op <- par(ask=TRUE)

	## [SC] win rates of AI opponents playing against each other
	aiWinRateAnalysis() 
	
	## [SC] Figure 3
	simOneTwoAccuracyOverIntervals()

	## [SC] Figure 4
	simTwoSelectionBias()

	## [SC] Figure 5
	simThreeSelectionBias()

	## [SC] Test of correlation between AI's win rates and the starting ratings in simulations 1 and 2
	print(cor.test(scenarioStartRatings, winProportions))

	## [SC] Figure 6
	simFourRatings()

	## [SC] Figure 7
	simFourFrequencies()

	## [SC] Test of correlation between AI's win rates and the final ratings in simulation 4
	simFourFinalRatingCorr()

	par(op)
}

doDataAnalysis()

