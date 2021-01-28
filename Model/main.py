import pandas as pd
import numpy as np
import plotly as py
from plotly import tools
import plotly.graph_objs as go
from indicators import *

#----------------------------------------------DATA CLEANING------------------------------------------------------
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

#---------------------------------------INDICATOR CALCULATIONS-------------------------------------------------------
#Get all indicator lists
highs = []
lows = []
swing4 = MySwing(4, highs, lows)
swing4.high_list, swing4.low_list = MySwing.Swing(MySwing, df, swing4.strength)

swingHighBar = []
swingLowBar = []
for i in range(len(df)) :
	swingHighBar.append(swing4.Swing_Bar(swing4.high_list[0:i], 1, swing4.strength))
	swingLowBar.append(swing4.Swing_Bar(swing4.low_list[0:i], 1, swing4.strength))

"""
TIH = TI(df, 10, True)
TIL = TI(df, 10, False)
RIH = RI(df, 10, True)
RIL = RI(df, 10, False)
VIH = VI(df, 10, True)
VIL = VI(df, 10, False)
SMA20  = SMA(df, 20)
SMA50  = SMA(df, 50)
SMA200 = SMA(df, 200)
ATR    = ATR(df, 100)
MACD   = MACD(df)
BB_Upper, BB_Lower = Bollinger_Bands(df, 20)
RSI = RSI(df, 14)
OBV = OBV(df, 10)
"""

#-------------------------------------------BUILD NEW DATASET--------------------------------------------------------
#Attach those lists to columns
df['SwingHigh'] = np.array(swing4.high_list)
df['SwingLow'] = np.array(swing4.low_list)
df['SwingHighBar'] = np.array(swingHighBar)
df['SwingLowBar'] = np.array(swingLowBar)

"""
df['TIH'] = np.array(TIH)
df['TIL'] = np.array(TIL)
df['RIH'] = np.array(RIH)
df['RIL'] = np.array(RIL)
df['VIH'] = np.array(VIH)
df['VIL'] = np.array(VIL)
df["RSI"] = np.array(RSI)
df['SMA20']  = np.array(SMA20)
df['SMA50']  = np.array(SMA50)
df['SMA200'] = np.array(SMA200)
df['ATR100'] = np.array(ATR)
df['MACD']   = np.array(MACD)
df["BB_Upper"] = np.array(BB_Upper)
df["BB_Lower"] = np.array(BB_Lower)
df["OBV"] = np.array(OBV)
"""

#--------------------------------------------TRADE_EMULATION---------------------------------------------------------

distance_to_BO = 0.0001

"""
dates = []
trade_type = []
#price_levels = []

for i in range(len(df)) :

	if i == 0 : continue
	if swingHigh[i] == 0 or swingLow[i] == 0 : continue
	if Swing_Bar(swingHigh[0:i], 1, 4) == -1 or Swing_Bar(swingLow[0:i], 1, 4) == -1 : continue

	is_BO_long = Swing_Found(df[0:i], swingLow[0:i], Swing_Bar(swingHigh[0:i], 1, 4), True, distance_to_BO)
	if is_BO_long :
		dates.append(df.index[i])
		trade_type.append("Long")
		#print("Long")

	is_BO_short = Swing_Found(df[0:i], swingHigh[0:i], Swing_Bar(swingLow[0:i], 1, 4), False, distance_to_BO)
	if is_BO_short :
		dates.append(df.index[i])
		trade_type.append("Short")
		#print("Short")

trades = pd.DataFrame()

trades['dates'] = np.array(dates)
trades['type']  = np.array(trade_type)
"""

"""
candles = go.Ohlc(x = df.index, open = df.open, high = df.high, low = df.low, close = df.close, name = "Data series")
SMA1 = go.Scatter(x = df.index, y = df.SMA200, name = 'SMA1')
SMA2 = go.Scatter(x = df.index, y = df.SMA50, name = 'SMA2')
SMA3 = go.Scatter(x = df.index, y = df.SMA20, name = 'SMA3')

fig = py.subplots.make_subplots(rows=2, cols=1, shared_xaxes=True)
fig.append_trace(candles, 1, 1)
fig.append_trace(SMA1, 1, 1)
fig.append_trace(SMA2, 1, 1)
fig.append_trace(SMA3, 1, 1)

py.offline.plot(fig, filename = "main.html")
"""

#Save the edited dataframe as a new .csv file
df.to_csv('Final_frame.csv')
#trades.to_csv('trades.csv')