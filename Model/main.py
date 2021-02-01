import pandas as pd
import numpy as np
import plotly as py
import plotly.graph_objects as go
from plotly.graph_objs import *
from indicators import *

# region DATA CLEANING
#Read historical exchange rate file (extracted from dukascopy.com)
df = pd.read_csv("EURUSD.csv")

#Rename df columns for cleaning porpuses
df.columns = ['date', 'open', 'high', 'low', 'close', 'volume']

#Reformat the date column
df.date = pd.to_datetime(df.date, format='%d.%m.%Y %H:%M:%S.%f')

#Set the date column as the index
df = df.set_index(df.date)

#Delete the old date column
del df['date']

#Drop duplicates leaving only the first value
df = df.drop_duplicates(keep = 'last')
# endregion

# region PARAMETERS
IncipientTrendFactor = 3
# endregion

# region INDICATOR CALCULATIONS
#Get all indicator lists
iswing4 = MySwing(4)
iswing4.highs, iswing4.lows = MySwing.Swing(MySwing, df, iswing4.strength)
iSMA20  = SMA(df, 20)
iSMA50  = SMA(df, 50)
iSMA200 = SMA(df, 200)
iATR100 = ATR(df, 100)

"""
TIH = TI(df, 10, True)
TIL = TI(df, 10, False)
RIH = RI(df, 10, True)
RIL = RI(df, 10, False)
VIH = VI(df, 10, True)
VIL = VI(df, 10, False)
MACD   = MACD(df)
BB_Upper, BB_Lower = Bollinger_Bands(df, 20)
RSI = RSI(df, 14)
OBV = OBV(df, 10)
"""
# endregion

# region BUILD NEW DATASET
#Attach those lists to columns
df['SMA20']  = np.array(iSMA20)
df['SMA50']  = np.array(iSMA50)
df['SMA200'] = np.array(iSMA200)
df['ATR100'] = np.array(iATR100)

"""
df['TIH'] = np.array(TIH)
df['TIL'] = np.array(TIL)
df['RIH'] = np.array(RIH)
df['RIL'] = np.array(RIL)
df['VIH'] = np.array(VIH)
df['VIL'] = np.array(VIL)
df["RSI"] = np.array(RSI)
df['MACD']   = np.array(MACD)
df["BB_Upper"] = np.array(BB_Upper)
df["BB_Lower"] = np.array(BB_Lower)
df["OBV"] = np.array(OBV)
"""
# endregion

# region TRADE_EMULATION

distance_to_BO = 0.0001

dates = []
incipient_type = []

is_upward = False
is_downward = False

for i in range(len(df)) :

	if i == 0 : continue

	if iSMA200[i] == -1 or iSMA50[i] == -1 or iSMA20[i] == -1 or iATR100[i] == -1 : continue

	SMA_dis = abs(iSMA200[i] - iSMA50[i])

	# region OVERALL MARKET MOVEMENT
	#If the second biggest SMA crosses above the biggest SMA it means the market is going upwards.
	#The ATR value is saved immediately, the is_upward flag is set to true while its opposite flag (is_downward) 
	#is set to false.
	#Finally the CurrentBar of the event is saved for later calculations.
	if Cross(True, iSMA50[0:i], iSMA200[0:i]) :
		ATR_crossing_value = iATR100[i]
		is_upward = True
		is_downward = False
		cross_above_bar = i

	#If the second biggest SMA crosses below the biggest SMA it means the market is going downwards.
	#The ATR value is saved immediately, the is_downward flag is set to true while its opposite flag (is_upward) 
	#is set to false.
	#Finally the CurrentBar of the event is saved for later calculations.
	if Cross(False, iSMA50[0:i], iSMA200[0:i]) :
		ATR_crossing_value = iATR100[i]
		is_downward = True
		is_upward = False
		cross_below_bar = i
	# endregion

	# region INCIPIENT TREND IDENTIFICATION
	#IncipientTrend: There has been a crossing event with the 2 biggest SMAs and
	#the distance between them is greater or equal to the ATR value at the SMAs crossing event multiplied by the 
	#IncipientTrendFactor parameter.

	#If the overall market movement is going upwards and the Incipient Trend is confirmed, 
	#the is_incipient_up_trend flag is set to true
	#while its opposite (is_incipient_down_trend) is set to false and the gray_ellipse_short flag is set to false.
	if is_upward and SMA_dis >= IncipientTrendFactor * ATR_crossing_value :
		is_incipient_up_trend = True
		is_incipient_down_trend = False
		gray_ellipse_short = False
		dates.append(df.index[i])
		incipient_type.append("Upwards")

	#If the overall market movement is going downwards and the Incipient Trend is confirmed, 
	#the is_incipient_down_trend flag is set to true
	#while its opposite (is_incipient_up_trend) is set to false and the gray_ellipse_long flag is set to false.		
	if is_downward and SMA_dis >= IncipientTrendFactor * ATR_crossing_value :
		is_incipient_down_trend = True
		is_incipient_up_trend = False
		gray_ellipse_long = False
		dates.append(df.index[i])
		incipient_type.append("Downwards")
	# endregion


trades = pd.DataFrame()

trades['dates'] = np.array(dates)
trades['cross_type']  = np.array(incipient_type)
# endregion

"""
candles = go.Ohlc(x = df.index[201:], open = df.open[201:], high = df.high[201:], low = df.low[201:], close = df.close[201:], name = "Data series")
SMA1 = go.Scatter(x = df.index[201:], y = df.SMA200[201:], name = 'SMA1')
SMA2 = go.Scatter(x = df.index[201:], y = df.SMA50[201:], name = 'SMA2')
#SMA3 = go.Scatter(x = df.index, y = df.SMA20, name = 'SMA3')

fig = py.subplots.make_subplots(rows=2, cols=1, shared_xaxes=True)
fig.append_trace(candles, 1, 1)
fig.append_trace(SMA1, 1, 1)
fig.append_trace(SMA2, 1, 1)
#fig.append_trace(SMA3, 1, 1)

fig = go.Figure(data=[go.Candlestick(x=df.index,
                open=df.open[201:],
                high=df.high[201:],
                low=df.low[201:],
                close=df.close[201:])])

SMA1 = go.Scatter(x = df.index[201:], y = df.SMA200[201:], name = 'SMA1')
SMA2 = go.Scatter(x = df.index[201:], y = df.SMA50[201:], name = 'SMA2')

fig = py.subplots.make_subplots(rows=2, cols=1, shared_xaxes=True)
fig.append_trace(SMA1, 1, 1)
fig.append_trace(SMA2, 1, 1)

py.offline.plot(fig, filename = "main.html")
"""

fig = go.Figure(data=[go.Candlestick(x=df.index[201:],
                                     open=df.open[201:], 
                                     high=df.high[201:],
                                     low=df.low[201:],
                                     close=df.close[201:]), 
                      go.Scatter(x=df.index[201:], y=df.SMA200[201:], line=dict(color='yellow', width=1)),
                      go.Scatter(x=df.index[201:], y=df.SMA50[201:], line=dict(color='red', width=1))])

py.offline.plot(fig, filename = "main.html")

#Save the edited dataframe as a new .csv file
df.to_csv('Final_frame.csv')
trades.to_csv('trades.csv')