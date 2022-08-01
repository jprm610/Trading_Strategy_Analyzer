# region LIBRARIES

from doctest import DocFileTest
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

RS_Look_Back = 252

SPY_SMA_Period = 200

DO_Screener_period = 252
RS_Rank_Minimum = 70
DO_Screener_Lower_Proportion = 1.25
DO_Screener_Upper_Proportion = 0.75
SMA1_Screener_Look_Back = 21

VPN_period = 10
DO_TradePoint_Period= 20
SMA1_period, SMA2_period, SMA3_period = 200, 150, 50

VPN_to_trade = 40
Flat_iDO_Period = 15
Min_Low_Proportion = 0.9

Stop_Lock_Proportion = 0.995

TS_proportion_SPY_above_200 = 0.8
TS_proportion_SPY_below_200 = 0.9

# Overall backtesting parameters
Start_Date = pd.to_datetime('2010-01-01')
End_Date = pd.to_datetime('today').normalize()
Risk_Unit = 60
Perc_In_Risk = 6
Trade_Slots = 10
Commission_Perc = 0.1
Account_Size = 10000
Filter_Mode = f'iVPN{VPN_period}'

# endregion

# First the Start_Date parameter is formatted and standarized.
Start_Date = pd.to_datetime(Start_Date)

def main() :

    unavailable_tickers = []

    # Here we create a df in which all trades are going to be saved.
    trades_global = pd.DataFrame()

    print("The current working directory is " + os.getcwd())

    SPY_global, iSPY_SMA_global = SPY_df(SPY_SMA_Period)

    #tickers_directory, cleaned_tickers = Survivorship_Bias(Start_Date)

    tickers = pd.read_csv("US_stock_universe.csv")
    tickers = tickers['symbol'].to_list()
    RS_df = Relative_Strength(tickers, RS_Look_Back, Start_Date, End_Date, Use_Pre_Charged_Data)

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
            
            # region Get_Data
            current_start_date = a * 2
            start = tickers_directory[ticker][current_start_date]
            end = tickers_directory[ticker][current_start_date + 1]

            if (end != cleaned_tickers['end_date'].values[-1] or
                Use_Pre_Charged_Data) :
                returned = Get_Data(ticker, start, end, True, max_period_indicator)
            else :
                returned = Get_Data(ticker, start, end, False, max_period_indicator)

            if isinstance(returned, int) :
                unavailable_tickers.append(f"{ticker}_({str(start)[:10]}) ({str(end)[:10]}))")
                continue
            
            df = returned

            # Here both SPY_SMA and SPY information is cut,
            # in order that the data coincides with the current df period.
            # This check is done in order that the SPY 
            # information coincides with the current ticker info.
            iSPY_SMAa = iSPY_SMA_global.loc[iSPY_SMA_global.index >= df.index[0]]
            iSPY_SMA = iSPY_SMAa.loc[iSPY_SMAa.index <= df.index[-1]]

            SPYa = SPY_global.loc[SPY_global.index >= df.index[0]]
            SPY = SPYa.loc[SPYa.index <= df.index[-1]]

            if len(df) != len(SPY) :
                drops = []
                for i in range(len(df)) :
                    if df.index[i].weekday() in [5,6] :
                        drops.append(df.index[i])
                    elif math.isnan(df.close[i]) :
                        drops.append(df.index[i])
                df.drop(drops, inplace=True)
            # endregion

            # region INDICATOR CALCULATIONS

            iVPN = VPN(df, VPN_period)
            iSMA1, iSMA2, iSMA3 = TA.SMA(df,SMA1_period), TA.SMA(df,SMA2_period), TA.SMA(df,SMA3_period)

            # Flip high and close columns to calculate
            # Donchian Channels. 
            #DO_df = df.copy()
            #DO_df.rename(columns={'high': 'close', 'close': 'high'}, inplace=True)
            iDO_Screener = TA.DO(df, DO_Screener_period, DO_Screener_period)
            iDO_Trade_Point = TA.DO(df, DO_TradePoint_Period, DO_TradePoint_Period)

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

            last_iDO_TradePoint_breakout = 0
            # Here occurs the OnBarUpdate() in which all strategie calculations happen.
            for i in range(len(df)) :

                # region CHART INITIALIZATION

                # Here we make sure that we have enough info to work with.
                if i == 0 or i == len(df) - 1 : continue

                if (math.isnan(iSPY_SMA['SMA'].values[i]) or math.isnan(iVPN[i]) or 
                    math.isnan(iSMA1[i]) or math.isnan(iSMA2[i]) or math.isnan(iSMA3[i]) or 
                    math.isnan(iDO_Screener.UPPER[i]) or iDO_Screener.LOWER[i] or
                    math.isnan(iDO_Trade_Point.UPPER[i])) : continue
                # endregion
                
                # region TRADE CALCULATION
                # Here the Regime Filter is done,
                # checking that the current SPY close is above the SPY's SMA.

                if df.close[i] > iDO_Trade_Point.UPPER[i - 1] :
                    last_iDO_TradePoint_breakout = i

                # region STAGE_2

                if df.index[i] not in list(RS_df.keys()) : 
                    continue

                if ticker not in RS_df[df.index[i]].index.to_list() : continue

                if RS_df[df.index[i]].loc[ticker, 'rank'] < RS_Rank_Minimum : continue
                
                if df.close[i] < iDO_Screener.LOWER[i] * DO_Screener_Lower_Proportion : continue

                if df.close[i] < iDO_Screener.UPPER[i] * DO_Screener_Upper_Proportion : continue

                if iSMA1[i-SMA1_Screener_Look_Back] > iSMA1[i] : continue

                if not (iSMA3[i] > iSMA2[i] > iSMA1[i]) : continue

                if df.close[i] < iSMA3[i] : continue

                """delta = 90
                while End - pd.Timedelta(delta, unit='D') not in RS_df.keys() : delta += 1

                if ticker not in RS_df[End - pd.Timedelta(delta, unit='D')].index.to_list() : continue
                
                if RS_df[End].loc[ticker, 'rs_SPY'] <= RS_df[End - pd.Timedelta(delta, unit='D')].loc[ticker, 'rs_SPY'] : continue"""

                # endregion

                if SPY.close[i] <= iSPY_SMA['SMA'].values[i] : continue

                if df.close[i] <= iDO_Trade_Point.UPPER[i - 1] : continue

                if i - last_iDO_TradePoint_breakout >= Flat_iDO_Period and last_iDO_TradePoint_breakout != 0 : continue
                
                min_Low_Flat_iDO_Period = min(list(df.low[i-last_iDO_TradePoint_breakout:]))
                if min_Low_Flat_iDO_Period < iDO_Trade_Point[i] * Min_Low_Proportion : continue

                if iVPN[i] <= VPN_to_trade : continue

                if i == len(df) - 1 :
                    # region Signals

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

                        if j == 0 :
                            stop_lock = min_Low_Flat_iDO_Period * Stop_Lock_Proportion

                        """if SPY.close[i + j] > iSPY_SMA['SMA'].values[i + j] :
                            trailling_stop = max_income * TS_proportion_SPY_above_200
                        else :
                            trailling_stop = max_income * TS_proportion_SPY_below_200"""

                        # Here the min_income variable is updated.
                        if new_df.low[j] < min_income :
                            min_income = new_df.low[j]

                        # trad management for GAPS  downs
                        if new_df.open[j] <= stop_lock :
                            
                            # To simulate that the order is executed in the next day, 
                            # the entry price is taken in the next candle open. 
                            # Nevertheless, when we are in the last candle that can't be done, 
                            # that's why the current close is saved in that case.
                            if j == len(new_df) - 1 :
                                outcome = ((new_df.open[j] * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                                y_index.append(new_df.index[j])
                                exit_price.append(round(new_df.open[j], 2))

                                close_tomorrow.append(True)
                            else :
                                outcome = ((new_df.open[j] * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                                y_index.append(new_df.index[j])
                                exit_price.append(round(new_df.open[j], 2))

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

                        if new_df.low[j] <= stop_lock :

                            # To simulate that the order is executed in the next day, 
                            # the entry price is taken in the next candle open. 
                            # Nevertheless, when we are in the last candle that can't be done, 
                            # that's why the current close is saved in that case.
                            if j == len(new_df) - 1 :
                                outcome = ((stop_lock * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                                y_index.append(new_df.index[j])
                                exit_price.append(round(stop_lock, 2))

                                close_tomorrow.append(True)
                            else :
                                outcome = ((stop_lock * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                                y_index.append(new_df.index[j + 1])
                                exit_price.append(round(stop_lock, 2))

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

                        """ elif new_df.close[j] < trailling_stop :

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
                            break"""
                        
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
                        
                        if j != 0 and iSMA3[i + j] >= entry_price[-1] :
                            stop_lock = iSMA3[i + j] * Stop_Lock_Proportion

                            # endregion
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