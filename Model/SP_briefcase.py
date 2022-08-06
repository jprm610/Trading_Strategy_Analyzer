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
Data_Path = "Model/SP_data"

# Indicators

RS_Look_Back = 252

SPY_SMA_Period = 200

DO_Screener_period = 252
RS_Rank_Minimum = 70
DO_Screener_Lower_Proportion = 1.25
DO_Screener_Upper_Proportion = 0.75
SMA1_Screener_Look_Back = 21

VPN_period = 10
DO_TradePoint_Period = 10
SMA1_period, SMA2_period, SMA3_period, SMA4_period = 200, 150, 50, 20

VPN_to_trade = 30
Flat_iDO_Period = 5
Min_Low_Proportion = 0.9
Target_Proportion = 3

Stop_Lock_Proportion = 0.995

TS_proportion_SPY_above_200 = 0.8
TS_proportion_SPY_below_200 = 0.9

# Overall backtesting parameters
Start_Date = pd.to_datetime('2020-01-01')
End_Date = pd.to_datetime('today').normalize()
Risk_Unit = 60
Perc_In_Risk = 6
Trade_Slots = 10
Commission_Perc = 0.1
Account_Size = 10000
Filter_Mode = f'iVPN{VPN_period}'

# endregion

def main() :

    unavailable_tickers = []

    # Here we create a df in which all trades are going to be saved.
    trades_global = pd.DataFrame()

    print("The current working directory is " + os.getcwd())

    SPY_global, iSPY_SMA_global = SPY_df(SPY_SMA_Period)

    #tickers_directory, cleaned_tickers = Survivorship_Bias(Start_Date)

    file = pd.read_csv('Model/SP500_historical.csv')
    tickers = file.CONSTITUENTS[232]
    tickers = tickers.replace('[','')
    tickers = tickers.replace(']','')
    tickers = tickers.replace("'",'')
    tickers = tickers.split(',')

    #tickers = pd.read_csv("Model/US_stock_universe.csv")
    #tickers = tickers['symbol'].to_list()
    #RS_df = Relative_Strength(tickers, RS_Look_Back, Start_Date, End_Date, Use_Pre_Charged_Data, Data_Path)

    max_period_indicator = max(VPN_period, DO_TradePoint_Period, SMA1_period, SMA2_period, SMA3_period, SMA4_period)

    print('------------------------------------------------------------')
    print("Trade Calculation!")
    asset_count = 1
    # For every ticker in the tickers_directory :
    for ticker in tickers :
        
        #if ticker != 'TSLA' : continue

        # This section is only for front-end purposes,
        # in order to show the current progress of the program when is running.
        print('------------------------------------------------------------')
        print(str(asset_count) + '/' + str(len(tickers)))
        print(ticker)
        asset_count += 1

        # For every ticker period.
        # NOTE: As the tickers come with pairs of start and end dates, 
        # then when the quiantity of elements is divided by 2, 
        # it gives us the number of periods of that ticker.
        #its = int(len(tickers_directory[ticker]) / 2)
        its = 1
        for a in range(its) :
            
            # region Get_Data
            """current_start_date = a * 2
            start = tickers_directory[ticker][current_start_date]
            end = tickers_directory[ticker][current_start_date + 1]

            if (end != cleaned_tickers['end_date'].values[-1] or
                Use_Pre_Charged_Data) :
                returned = Get_Data(ticker, start, end, True, max_period_indicator)
            else :
                returned = Get_Data(ticker, start, end, False, max_period_indicator)"""

            returned = Get_Data(ticker, Start_Date, End_Date, Use_Pre_Charged_Data, max_period_indicator, Data_Path)

            if isinstance(returned, int) :
                unavailable_tickers.append(f"{ticker}_({str(Start_Date)[:10]}) ({str(End_Date)[:10]})")
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
            iSMA1, iSMA2, iSMA3, iSMA4 = TA.SMA(df,SMA1_period), TA.SMA(df,SMA2_period), TA.SMA(df,SMA3_period), TA.SMA(df,SMA4_period)

            # Flip high and close columns to calculate
            # Donchian Channels. 
            #DO_df = df.copy()
            #DO_df.rename(columns={'high': 'close', 'close': 'high'}, inplace=True)
            iDO_Screener = TA.DO(df, DO_Screener_period, DO_Screener_period)
            iDO_Trade_Point = TA.DO(df, DO_TradePoint_Period, DO_TradePoint_Period)

            # endregion

            # region TRADE_EMULATION

            trades = pd.DataFrame(
                    columns=['entry_date','exit_date','trade_type','stock','is_signal','entry_price','exit_price',
                        'y','y_raw','y%','shares_to_trade','close_tomorrow','max','min','y2','y3','y2_raw','y3_raw'])

            # Here are declared all lists that are going to be used 
            # to save trades characteristics.
            VPN_ot = []

            iDO_breakouts_ids = []
            iDO_breakouts = []

            """entry_dates = []
            trade_type = []
            stock = []

            entry_price = []
            exit_price = []
            shares_to_trade_list = []

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
            close_tomorrow = []"""
            # Here occurs the OnBarUpdate() in which all strategie calculations happen.
            for i in range(len(df)) :

                d = df.index[i]
                c = df.close[i]
                a = i

                # region CHART INITIALIZATION

                # Here we make sure that we have enough info to work with.
                if i == 0 or i == len(df) - 1 : continue

                if (math.isnan(iSPY_SMA['SMA'].values[i]) or math.isnan(iVPN[i]) or 
                    math.isnan(iSMA1[i]) or math.isnan(iSMA2[i]) or math.isnan(iSMA3[i]) or 
                    math.isnan(iDO_Screener.UPPER[i]) or math.isnan(iDO_Screener.LOWER[i]) or
                    math.isnan(iDO_Trade_Point.UPPER[i])) : continue
                # endregion
                
                # region TRADE CALCULATION
                # Here the Regime Filter is done,
                # checking that the current SPY close is above the SPY's SMA.

                d = df.index[i]
                c = df.close[i]
                a = i

                if df.high[i] > iDO_Trade_Point.UPPER[i - 1] :
                    if len(iDO_breakouts_ids) >= 10 :
                        iDO_breakouts_ids.pop(0)
                    iDO_breakouts_ids.append(i)

                    if len(iDO_breakouts) >= 10 :
                        iDO_breakouts.pop(0)
                    iDO_breakouts.append(df.high[i])

                # region STAGE_2

                #if df.index[i] not in list(RS_df.keys()) : continue

                #if ticker not in RS_df[df.index[i]].index.to_list() : continue

                #if RS_df[df.index[i]].loc[ticker, 'rank'] < RS_Rank_Minimum : continue
                
                if df.close[i] < iDO_Screener.LOWER[i] * DO_Screener_Lower_Proportion : continue

                if df.close[i] < iDO_Screener.UPPER[i] * DO_Screener_Upper_Proportion : continue

                if iSMA1[i-SMA1_Screener_Look_Back] > iSMA1[i] : continue

                S1, S2, S3 = iSMA1[i], iSMA2[i], iSMA3[i]
                if not (iSMA3[i] > iSMA2[i] > iSMA1[i]) : continue

                if df.close[i] < iSMA3[i] : continue

                """delta = 90
                while End - pd.Timedelta(delta, unit='D') not in RS_df.keys() : delta += 1

                if ticker not in RS_df[End - pd.Timedelta(delta, unit='D')].index.to_list() : continue
                
                if RS_df[End].loc[ticker, 'rs_SPY'] <= RS_df[End - pd.Timedelta(delta, unit='D')].loc[ticker, 'rs_SPY'] : continue"""

                # endregion

                #if SPY.close[i] <= iSPY_SMA['SMA'].values[i] : continue

                if df.high[i] <= iDO_Trade_Point.UPPER[i - 1] : continue

                if len(iDO_breakouts) < 2 : continue

                if i - iDO_breakouts_ids[-2] < Flat_iDO_Period : continue
                
                min_Low_Flat_iDO_Period = min(list(df.low[iDO_breakouts_ids[-2]:i]))
                if min_Low_Flat_iDO_Period < iDO_breakouts[-2] * Min_Low_Proportion : continue

                if iVPN[i] <= VPN_to_trade : continue

                if df.close[i] <= iDO_breakouts[-2] : continue

                # SIGNALS
                if i == len(df) - 1 :
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
                    trade_info = {
                        'entry_price': round(df.close[i], 2),
                        'entry_date': df.index[i],
                        'shares_to_trade': shares_to_trade,
                        'trade_type': "Long",
                        'stock': ticker,
                        'max_price': df.close[i],
                        'min_price': df.close[i]
                    }
                    
                    VPN_ot.append(round(iVPN[i], 2))
                    
                    # Here all y variables are set to 0, 
                    # in order to differentiate the signal operation in the trdes df.
                    trades = Close_Trade(trades, trade_info, df.close[i], df.index[i], Commission_Perc, Is_Signal=True)
                # BACKTESTING
                else :
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
                    trade_info = {
                        'entry_price': round(df.open[i + 1], 2),
                        'entry_date': df.index[i + 1],
                        'shares_to_trade': round(shares_to_trade, 1),
                        'trade_type': "Long",
                        'stock': ticker,
                        'max_price': df.high[i + 1],
                        'min_price': df.low[i + 1]
                    }

                    VPN_ot.append(round(iVPN[i], 2))

                    new_df = df.loc[df.index >= df.index[i]]
                    for j in range(len(new_df)) :
                        # Ordinary check to avoid errors.
                        #if len(trade_type) == 0 : continue
                        
                        # Here the max_income variable is updated.
                        if new_df.high[j] > trade_info['max_price'] :
                            trade_info['max_price'] = new_df.high[j]

                        # Here the min_income variable is updated.
                        if new_df.low[j] < trade_info['min_price'] :
                            trade_info['min_price'] = new_df.low[j]

                        if j == 0 :
                            stop_lock = min_Low_Flat_iDO_Period * Stop_Lock_Proportion
                            stop_perc = 1 - (stop_lock/trade_info['entry_price'])

                        """if SPY.close[i + j] > iSPY_SMA['SMA'].values[i + j] :
                            trailling_stop = max_income * TS_proportion_SPY_above_200
                        else :
                            trailling_stop = max_income * TS_proportion_SPY_below_200"""

                        # trade management for GAPS  downs
                        if new_df.open[j] <= stop_lock :
                            
                            # To simulate that the order is executed in the next day, 
                            # the entry price is taken in the next candle open. 
                            # Nevertheless, when we are in the last candle that can't be done, 
                            # that's why the current close is saved in that case.

                            if j == len(new_df) - 1 :
                                trades = Close_Trade(trades, trade_info, new_df.open[j], new_df.index[j], Commission_Perc, Close_Tomorrow=True)
                            else :
                                trades = Close_Trade(trades, trade_info, new_df.open[j], new_df.index[j], Commission_Perc)
                            break

                        if new_df.close[j] <= stop_lock :
                            
                            # To simulate that the order is executed in the next day, 
                            # the entry price is taken in the next candle open. 
                            # Nevertheless, when we are in the last candle that can't be done, 
                            # that's why the current close is saved in that case.

                            if j == len(new_df) - 1 :
                                trades = Close_Trade(trades, trade_info, new_df.close[j], new_df.index[j], Commission_Perc, Close_Tomorrow=True)
                            else :
                                trades = Close_Trade(trades, trade_info, new_df.open[j + 1], new_df.index[j + 1], Commission_Perc)
                            break
                            
                        target = trade_info['entry_price'] * (1 + (stop_perc * Target_Proportion))

                        """if new_df.close[j] < trailling_stop :

                            # To simulate that the order is executed in the next day, 
                            # the entry price is taken in the next candle open. 
                            # Nevertheless, when we are in the last candle that can't be done, 
                            # that's why the current close is saved in that case.
                            if j == len(new_df) - 1 :
                                trades = Close_Trade(trades, trade_info, new_df.close[j], new_df.index[j], Commission_Perc, Close_Tomorrow=True)
                            else :
                                trades = Close_Trade(trades, trade_info, new_df.open[j + 1], new_df.index[j + 1], Commission_Perc)
                            break"""

                        """if new_df.high[j] > target :
                            # To simulate that the order is executed in the next day, 
                            # the entry price is taken in the next candle open. 
                            # Nevertheless, when we are in the last candle that can't be done, 
                            # that's why the current close is saved in that case.

                            if j == len(new_df) - 1 :
                                trades = Close_Trade(trades, trade_info, target, new_df.index[j], Commission_Perc, Close_Tomorrow=True)
                            else :
                                trades = Close_Trade(trades, trade_info, target, new_df.index[j], Commission_Perc)
                            break"""

                        if j == len(new_df) - 1 :
                            # All characteristics are saved 
                            # as if the trade was exited in this moment.

                            trades = Close_Trade(trades, trade_info, new_df.close[j], new_df.index[j], Commission_Perc)
                            break
                        
                        if j != 0 and iSMA3[i + j] >= trade_info['entry_price'] :
                            stop_lock = iSMA4[i + j] * Stop_Lock_Proportion
                # endregion
            # region TRADES_DF

            trades[f'iVPN{VPN_period}'] = np.array(VPN_ot)

            # Here the current trades df is added to 
            # the end of the global_trades df.
            trades_global = pd.concat([trades_global, trades])

            # endregion

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