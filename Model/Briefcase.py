# region LIBRARIES

from My_Library import *

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
import plotly.graph_objects as go
from plotly.graph_objs import *

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

import binance as bnb
client = bnb.Client('yp4IdJHY5YdmWMM3KF6fmW8wynwqet3t8PGP4pxkXKJrmomL2Odxo3EUbmLzxVb4', 'mq8rq16gFB132Xd1wkwjya6XMt9UzBZFWhnb2cKiAoWlNV3oikD1Hi7UY1OJarL2')

# endregion

# region PARAMETERS

Use_Pre_Charged_Data = True
# Indicators

BTC_SMA_Period = 50
BTC_Stop_SMA_Period = 100

VPN_period = 10
DO_period = 50

VPN_to_trade = -101
Flat_iDO = 15   

TS_proportion_BTC_above_200 = 0.65
TS_proportion_BTC_below_200 = 0.85
Stop_Lock = 0.66
Stop_Lock_Trail = 0.5
Stop_Limit_Distance = 0.98

# Overall backtesting parameters
Start_Date = '2000-01-01'
Risk_Unit = 24
Perc_In_Risk = 16
Trade_Slots = 50
Commission_Perc = 0.1
Account_Size = 7500
Filter_Mode = f'iVPN{VPN_period}'

# endregion

unavailable_tickers = []

def main() :
    trades_global = pd.DataFrame()

    print("The current working directory is " + os.getcwd())

    commodities = pd.read_csv('Model\commodities_tickers.csv')
    tickers = list(commodities['tickers'])

    print('------------------------------------------------------------')
    print("Trade Calculation!")
    asset_count = 1
    # For every ticker in the tickers_directory :
    # for ticker in tickers
    for ticker in tickers :

        # This section is only for front-end purposes,
        # in order to show the current progress of the program when is running.
        print('------------------------------------------------------------')
        print(f"{asset_count}/{len(tickers)}")
        print(ticker)
        asset_count += 1

        # region GET DATA

        # If we want to use already downloaded data :
        if Use_Pre_Charged_Data :

            # Then try to get the .csv file of the ticker.
            try :
                df = pd.read_csv(f"Model/Commodities_data/{ticker}.csv")
            # If that's not possible, raise an error, 
            # save that ticker in unavailable tickers list 
            # and skip this ticker calculation.
            except :
                print(f"ERROR: Not available data for {ticker}.")
                unavailable_tickers.append(ticker)
                continue
            
            # Reformat the df, standarizing dates and index.
            df['date'] = pd.to_datetime(df['date'], format='%Y-%m-%d')
            df.set_index(df['date'], inplace=True)
            del df['date']

            # If the ticker df doesn't have any information skip it.
            if df.empty : continue

            print('Charged!')
        # If we want to downloaded new data :
        else :
            
            try :
                # Then download the information from Yahoo Finance 
                # and rename the columns for standarizing data.
                df = yf.download(f"{ticker}")
                df.columns = ['open', 'high', 'low', 'close', 'adj close', 'volume']
                df.index.names = ['date']
                del df['adj close']
            # If that's not possible :
            except :
                # If that's not possible, raise an error, 
                # save that ticker in unavailable tickers list 
                # and skip this ticker calculation.
                print(f"ERROR: Not available data for {ticker} in YF.")
                df = pd.DataFrame()

            if df.empty :
                print('Failed!')
                continue

            print('Downloaded!')

            # Try to create a folder to save all the data, 
            # if there isn't one available yet.
            try :
                # Create dir.
                os.mkdir('Model/Commodities_data')
                df.to_csv(f"Model/Commodities_data/{ticker}.csv")
            except :
                # Save the data.
                df.to_csv(f"Model/Commodities_data/{ticker}.csv")

        df.columns = ['open', 'high', 'low', 'close', 'volume']

        # Fill the missing dates if needed, in order to avoid
        # holes in the data.
        missing = pd.date_range(start=df.index[0], end=df.index[len(df) - 1]).difference(df.index)

        for day in missing :
            # The holes are filled with the same values
            # of the previous date.
            new_open = df['open'].loc[day - datetime.timedelta(days=1)]
            new_high = df['high'].loc[day - datetime.timedelta(days=1)]
            new_low = df['low'].loc[day - datetime.timedelta(days=1)]
            new_close = df['close'].loc[day - datetime.timedelta(days=1)]
            new_volume = df['volume'].loc[day - datetime.timedelta(days=1)]
            df.loc[day] = [new_open, new_high, new_low, new_close, new_volume]
            df = df.sort_index()

        # endregion

        # region INDICATOR CALCULATIONS

        iVPN = VPN(df, VPN_period)
        Stop_EMA = TA.EMA(df, 100)
        Trend_EMA = TA.EMA(df, 50)
        Limit_EMA = TA.EMA(df, 50)

        # Flip high and close columns to calculate
        # Donchian Channels. 
        DO_df = df.copy()
        DO_df.rename(columns={'high': 'close', 'close': 'high'}, inplace=True)
        iDO = TA.DO(DO_df, DO_period)

        # endregion

        # region TRADE_EMULATION

        # Here are declared all lists that are going to be used 
        # to save trades characteristics.
        entry_dates = []
        trade_type = []
        crypto = []

        entry_price = []
        exit_price = []
        stop_price = []
        stop_limit = []
        shares_to_trade_list = []

        VPN_ot = []

        y_raw = []
        y_perc = []
        y_ru = []
        y2_raw = []
        y3_raw = []
        y = []
        y2 = []
        y2_perc = []
        y2_ru = []
        ru_left =[]
        y3 = []
        y3_perc =[]
        y3_ru = []
        max_price = []
        min_price = []
        y_index = []
        duration_days = []
        #duration_months = []

        is_signal = []
        close_tomorrow = []
        stop_change = []

        last_iDO_breakout = 0
        # Here occurs the OnBarUpdate() in which all strategie calculations happen.
        for i in range(len(df)) :

            # region CHART INITIALIZATION

            # Here we make sure that we have enough info to work with.
            if i == 0 : continue

            if (math.isnan(iVPN[i]) or math.isnan(Stop_EMA[i]) or
                math.isnan(Trend_EMA[i]) or math.isnan(Limit_EMA[i]) or math.isnan(iDO.UPPER[i])) : continue
            # endregion

            # region TRADE CALCULATION
            # Here the Regime Filter is done,
            # checking that the current BTC close is above the BTC's SMA.
            if df.close[i] > ((iDO.UPPER[i - 1])) :

                #Condition to trade in favor of the current trend of the ticker, the BTC or both
                if df.close[i] > Trend_EMA[i] :
                #if BTC.close[i] > iBTC_SMA['SMA'].values[i] :
                #if df.close[i] > Trend_EMA[i] :

                    # added the option here to test with or without the first BO
                    if i - last_iDO_breakout > Flat_iDO and last_iDO_breakout != 0 :
                    #if i - last_iDO_breakout > Flat_iDO :
                        
                        #Condition to prevent extended trades
                        #if df.close[i] < Limit_EMA[i] * 2 :

                            if i == len(df) - 1 :
                                # region Signals
                                if iVPN[i] > VPN_to_trade :

                                    # Before entering the trade,
                                    # calculate the shares_to_trade,
                                    # in order to risk the Perc_In_Risk of the crypto,
                                    # finally make sure that we can afford those shares to trade.
                                    current_avg_lose = df.close[i] * (Perc_In_Risk / 100)

                                    shares_to_trade = abs(Risk_Unit / current_avg_lose)

                                    # Here the order is set, saving all variables 
                                    # that characterizes the operation.
                                    # The on_trade flag is updated 
                                    # in order to avoid more than one operation calculation in later candles, 
                                    # until the operation is exited.
                                    is_signal.append(True)
                                    trade_type.append("Long")
                                    crypto.append(ticker)
                                    shares_to_trade_list.append(shares_to_trade)
                                    
                                    VPN_ot.append(iVPN[i])

                                    # Here all y variables are set to 0, 
                                    # in order to differentiate the signal operation in the trdes df.
                                    entry_dates.append(df.index[i])
                                    y_index.append(df.index[i])
                                    entry_price.append(df.close[i])
                                    exit_price.append(df.close[i])
                                    stop_price.append(df.close[i] * Stop_Lock)
                                    stop_limit.append(df.close[i] * (Stop_Lock * Stop_Limit_Distance))

                                    duration_days.append(0)
                                    #duration_months.append(0)
                                    y.append(0)
                                    y2.append(0)
                                    y2_perc.append(0)
                                    y2_ru.append(0)
                                    ru_left.append(0)
                                    y3.append(0)
                                    y3_perc.append(0)
                                    y3_ru.append(0)
                                    y_raw.append(0)
                                    y_perc.append(0)
                                    y_ru.append(0)
                                    y2_raw.append(0)
                                    y3_raw.append(0)
                                    max_price.append(entry_price)
                                    min_price.append(entry_price)
                                    close_tomorrow.append(False)
                                    stop_change.append(False)

                                # endregion
                            else :
                                # region Backtesting
                                if iVPN[i] > VPN_to_trade :

                                    # Before entering the trade,
                                    # calculate the shares_to_trade,
                                    # in order to risk the Perc_In_Risk of the crypto,
                                    # finally make sure that we can afford those shares to trade.
                                    current_avg_lose = df.close[i] * (Perc_In_Risk / 100)

                                    shares_to_trade = abs(Risk_Unit / current_avg_lose)

                                    # Here the order is set, saving all variables 
                                    # that characterizes the operation.
                                    # The on_trade flag is updated 
                                    # in order to avoid more than one operation calculation in later candles, 
                                    # until the operation is exited.
                                    is_signal.append(False)
                                    trade_type.append("Long")
                                    crypto.append(ticker)
                                    shares_to_trade_list.append(shares_to_trade)
                                    
                                    VPN_ot.append(iVPN[i])
                                    
                                    # To simulate that the order is executed in the next day, 
                                    # the entry price is taken in the next candle open. 
                                    # Nevertheless, when we are in the last candle that can't be done, 
                                    # that's why the current close is saved in that case.
                                    entry_dates.append(df.index[i + 1])
                                    entry_price.append(df.open[i + 1])
                                    max_income = df.high[i + 1]
                                    min_income = df.low[i + 1]

                                    # region Trade Management
                                    new_df = df.loc[df.index >= df.index[i + 1]]
                                    for j in range(len(new_df)) :

                                        # Ordinary check to avoid errors.
                                        if len(trade_type) == 0 : 
                                            last_iDO_breakout = i
                                            continue
                                        
                                        # Here the max_income variable is updated.
                                        if new_df.high[j] > max_income :
                                            max_income = new_df.high[j]

                                        # We have all the option to perform the trailing stop (with respect to the ticker EMA or regarding BTC EMA or both)
                                        if new_df.close[j] > Stop_EMA[i + j + 1] :
                                        #if new_df.close[j] > Stop_EMA[i + j + 1] :
                                        #if BTC.close[i + j + 1] > iBTC_Stop_SMA['SMA'].values[i + j + 1] :
                                            trailling_stop = max_income * TS_proportion_BTC_above_200
                                        else :
                                            trailling_stop = max_income * TS_proportion_BTC_below_200

                                        if j == 0 :
                                            stop_lock = new_df.open[j] * Stop_Lock

                                        # Here the min_income variable is updated.
                                        if new_df.low[j] < min_income :
                                            min_income = new_df.low[j]

                                        # region Stop Loss
                                        if new_df.low[j] <= stop_lock :

                                            # To simulate that the order is executed in the next day, 
                                            # the entry price is taken in the next candle open. 
                                            # Nevertheless, when we are in the last candle that can't be done, 
                                            # that's why the current close is saved in that case.
                                            if j == len(new_df) - 1 :
                                                outcome = ((stop_lock * (1 - (Commission_Perc / 100))) - (entry_price[-1] * (1 + (Commission_Perc / 100)))) * shares_to_trade

                                                y_index.append(new_df.index[j])
                                                exit_price.append(stop_lock)

                                                close_tomorrow.append(True)
                                            else :
                                                outcome = ((stop_lock * (1 - (Commission_Perc / 100))) - (entry_price[-1] * (1 + (Commission_Perc / 100)))) * shares_to_trade

                                                y_index.append(new_df.index[j + 1])
                                                exit_price.append(stop_lock)

                                                close_tomorrow.append(False)

                                            if exit_price[-1] > max_income : max_income = exit_price[-1]

                                            if exit_price[-1] < min_income : min_income = exit_price[-1]

                                            # Saving all missing trade characteristics.
                                            duration_days.append(y_index[-1] - entry_dates[-1])
                                            #duration_months.append(duration_days[-1] / 30)
                                            y_raw.append(exit_price[-1] - entry_price[-1])
                                            y_perc.append(y_raw[-1] / entry_price[-1] * 100)
                                            y_ru.append(y_perc[-1] / Perc_In_Risk)
                                            y2_raw.append(max_income - entry_price[-1])
                                            y3_raw.append(min_income - entry_price[-1])
                                            y.append(outcome)
                                            y2.append(y2_raw[-1] * shares_to_trade)
                                            y2_perc.append(y2_raw[-1]  / entry_price[-1] * 100)
                                            y2_ru.append(y2_perc[-1] / Perc_In_Risk)
                                            ru_left.append(y2_ru[-1] - y_ru[-1])
                                            y3.append(y3_raw[-1] * shares_to_trade)
                                            y3_perc.append(y3_raw[-1]  / entry_price[-1] * 100)
                                            y3_ru.append(y3_perc[-1] / Perc_In_Risk)
                                            max_price.append(max_income)
                                            min_price.append(min_income)
                                            stop_price.append(stop_lock)
                                            stop_limit.append(stop_lock * Stop_Limit_Distance)
                                            stop_change.append(False)
                                            break
                                        # endregion

                                        # region Trailing Stop
                                        # If the current close is below SMA (Stop_EMA[i + j + 1] or trailling_stop) :
                                        elif new_df.close[j] < trailling_stop :

                                            # To simulate that the order is executed in the next day, 
                                            # the entry price is taken in the next candle open. 
                                            # Nevertheless, when we are in the last candle that can't be done, 
                                            # that's why the current close is saved in that case.
                                            if j == len(new_df) - 1 :
                                                outcome = ((new_df.close[j] * (1 - (Commission_Perc / 100))) - (entry_price[-1] * (1 + (Commission_Perc / 100)))) * shares_to_trade

                                                y_index.append(new_df.index[j])
                                                exit_price.append(new_df.close[j])

                                                close_tomorrow.append(True)
                                            else :
                                                outcome = ((new_df.open[j + 1] * (1 - (Commission_Perc / 100))) - (entry_price[-1] * (1 + (Commission_Perc / 100)))) * shares_to_trade

                                                y_index.append(new_df.index[j + 1])
                                                exit_price.append(new_df.open[j + 1])

                                                close_tomorrow.append(False)

                                            if exit_price[-1] > max_income : max_income = exit_price[-1]

                                            if exit_price[-1] < min_income : min_income = exit_price[-1]

                                            # Saving all missing trade characteristics.
                                            duration_days.append(y_index[-1] - entry_dates[-1])
                                            #duration_months.append(duration_days[-1] / 30)
                                            y_raw.append(exit_price[-1] - entry_price[-1])
                                            y_perc.append(y_raw[-1] / entry_price[-1] * 100)
                                            y_ru.append(y_perc[-1] / Perc_In_Risk)
                                            y2_raw.append(max_income - entry_price[-1])
                                            y3_raw.append(min_income - entry_price[-1])
                                            y.append(outcome)
                                            y2.append(y2_raw[-1] * shares_to_trade)
                                            y2_perc.append(y2_raw[-1]  / entry_price[-1] * 100)
                                            y2_ru.append(y2_perc[-1] / Perc_In_Risk)
                                            ru_left.append(y2_ru[-1] - y_ru[-1])
                                            y3.append(y3_raw[-1] * shares_to_trade)
                                            y3_perc.append(y3_raw[-1]  / entry_price[-1] * 100)
                                            y3_ru.append(y3_perc[-1] / Perc_In_Risk)
                                            max_price.append(max_income)
                                            min_price.append(min_income)
                                            stop_price.append(stop_lock)
                                            stop_limit.append(stop_lock * Stop_Limit_Distance)
                                            stop_change.append(False)
                                            break
                                        # endregion
                                        """"
                                        # region Taking Profits (Limit Price)
                                        elif new_df.close[j] > Limit_EMA[i + j + 1] * 7 :

                                            # To simulate that the order is executed in the next day, 
                                            # the entry price is taken in the next candle open. 
                                            # Nevertheless, when we are in the last candle that can't be done, 
                                            # that's why the current close is saved in that case.
                                            if j == len(new_df) - 1 :
                                                outcome = ((new_df.close[j] * (1 - (Commission_Perc / 100))) - (entry_price[-1] * (1 + (Commission_Perc / 100)))) * shares_to_trade

                                                y_index.append(new_df.index[j])
                                                exit_price.append(new_df.close[j])

                                                close_tomorrow.append(True)
                                            else :
                                                outcome = ((new_df.open[j + 1] * (1 - (Commission_Perc / 100))) - (entry_price[-1] * (1 + (Commission_Perc / 100)))) * shares_to_trade

                                                y_index.append(new_df.index[j + 1])
                                                exit_price.append(new_df.open[j + 1])

                                                close_tomorrow.append(False)

                                            if exit_price[-1] > max_income : max_income = exit_price[-1]

                                            if exit_price[-1] < min_income : min_income = exit_price[-1]

                                            # Saving all missing trade characteristics.
                                            duration_days.append(y_index[-1] - entry_dates[-1])
                                            #duration_months.append(duration_days[-1] / 30)
                                            y_raw.append(exit_price[-1] - entry_price[-1])
                                            y_perc.append(y_raw[-1] / entry_price[-1] * 100)
                                            y_ru.append(y_perc[-1] / Perc_In_Risk)
                                            y2_raw.append(max_income - entry_price[-1])
                                            y3_raw.append(min_income - entry_price[-1])
                                            y.append(outcome)
                                            y2.append(y2_raw[-1] * shares_to_trade)
                                            y2_perc.append(y2_raw[-1]  / entry_price[-1] * 100)
                                            y2_ru.append(y2_perc[-1] / Perc_In_Risk)
                                            ru_left.append(y2_ru[-1] - y_ru[-1])
                                            y3.append(y3_raw[-1] * shares_to_trade)
                                            y3_perc.append(y3_raw[-1]  / entry_price[-1] * 100)
                                            y3_ru.append(y3_perc[-1] / Perc_In_Risk)
                                            max_price.append(max_income)
                                            min_price.append(min_income)
                                            stop_price.append(stop_lock)
                                            stop_limit.append(stop_lock * Stop_Limit_Distance)
                                            stop_change.append(False)
                                            break
                                        """
                                        # endregion
                                        
                                        # if the trade is still open at the last candle then append the values regarding the last close
                                        if j == len(new_df) - 1 :
                                            # All characteristics are saved 
                                            # as if the trade was exited in this moment.
                                            outcome = ((new_df.close[j] * (1 - (Commission_Perc / 100))) - (entry_price[-1] * (1 + (Commission_Perc / 100)))) * shares_to_trade

                                            exit_price.append(new_df.close[j])

                                            y_index.append(new_df.index[j])
                                            close_tomorrow.append(False)                                           

                                            duration_days.append(y_index[-1] - entry_dates[-1])
                                            #duration_months.append(duration_days[-1] / 30)
                                            y_raw.append(exit_price[-1] - entry_price[-1])
                                            y_perc.append(y_raw[-1] / entry_price[-1] * 100)
                                            y_ru.append(y_perc[-1] / Perc_In_Risk)
                                            y2_raw.append(max_income - entry_price[-1])
                                            y3_raw.append(min_income - entry_price[-1])
                                            y.append(outcome)
                                            y2.append(y2_raw[-1] * shares_to_trade)
                                            y2_perc.append(y2_raw[-1]  / entry_price[-1] * 100)
                                            y2_ru.append(y2_perc[-1] / Perc_In_Risk)
                                            ru_left.append(y2_ru[-1] - y_ru[-1])
                                            y3.append(y3_raw[-1] * shares_to_trade)
                                            y3_perc.append(y3_raw[-1]  / entry_price[-1] * 100)
                                            y3_ru.append(y3_perc[-1] / Perc_In_Risk)
                                            max_price.append(max_income)
                                            min_price.append(min_income)                                                                                

                                            #snipet to show the real stop at the current candle when the trade is still opened
                                            if (max_income * Stop_Lock_Trail) > stop_lock :
                                                stop_lock = max_income * Stop_Lock_Trail
                                                stop_change.append(True)
                                            else :
                                                stop_change.append(False)

                                            stop_price.append(stop_lock)
                                            stop_limit.append(stop_lock * Stop_Limit_Distance)   

                                            break

                                        if j != 0 and (max_income * Stop_Lock_Trail) > stop_lock :
                                            stop_lock = max_income * Stop_Lock_Trail   
                                    # endregion
                                # endregion
                last_iDO_breakout = i
            # endregion
        # endregion

        # region TRADES_DF

        # A new df is created in order to save the trades done in the current ticker.
        trades = pd.DataFrame()

        # Here all trades including their characteristics are saved in a df.
        trades['entry_date'] = np.array(entry_dates)
        trades['exit_date'] = np.array(y_index)
        trades['days'] = np.array(duration_days)
        #trades['months'] = np.array(duration_months)        
        trades['trade_type']  = np.array(trade_type)
        trades['crypto'] = np.array(crypto)
        trades['is_signal'] = np.array(is_signal)
        trades['entry_price'] = np.array(entry_price)
        trades['exit_price'] = np.array(exit_price)
        trades['stop_price'] = np.array(stop_price)
        trades['stop_limit'] = np.array(stop_limit)
        trades['y']  = np.array(y)
        trades['y_raw'] = np.array(y_raw)
        trades['y%'] = np.array(y_perc)
        trades['y_ru'] = np.array(y_ru)
        trades['shares_to_trade'] = np.array(shares_to_trade_list)
        trades['stop_change'] = np.array(stop_change)
        trades['close_tomorrow'] = np.array(close_tomorrow)

        trades[f'iVPN{VPN_period}'] = np.array(VPN_ot)
        
        trades['max'] = np.array(max_price)
        trades['min'] = np.array(min_price)
        trades['MFE'] = np.array(y2)
        trades['MFE_%'] = np.array(y2_perc)
        trades['MFE_ru'] = np.array(y2_ru)
        trades['RU_left'] = np.array(ru_left)
        trades['MAE'] = np.array(y3)
        trades['MAE_%'] = np.array(y3_perc)
        trades['MAE_ru'] = np.array(y3_ru)
        trades['y2_raw'] = np.array(y2_raw)
        trades['y3_raw'] = np.array(y3_raw)

        # endregion

        # Here the current trades df is added to 
        # the end of the global_trades df.
        trades_global = trades_global.append(trades, ignore_index=True)

    try :
        # Create dir.
        os.mkdir('Model/Files')
        print('Created!')
    except :
        print('Created!')

    # Here the trades_global df is edited 
    # in order to set the entry_date characteristic as the index.
    #trades_global.drop_duplicates(keep='first', inplace=True)
    trades_global = trades_global.sort_values(by=['entry_date'], ignore_index=True)
    trades_global.to_csv('Model/Files/Commodities_trades_raw.csv')

    trades_global = Portfolio(trades_global, Trade_Slots, Filter_Mode)

    return_table = Return_Table(trades_global)

    trades_global = trades_global.sort_values(by=['entry_date'], ignore_index=True)
    trades_global.set_index(trades_global['entry_date'], inplace=True)
    del trades_global['entry_date']

    trades_global.to_csv('Model/Files/Commodities_trades.csv')

    #stats = Stats(trades_global, Account_Size)

    #Files_Return(trades_global, return_table, stats, unavailable_tickers)

def VPN(df, period, EMA_period=3) :

    """
    Volume Positive Negative (VPN)
    """

    #Calculate relative volume for every candle.
    VPNs = {}
    for i in range(len(df)) :
        if i < period :
            VPNs[df.index[i]] = float('NaN')
        else :
            Vp = 0
            Vn = 0
            Vtotal = 0
            for j in range(period) :
                if df.close[i - j] >= df.close[i - j - 1] :
                    Vp += df.volume[i - j]
                else :
                    Vn += df.volume[i - j]

                Vtotal += df.volume[i - j]
            VPNs[df.index[i]] = 100 * ((Vp - Vn) / Vtotal)

    # In order to apply EMA to the volumes,
    # create a new df so that it can be inputed
    # to the finta's EMA function.
    VPNs = pd.Series(VPNs)
    df2 = pd.DataFrame({
        'date': VPNs.index,
        'open': -1,
        'high': -1,
        'low': -1,
        'close': VPNs
    })
    df2.set_index(df2['date'], inplace=True)
    del df2['date']
    
    # Calculate and return volume's EMA.
    VPNs = TA.EMA(df2, EMA_period)
    
    return VPNs

main()