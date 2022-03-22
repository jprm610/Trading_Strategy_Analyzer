# region LIBRARIES

from My_Library import *

# Pandas and numpy are the base libraries in order to manipulate
# all data for analysis and strategies development.
import pandas as pd
import numpy as np
import math

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

import win10toast

import warnings
warnings.filterwarnings('ignore')

# endregion

# region PARAMETERS

Use_Pre_Charged_Data = True
# Indicators

SPY_SMA_Period = 200

VPN_period = 10
DO_period = 250

VPN_to_trade = 40

TS_proportion_SPY_above_200 = 0.8
TS_proportion_SPY_below_200 = 0.9

# Overall backtesting parameters
Start_Date = '2010-01-01'
Risk_Unit = 60
Perc_In_Risk = 6
Trade_Slots = 10
Commission_Perc = 0.1
Account_Size = 10000
Filter_Mode = f'iVPN{VPN_period}'

# endregion

unavailable_tickers = []

# First the Start_Date parameter is formatted and standarized.
Start_Date = pd.to_datetime(Start_Date)

def main() :

    # Here we create a df in which all trades are going to be saved.
    trades_global = pd.DataFrame()

    print("The current working directory is " + os.getcwd())

    SPY_global, iSPY_SMA_global = SPY_df(SPY_SMA_Period)

    tickers_directory, cleaned_tickers = Survivorship_Bias(Start_Date)

    max_period_indicator = max(VPN_period, DO_period)

    print('------------------------------------------------------------')
    print("Trade Calculation!")
    asset_count = 1
    # For every ticker in the tickers_directory :
    for ticker in tickers_directory.keys() :

        # This section is only for front-end purposes,
        # in order to show the current progress of the program when is running.
        print('------------------------------------------------------------')
        print(str(asset_count) + '/' + str(len(tickers_directory.keys())))
        print(ticker)
        asset_count += 1

        # For every ticker period.
        # NOTE: As the tickers come with pairs of start and end dates, 
        # then when the quiantity of elements is divided by 2, 
        # it gives us the number of periods of that ticker.
        its = int(len(tickers_directory[ticker]) / 2)
        for a in range(its) :
            
            returned_tuple = Get_Data(tickers_directory, cleaned_tickers, ticker, a, iSPY_SMA_global, SPY_global, max_period_indicator, Start_Date, Use_Pre_Charged_Data)
            if not isinstance(returned_tuple, tuple) :
                continue
            
            df, SPY, iSPY_SMA, unavailable_tickers = returned_tuple

            # region INDICATOR CALCULATIONS

            iVPN = VPN(df, VPN_period)
            iSMA = TA.SMA(df, 100)

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
            max_price = []
            min_price = []
            y_index = []

            is_signal = []
            close_tomorrow = []

            last_iDO_breakout = 0
            # Here occurs the OnBarUpdate() in which all strategie calculations happen.
            for i in range(len(df)) :

                # region CHART INITIALIZATION

                # Here we make sure that we have enough info to work with.
                if i == 0 or i == len(df) - 1 : continue

                if (math.isnan(iSPY_SMA['SMA'].values[i]) or math.isnan(iVPN[i]) or 
                    math.isnan(iSMA[i]) or math.isnan(iDO.UPPER[i])) : continue
                # endregion
                
                # region TRADE CALCULATION
                # Here the Regime Filter is done,
                # checking that the current SPY close is above the SPY's SMA.
                if df.close[i] > iDO.UPPER[i - 1] :
                    if SPY.close[i] > iSPY_SMA['SMA'].values[i] : 
                        if i - last_iDO_breakout > 15 and last_iDO_breakout != 0 :
                            if i == len(df) - 1 :
                                # region Signals
                                if iVPN[i] > VPN_to_trade :

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
                                    is_signal.append(True)
                                    trade_type.append("Long")
                                    stock.append(ticker)
                                    shares_to_trade_list.append(shares_to_trade)
                                    
                                    VPN_ot.append(iVPN[i])
                                    
                                    # Here all y variables are set to 0, 
                                    # in order to differentiate the signal operation in the trdes df.
                                    entry_dates.append(df.index[i])
                                    y_index.append(df.index[i])
                                    entry_price.append(round(df.close[i], 2))
                                    exit_price.append(round(df.close[i], 2))
                                    y.append(0)
                                    y2.append(0)
                                    y3.append(0)
                                    y_raw.append(0)
                                    y_perc.append(0)
                                    y2_raw.append(0)
                                    y3_raw.append(0)
                                    max_price.append(entry_price)
                                    min_price.append(entry_price)
                                    close_tomorrow.append(False)

                                # endregion
                            else :
                                # region Backtesting
                                if iVPN[i] > VPN_to_trade :

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
                                    stock.append(ticker)
                                    shares_to_trade_list.append(shares_to_trade)
                                    
                                    VPN_ot.append(iVPN[i])
                                    
                                    # To simulate that the order is executed in the next day, 
                                    # the entry price is taken in the next candle open. 
                                    # Nevertheless, when we are in the last candle that can't be done, 
                                    # that's why the current close is saved in that case.
                                    entry_dates.append(df.index[i + 1])
                                    entry_price.append(round(df.open[i + 1], 2))
                                    max_income = df.high[i + 1]
                                    min_income = df.low[i + 1]

                                    new_df = df.loc[df.index >= df.index[i]]
                                    for j in range(len(new_df)) :
                                        # Ordinary check to avoid errors.
                                        if len(trade_type) == 0 : continue
                                        
                                        # Here the max_income variable is updated.
                                        if new_df.high[j] > max_income :
                                            max_income = new_df.high[j]

                                        if SPY.close[i + j] > iSPY_SMA['SMA'].values[i + j] :
                                            trailling_stop = max_income * TS_proportion_SPY_above_200
                                        else :
                                            trailling_stop = max_income * TS_proportion_SPY_below_200

                                        # Here the min_income variable is updated.
                                        if new_df.low[j] < min_income :
                                            min_income = new_df.low[j]

                                        # If the current close is below SMA :
                                        if new_df.close[j] < trailling_stop :

                                            # To simulate that the order is executed in the next day, 
                                            # the entry price is taken in the next candle open. 
                                            # Nevertheless, when we are in the last candle that can't be done, 
                                            # that's why the current close is saved in that case.
                                            if j == len(new_df) - 1 :
                                                outcome = ((new_df.close[j] * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                                                y_index.append(new_df.index[j])
                                                exit_price.append(round(new_df.close[j], 2))

                                                close_tomorrow.append(True)
                                            else :
                                                outcome = ((new_df.open[j + 1] * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                                                y_index.append(new_df.index[j + 1])
                                                exit_price.append(round(new_df.open[j + 1], 2))

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
                                            max_price.append(max_income)
                                            min_price.append(min_income)
                                            break
                                        
                                        if j == len(new_df) - 1 :
                                            # All characteristics are saved 
                                            # as if the trade was exited in this moment.
                                            outcome = ((new_df.close[j] * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                                            exit_price.append(round(new_df.close[j], 2))

                                            y_index.append(new_df.index[j])
                                            close_tomorrow.append(False)

                                            y_raw.append(exit_price[-1] - entry_price[-1])
                                            y_perc.append(round(y_raw[-1] / entry_price[-1] * 100, 2))
                                            y2_raw.append(max_income - entry_price[-1])
                                            y3_raw.append(min_income - entry_price[-1])
                                            y.append(outcome)
                                            y2.append(y2_raw[-1] * shares_to_trade)
                                            y3.append(y3_raw[-1] * shares_to_trade)
                                            max_price.append(max_income)
                                            min_price.append(min_income)
                                            break
                                
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

            trades['iVPN' + str(VPN_period)] = np.array(VPN_ot)
            
            trades['max'] = np.array(max_price)
            trades['min'] = np.array(min_price)
            trades['y2'] = np.array(y2)
            trades['y3'] = np.array(y3)
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
    trades_global.drop_duplicates(keep='first', inplace=True)
    trades_global = trades_global.sort_values(by=['entry_date'], ignore_index=True)
    trades_global.to_csv('Model/Files/SP_trades_raw.csv')

    portfolio_trades = Portfolio(trades_global, Trade_Slots, Filter_Mode, is_asc=True)

    #return_table = Return_Table(portfolio_trades)
    return_table = pd.DataFrame()

    portfolio_trades = portfolio_trades.sort_values(by=['entry_date'], ignore_index=True)
    portfolio_trades.set_index(portfolio_trades['entry_date'], inplace=True)
    del portfolio_trades['entry_date']

    stats = Stats(portfolio_trades, Account_Size)

    Files_Return(portfolio_trades, return_table, stats, unavailable_tickers)

    notification = win10toast.ToastNotifier()
    notification.show_toast('Alert!', 'Backtesting finished.')

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