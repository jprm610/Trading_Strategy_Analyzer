import pandas as pd
import numpy as np
import statistics

#Define the MySwing object:
class MySwing(object) :
    #The lists in which the swingLows and swingHighs are going to be saved.
    swingHigh = []
    swingLow = []

    #Define the swing turn on function.
    def __init__(self, strength):
        self.strength = strength

    def Swing(self, prices, period) :

        #Define the mobile swing review window.
        window_constant = (2 * period)

        #Create the lists in where the swings are going to be saved.
        swing_Highs = []
        swing_Lows = []

        #For each candle:
        for i in range (len(prices)) :
            #If there isn't enough data, 
            #set the current swing values to 0.
            if i < window_constant :
                swing_Highs.append(0)
                swing_Lows.append(0)
                last_swing_high = 0
                last_swing_low  = 0
            else :
                #Define the high and the low values 
                #that are going to be evaluated in order to recognize swings.
                current_high = prices.high[i - period]
                current_low  = prices.low[i - period]

                #SwingHigh review.
                #Review the last n (n = window_consant) candles:
                for j in range (window_constant + 1) :
                    #If there is a high over the current_high 
                    #means that there isn't any chance of swing formation, 
                    #so the current swing is the same as the last and the loop is finished.
                    if prices.high[i - j] > current_high :
                        swing_Highs.append(last_swing_high)
                        break
                    #If any of the highs overpassed the current high, 
                    #it means that there's a swing formation. 
                    elif j == window_constant:
                        swing_Highs.append(current_high)
                        last_swing_high = current_high
                
                #SwingLow review.
                #Review the last n (n = window_consant) candles:
                for j in range (window_constant + 1) :
                    #If there is a low under the current_low 
                    #means that there isn't any chance of swing formation, 
                    #so the current swing is the same as the last and the loop is finished.
                    if prices.low[i - j] < current_low :
                        swing_Lows.append(last_swing_low)
                        break
                    #If any of the highs overpassed the current high, 
                    #it means that there's a swing formation.
                    elif j == window_constant:
                        swing_Lows.append(current_low)
                        last_swing_low = current_low
        
        #Return the 2 lists as a tupple.
        return swing_Highs, swing_Lows

    def Swing_Bar(self, swing_list, instance, strength) :

        #In the first iteration return -1 in order to avoid bugs 
        #when trying to access a value in a list without values.
        if len(swing_list) == 0 :
            return -1

        #Create a list in which the function values are going to be saved.
        bars_ago = []

        #For each candle, always keeping in track the changes of swing, 
        #compute the number of candles ago (bars_ago) when the swing last changed.
        #The candles are walked backwards.
        #Always add the strength keeping in track the real point in where a swing comes up. 
        last_swing = swing_list[-1]
        for i in reversed(range(len(swing_list))) :
            if swing_list[i] != last_swing :
                bars_ago.append(len(swing_list) + strength - i - 1)
                last_swing = swing_list[i]

        #If there isn't enoug swings charged, return -1
        if len(bars_ago) < instance :
            return -1

        #Else, return the instance bars_ago, (instance - 1 according to lists comprenhension).
        return bars_ago[instance - 1]

#Simple Moving Average (SMA)
def SMA(prices, period) :

    #Create the list of moving averages results
    moving_avg = []

    #For each candle:
    for i in range(len(prices)) :
        #If there isn't enough data to calculate,
        #set the current average to -1.
        if i < period :
            moving_avg.append(-1)
        #If there is:
        else :
            #Add all the last (period) closes and average them,
            #then save it in the averages list.
            sum = 0
            for j in range(0, period) :
                sum += prices.close[i - j]
            moving_avg.append(sum / period)

    return moving_avg

#Average True Range (ATR)
def ATR(prices, period) :

    #Calculate all the ranges (high - close)
    #and save it in the ranges list.
    ranges = []
    for i in range(len(prices)) :
        ranges.append(prices.high[i] - prices.low[i])

    #For each candle:
    ATRs = []
    for i in range(len(prices)) :
        #If there isn't enough data to calculate,
        #set the current average to -1.
        if i < period :
            ATRs.append(-1)
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

    #For each candle:
    EMAs = []
    for i in range(len(prices)) :
        #If there isn't enough data to calculate,
        #set the current average to -1.
        if i < period - 1 :
            EMAs.append(-1)
        #If it is the first calculation:
        elif i == period - 1:
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

    #For each candle:
    for i in range(len(prices)) :
        #If the EMAs hasn't charged yet, set MACD to -1.
        if i < EMA1_period or i < EMA2_period :
            MACDs.append(-1)
        #If all is ready apply the formula.
        else :
            MACDs.append(EMAs_1[i] - EMAs_2[i])
    
    return MACDs

def Bollinger_Bands(prices, period, standard_deviations = 2) :

    #Get the SMAs values.
    SMAs = SMA(prices, period)

    #Create the lists in where the bollinger bands are going to be saved.
    Upper = []
    Lower = []

    #For each candle:
    for i in range(len(prices)) :
        #If there isn't enough data, 
        #set the current bollinger bands values to -1.
        if i < period :
            Upper.append(-1)
            Lower.append(-1)
        else :
            #Calculate the bollinger bands.
            #The standard deviation is calculated with the last n closes (n = period).
            Upper.append(SMAs[i] + (standard_deviations * statistics.stdev(prices.close[i-period:i])))
            Lower.append(SMAs[i] - (standard_deviations * statistics.stdev(prices.close[i-period:i])))

    #Return the 2 lists as a tupple.
    return Upper, Lower

#Relative Strength Index (RSI)
def RSI(prices, period) :
    
    #Create a list in which the RSI values are going to be saved.
    RSIs = []

    #For each candle:
    for i in range(len(prices)) :
        #If there isn't enough data, 
        #set the current RSI value to -1.
        if i < period - 1 :
            RSIs.append(-1)
        else :
            #Create 2 lists in which the positive and negative candles are going to be saved. 
            gains = []
            loses = []

            #Reviews the last n candles (n == period).
            for j in range(period + 1) :
                #Find the type of candle (gain or lose) and save it in the 2 lists.
                c = prices.close[i - j]
                o = prices.open[i - j]
                difference = prices.close[i - j] - prices.open[i - j]
                if difference >= 0 :
                    gains.append(difference)
                else :
                    loses.append(difference)
            
            #Check the corner cases in which there isn't gains or loses, 
            #so arbitrary values are set to avoid indeterminations 
            #in the RS and RSI formulas calculations.
            if len(gains) == 0 :
                gain_avg = 0
            else :
                gain_avg = sum(gains) / len(gains)

            if len(loses) == 0 :
                lose_avg = 1
            else :
                lose_avg = abs(sum(loses) / len(loses))

            #Calculate the Relative Strength.
            RS = gain_avg / lose_avg
            
            #And finally calculate the current RSI value in a range between 0 and 100 inclusive.
            RSIs.append(100 - (100 / (1 + RS)))

    return RSIs

#On-Balance Volume (OBV)
def OBV(prices, period) :

    #Create a list in which the OBV values are going to be saved.
    OBVs = []

    #For each candle:
    for i in range(len(prices)) :
        #If there isn't enough data, 
        #set the current OBV value to -1.
        if i < period - 1:
            volume = -1
        else :
            #Calculate the accumulated volume of the last n candles (n == period),
            #adding the green candles volume and substracting the red candles volume.
            volume = 0
            for j in range(period) :
                if (prices.close[i - j] >= prices.open[i - j]) :
                    volume += prices.volume[i - j]
                else :
                    volume -= prices.volume[i - j]
        
        #Add the current volume as the current OBV.
        OBVs.append(volume)

    return OBVs

#Range Index
def RI(prices, period, is_long) :

    #Calculate all the ranges (high - close)
    #and save it in the ranges list.
    ranges = []
    for i in range(len(prices)) :
        ranges.append(prices.high[i] - prices.low[i])

    #Create a list in which the RI values are going to be saved.
    RIs = []

    #For each candle:
    for i in range(len(prices)) :
        #If there isn't enough data, 
        #set the current RI value to -1.
        if i < period - 1 :
            RIs.append(-1)
        else :
            #Create 2 lists in which the positive and negative candle ranges are going to be saved.
            gains = []
            loses = []

            #For the last n candles (n == period)
            #filter the positive and negative ranges according to the open and close of each candle.
            for j in range(period) :
                if prices.close[i - j] >= prices.open[i - j] :
                    gains.append(ranges[i - j])
                else :
                    loses.append(ranges[i - j])

            #If the calculation will truncate, 
            #set the current value to 0 and pass to the next candle.
            if sum(gains) == 0 or sum(loses) == 0 :
                RIs.append(0)
                continue
            
            #Calculate the range index.
            RI = sum(gains) / sum(loses)

            #The range index is calculated in regard to the positive candles initially (long),
            #so in case it is regarding to negative candles (short), apply the multiplicative inverse.
            if is_long :
                RIs.append(RI)
            else :
                RIs.append(1 / RI)

    return RIs

#Volume Index
def VI(prices, period, is_long) :

    #Create a list in which the VI values are going to be saved.
    VIs = []

    #For each candle:
    for i in range(len(prices)) :
        #If there isn't enough data, 
        #set the current VI value to -1.
        if i < period - 1 :
            VIs.append(-1)
        else :
            #Create 2 lists in which the positive and negative candle volumes are going to be saved.
            gains = []
            loses = []

            #For the last n candles (n == period)
            #filter the positive and negative volumes according to the open and close of each candle.
            for j in range(period) :
                if prices.close[i - j] >= prices.open[i - j] :
                    gains.append(prices.volume[i - j])
                else :
                    loses.append(prices.volume[i - j])

            #If the calculation will truncate, 
            #set the current value to 0 and pass to the next candle.
            if sum(gains) == 0 or sum(loses) == 0 :
                VIs.append(0)
                continue
                
            #Calculate the volume index
            VI = sum(gains) / sum(loses)

            #The volume index is calculated in regard to the positive candles initially (long),
            #so in case it is regarding to negative candles (short), apply the multiplicative inverse.
            if is_long :
                VIs.append(VI)
            else :
                VIs.append(1 / VI)

    return VIs

#Tails index
def TI(prices, period, is_long) :

    #Create 2 lists in where the tails are going to be saved.
    upper_tails = []
    lower_tails = []

    #For each candle:
    #Check the direction of the candle to take its tails.
    for i in range(len(prices)) :
        if prices.close[i] >= prices.open[i] :
            upper_tails.append(prices.high[i] - prices.close[i])
            lower_tails.append(prices.open[i] - prices.low[i])
        else :
            upper_tails.append(prices.high[i] - prices.open[i])
            lower_tails.append(prices.close[i] - prices.low[i])

    #Create a list in which the TI values are going to be saved.
    TIs = []

    #For each candle:
    for i in range(len(prices)) :
        #If there isn't enough data, 
        #set the current TI value to -1.
        if i < period - 1 :
            TIs.append(-1)
        else :
            #If the calculation will truncate, 
            #set the current value to 0 and pass to the next candle.
            if sum(upper_tails[i-period:i]) == 0 or sum(lower_tails[i-period:i]) == 0 :
                TIs.append(0)
                continue
                
            #Calculate the tails index
            TI = sum(upper_tails[i-period:i]) / sum(lower_tails[i-period:i])

            #The tail index is calculated in regard to the positive candles initially (long),
            #so in case it is regarding to negative candles (short), apply the multiplicative inverse.
            if is_long :
                TIs.append(TI)
            else :
                TIs.append(1 / TI)

    return TIs

#----------------------------------------------TRADE FUNCTIONS----------------------------------------------------
def Swing_Found(prices, opposite_swing, reference_swing_bar, is_swingHigh, distance_to_BO = 0.0001) :
    
    #If there isn't enough data, return False.
    if len(prices) == 0 :
        return False

    is_potential_swing = True

    if is_swingHigh :
        for i in range(reference_swing_bar + 1) :

            close = prices.close[-(i + 1)]
            swing = opposite_swing[-(i + 1)]

            if prices.close[-(i + 1)] < opposite_swing[-(i + 1)] + distance_to_BO :
                is_potential_swing = False
                break

            swing = opposite_swing[-(reference_swing_bar + 2)]

            if prices.close[-(i + 1)] < opposite_swing[-(reference_swing_bar + 2)] + distance_to_BO :
                is_potential_swing = False
                break
    else :
        for i in range(reference_swing_bar + 1) :

            close = prices.close[-(i + 1)]
            swing = opposite_swing[-(i + 1)]

            if prices.close[-(i + 1)] > opposite_swing[-(i + 1)] - distance_to_BO :
                is_potential_swing = False
                break
            
            swing = opposite_swing[-(reference_swing_bar + 2)]

            if prices.close[-(i + 1)] > opposite_swing[-(reference_swing_bar + 2)] - distance_to_BO :
                is_potential_swing = False
                break

    return is_potential_swing

def Cross(is_above, indicator_that_crosses, indicator_base) :
    
    #If the SMAs aren't completely charged return False.
    if indicator_that_crosses[-2] == -1 or indicator_base[-2] == -1 :
        return False

    if is_above :
        #If the indicator_that_crosses crosses above, 
        #it means that it was less or equal than the indicator_base 
        #and then it became greater than the indicator_base.
        if indicator_that_crosses[-2] <= indicator_base[-2] and indicator_that_crosses[-1] > indicator_base[-1] :
            return True
    else:
        #If the indicator_that_crosses crosses below, 
        #it means that it was greater or equal than the indicator_base 
        #and then it became less than the indicator_base.
        if indicator_that_crosses[-2] >= indicator_base[-2] and indicator_that_crosses[-1] < indicator_base[-1] :
            return True

    return False
