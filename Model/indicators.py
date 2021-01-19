import pandas as pd
import numpy as np

class holder :
    1

#Simple Moving Average (SMA)
def SMA(prices, period) :

    #Create the list of moving averages results
    moving_avg = []

    #For every row:
    for i in range(0, len(prices)) :
        #If there isn't enough data to calculate,
        #set the current average to 0.
        if i < period :
            moving_avg.append(0)
        #If there is:
        else :
            #Add all the last (period) closes and average them,
            #then save it in the averages list.
            sum = 0
            for j in range(0, period) :
                sum += prices.close[i - j - 1]
            moving_avg.append(sum / period)

    return moving_avg

#Average True Range (ATR)
def ATR(prices, period) :

    #Calculate all the ranges (high - close)
    #and save it in the ranges list.
    ranges = []
    for i in range(0, len(prices)) :
        ranges.append(prices.high[i] - prices.low[i])

    #For every row:
    ATRs = []
    for i in range(0, len(prices)) :
        #If there isn't enough data to calculate,
        #set the current average to 0.
        if i < period :
            ATRs.append(0)
        #If there is:
        else :
            #Add all the last (period) ranges and average them,
            #then save it in the ATRs list.
            sum = 0
            for j in range(0, period) :
                sum += ranges[i - j - 1]
            ATRs.append(sum / period)

    return ATRs

#Exponential Moving Average (EMA)
def EMA(prices, period, smoothing_factor = 2) :

    #Declare the constants so that the operation doesn't turn long and difficult to read.
    constant_1 = smoothing_factor / (1 + period)
    constant_2 = 1 - (smoothing_factor / (1 + period))

    #For every row:
    EMAs = []
    for i in range(0, len(prices)) :
        #If there isn't enough data to calculate,
        #set the current average to 0.
        if i < period :
            EMAs.append(0)
        #If it is the first calculation:
        elif i == period :
            #Use the current SMA as the last EMA in the formula.
            SMAs = SMA(prices, period)
            EMAs.append((prices.close[i] * constant_1) + (SMAs[period] * constant_2))
        else :
            #Operate the formula normally using the last EMA.
            EMAs.append((prices.close[i] * constant_1) + (EMAs[i - 1] * constant_2))

    return EMAs

#Moving Average Convergence Divergence (MACD)
def MACD(prices, EMA1_period = 12, EMA2_period = 26) :

    #Check function parameters.
    if (EMA1_period >= EMA2_period) :
        print("ERROR: Wrong parameters. Usage: EMA1_period < EMA2_period")
        return

    #Create lists to store values for later calculation.
    MACDs = []
    EMAs_1 = EMA(prices, EMA1_period) 
    EMAs_2 = EMA(prices, EMA2_period)

    #For each row:
    for i in range(0, len(prices)) :
        #If the EMAs hasn't charged yet, set MACD to false.
        if i < EMA1_period or i < EMA2_period :
            MACDs.append(0)
        #If all is ready apply the formula.
        else :
            MACDs.append(EMAs_1[i] - EMAs_2[i])
    
    return MACDs