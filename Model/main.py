import pandas as pd
import numpy as np
import plotly as py
from plotly import tools
import plotly.graph_objs as go
from indicators import *

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

#print(df.head())

#Get all indicator lists
SMA20  = SMA(df, 20)
SMA50  = SMA(df, 50)
SMA200 = SMA(df, 200)
ATR = ATR(df, 100)
EMA = EMA(df, 20)

#Attach those lists to columns
df['SMA20']  = np.array(SMA20)
df['SMA50']  = np.array(SMA50)
df['SMA200'] = np.array(SMA200)
df['ATR100'] = np.array(ATR)
df['EMA'] =    np.array(EMA)

#print(df.head(20))

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