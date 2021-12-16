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

Use_Pre_Charged_Data = False
# Indicators

BTC_SMA_Period = 200

VPN_period = 10
DO_period = 130

VPN_to_trade = -101
Flat_iDO = 15

TS_proportion_SPY_above_200 = 0.7
TS_proportion_SPY_below_200 = 0.85
Stop_Lock = 0.5

# Overall backtesting parameters
Start_Date = '2011-01-01'
Risk_Unit = 100
Perc_In_Risk = 16
Trade_Slots = 10
Commission_Perc = 0.1
Account_Size = 10000

# endregion

unavailable_tickers = []

def main() :
    trades_global = pd.DataFrame()

    print("The current working directory is " + os.getcwd())

    BTC_global, iBTC_SMA_global = BTC_regime()

    exchange_info = client.get_exchange_info()
    bnb_cryptos = [i['symbol'] for i in exchange_info['symbols']]
    bnb_cryptos = [i for i in bnb_cryptos if i[-4:] == 'USDT']
    bnb_cryptos = set([i[:-4] for i in bnb_cryptos])
    cryptos = bnb_cryptos
    """
    yf_cryptos = pd.read_csv('Model\cryptos_tickers_YF.csv')
    yf_cryptos = [i for i in yf_cryptos['tickers']]
    cryptos_bnb = set([i[:-4] for i in bnb_cryptos])
    cryptos_yf = set([i[:-4] for i in yf_cryptos])
    cryptos = set.union(cryptos_yf, cryptos_bnb)
    cryptos = list(cryptos)
    """

    print('------------------------------------------------------------')
    print("Trade Calculation!")
    asset_count = 1
    # For every ticker in the tickers_directory :
    for ticker in cryptos :

        # This section is only for front-end purposes,
        # in order to show the current progress of the program when is running.
        print('------------------------------------------------------------')
        print(str(asset_count) + '/' + str(len(cryptos)))
        print(ticker)
        asset_count += 1

        if ticker in ['EUR', 'AUD'] : continue

        # region GET DATA

        # If we want to use already downloaded data :
        if Use_Pre_Charged_Data :

            # Then try to get the .csv file of the ticker.
            try :
                df = pd.read_csv(f"Model/Crypto_data/{ticker}USD.csv", sep=';')
            # If that's not possible, raise an error, 
            # save that ticker in unavailable tickers list 
            # and skip this ticker calculation.
            except :
                print(f"ERROR: Not available data for {ticker}USD.")
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
            """
            try :
                # Then download the information from Yahoo Finance 
                # and rename the columns for standarizing data.
                yf_df = yf.download(f"{ticker}-USD")
                yf_df.columns = ['open', 'high', 'low', 'close', 'adj close', 'volume']
                yf_df.index.names = ['date']
                del yf_df['adj close']
            # If that's not possible :
            except :
                # If that's not possible, raise an error, 
                # save that ticker in unavailable tickers list 
                # and skip this ticker calculation.
                print(f"ERROR: Not available data for {ticker}USD in YF.")
                yf_df = pd.DataFrame()
                """

            try :
                # Then download the information from Yahoo Finance 
                # and rename the columns for standarizing data.
                first_date = client._get_earliest_valid_timestamp(f"{ticker}USDT", '1d')
                historical = client.get_historical_klines(f"{ticker}USDT", '1d', first_date)
                for line in historical :
                    del line[6:]
                bnb_df = pd.DataFrame(historical, columns=['date', 'open', 'high', 'low', 'close', 'volume'])
                bnb_df.set_index('date', inplace=True)
                bnb_df.index = pd.to_datetime(bnb_df.index, unit='ms')

                cols = ['open', 'high', 'low', 'close', 'volume']
                for col in cols :
                    bnb_df[col] = [float(i) for i in bnb_df[col]]
            # If that's not possible :
            except :
                # If that's not possible, raise an error, 
                # save that ticker in unavailable tickers list 
                # and skip this ticker calculation.
                print(f"ERROR: Not available data for {ticker}USD in BNB.")
                bnb_df = pd.DataFrame()

            df = bnb_df.copy()
            df = df[:-1]

            """if len(bnb_df) >= len(yf_df) :
                df = bnb_df.copy()
            else :
                df = yf_df.copy()"""

            if df.empty :
                print('Failed!')
                continue

            print('Downloaded!')

            # Try to create a folder to save all the data, 
            # if there isn't one available yet.
            try :
                # Create dir.
                os.mkdir('Model/Crypto_data')
                df.to_csv(f"Model/Crypto_data/{ticker}USD.csv", sep=';')
            except :
                # Save the data.
                df.to_csv(f"Model/Crypto_data/{ticker}USD.csv", sep=';')

        df.columns = ['open', 'high', 'low', 'close', 'volume']

        missing = pd.date_range(start=df.index[0], end=df.index[len(df) - 1]).difference(df.index)

        for day in missing :
            new_open = df['open'].loc[day - datetime.timedelta(days=1)]
            new_high = df['high'].loc[day - datetime.timedelta(days=1)]
            new_low = df['low'].loc[day - datetime.timedelta(days=1)]
            new_close = df['close'].loc[day - datetime.timedelta(days=1)]
            new_volume = df['volume'].loc[day - datetime.timedelta(days=1)]
            df.loc[day] = [new_open, new_high, new_low, new_close, new_volume]
            df = df.sort_index()

        # Here both BTC_SMA and BTC information is cut,
        # in order that the data coincides with the current df period.
        # This check is done in order that the BTC 
        # information coincides with the current ticker info.
        iBTC_SMAa = iBTC_SMA_global.loc[iBTC_SMA_global.index >= df.index[0]]
        iBTC_SMA = iBTC_SMAa.loc[iBTC_SMAa.index <= df.index[-1]]

        BTCa = BTC_global.loc[BTC_global.index >= df.index[0]]
        BTC = BTCa.loc[BTCa.index <= df.index[-1]]

        # endregion

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
        crypto = []

        entry_price = []
        exit_price = []
        stop_price = []
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
            if i == 0 : continue

            if (math.isnan(iBTC_SMA['SMA'].values[i]) or math.isnan(iVPN[i]) or 
                math.isnan(iSMA[i]) or math.isnan(iDO.UPPER[i])) : continue
            # endregion

            # region TRADE CALCULATION
            # Here the Regime Filter is done,
            # checking that the current BTC close is above the BTC's SMA.
            if df.close[i] > iDO.UPPER[i - 1] :
                if BTC.close[i] > iBTC_SMA['SMA'].values[i] : 
                    if i - last_iDO_breakout > Flat_iDO and last_iDO_breakout != 0 :
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

                                new_df = df.loc[df.index >= df.index[i + 1]]
                                for j in range(len(new_df)) :

                                    # Ordinary check to avoid errors.
                                    if len(trade_type) == 0 : 
                                        last_iDO_breakout = i
                                        continue
                                    
                                    # Here the max_income variable is updated.
                                    if new_df.high[j] > max_income :
                                        max_income = new_df.high[j]

                                    if BTC.close[i + j + 1] > iBTC_SMA['SMA'].values[i + j + 1] :
                                        trailling_stop = max_income * TS_proportion_SPY_above_200
                                    else :
                                        trailling_stop = max_income * TS_proportion_SPY_below_200

                                    stop_lock = max_income * Stop_Lock

                                    # Here the min_income variable is updated.
                                    if new_df.low[j] < min_income :
                                        min_income = new_df.low[j]

                                    if new_df.low[j] <= stop_lock :

                                        # To simulate that the order is executed in the next day, 
                                        # the entry price is taken in the next candle open. 
                                        # Nevertheless, when we are in the last candle that can't be done, 
                                        # that's why the current close is saved in that case.
                                        if j == len(new_df) - 1 :
                                            outcome = ((stop_lock * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                                            y_index.append(new_df.index[j])
                                            exit_price.append(stop_lock)

                                            close_tomorrow.append(True)
                                        else :
                                            outcome = ((stop_lock * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                                            y_index.append(new_df.index[j + 1])
                                            exit_price.append(stop_lock)

                                            close_tomorrow.append(False)

                                        if exit_price[-1] > max_income : max_income = exit_price[-1]

                                        if exit_price[-1] < min_income : min_income = exit_price[-1]

                                        # Saving all missing trade characteristics.
                                        y_raw.append(exit_price[-1] - entry_price[-1])
                                        y_perc.append(y_raw[-1] / entry_price[-1] * 100)
                                        y2_raw.append(max_income - entry_price[-1])
                                        y3_raw.append(min_income - entry_price[-1])
                                        y.append(outcome)
                                        y2.append(y2_raw[-1] * shares_to_trade)
                                        y3.append(y3_raw[-1] * shares_to_trade)
                                        max_price.append(max_income)
                                        min_price.append(min_income)
                                        stop_price.append(stop_lock)
                                        break
                                    # If the current close is below SMA :
                                    elif new_df.close[j] < trailling_stop :

                                        # To simulate that the order is executed in the next day, 
                                        # the entry price is taken in the next candle open. 
                                        # Nevertheless, when we are in the last candle that can't be done, 
                                        # that's why the current close is saved in that case.
                                        if j == len(new_df) - 1 :
                                            outcome = ((new_df.close[j] * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                                            y_index.append(new_df.index[j])
                                            exit_price.append(new_df.close[j])

                                            close_tomorrow.append(True)
                                        else :
                                            outcome = ((new_df.open[j + 1] * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                                            y_index.append(new_df.index[j + 1])
                                            exit_price.append(new_df.open[j + 1])

                                            close_tomorrow.append(False)

                                        if exit_price[-1] > max_income : max_income = exit_price[-1]

                                        if exit_price[-1] < min_income : min_income = exit_price[-1]

                                        # Saving all missing trade characteristics.
                                        y_raw.append(exit_price[-1] - entry_price[-1])
                                        y_perc.append(y_raw[-1] / entry_price[-1] * 100)
                                        y2_raw.append(max_income - entry_price[-1])
                                        y3_raw.append(min_income - entry_price[-1])
                                        y.append(outcome)
                                        y2.append(y2_raw[-1] * shares_to_trade)
                                        y3.append(y3_raw[-1] * shares_to_trade)
                                        max_price.append(max_income)
                                        min_price.append(min_income)
                                        stop_price.append(stop_lock)
                                        break
                                    
                                    if j == len(new_df) - 1 :
                                        # All characteristics are saved 
                                        # as if the trade was exited in this moment.
                                        outcome = ((new_df.close[j] * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                                        exit_price.append(new_df.close[j])

                                        y_index.append(new_df.index[j])
                                        close_tomorrow.append(False)

                                        y_raw.append(exit_price[-1] - entry_price[-1])
                                        y_perc.append(y_raw[-1] / entry_price[-1] * 100)
                                        y2_raw.append(max_income - entry_price[-1])
                                        y3_raw.append(min_income - entry_price[-1])
                                        y.append(outcome)
                                        y2.append(y2_raw[-1] * shares_to_trade)
                                        y3.append(y3_raw[-1] * shares_to_trade)
                                        max_price.append(max_income)
                                        min_price.append(min_income)
                                        stop_price.append(stop_lock)
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
        trades['crypto'] = np.array(crypto)
        trades['is_signal'] = np.array(is_signal)
        trades['entry_price'] = np.array(entry_price)
        trades['exit_price'] = np.array(exit_price)
        trades['stop_price'] = np.array(stop_price)
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
    #trades_global.drop_duplicates(keep='first', inplace=True)
    trades_global = trades_global.sort_values(by=['entry_date'], ignore_index=True)
    trades_global.to_csv('Model/Files/Crypto_trades_raw.csv')

    trades_global = Portfolio(trades_global)

    #return_table = Return_Table(trades_global)

    trades_global = trades_global.sort_values(by=['entry_date'], ignore_index=True)
    trades_global.set_index(trades_global['entry_date'], inplace=True)
    del trades_global['entry_date']

    stats = Stats(trades_global)

    #Files_Return(trades_global, return_table, stats, unavailable_tickers)
    Files_Return(trades_global, stats)

def BTC_regime() :
    
    # Here the BTC data is downloaded 
    print('BTC')
    BTC_global = yf.download('BTC-USD')
    BTC_global.columns = ['open', 'high', 'low', 'close', 'adj close', 'volume']
    BTC_global.index.names = ['date']
    del BTC_global['adj close']

    missing = pd.date_range(start=BTC_global.index[0], end=BTC_global.index[len(BTC_global) - 1]).difference(BTC_global.index)

    for day in missing :
        new_open = BTC_global['open'].loc[day - datetime.timedelta(days=1)]
        new_high = BTC_global['high'].loc[day - datetime.timedelta(days=1)]
        new_low = BTC_global['low'].loc[day - datetime.timedelta(days=1)]
        new_close = BTC_global['close'].loc[day - datetime.timedelta(days=1)]
        new_volume = BTC_global['volume'].loc[day - datetime.timedelta(days=1)]
        BTC_global.loc[day] = [new_open, new_high, new_low, new_close, new_volume]
        BTC_global = BTC_global.sort_index()

    # Here the BTC SMA is calculated for the Regime filter process,
    # establishing -1 as a value when the SMA is not calculated completely.
    BTC_SMA = TA.SMA(BTC_global, BTC_SMA_Period)

    # The BTC SMA is then saved into a dataframe with it's date, 
    # this in order to "cut" the SMA data needed for a ticker in the backtesting process.
    iBTC_SMA_global = pd.DataFrame({'date' : BTC_global.index, 'SMA' : BTC_SMA})
    iBTC_SMA_global.set_index(iBTC_SMA_global['date'], inplace=True)

    return BTC_global, iBTC_SMA_global

def Portfolio(trades_global) :
    
    # region Number of Trades DF

    # Here is built a df with the number of trades by every date,
    # in order to track how many trades were executed in every date.
    Trading_dates = []
    Number_Trades_per_Date = []
    Number_Closed_Trades_per_Date = []
    Trades_per_date = 0
    last_date = trades_global['entry_date'].values[0]
    for i in range(len(trades_global)) :
        if trades_global['entry_date'].values[i] == last_date :
            Trades_per_date += 1
        else : 
            Closed_Trades_per_Date = sum(1 for x in trades_global['exit_date'][0:i-1] if x <= last_date) - sum(Number_Closed_Trades_per_Date)
            Trading_dates.append(last_date)
            Number_Trades_per_Date.append(Trades_per_date)
            Number_Closed_Trades_per_Date.append(Closed_Trades_per_Date)
            last_date = trades_global['entry_date'].values[i]
            Trades_per_date = 1

    Number_of_trades = pd.DataFrame()
    Number_of_trades['date'] = np.array(Trading_dates)
    Number_of_trades['# of trades'] = np.array(Number_Trades_per_Date)
    Number_of_trades['# of closed trades'] = np.array(Number_Closed_Trades_per_Date)
    Number_of_trades.set_index(Number_of_trades['date'], drop=True, inplace=True)
    del Number_of_trades['date']

    # endregion

    # region Clean DF

    # Here is built a df just with the trades that can be entered
    # taking into account the portfolio rules and the Number_of_trades df.
    Portfolio_Trades = pd.DataFrame()
    Counter = 0
    Slots = Trade_Slots
    Acum_Opened = 0
    Acum_Closed = 0
    for i in range(len(Number_of_trades)) :
        if i != 0 :
            Acum_Closed = sum(1 for x in Portfolio_Trades['exit_date'] if x <= Number_of_trades.index.values[i])
        Counter = Acum_Opened - Acum_Closed
        if Counter < Slots :
            if Number_of_trades['# of trades'].values[i] > Slots - Counter :
                Filtered_Trades = pd.DataFrame()
                Filtered_Trades = Filtered_Trades.append(trades_global[trades_global['entry_date'] == Number_of_trades.index.values[i]], ignore_index=True)
                Filtered_Trades = Filtered_Trades.sample(frac=1)
                Portfolio_Trades = Portfolio_Trades.append(Filtered_Trades[: Slots - Counter], ignore_index=True)
            else :
                Portfolio_Trades = Portfolio_Trades.append(trades_global[trades_global['entry_date'] == Number_of_trades.index.values[i]], ignore_index=True)
        Acum_Opened = sum(1 for x in Portfolio_Trades['entry_date'] if x == Number_of_trades.index.values[i]) + Acum_Opened

    # endregion

    return Portfolio_Trades

def Return_Table(trades_global) :
    # Here the trades_global df is edited 
    # in order to sart the df by exit_date.
    trades_global_rt = trades_global.copy()
    trades_global_rt['exit_date'] =  pd.to_datetime(trades_global_rt['exit_date'], format='%d/%m/%Y')
    trades_global_rt = trades_global_rt.sort_values(by=['exit_date'])
    trades_global_rt.set_index(trades_global_rt['entry_date'], drop=True, inplace=True)

    # Here is built the return_table df with the number of trades by every date.
    Profit_perc = []
    for i in range(len(trades_global_rt)) :
        Profit_perc.append(round(((trades_global_rt['y_raw'].values[i] / trades_global_rt['entry_price'].values[i]) / 10) * 100, 2))
    trades_global_rt['Profit_perc'] = np.array(Profit_perc)
    trades_global_rt['year'] = pd.DatetimeIndex(trades_global_rt['exit_date']).year
    trades_global_rt['month'] = pd.DatetimeIndex(trades_global_rt['exit_date']).month

    return_table = pd.DataFrame()
    df1 = pd.DataFrame()
    return_table = trades_global_rt.groupby([trades_global_rt['year'], trades_global_rt['month']])['Profit_perc'].sum().unstack(fill_value=0)
    df1 = return_table/100 + 1
    df1.columns = return_table.columns.map(str)
    return_table['Y%'] = round(((df1['1'] * df1['2'] * df1['3'] * df1['4'] * df1['5'] * df1['6'] *df1['7'] * df1['8'] * df1['9'] * df1['10'] * df1['11'] * df1['12'])-1)*100,2)

    return return_table

def Stats(trades_global) :
    
    # region General_Stats

    # Here a the stats df is created, 
    # in which all global_stadistics are going to be saved for analysis.
    stats = pd.DataFrame(columns=['stat', 'value'])

    # First the winner and loser trades are separated.
    global_wins = [i for i in trades_global['y'] if i >= 0]
    global_loses = [i for i in trades_global['y'] if i < 0]

    # Then the time period is written.
    stats.loc[len(stats)] = ['Start date', trades_global.index.values[0]]
    stats.loc[len(stats)] = ['End date', trades_global['exit_date'].values[-1]]

    # Then the overall profit is calculated, and the amount of trades is saved.
    stats.loc[len(stats)] = ['Net profit', trades_global['y'].sum()]
    stats.loc[len(stats)] = ['Total # of trades', len(trades_global)]
    stats.loc[len(stats)] = ['# of winning trades', len(global_wins)]
    stats.loc[len(stats)] = ['# of losing trades', len(global_loses)]

    # Here the winning weight is calculated, also known as % profitable.
    profitable = len(global_wins) / len(trades_global)
    stats.loc[len(stats)] = ['Winning Weight', profitable * 100]

    # The total win and lose is saved.
    total_win = sum(global_wins)
    total_lose = sum(global_loses)
    stats.loc[len(stats)] = ['Total win', total_win]
    stats.loc[len(stats)] = ['Total lose', total_lose]

    # Here the profit factor is calculated.
    stats.loc[len(stats)] = ['Profit factor', total_win / abs(total_lose)]

    # The avg win and lose is calculated.
    avg_win = statistics.mean(global_wins)
    avg_lose = statistics.mean(global_loses)
    stats.loc[len(stats)] = ['Avg win', avg_win]
    stats.loc[len(stats)] = ['Avg lose', avg_lose]
    stats.loc[len(stats)] = ['Reward to risk ratio', avg_win / abs(avg_lose)]
    stats.loc[len(stats)] = ['Avg max win', statistics.mean(trades_global['y2'])]
    stats.loc[len(stats)] = ['Avg max lose', statistics.mean(trades_global['y3'])]

    # Best and worst trades, includin the expectancy are saved.
    stats.loc[len(stats)] = ['Best trade', max(global_wins)]
    stats.loc[len(stats)] = ['Worst trade', min(global_loses)]
    stats.loc[len(stats)] = ['Expectancy', (profitable * avg_win) - ((1 - profitable) * -avg_lose)]
    # endregion

    # region Streaks

    # In the first section the best streak is determined.
    winning_streaks = []
    win_streak = 0
    for i in range(len(trades_global)) :
        if trades_global['y'].values[i] >= 0 :
            win_streak += 1
        else : 
            winning_streaks.append(win_streak)
            win_streak = 0
    stats.loc[len(stats)] = ['Best streak', max(winning_streaks)]

    # In the last section the worst streak is determined.
    losing_streaks = []
    lose_streak = 0
    for i in range(len(trades_global)) :
        if trades_global['y'].values[i] < 0 :
            lose_streak += 1
        else : 
            losing_streaks.append(lose_streak)
            lose_streak = 0
    stats.loc[len(stats)] = ['Worst streak', max(losing_streaks)]
    # endregion

    # region Analysis_chart

    # A new df is created in order to save accumulated sum of all trades.
    accumulate_y = pd.DataFrame()
    accumulate_y['acc_y'] = np.cumsum(trades_global['y'])
    accumulate_y['entry_dates'] = trades_global.index
    accumulate_y.set_index(accumulate_y['entry_dates'], drop=True, inplace=True)

    accumulate_y['acc_y'] = accumulate_y['acc_y'].apply(lambda x : x + Account_Size)

    # With this information an analysis chart can be printed 
    # showing the strategy evolution through time.
    fig = make_subplots(rows=1, cols=1, shared_xaxes=True)

    fig.add_trace(
        go.Scatter(x=accumulate_y.index, y=accumulate_y['acc_y'], 
        line=dict(color='rgba(26,148,49)', width=1),
        fill='tozeroy'),
        row=1, col=1
    )

    fig.update_layout(paper_bgcolor='rgba(0,0,0)', plot_bgcolor='rgba(0,0,0)')

    py.offline.plot(fig, filename = "Model/Files/Crypto_Analysis_chart.html")

    # endregion

    # region Drawdown

    # Here the max drawdown an its portion is calculated.
    piecks = []
    dd_piecks = []
    drawdowns = []
    for i in range(len(trades_global)) :
        if i == 0 : 
            piecks.append(accumulate_y['acc_y'].values[i])
            drawdowns.append(0)
            continue
        
        piecks.append(max(accumulate_y['acc_y'].values[i], piecks[i - 1]))

        if accumulate_y['acc_y'].values[i] < piecks[i] : 
            drawdowns.append(piecks[i] - accumulate_y['acc_y'].values[i])
            dd_piecks.append(piecks[i])

    perc_max_dd = 0
    max_dd = 0
    try :
        for i in range(len(drawdowns)) :
            if drawdowns[i] > max_dd :
                max_dd = drawdowns[i]
                perc_max_dd = drawdowns[i] / dd_piecks[i] * 100

        stats.loc[len(stats)] = ['Max drawdown', max_dd]
        stats.loc[len(stats)] = ['Max drawdown %', perc_max_dd]
    except :
        stats.loc[len(stats)] = ['Max drawdown', 'unknown']
        stats.loc[len(stats)] = ['Max drawdown %', 'unknown']

    # endregion

    return stats

def Files_Return(trades_global, stats) :
    trades_global.to_csv('Model/Files/Crypto_trades.csv')
    #return_table.to_csv('Model/Files/Crypto_Return_Table.csv')
    stats.to_csv('Model/Files/Crypto_stats.csv')

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