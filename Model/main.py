import pandas as pd
import numpy as np
import plotly as py
from plotly.subplots import make_subplots
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
distance_to_BO = 0.0001
# endregion

# region INDICATOR CALCULATIONS
#Get all indicator lists
iSwing4 = MySwing(4)
iSwing4.swingHigh, iSwing4.swingLow = MySwing.Swing(MySwing, df, iSwing4.strength)

iATR100 = ATR(df, 100)

iSMA20  = SMA(df, 20)
iSMA50  = SMA(df, 50)
iSMA200 = SMA(df, 200)

iEMA20  = EMA(df, 20)
iEMA50  = EMA(df, 50)
iEMA200 = EMA(df, 200)

iMACD_12_26 = MACD(df)

iBB_Upper_20, iBB_Lower_20 = Bollinger_Bands(df, 20)

iROC20 = ROC(df, 20)
iROC50 = ROC(df, 50)
iROC200 = ROC(df, 200)

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
iROC20_ot = []
iROC50_ot = []
iROC200_ot = []
iEMA20_ot = []
iEMA50_ot = []
iEMA200_ot = []

iRI_ot = []
iOBV_ot = []

y = []
y2 = []
y_index = []

is_upward = False
is_downward = False
on_trade = False
trade_point_long = -1
trade_point_short = -1

for i in range(len(df)) :

	if i == 0 : continue

	if iSwing4.swingHigh[i] == 0 or iSwing4.swingLow[i] == 0 : continue

	if iSwing4.Swing_Bar(iSwing4.swingHigh[0:i], 1, iSwing4.strength) == -1 or iSwing4.Swing_Bar(iSwing4.swingLow[0:i], 1, iSwing4.strength) == -1 : continue

	if iSMA200[i] == -1 or iSMA50[i] == -1 or iSMA20[i] == -1 or iATR100[i] == -1 : continue

	trade_point_short = -1
	trade_point_long = -1

	current_stop = iATR100[i] * 3
	
	if not on_trade :

		if Swing_Found(df[0:i], iSwing4.swingLow[0:i], iSwing4.Swing_Bar(iSwing4.swingHigh[0:i], 1, iSwing4.strength), True, distance_to_BO) :
			trade_point_long = iSwing4.swingHigh[i] + distance_to_BO

		if Swing_Found(df[0:i], iSwing4.swingHigh[0:i], iSwing4.Swing_Bar(iSwing4.swingLow[0:i], 1, iSwing4.strength), False, distance_to_BO) :
			trade_point_short = iSwing4.swingLow[i] - distance_to_BO

		if trade_point_long != -1 :
			if df.close[i] >= trade_point_long and trade_point_long - iSMA50[i] > 0 :
				if abs(df.close[i] - iSMA50[i]) <= current_stop :
					on_trade = True
					entry_close = df.close[i]
					max_income = df.high[i]

					dates.append(df.index[i])
					trade_type.append("Long")
					iSMA20_ot.append(iSMA20[i - 1])
					iSMA50_ot.append(iSMA50[i - 1])
					iSMA200_ot.append(iSMA200[i - 1])
					iATR100_ot.append(iATR100[i - 1])
					iMACD_12_26_ot.append(iMACD_12_26[i - 1])
					iBB_Upper_20_ot.append(iBB_Upper_20[i - 1])
					iBB_Lower_20_ot.append(iBB_Lower_20[i - 1])
					iROC20_ot.append(iROC20[i - 1])
					iROC50_ot.append(iROC50[i - 1])
					iROC200_ot.append(iROC200[i - 1])
					iEMA20_ot.append(iEMA20[i - 1])
					iEMA50_ot.append(iEMA50[i - 1])
					iEMA200_ot.append(iEMA200[i - 1])
					iOBV_ot.append(OBV(df[0:i], 10))
					iRI_ot.append(RI(df[0:i], 10, True))
		
		if trade_point_short != -1 :
			if df.close[i] <= trade_point_short and trade_point_short - iSMA50[i] < 0 :
				if abs(df.close[i] - iSMA50[i]) <= current_stop :
					on_trade = True
					entry_close = df.close[i]
					max_income = df.low[i]

					dates.append(df.index[i])
					trade_type.append("Short")
					iSMA20_ot.append(iSMA20[i - 1])
					iSMA50_ot.append(iSMA50[i - 1])
					iSMA200_ot.append(iSMA200[i - 1])
					iATR100_ot.append(iATR100[i - 1])
					iMACD_12_26_ot.append(iMACD_12_26[i - 1])
					iBB_Upper_20_ot.append(iBB_Upper_20[i - 1])
					iBB_Lower_20_ot.append(iBB_Lower_20[i - 1])
					iROC20_ot.append(iROC20[i - 1])
					iROC50_ot.append(iROC50[i - 1])
					iROC200_ot.append(iROC200[i - 1])
					iEMA20_ot.append(iEMA20[i - 1])
					iEMA50_ot.append(iEMA50[i - 1])
					iEMA200_ot.append(iEMA200[i - 1])
					iOBV_ot.append(OBV(df[0:i], 10))
					iRI_ot.append(RI(df[0:i], 10, False))
	else :
		if len(trade_type) == 0 : continue

		if trade_type[-1] == "Long" :
			if df.high[i] > max_income :
				max_income = df.high[i]
			
			if df.close[i] <= iSMA50[i] :
				on_trade = False
				outcome = df.close[i] - entry_close
				trade_point_long = -1
				trade_point_short = -1

				y.append(outcome)
				y_index.append(df.index[i])
				y2.append(max_income - entry_close)
		else :
			if df.low[i] < max_income :
				max_income = df.low[i]

			if df.close[i] >= iSMA50[i] :
				on_trade = False
				outcome = df.close[i] - entry_close
				trade_point_long = -1
				trade_point_short = -1

				if outcome >= 0 :
					y.append(-outcome)
				else :
					y.append(abs(outcome))
				y_index.append(df.index[i])
				y2.append(abs(max_income - entry_close))
	
	if i == len(df) - 1 and on_trade :
		y.append(-1)
		y_index.append(df.index[i])
# endregion

# region TRADES_DF
trades = pd.DataFrame()

trades['entry_date'] = np.array(dates)
trades['trade_type']  = np.array(trade_type)

trades['iATR100']  = np.array(iATR100_ot)

trades['iSMA20']  = np.array(iSMA20_ot)
trades['iSMA50']  = np.array(iSMA50_ot)
trades['iSMA200']  = np.array(iSMA200_ot)

trades['iROC20'] = np.array(iROC20_ot)
trades['iROC50'] = np.array(iROC50_ot)
trades['iROC200'] = np.array(iROC200_ot)

trades['iEMA20'] = np.array(iEMA20_ot)
trades['iEMA50'] = np.array(iEMA50_ot)
trades['iEMA200'] = np.array(iEMA200_ot)

trades['iBBUpper20'] = np.array(iBB_Upper_20_ot)
trades['iBBLower20'] = np.array(iBB_Lower_20_ot)

trades['iMACD1226'] = np.array(iMACD_12_26_ot)

trades['iOBV'] = np.array(iOBV_ot)
trades['iRI'] = np.array(iRI_ot)

trades['y']  = np.array(y)
trades['y2'] = np.array(y2)
trades['exit_date'] = np.array(y_index)
# endregion

# region BUILD NEW DATASET
#Attach those lists to columns
df['ATR100'] = np.array(iATR100)

df['SMA20']  = np.array(iSMA20)
df['SMA50']  = np.array(iSMA50)
df['SMA200'] = np.array(iSMA200)

df['ROC20'] = np.array(iROC20)
df['ROC50'] = np.array(iROC50)
df['ROC200'] = np.array(iROC200)

df['EMA20'] = np.array(iEMA20)
df['EMA50'] = np.array(iEMA50)
df['EMA200'] = np.array(iEMA200)

df['MACD1226'] = np.array(iMACD_12_26)

df['BBLower20'] = np.array(iBB_Lower_20)
df['BBUpper20'] = np.array(iBB_Upper_20)

df['swing4high'] = np.array(iSwing4.swingHigh)
df['swing4low'] = np.array(iSwing4.swingLow)

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

# region PLOT CANDLES
fig = make_subplots(rows=2, cols=1, shared_xaxes=True)

fig.add_trace(
	go.Candlestick(x=df.index[201:1000],
                   	open=df.open[201:1000], 
                   	high=df.high[201:1000],
                   	low=df.low[201:1000],
                    close=df.close[201:1000]), 
	row=1, col=1
)

fig.add_trace(
	go.Scatter(x=df.index[201:1000], y=df.swing4high[201:1000], line=dict(color='orange', width=1)),
	row=1, col=1
)

fig.add_trace(
	go.Scatter(x=df.index[201:1000], y=df.swing4low[201:1000], line=dict(color='blue', width=1)),
	row=1, col=1
)

fig.add_trace(
    go.Scatter(x=df.index[201:1000], y=df.SMA200[201:1000], line=dict(color='red', width=1)),
	row=1, col=1
)

fig.add_trace(
    go.Scatter(x=df.index[201:1000], y=df.SMA50[201:1000], line=dict(color='yellow', width=1)),
	row=1, col=1
)

fig.add_trace(
    go.Scatter(x=df.index[201:1000], y=df.ROC200[201:1000], line=dict(color='white', width=1)),
	row=2, col=1
)

fig.update_layout(paper_bgcolor='rgba(0,0,0)', plot_bgcolor='rgba(0,0,0)')

py.offline.plot(fig, filename = "main.html")
# endregion

#Save the edited dataframe as a new .csv file
df.to_csv('Final_frame.csv')
trades.to_csv('trades.csv')