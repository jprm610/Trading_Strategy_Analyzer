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

df2 = df[0:1500].copy()
# endregion

# region PARAMETERS
IncipientTrendFactor = 3
distance_to_BO = 0.0001
# endregion

# region INDICATOR CALCULATIONS
#Get all indicator lists
iSwing4 = MySwing(4)
iSwing4.swingHigh, iSwing4.swingLow = MySwing.Swing(MySwing, df2, iSwing4.strength)
iSMA20  = SMA(df2, 20)
iSMA50  = SMA(df2, 50)
iSMA200 = SMA(df2, 200)
iATR100 = ATR(df2, 100)
iMACD_12_26 = MACD(df2)
iBB_Upper_20, iBB_Lower_20 = Bollinger_Bands(df2, 20)

"""
iTIH = TI(df, 10, True)
iTIL = TI(df, 10, False)
iRIH = RI(df, 10, True)
iRIL = RI(df, 10, False)
iVIH = VI(df, 10, True)
iVIL = VI(df, 10, False)
iOBV = OBV(df, 10)

RSI = RSI(df, 14)
"""
# endregion

# region TRADE_EMULATION

dates = []
trade_type = []
iSMA20_ot = []
iSMA50_ot = [] 
iSMA200_ot = [] 
iATR100_ot = []
iMACD_12_26_ot = []
iBB_Upper_20_ot = []
iBB_Lower_20_ot = []
y = []
y_index = []

"""
tradeps = []
tradepl = []
"""

is_upward = False
is_downward = False
on_trade = False
trade_point_long = -1
trade_point_short = -1

for i in range(len(df2)) :

	if i == 0 : continue

	if iSwing4.swingHigh[i] == 0 or iSwing4.swingLow[i] == 0 : continue

	if iSwing4.Swing_Bar(iSwing4.swingHigh[0:i], 1, iSwing4.strength) == -1 or iSwing4.Swing_Bar(iSwing4.swingLow[0:i], 1, iSwing4.strength) == -1 : continue

	if iSMA200[i] == -1 or iSMA50[i] == -1 or iSMA20[i] == -1 or iATR100[i] == -1 : continue

	current_stop = iATR100[i] * 3
	
	if not on_trade :

		if Swing_Found(df2[0:i], iSwing4.swingLow[0:i], iSwing4.Swing_Bar(iSwing4.swingHigh[0:i], 1, iSwing4.strength), True, distance_to_BO) :
			trade_point_long = iSwing4.swingHigh[i] + distance_to_BO

		if Swing_Found(df2[0:i], iSwing4.swingHigh[0:i], iSwing4.Swing_Bar(iSwing4.swingLow[0:i], 1, iSwing4.strength), False, distance_to_BO) :
			trade_point_short = iSwing4.swingLow[i] - distance_to_BO

		if trade_point_long != -1 :
			if df2.close[i] >= trade_point_long and trade_point_long - iSMA50[i] > 0 :
				if abs(df2.close[i] - iSMA50[i]) <= current_stop : #cambiar trade point
					on_trade = True
					entry_close = df2.close[i]

					dates.append(df2.index[i])
					trade_type.append("Long")
					iSMA20_ot.append(iSMA20[i - 1])
					iSMA50_ot.append(iSMA50[i - 1])
					iSMA200_ot.append(iSMA200[i - 1])
					iATR100_ot.append(iATR100[i - 1])
					iMACD_12_26_ot.append(iMACD_12_26[i - 1])
					iBB_Upper_20_ot.append(iBB_Upper_20[i - 1])
					iBB_Lower_20_ot.append(iBB_Lower_20[i - 1])
		
		if trade_point_short != -1 :
			if df2.close[i] <= trade_point_short and trade_point_short - iSMA50[i] < 0 :
				if abs(df2.close[i] - iSMA50[i]) <= current_stop :
					on_trade = True
					entry_close = df2.close[i]

					dates.append(df2.index[i])
					trade_type.append("Short")
					iSMA20_ot.append(iSMA20[i - 1])
					iSMA50_ot.append(iSMA50[i - 1])
					iSMA200_ot.append(iSMA200[i - 1])
					iATR100_ot.append(iATR100[i - 1])
					iMACD_12_26_ot.append(iMACD_12_26[i - 1])
					iBB_Upper_20_ot.append(iBB_Upper_20[i - 1])
					iBB_Lower_20_ot.append(iBB_Lower_20[i - 1])
	else :
		if len(trade_type) == 0 : continue

		if trade_type[-1] == "Long" :
			if df2.close[i] <= iSMA50[i] :
				on_trade = False
				outcome = df2.close[i] - entry_close
				trade_point_long = -1
				trade_point_short = -1

				y.append(outcome)
				y_index.append(df2.index[i])
		else :
			if df2.close[i] >= iSMA50[i] :
				on_trade = False
				outcome = df2.close[i] - entry_close
				trade_point_long = -1
				trade_point_short = -1

				if outcome >= 0 :
					y.append(-outcome)
				else :
					y.append(abs(outcome))
				y_index.append(df2.index[i])
	
	if i == len(df2) - 1 and on_trade :
		y.append(-1)
		y_index.append(df2.index[i])

	"""
	trade_point_long = -1
	trade_point_short = -1

	if Swing_Found(df2[0:i], iSwing4.swingLow[0:i], iSwing4.Swing_Bar(iSwing4.swingHigh[0:i], 1, iSwing4.strength), True, distance_to_BO) :
		trade_point_long = iSwing4.swingHigh[i] + distance_to_BO

	if Swing_Found(df2[0:i], iSwing4.swingHigh[0:i], iSwing4.Swing_Bar(iSwing4.swingLow[0:i], 1, iSwing4.strength), False, distance_to_BO) :
		trade_point_short = iSwing4.swingLow[i] - distance_to_BO

	dates.append(df2.index[i])
	tradepl.append(trade_point_long)
	tradeps.append(trade_point_short)
	"""
	


trades = pd.DataFrame()

trades['entry_date'] = np.array(dates)
trades['trade_type']  = np.array(trade_type)
trades['iSMA20']  = np.array(iSMA20_ot)
trades['iSMA50']  = np.array(iSMA50_ot)
trades['iSMA200']  = np.array(iSMA200_ot)
trades['iATR100']  = np.array(iSMA200_ot)
trades['iMACD_12_26']  = np.array(iMACD_12_26_ot)
trades['iBB_Lower_20']  = np.array(iBB_Lower_20_ot)
trades['iBB_Upper_20']  = np.array(iBB_Upper_20_ot)
trades['y']  = np.array(y)
trades['exit_date'] = np.array(y_index) 

"""
trades['tradesh'] = np.array(tradeps)
trades['tradelo'] = np.array(tradepl)
"""
# endregion

# region BUILD NEW DATASET
#Attach those lists to columns
df2['SMA20']  = np.array(iSMA20)
df2['SMA50']  = np.array(iSMA50)
df2['SMA200'] = np.array(iSMA200)
df2['ATR100'] = np.array(iATR100)
df2['swing4high'] = np.array(iSwing4.swingHigh)
df2['swing4low'] = np.array(iSwing4.swingLow)

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
fig = go.Figure(data=[go.Candlestick(x=df2.index[201:],
                                     open=df2.open[201:], 
                                     high=df2.high[201:],
                                     low=df2.low[201:],
                                     close=df2.close[201:]), 
					  go.Scatter(x=df2.index[201:], y=df2.swing4high[201:], line=dict(color='orange', width=1)),
					  go.Scatter(x=df2.index[201:], y=df2.swing4low[201:], line=dict(color='blue', width=1)),
                      go.Scatter(x=df2.index[201:], y=df2.SMA200[201:], line=dict(color='red', width=1)),
                      go.Scatter(x=df2.index[201:], y=df2.SMA50[201:], line=dict(color='yellow', width=1))],
					  layout = Layout(
    						plot_bgcolor='rgba(0,0,0)',
							paper_bgcolor='rgba(0,0,0)'
					  ))


py.offline.plot(fig, filename = "main.html")

#Save the edited dataframe as a new .csv file
df2.to_csv('Final_frame.csv')
trades.to_csv('trades.csv')