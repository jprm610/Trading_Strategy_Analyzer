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

    """
    def Swing_Bar_by_Value(self, swing_list, instance, strength) :

        #In the first iteration return -1 in order to avoid bugs 
        #when trying to access a value in a list without values.
        if len(swing_list) == 0 :
            return -1

        #Create a list in which the function values are going to be saved.
        bars_ago = {}

        #For each candle, always keeping in track the changes of swing, 
        #compute the number of candles ago (bars_ago) when the swing last changed.
        #The candles are walked backwards.
        #Always add the strength keeping in track the real point in where a swing comes up. 
        last_swing = swing_list[-1]
        for i in reversed(range(len(swing_list))) :
            if swing_list[i] != last_swing :
                bars_ago[swing_list[i]] = len(swing_list) + strength - i - 1
                last_swing = swing_list[i]

        #If there isn't enoug swings charged, return -1
        if bars_ago[j] < instance :
            return -1

        #Else, return the instance bars_ago, (instance - 1 according to lists comprenhension).
        return bars_ago[instance - 1]
    """

#Price Rate of Change (ROC)
def ROC(prices, period) :
    """
    Price Rate of Change (ROC)

    INPUTS:
    *prices: The df with opens, highs, lows and closes.
    *period: The period used to calculate indicator (int).

    OUTPUTS:
    *ROCs: A list of all ROCs according to the dataframe.

    NOTE:
    If there isn't enough data, -1 is set in the list.
    """

    #Create the list of ROCs results
    ROCs = []

    #For each candle:
    for i in range(len(prices)) :
        #If there isn't enough data to calculate,
        #set the current average to -1.
        if i < period :
            ROCs.append(-1)
        #If there is:
        else :
            #Calculate the current ROC. Formula:
            #     ROC = 100 * Closing_Price[i] - Closing_Price[i - period]
            #                   Closing_Price[i - period]

            current_ROC = 100 * ((prices.close[i] - prices.close[i - period]) / prices.close[i - period])
            ROCs.append(current_ROC)

    return ROCs

#Simple Moving Average (SMA)
def SMA(prices, period) :
    """
    Simple Moving Average (SMA)

    INPUTS:
    *prices: The df with opens, highs, lows and closes.
    *period: The period used to calculate indicator (int).

    OUTPUTS:
    *SMAs: A list of all SMAs according to the dataframe.

    NOTE:
    If there isn't enough data, -1 is set in the list.
    """

    #Create the list of moving averages results
    SMAs = []

    #For each candle:
    for i in range(len(prices)) :
        #If there isn't enough data to calculate,
        #set the current average to -1.
        if i < period :
            SMAs.append(-1)
        #If there is:
        else :
            #Add all the last (period) closes and average them,
            #then save it in the averages list.
            sum = 0
            for j in range(0, period) :
                sum += prices.close[i - j]
            SMAs.append(sum / period)

    return SMAs

#Average True Range (ATR)
def ATR(prices, period) :
    """
    Average True Range (ATR)

    INPUTS:
    *prices: The df with opens, highs, lows and closes.
    *period: The period used to calculate indicator (int).

    OUTPUTS:
    *ATRs: A list of all ATRs according to the dataframe.

    NOTE:
    If there isn't enough data, -1 is set in the list.
    """

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
    """
    Exponential Moving Average (EMA)

    INPUTS:
    *prices: The df with opens, highs, lows and closes.
    *period: The period used to calculate indicator (int).
    *smoothing_factor: Controls the weight of the last observation (the greater it is, the heavier it becomes the current observation) (double).

    OUTPUTS:
    *EMAs: A list of all EMAs according to the dataframe.

    NOTE:
    If there isn't enough data, -1 is set in the list.
    """

    #Declare the constants so that the operation doesn't turn long and difficult to read.
    constant_1 = smoothing_factor / (1 + period)
    constant_2 = 1 - (smoothing_factor / (1 + period))

    #For each candle:
    EMAs = []
    for i in range(len(prices)) :
        #If there isn't enough data to calculate,
        #set the current average to -1.
        if i < period :
            EMAs.append(-1)
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
    """
    Moving Average Convergence Divergence (MACD)
    
    INPUTS:
    *prices: The df with opens, highs, lows and closes.
    *EMA1_period: The period used to calculate the first EMA needed for calculations (int).
    *EMA1_period: The period used to calculate the second EMA needed for calculations (int).

    OUTPUTS:
    *MACDs: A list of all MACDs according to the dataframe.

    NOTE:
    If there isn't enough data, -1 is set in the list.
    """

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
    """
    INPUTS:
    *prices: The df with opens, highs, lows and closes.
    *period: The period used to calculate indicator (int).
    *standard_deviations: Determine the bollinger bands range width (double).

    OUTPUTS:
    *BBUpper: A list of all BBUpper values according to the dataframe.
    *BBLower: A list of all BBLower values according to the dataframe.

    NOTE:
    If there isn't enough data, -1 is set in the list.
    """

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
    """
    Isn't working!!!!!!!!!!!!
    """

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
    """
    On-Balance Volume (OBV)

    INPUTS:
    *prices: The PORTION of df with opens, highs, lows and closes.
    *period: The period used to calculate indicator (int).

    OUTPUTS:
    *net_volume: A number indicating the net volume of the last n candles (n == period) (double).

    NOTE:
    If the period isn't coherent with the DF portion given, -1 is set in the list.
    """

    #If the period parameter isn't coherent, return -1.
    if len(prices) < period : return -1

    #Calculate the net volume, adding positive candles and substracting negative candles volumes.
    net_volume = 0
    for i in range(period) :
        if (prices.close[-(i + 1)] >= prices.open[-(i + 1)]) :
            volume = prices.volume[-(i + 1)]
            net_volume += prices.volume[-(i + 1)]
        else :
            volume = prices.volume[-(i + 1)]
            net_volume -= prices.volume[-(i + 1)]

    return net_volume

#Range Index
def RI(prices, period, is_long) :
    """
    Range Index (RI)

    INPUTS:
    *prices: The PORTION of df with opens, highs, lows and closes.
    *period: The period used to calculate indicator (int).
    *is_long: Indicating in which operation type is been used (bool).

    OUTPUTS:
    *range_index: A number indicating the range index of the last n candles (n == period) (double).

    NOTE:
    If the period isn't coherent with the DF portion given, -1 is set in the list.
    If there is an indetermination returns -2.
    """

    #If the period parameter isn't coherent, return -1.
    if len(prices) < period : return -1

    #Calculate all the ranges (high - close)
    #and save it in the ranges list.
    ranges = []
    for i in range(period) :
        high = prices.high[-(i + 1)]
        low = prices.low[-(i + 1)]
        ranges.append(prices.high[-(i + 1)] - prices.low[-(i + 1)])

    #Create 2 lists in which the positive and negative candle ranges are going to be saved.
    gains = []
    loses = []

    #For the last n candles (n == period)
    #filter the positive and negative ranges according to the open and close of each candle.
    for i in range(period) :
        if prices.close[-(i + 1)] >= prices.open[-(i + 1)] :
            gains.append(ranges[-(i + 1)])
        else :
            loses.append(ranges[-(i + 1)])

    #If the calculation will truncate, 
    #set the current value to -2.
    if sum(gains) == 0 or sum(loses) == 0 : return -2
    
    #Calculate the range index.
    range_index = sum(gains) / sum(loses)

    #The range index is calculated in regard to the positive candles initially (long),
    #so in case it is regarding to negative candles (short), apply the multiplicative inverse.
    if not is_long :
        range_index = 1 / range_index

    return range_index

#Volume Index
def VI(prices, period, is_long) :
    """
    Volume Index (VI)

    INPUTS:
    *prices: The PORTION of df with opens, highs, lows and closes.
    *period: The period used to calculate indicator (int).
    *is_long: Indicating in which operation type is been used (bool).

    OUTPUTS:
    *volume_index: A number indicating the volume index of the last n candles (n == period) (double).

    NOTE:
    If the period isn't coherent with the DF portion given, -1 is set in the list.
    If there is an indetermination returns -2.
    """

    #If the period parameter isn't coherent, return -1.
    if len(prices) < period : return -1

    #Create 2 lists in which the positive and negative candle volumes are going to be saved.
    gains = []
    loses = []

    #For the last n candles (n == period)
    #filter the positive and negative volumes according to the open and close of each candle.
    for i in range(period) :
        if prices.close[-(i + 1)] >= prices.open[-(i + 1)] :
            gains.append(prices.volume[-(i + 1)])
        else :
            loses.append(prices.volume[-(i + 1)])

    #If the calculation will truncate, 
    #set the current value to -2.
    if sum(gains) == 0 or sum(loses) == 0 : return -2
        
    #Calculate the volume index
    volume_index = sum(gains) / sum(loses)

    #The volume index is calculated in regard to the positive candles initially (long),
    #so in case it is regarding to negative candles (short), apply the multiplicative inverse.
    if not is_long :
        volume_index = 1 / volume_index

    return volume_index

#Tails index
def TI(prices, period, is_long) :
    """
    Tail Index (TI)

    INPUTS:
    *prices: The PORTION of df with opens, highs, lows and closes.
    *period: The period used to calculate indicator (int).
    *is_long: Indicating in which operation type is been used (bool).

    OUTPUTS:
    *tail_index: A number indicating the tail index of the last n candles (n == period) (double).

    NOTE:
    If the period isn't coherent with the DF portion given, -1 is set in the list.
    If there is an indetermination returns -2.
    """

    #If the period parameter isn't coherent, return -1.
    if len(prices) < period : return -1

    #Create 2 lists in where the tails are going to be saved.
    upper_tails = []
    lower_tails = []

    #For each candle:
    #Check the direction of the candle to take its tails.
    for i in range(period) :
        if prices.close[-(i + 1)] >= prices.open[-(i + 1)] :
            upper_tails.append(prices.high[-(i + 1)] - prices.close[-(i + 1)])
            lower_tails.append(prices.open[-(i + 1)] - prices.low[-(i + 1)])
        else :
            upper_tails.append(prices.high[-(i + 1)] - prices.open[-(i + 1)])
            lower_tails.append(prices.close[-(i + 1)] - prices.low[-(i + 1)])

    #If the calculation will truncate, 
    #set the current value to 0.
    if sum(upper_tails) == 0 or sum(lower_tails) == 0 : return -2
        
    #Calculate the tails index
    tail_index = sum(upper_tails) / sum(lower_tails)

    #The tail index is calculated in regard to the positive candles initially (long),
    #so in case it is regarding to negative candles (short), apply the multiplicative inverse.
    if not is_long :
        tail_index = 1 / tail_index

    return tail_index

def Swing_Dimensions(prices, reference_swings_list, opposite_swings_list, swing_strength) :

    dimensions = {}

    # X variables
    pullback_movement_x = MySwing.Swing_Bar(MySwing, reference_swings_list, 1, swing_strength) - 1
    dimensions["pullback_x"] = pullback_movement_x

    init_movement_x = MySwing.Swing_Bar(MySwing, opposite_swings_list[:-(pullback_movement_x + 2)], 1, swing_strength) + 1
    dimensions["init_x"] = init_movement_x

    #Limits Range (Y)
    #range = abs(reference_swings_list[-1] - opposite_swings_list[-(MySwing.Swing_Bar(reference_swings_list, 1, swing_strength) + 2)]

    #Length (X)

    return dimensions

#----------------------------------------------TRADE FUNCTIONS----------------------------------------------------
def Swing_Found(prices, opposite_swing, reference_swing_bar, is_swingHigh, distance_to_BO = 0.0001) :
    
    #If there isn't enough data, return False.
    if len(prices) == 0 :
        return False

    is_potential_swing = True

    if is_swingHigh :
        for i in range(reference_swing_bar + 1) :
            if prices.close[-(i + 1)] < opposite_swing[-(i + 1)] + distance_to_BO :
                is_potential_swing = False
                break

            if prices.close[-(i + 1)] < opposite_swing[-(reference_swing_bar + 2)] + distance_to_BO :
                is_potential_swing = False
                break
    else :
        for i in range(reference_swing_bar + 1) :
            if prices.close[-(i + 1)] > opposite_swing[-(i + 1)] - distance_to_BO :
                is_potential_swing = False
                break
            
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
