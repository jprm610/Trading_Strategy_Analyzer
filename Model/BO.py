# region LIBRARIES

# Pandas and numpy are the base libraries in order to manipulate
# all data for analysis and strategies development.
import pandas as pd
import numpy as np

# Thanks to Yahoo Finance library we can get historical data
# for all tickers without any cost.
import yfinance as yf

# Statistics library allows us to use some needed operations for analysis.
import statistics

# Plotly allows us to create graphs needed to do analysis.
import plotly as py
from plotly.subplots import make_subplots
#import plotly.graph_obiects as go
#from plotly.graph_obis import *

# TA libraty from finta allows us to use their indicator 
# functions that have proved to be correct.
from finta import TA

# os library allows us to create directories, 
# will be useful in the Get Data region.
import os

# datetime library allows us to standarize dates format 
# for comparisons between them while calculating trades.
import datetime

import math

# endregion

# region PARAMETERS

Use_Pre_Charged_Data = False

# Indicators

SPY_SMA_Period = 200

VPN_period = 10
DO_period = 260

VPN_to_trade = 40

signals_after_BO = 15

proportion_gain = 1.25
proportion_loss = 0.75
time_stop = 60

# Overall backtesting parameters
Start_Date = '2011-01-01'
Risk_Unit = 100
Perc_In_Risk = 3.2
Trade_Slots = 10
Commission_Perc = 0.1
Account_Size = 10000

# endregion

df = yf.download("AAPL", start='2011-01-01', end='2021-01-01')
#df = pd.read_csv("Model/SP_data/AAPL0.csv", sep=';')
df.columns = ['open', 'high', 'low', 'close', 'adi close', 'volume']
df.index.names = ['date']

DO_df = df.copy()
DO_df.rename(columns={'high': 'close', 'close': 'high'}, inplace=True)
iDO = TA.DO(DO_df, DO_period)

entry_dates = []
trade_type = []
stock = []

entry_price = []
exit_price = []
shares_to_trade_list = []

VPN_ot = []

y_raw = []
y_perc = []
y2_raw = []
y3_raw = []
y = []
y2 = []
y3 = []
y_index = []

is_signal = []
close_tomorrow = []

active_trade_flag = False
last_260_day_high_close = len(df)

for i in range(len(df)) :

    #if i == 0 or i == len(df) - 1 : continue

    if (math.isnan(iDO.UPPER[i])) : continue

    if active_trade_flag :    
        
        # Here the max_income variable is updated.
        if df.high[i] > max_income :
            max_income = df.high[i]

        # Here the min_income variable is updated.
        if df.low[i] < min_income :
            min_income = df.low[i]

        if df.close[i] > iDO.UPPER[i - 1] :
            last_260_day_high_close = i

        # If the current close is below SMA :
        if (df.close[i] <= entry_price[-1] * proportion_loss or
            df.close[i] >= entry_price[-1] * proportion_gain or
            i - trade_candle > time_stop) :

            active_trade_flag = False

            # To simulate that the order is executed in the next day, 
            # the entry price is taken in the next candle open. 
            # Nevertheless, when we are in the last candle that can't be done, 
            # that's why the current close is saved in that case.
            if i == len(df) - 1 :
                outcome = ((df.close[i] * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                y_index.append(df.index[i])
                exit_price.append(round(df.close[i], 2))

                close_tomorrow.append(True)
            else :
                outcome = ((df.open[i + 1] * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                y_index.append(df.index[i + 1])
                exit_price.append(round(df.open[i + 1], 2))

                close_tomorrow.append(False)

            if exit_price[-1] > max_income : max_income = exit_price[-1]

            if exit_price[-1] < min_income : min_income = exit_price[-1]

            # Saving all missing trade characteristics.
            y_raw.append(exit_price[-1] - entry_price[-1])
            y_perc.append(round(y_raw[-1] / entry_price[-1] * 100, 2))
            y2_raw.append(max_income - entry_price[-1])
            y3_raw.append(min_income - entry_price[-1])
            y.append(outcome)
            y2.append(y2_raw[-1] * shares_to_trade)
            y3.append(y3_raw[-1] * shares_to_trade)
            continue
        
        if i == len(df) - 1 :
            active_trade_flag = False
            # All characteristics are saved 
            # as if the trade was exited in this moment.
            outcome = ((df.close[i] * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

            exit_price.append(round(df.close[i], 2))

            y_index.append(df.index[i])
            close_tomorrow.append(False)

            y_raw.append(exit_price[-1] - entry_price[-1])
            y_perc.append(round(y_raw[-1] / entry_price[-1] * 100, 2))
            y2_raw.append(max_income - entry_price[-1])
            y3_raw.append(min_income - entry_price[-1])
            y.append(outcome)
            y2.append(y2_raw[-1] * shares_to_trade)
            y3.append(y3_raw[-1] * shares_to_trade)
            continue
    else :

        if df.close[i] > iDO.UPPER[i - 1] :                       

            if i - last_260_day_high_close > 15 :

                active_trade_flag = True
                trade_candle = i

                # Before entering the trade,
                # calculate the shares_to_trade,
                # in order to risk the Perc_In_Risk of the stock,
                # finally make sure that we can afford those shares to trade.
                current_avg_lose = df.close[i] * (Perc_In_Risk / 100)

                shares_to_trade = round(abs(Risk_Unit / current_avg_lose), 1)
                if round(shares_to_trade) == 0 : continue

                # Here the order is set, saving all variables 
                # that characterizes the operation.
                # The on_trade flag is updated 
                # in order to avoid more than one operation calculation in later candles, 
                # until the operation is exited.
                is_signal.append(False)
                trade_type.append("Long")
                stock.append("AAPL")
                shares_to_trade_list.append(shares_to_trade)
                
                #VPN_ot.append(iVPN[i + k])
                
                # To simulate that the order is executed in the next day, 
                # the entry price is taken in the next candle open. 
                # Nevertheless, when we are in the last candle that can't be done, 
                # that's why the current close is saved in that case.
                entry_dates.append(df.index[i + 1])
                entry_price.append(round(df.open[i + 1], 2))
                max_income = df.high[i + 1]
                min_income = df.low[i + 1]

            last_260_day_high_close = i

# A new df is created in order to save the trades done in the current ticker.
trades = pd.DataFrame()

# Here all trades including their characteristics are saved in a df.
trades['entry_date'] = np.array(entry_dates)
trades['exit_date'] = np.array(y_index)
trades['trade_type']  = np.array(trade_type)
trades['stock'] = np.array(stock)
trades['is_signal'] = np.array(is_signal)
trades['entry_price'] = np.array(entry_price)
trades['exit_price'] = np.array(exit_price)
trades['y']  = np.array(y)
trades['y_raw'] = np.array(y_raw)
trades['y%'] = np.array(y_perc)
trades['shares_to_trade'] = np.array(shares_to_trade_list)
trades['close_tomorrow'] = np.array(close_tomorrow)

#trades['iVPN' + str(VPN_period)] = np.array(VPN_ot)

trades['y2'] = np.array(y2)
trades['y3'] = np.array(y3)
trades['y2_raw'] = np.array(y2_raw)
trades['y3_raw'] = np.array(y3_raw)

trades.to_csv('Model/Files/AAPL.csv')