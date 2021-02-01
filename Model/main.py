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
iswing4 = MySwing(4)
iswing4.highs, iswing4.lows = MySwing.Swing(MySwing, df, iswing4.strength)
iSMA20  = SMA(df, 20)
iSMA50  = SMA(df, 50)
iSMA200 = SMA(df, 200)

"""
ATR100 = ATR(df, 100)
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

#-------------------------------------------BUILD NEW DATASET--------------------------------------------------------
#Attach those lists to columns
df['SMA20']  = np.array(iSMA20)
df['SMA50']  = np.array(iSMA50)
df['SMA200'] = np.array(iSMA200)

"""
df['ATR100'] = np.array(ATR100)
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

#--------------------------------------------TRADE_EMULATION---------------------------------------------------------

distance_to_BO = 0.0001

dates = []
cross_type = []

for i in range(len(df)) :

	if i == 0 : continue

	if iSMA200[i] == -1 : continue

	if Cross(True, iSMA50[0:i], iSMA200[0:i]) :
		dates.append(df.index[i])
		cross_type.append("Above")

	if Cross(False, iSMA50[0:i], iSMA200[0:i]) :
		dates.append(df.index[i])
		cross_type.append("Below")


trades = pd.DataFrame()

trades['dates'] = np.array(dates)
trades['cross_type']  = np.array(cross_type)

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

#Save the edited dataframe as a new .csv file
df.to_csv('Final_frame.csv')
trades.to_csv('trades.csv')