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

import win10toast

import warnings
warnings.filterwarnings('ignore')

# endregion

# region PARAMETERS

Use_Pre_Charged_Data = True
Data_Path = "Model/SP_data"

# Indicators
SMA1_Period = 100
SMA2_Period = 5
MSD_Period = 100
ATR_Period = 10
Tradepoint_Factor = 0.5
SPY_SMA_Period = 200
Consecutive_Lower_Lows = 2

# Overall backtesting parameters
Start_Date = pd.to_datetime('2010-01-01')
End_Date = pd.to_datetime('today').normalize()
Risk_Unit = 100
Perc_In_Risk = 2.3
Trade_Slots = 10
Commission_Perc = 0.1
Account_Size = 10000
Filter_Mode = 'volatility'

# endregion

def main() :

    unavailable_tickers = []

    # Here we create a df in which all trades are going to be saved.
    trades_global = pd.DataFrame()

    print(f"The current working directory is {os.getcwd()}")

    SPY_global, iSPY_SMA_global = SPY_df(SPY_SMA_Period)

    tickers_directory, cleaned_tickers = Survivorship_Bias(Start_Date)

    max_period_indicator = max(SMA1_Period, SMA2_Period, MSD_Period, ATR_Period)

    print('------------------------------------------------------------')
    print("Trade Calculation!")
    asset_count = 1

    # For every ticker in the tickers_directory :
    for ticker in tickers_directory.keys() :

        # This section is only for front-end purposes,
        # in order to show the current progress of the program when is running.
        print('------------------------------------------------------------')
        print(f"{asset_count}/{len(tickers_directory)}")
        print(ticker)
        asset_count += 1

        # For every ticker period.
        # NOTE: As the tickers come with pairs of start and end dates, 
        # then when the quantity of elements is divided by 2, 
        # it gives us the number of periods of that ticker.
        its = int(len(tickers_directory[ticker]) / 2)
        for a in range(its) :
            # region GET_DATA

            current_start_date = a * 2
            start = tickers_directory[ticker][current_start_date]
            end = tickers_directory[ticker][current_start_date + 1]

            if (end != cleaned_tickers['end_date'].values[-1] or
                Use_Pre_Charged_Data) :
                returned = Get_Data(ticker, start, end, True, max_period_indicator)
            else :
                returned = Get_Data(ticker, start, end, False, max_period_indicator)

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

            # region INDICATOR_CALCULATIONS

            iSMA1, iSMA2 = TA.SMA(df, SMA1_Period), TA.SMA(df, SMA2_Period)
            iMSD = TA.MSD(df, MSD_Period)
            iATR = TA.ATR(df, ATR_Period)

            # endregion

            # region TRADE_SIMULATION

            trades_df = pd.DataFrame(
                    columns=['entry_date','exit_date','trade_type','stock','is_signal','entry_price','exit_price',
                        'y','y_raw','y%','shares_to_trade','close_tomorrow','max','min','y2','y3','y2_raw','y3_raw'])

            # Here are declared all lists that are going to be used 
            # to save trades characteristics.
            SMA1_ot, SMA2_ot, MSD_ot, ATR_ot, volatility_ot = [], [], [], [], []

            # Here occurs the OnBarUpdate() in which all strategies calculations happen.
            for i in range(len(df)) :
                # region CHART_INITIALIZATION

                # Here we make sure that we have enough info to work with.
                if i == 0 or i == len(df) : continue
                
                if (math.isnan(iSPY_SMA['SMA'].values[i]) or 
                    math.isnan(iSMA1[i]) or math.isnan(iSMA2[i]) or
                    math.isnan(iMSD[i]) or math.isnan(iATR[i])) :
                    continue

                # endregion

                # region TRADE_CALCULATION

                # Here the Regime Filter is done, 
                # checking that the current SPY close is above the SPY's SMA.
                if SPY.close[i] <= iSPY_SMA['SMA'].values[i] : continue
                
                # Check if the current price is above the SMA1,
                # this in purpose of determining whether the stock is in an up trend
                # and if that same close is below the SMA2, 
                # reflecting a recoil movement.
                if df.close[i] <= iSMA1[i] or df.close[i] >= iSMA2[i] : continue

                # Review wheter there are the required consecutive lower lows,
                # first checking the given parameter.
                if Consecutive_Lower_Lows <= 0 : is_consecutive_check = True
                else :
                    is_consecutive_check = True
                    for j in range(Consecutive_Lower_Lows) :
                        if df.low[i - j] > df.low[i - j - 1] :
                            is_consecutive_check = False
                            break

                if not is_consecutive_check : continue

                # Here the tradepoint for the Limit Operation is calculated, 
                # taking into account the ATR.
                tradepoint = df.close[i] - Tradepoint_Factor * iATR[i]

                # SIGNALS
                if i == len(df) - 1 :
                    # Before entering the trade,
                    # calculate the shares_to_trade,
                    # in order to risk the Perc_In_Risk of the stock,
                    # finally make sure that we can afford those shares to trade.
                    current_avg_lose = tradepoint * (Perc_In_Risk / 100)

                    shares_to_trade = round(abs(Risk_Unit / current_avg_lose), 1)
                    if shares_to_trade == 0 : continue

                    # Here the order is set, saving all variables 
                    # that characterizes the operation.
                    # The on_trade flag is updated 
                    # in order to avoid more than one operation calculation in later candles, 
                    # until the operation is exited.

                    trade = Trade(tradepoint, df.index[i], shares_to_trade, "Long", ticker, df.close[i], df.close[i], Commission_Perc)

                    # Save indicators information on trade
                    SMA1_ot.append(round(iSMA1[i], 2))
                    SMA2_ot.append(round(iSMA2[i], 2))
                    MSD_ot.append(round(iMSD[i], 2))
                    ATR_ot.append(round(iATR[i], 2))
                    volatility_ot.append(round(iMSD[i] / df.close[i] * 100, 2))

                    # Here all y variables are set to 0, 
                    # in order to differentiate the signal operation in the trdes df.
                    trades_df = trade.Close(trades_df, tradepoint, df.index[i], Is_Signal=True)

                # BACKTESTING
                else :
                    
                    # Here is reviewed if the tradepoint is triggered.
                    if df.low[i + 1] > tradepoint : continue

                    # If the open is below the tradepoint, 
                    # the tradepoint is set as that open, 
                    # adding more reality to te operation.
                    if df.open[i + 1] < tradepoint : tradepoint = df.open[i + 1]

                    # Before entering the trade,
                    # calculate the shares_to_trade,
                    # in order to risk the Perc_In_Risk of the stock,
                    # finally make sure that we can afford those shares to trade.
                    current_avg_lose = tradepoint * (Perc_In_Risk / 100)

                    shares_to_trade = round(abs(Risk_Unit / current_avg_lose), 1)
                    if shares_to_trade == 0 : continue

                    # Here the order is set, saving all variables 
                    # that characterizes the operation.
                    # The on_trade flag is updated 
                    # in order to avoid more than one operation calculation in later candles, 
                    # until the operation is exited.

                    trade = Trade(tradepoint, df.index[i + 1], shares_to_trade, "Long", ticker, df.high[i + 1], df.low[i + 1], Commission_Perc)

                    # Save indicators information on trade
                    SMA1_ot.append(round(iSMA1[i], 2))
                    SMA2_ot.append(round(iSMA2[i], 2))
                    MSD_ot.append(round(iMSD[i], 2))
                    ATR_ot.append(round(iATR[i], 2))
                    volatility_ot.append(round(iMSD[i] / df.close[i] * 100, 2))

                    # region TRADE_MANAGEMENT

                    new_df = df.loc[df.index >= df.index[i]]
                    for j in range(len(new_df)) :
                        
                        if j == 0 : continue

                        # Here the max_income and min_income variable are updated.
                        if new_df.high[j] > trade.max_price :
                            trade.max_price = new_df.high[j]

                        if new_df.low[j] < trade.min_price :
                            trade.min_price = new_df.low[j]

                        # If the current close is above the last close:
                        if new_df.close[j] > new_df.close[j - 1] :
                            
                            # To simulate that the order is executed in the next day, 
                            # the entry price is taken in the next candle open. 
                            # Nevertheless, when we are in the last candle that can't be done, 
                            # that's why the current close is saved in that case.
                            if j == len(new_df) - 1 :
                                trades_df = trade.Close(trades_df, new_df.close[j], new_df.index[j], Close_Tomorrow=True)
                            else :
                                trades_df = trade.Close(trades_df, new_df.open[j + 1], new_df.index[j + 1])
                            break

                        if j == len(new_df) - 1 :

                            # All characteristics are saved 
                            # as if the trade was exited in this moment.
                            trades_df = trade.Close(trades_df, new_df.close[j], new_df.index[j])
                            break

                    # endregion 
                
                # endregion

            # region TRADES_DF

            # Add indicators information to the df
            if len(SMA1_ot) > 0 :
                trades_df[f'iSMA{SMA1_Period}'] = np.array(SMA1_ot)
                trades_df[f'iSMA{SMA2_Period}'] = np.array(SMA2_ot)
                trades_df[f'iMSD{MSD_Period}'] = np.array(MSD_ot)
                trades_df[f'iATR{ATR_Period}'] = np.array(ATR_ot)
                trades_df[f'volatility'] = np.array(volatility_ot)
            
            # Here the current trades df is added to 
            # the end of the global_trades df.
            trades_global = pd.concat([trades_global, trades_df])

            # endregion   
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

    return_table = Return_Table(portfolio_trades)
    #return_table = pd.DataFrame()

    portfolio_trades = portfolio_trades.sort_values(by=['entry_date'], ignore_index=True)
    portfolio_trades.set_index(portfolio_trades['entry_date'], inplace=True)
    del portfolio_trades['entry_date']

    stats = Stats(portfolio_trades, Account_Size)

    Files_Return(portfolio_trades, return_table, stats, unavailable_tickers)

    notification = win10toast.ToastNotifier()
    notification.show_toast('Alert!', 'Backtesting finished.')

main()
