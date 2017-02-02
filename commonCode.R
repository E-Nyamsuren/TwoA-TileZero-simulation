myred <- "brown3"
myblue <- "blue3"
mygreen <- "darkolivegreen"
myblack <- "black"

#### vector, atomic value, matrix, list
#### numeric

isVector <- function(vectorValues){
	if(is.vector(vectorValues)){ 
		return(TRUE)
	} else{
		print("The supplied value is not a vector.")
		return(FALSE)
	}
}

getSE <- function(vectorValues){

	if(!isVector(vectorValues)){ return(NULL) }
	
	seVal <- (sd(vectorValues)/(length(vectorValues)^0.5))

	return(seVal)
}

getSEI <- function(vectorValues){
	if(!isVector(vectorValues)){ return(NULL) }
	
	seVal <- getSE(vectorValues)
	meanVal <- mean(vectorValues)

	return(c(meanVal - seVal, meanVal + seVal))
}

getCI <- function(vectorValues){
	if(!isVector(vectorValues)){ return(NULL) }
	
	approxVal <- 1.96

	ciVal <- approxVal * getSE(vectorValues)
}

getMean <- function(data){
	return(mean(as.numeric(data), na.rm=TRUE))
}

getAbsDeviation <- function(variable, mean){
	return(abs(as.numeric(variable) - as.numeric(mean)))
}

getAbsDeviations <- function(data){
	myMean <- getMean(data)
	absDevs <- numeric()
	for(i in 1:length(data)){
		if(!is.na(data[i])){
			absDevs <- c(absDevs, getAbsDeviation(as.numeric(data[i]), myMean))
		}
	}
	return(absDevs)
}

getMeanDeviation <- function(data){
	return(getMean(getAbsDeviations(data)))
}

getVar <- function(data){
	absDevs <- getAbsDeviations(data)
	absDevs <- absDevs^2
	return(getMean(absDevs))
}

getSD <- function(data){
	return(sqrt(getVar(data)))
}

getRowMeans <- function(data, cutSize){
	rmeans <- numeric()
	if(!is.null(nrow(data))){
		if(cutSize <= 0){ cutSize <- nrow(data) }

		for(i in 1:cutSize){
			if(i <= nrow(data)){
				rmeans <- c(rmeans, getMean(data[i,]))
			} else{
				rmeans <- c(rmeans, NA)
			}
		}
		return(rmeans)
	}
	return(NULL)
}

getColMeans <- function(data){
	cmeans <- numeric()
	if(!is.null(ncol(data))){
		for(i in 1:ncol(data)){
			cmeans <- c(cmeans, getMean(data[,i]))
		}
		return(cmeans)
	}
	return(NULL)
}

mergeDF <- function(dfOne, dfTwo){
	if(ncol(dfOne) != ncol(dfTwo)){ return(NULL) }
	colnames(dfTwo) <- colnames(dfOne)
	for(i in 1:nrow(dfTwo)){
		dfOne <- rbind(dfOne, dfTwo[i,])
	}
	return(dfOne)
}