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