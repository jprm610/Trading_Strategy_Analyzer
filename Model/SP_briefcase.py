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

import os

import datetime

import quandl
quandl.ApiConfig.api_key = "RA5Lq7EJx_4NPg64UULv"
# endregion

# read_html() allows as to read tables in any webpage,
# in this case we are reading a wikipedia article in which
# are listed all current stocks of S&P500.
# Then only the symbols are extracted and cleaned.
sp500 = pd.read_html('https://en.wikipedia.org/wiki/List_of_S%26P_500_companies')
actives = sp500[0]
tickers = actives['Symbol'].to_list()
tickers = [i.replace('.','-') for i in tickers]

# Here we create a df in which all trades are going to be saved.
trades_global = pd.DataFrame()

# region PARAMETERS
Start_Date = '2011-01-01'

Use_Pre_Charged_Data = True

Use_Last_Candle_Weakness = False
Use_Tradepoint_Check = False

# Indicators
SMA_Period = 200
MSD_Period = 100
RSI_Period = 3
ATR_Period = 10
Max_Days_On_Trade = 10

Consecutive_Lower_Lows = 3

SPY_SMA_Period = 200

# Entry and Exit conditions
Entry_RSI = 10
Exit_RSI = 70

Risk_Unit = 50
Perc_In_Risk = 4
Last_Candle_Weakness = 25
Trade_Slots = 10
Commission_Perc = 0.1

# endregion

#tickers = tickers[0:10].copy()

# region SPY df
print('SPY')

SPY_global = yf.download('SPY', start=Start_Date)

# Rename df columns for cleaning porpuses 
# and applying recommended "adjusted close" info.
SPY_global.columns = ['open', 'high', 'low', 'close', 'adj close', 'volume']

# Drop duplicates leaving only the first value,
# this due to the resting entry_dates in which the market is not moving.
SPY_global = SPY_global.drop_duplicates(keep=False)
# endregion

# region Tickers_df

Start_Date = pd.to_datetime(Start_Date)

tickers_df = pd.read_csv('SP500_historical.csv', sep=';')

tickers_df['date'] = [i.replace('/','-') for i in tickers_df['date']]
tickers_df['date'] = [pd.to_datetime(i, format='%d-%m-%Y') for i in tickers_df['date']]

tickers_df1 = pd.DataFrame(columns=['start_date', 'end_date', 'symbols'])
for i in range(len(tickers_df)) :

    tickers = {}
    ticks = []
    tickers['start_date'] = tickers_df.date[i]
    if i == len(tickers_df) - 1 :
        tickers['end_date'] = datetime.datetime.today().strftime('%Y-%m-%d')
    else :
        tickers['end_date'] = tickers_df.date[i]

    for j in tickers_df.columns[1:] :
        ticks.append(tickers_df[j].values[i])

    ticks = [x for x in ticks if x == x]
    ticks = [x.replace('.','_') for x in ticks]
    ticks.sort()
    tickers['symbols'] = ticks

    tickers_df1.loc[len(tickers_df1)] = [tickers['start_date'], tickers['end_date'], tickers['symbols']]

tickers_df2 = pd.DataFrame(columns=['start_date', 'symbols'])
end_dates = []
for i in range(len(tickers_df1)) :
    if i == 0 : tickers_df2.loc[len(tickers_df2)] = tickers_df1.iloc[i]
    elif i == len(tickers_df1) - 1 :
        end_dates.append(datetime.datetime.today().strftime('%Y-%m-%d'))
    else :
        if tickers_df2['symbols'].values[-1] != tickers_df1['symbols'].values[i] :
            tickers_df2.loc[len(tickers_df2)] = tickers_df1.iloc[i]
            end_dates.append(tickers_df1['end_date'].values[i] - datetime.timedelta(days=1))

tickers_df2['end_date'] = np.array(end_dates)

cleaned_tickers = pd.DataFrame(columns=['start_date', 'symbols', 'end_date'])

for i in range(len(tickers_df2)) :
    if tickers_df2['start_date'].values[i] >= Start_Date :
        cleaned_tickers.loc[len(cleaned_tickers)] = tickers_df2.iloc[i]

# endregion

# region Tickers_Periods

tick_dict = {}
for i in range(len(cleaned_tickers)) :
    for asset in cleaned_tickers['symbols'].values[i] :
        if asset not in tick_dict :
            tick_dict[asset] = []
        tick_dict[asset].append(tuple((cleaned_tickers['start_date'].values[i], cleaned_tickers['end_date'].values[i])))

for e in tick_dict.keys() :
    clean_dates = []
    for i in range(len(tick_dict[e])) :
        if len(tick_dict[e]) == 1 :
            clean_dates.append(tick_dict[e][i][0])
            clean_dates.append(tick_dict[e][i][1])
        elif i == 0 : clean_dates.append(tick_dict[e][i][0])
        elif i == len(tick_dict[e]) - 1 : clean_dates.append(tick_dict[e][i][1])
        else :
            if tick_dict[e][i - 1][1] != tick_dict[e][i][0] - np.timedelta64(1,'D') :
                clean_dates.append(tick_dict[e][i - 1][1])
                clean_dates.append(tick_dict[e][i][0])
            
    tick_dict[e] = clean_dates

# endregion

# In this loop the evaluation of all stocks is done.
asset_count = 1
for asset in tick_dict.keys() :
    # This section is only for front-end purposes,
    # in order to show the current progress of the program when is running.
    print(str(asset_count) + '/' + str(len(tick_dict.keys())))
    print(asset)
    asset_count += 1

    its = int(len(tick_dict[asset]) / 2)
    for a in range(its) :
        if Use_Pre_Charged_Data :
                current_date = a * 2
                try :
                    df = pd.read_csv('SP_data/' + str(asset) + str(a) + '.csv', sep=';')
                except :
                    print('ERROR: Not available data for ' + str(asset) + '.')
                    continue

                print('Charged!')

                df['date'] = [pd.to_datetime(i, format='%Y-%m-%d') for i in df['date']]
                df.set_index(df['date'], inplace=True)
                del df['date']

                # If the ticker df doesn't have any information skip it.
                if len(df) == 0 : continue
        else :
            for a in range(its) :
                current_date = a * 2
                try :
                    df = quandl.get_table('SHARADAR/SEP', ticker=str(asset), date={'gte': tick_dict[asset][current_date], 'lte': tick_dict[asset][current_date + 1]})
                except :
                    print('ERROR: Not available data for ' + str(asset) + '.')
                    continue

                print('Downloaded!')

                df.drop(['closeunadj', 'lastupdated'], axis=1, inplace=True)
                df.sort_values(by=['date'], ignore_index=True, inplace=True)
                df.set_index(df['date'], inplace=True)
                del df['date']

                # If the ticker df doesn't have any information skip it.
                if len(df) == 0 : continue

                # If this is the first ticker to save, create a folder to save all the data, 
                # if there isn't one available yet.
                if asset_count == 2 :
                    try :
                        # Create dir.
                        os.mkdir('SP_data')
                    except :
                        # Save the data.
                        df.to_csv('SP_data/' + str(asset) + str(a) + '.csv', sep=';')

                # Save the data.
                df.to_csv('SP_data/' + str(asset) + str(a) + '.csv', sep=';')

        # This check is done in order that the SPY 
        # information coincides with the current ticker info.
        SPYa = SPY_global.loc[SPY_global.index >= df.index[0]]
        SPY = SPYa.loc[SPYa.index <= df.index[-1]]

        # Get all indicator lists,
        # for this strategy we only need SMA 200 and RSI 10.
        iSPY_SMA = TA.SMA(SPY, SPY_SMA_Period)
        iSPY_SMA[0 : SPY_SMA_Period] = -1

        iSMA1 = TA.SMA(df, SMA_Period)
        iRSI = TA.RSI(df, RSI_Period)
        iMSD = TA.MSD(df, MSD_Period)
        iTR = TA.TR(df)
        iATR = TA.ATR(df, ATR_Period)

        # endregion

        # region TRADE_EMULATION

        # Here are declared all lists that are going to be used 
        # to save trades characteristics.
        dates = []
        trade_type = []
        stock = []

        entry_price = []
        exit_price = []
        shares_to_trade_list = []

        iSMA1_ot = []
        iRSI_ot = []
        iMSD_ot = []
        iTR_ot = []
        iATR_ot = []
        volatity_ot = []
        candle_weakness_ot = []

        close_ot = []
        low_ot = []

        y_raw = []
        y2_raw = []
        y3_raw = []
        y = []
        y2 = []
        y3 = []
        y_index = []

        # Here occurs the OnBarUpdate() in which all strategie calculations happen.
        on_trade = False
        for i in range(len(df)) :

            # region CHART INITIALIZATION

            # Here we make sure that we have enough info to work with.
            if i == 0 : continue

            if iSMA1[i] == -1 or iRSI[i] == -1 : continue

            # endregion
            
            # region TRADE CALCULATION

            # If there isn't a trade in progress:
            if not on_trade :
                if SPY.close[i] > iSPY_SMA[i] :
                    # Check if the current price is above the SMA1,
                    # this in purpose of determining whether the stock is in an up trend
                    # and if the RSI is below 10, enter the trade in the next open.
                    if df.close[i] > iSMA1[i] and iRSI[i] < Entry_RSI :
                        
                        # Review wheter there are the required consecutive lower lows.
                        if Consecutive_Lower_Lows <= 0 : is_consecutive_check = True
                        else :
                            is_consecutive_check = True
                            for j in range(Consecutive_Lower_Lows) :
                                if df.low[i - j] > df.low[i - j - 1] :
                                    is_consecutive_check = False
                                    break

                        if is_consecutive_check :
                            
                            if Use_Last_Candle_Weakness :
                                lower_tail = df.close[i] - df.low[i]
                                close_proportion = (lower_tail / iTR[i]) * 100
                            else :
                                close_proportion = Last_Candle_Weakness

                            if close_proportion <= Last_Candle_Weakness :
                                if Use_Tradepoint_Check :
                                    # region Limit Operation
                                    tradepoint = df.close[i] - 0.5 * iATR[i]

                                    if i == len(df) - 1 :
                                        current_avg_lose = tradepoint * (Perc_In_Risk / 100)

                                        shares_to_trade = round(abs(Risk_Unit / current_avg_lose))
                                        if shares_to_trade == 0 : continue

                                        # Here the order is set, saving all variables 
                                        # that characterizes the operation.
                                        # The on_trade flag is updated 
                                        # in order to avoid more than one operation calculation in later candles, 
                                        # until the operation is exited.
                                        trade_type.append("Long")
                                        stock.append(asset)
                                        entry_candle = i
                                        shares_to_trade_list.append(shares_to_trade)
                                        iSMA1_ot.append(iSMA1[i])
                                        iRSI_ot.append(iRSI[i])
                                        iMSD_ot.append(iMSD[i])
                                        iTR_ot.append(iTR[i])
                                        iATR_ot.append(iATR[i])
                                        volatity_ot.append(iMSD[i] / df.close[i] * 100)

                                        candle_weakness_ot.append(close_proportion)
                                        
                                        # To simulate that the order is executed in the next day, 
                                        # the entry price is taken in the next candle open. 
                                        # Nevertheless, when we are in the last candle that can't be done, 
                                        # that's why the current close is saved in that case.
                                        dates.append(df.index[i])
                                        max_income = df.high[i]
                                        entry_price.append(tradepoint)

                                        # All characteristics are saved 
                                        # as if the trade was exited in this moment.
                                        outcome = (tradepoint - entry_price[-1]) * shares_to_trade

                                        y2.append((max_income - entry_price[-1]) * shares_to_trade)
                                        y.append(outcome)
                                        y_raw.append(outcome / shares_to_trade)
                                        y2_raw.append(y2[-1] / shares_to_trade)
                                        y_index.append(df.index[i])
                                        exit_price.append(tradepoint)
                                        break
                                    else :
                                        if df.low[i + 1] <= tradepoint :
                                            # Before entering the trade,
                                            # calculate the shares_to_trade,
                                            # in order to risk the Perc_In_Risk of the stock,
                                            # finally make sure that we can afford those shares to trade.
                                            current_avg_lose = tradepoint * (Perc_In_Risk / 100)

                                            shares_to_trade = round(abs(Risk_Unit / current_avg_lose))
                                            if shares_to_trade == 0 : continue

                                            # Here the order is set, saving all variables 
                                            # that characterizes the operation.
                                            # The on_trade flag is updated 
                                            # in order to avoid more than one operation calculation in later candles, 
                                            # until the operation is exited.
                                            on_trade = True
                                            trade_type.append("Long")
                                            stock.append(asset)
                                            entry_candle = i - 1
                                            shares_to_trade_list.append(shares_to_trade)
                                            iSMA1_ot.append(iSMA1[i])
                                            iRSI_ot.append(iRSI[i])
                                            iMSD_ot.append(iMSD[i])
                                            iTR_ot.append(iTR[i])
                                            iATR_ot.append(iATR[i])
                                            volatity_ot.append(iMSD[i] / df.close[i] * 100)

                                            candle_weakness_ot.append(close_proportion)
                                            
                                            # To simulate that the order is executed in the next day, 
                                            # the entry price is taken in the next candle open. 
                                            # Nevertheless, when we are in the last candle that can't be done, 
                                            # that's why the current close is saved in that case.
                                            dates.append(df.index[i + 1])
                                            max_income = df.high[i + 1]
                                            entry_price.append(tradepoint)
                                    # endregion
                                else :
                                    # region Market Operation

                                    # Before entering the trade,
                                    # calculate the shares_to_trade,
                                    # in order to risk the Perc_In_Risk of the stock,
                                    # finally make sure that we can afford those shares to trade.
                                    if i == len(df) - 1 : current_avg_lose = df.close[i] * (Perc_In_Risk / 100)
                                    else : current_avg_lose = df.open[i + 1] * (Perc_In_Risk / 100)

                                    shares_to_trade = round(abs(Risk_Unit / current_avg_lose), 1)
                                    if shares_to_trade == 0 : continue

                                    # Here the order is set, saving all variables 
                                    # that characterizes the operation.
                                    # The on_trade flag is updated 
                                    # in order to avoid more than one operation calculation in later candles, 
                                    # until the operation is exited.
                                    on_trade = True
                                    trade_type.append("Long")
                                    stock.append(asset)
                                    entry_candle = i
                                    shares_to_trade_list.append(shares_to_trade)
                                    iSMA1_ot.append(iSMA1[i])
                                    iRSI_ot.append(iRSI[i])
                                    iMSD_ot.append(iMSD[i])
                                    iTR_ot.append(iTR[i])
                                    volatity_ot.append(iMSD[i] / df.close[i] * 100)

                                    candle_weakness_ot.append(close_proportion)
                                    
                                    # To simulate that the order is executed in the next day, 
                                    # the entry price is taken in the next candle open. 
                                    # Nevertheless, when we are in the last candle that can't be done, 
                                    # that's why the current close is saved in that case.
                                    if i == len(df) - 1 :
                                        dates.append(df.index[i])
                                        max_income = df.high[i]
                                        min_income = df.low[i]
                                        entry_price.append(df.close[i])
                                    else :
                                        dates.append(df.index[i + 1])
                                        max_income = df.high[i + 1]
                                        min_income = df.low[i + 1]
                                        entry_price.append(df.open[i + 1])
                                    # endregion
            # endregion

            # region TRADE MANAGEMENT
            # If there is a trade in progress.
            else :
                # Ordinary check to avoid errors.
                if len(trade_type) == 0 : continue
                
                # Here the max_income variable is updated.
                if df.high[i] > max_income and i != entry_candle :
                    max_income = df.high[i]

                if df.low[i] < min_income and i != entry_candle :
                    min_income = df.low[i]

                # If the RSI is over 50 or 10 days have passed:
                if iRSI[i] > Exit_RSI or i == entry_candle + Max_Days_On_Trade :

                    # The trade is exited.
                    # First updating the on_trade flag.
                    on_trade = False

                    # To simulate that the order is executed in the next day, 
                    # the entry price is taken in the next candle open. 
                    # Nevertheless, when we are in the last candle that can't be done, 
                    # that's why the current close is saved in that case.
                    if i == len(df) - 1 :
                        outcome = ((df.close[i] * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                        y_index.append(df.index[i])
                        exit_price.append(df.close[i])
                    else :
                        outcome = ((df.open[i + 1] * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                        y_index.append(df.index[i + 1])
                        exit_price.append(df.open[i + 1])

                    # Saving all missing trade characteristics.
                    y.append(outcome)
                    y2.append((max_income - entry_price[-1]) * shares_to_trade)
                    y3.append((min_income - entry_price[-1]) * shares_to_trade)
                    y_raw.append(outcome / shares_to_trade)
                    y2_raw.append(y2[-1] / shares_to_trade)
                    y3_raw.append(y3[-1] / shares_to_trade)

            # If a trade is in progress in the last candle:
            if i == len(df) - 1 and on_trade :

                # All characteristics are saved 
                # as if the trade was exited in this moment.
                outcome = ((df.close[i] * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                y.append(outcome)
                y2.append((max_income - entry_price[-1]) * shares_to_trade)
                y3.append((min_income - entry_price[-1]) * shares_to_trade)
                y_raw.append(outcome / shares_to_trade)
                y2_raw.append(y2[-1] / shares_to_trade)
                y3_raw.append(y3[-1] / shares_to_trade)
                y_index.append(df.index[i])
                exit_price.append(df.close[i])
            # endregion
        # endregion

        # region TRADES_DF

        # A new df is created in order to save the trades done in the current ticker.
        trades = pd.DataFrame()

        # Here all trades including their characteristics are saved in a df.
        trades['entry_date'] = np.array(dates)
        trades['exit_date'] = np.array(y_index)
        trades['trade_type']  = np.array(trade_type)
        trades['stock'] = np.array(stock)

        trades['entry_price'] = np.array(entry_price)
        trades['exit_price'] = np.array(exit_price)

        trades['iSMA1']  = np.array(iSMA1_ot)
        trades['iRSI'] = np.array(iRSI_ot)
        trades['iMSD'] = np.array(iMSD_ot)
        trades['iTR'] = np.array(iTR_ot)
        
        if Use_Tradepoint_Check : trades['iATR'] = np.array(iATR_ot)
    
        trades['volatility'] = np.array(volatity_ot)

        if Use_Last_Candle_Weakness : trades['candle_weakness'] = np.array(candle_weakness_ot)

        trades['y']  = np.array(y)
        trades['y2'] = np.array(y2)
        trades['y3'] = np.array(y3)
        trades['y_raw'] = np.array(y_raw)
        trades['y2_raw'] = np.array(y2_raw)
        trades['y3_raw'] = np.array(y3_raw)
        trades['shares_to_trade'] = np.array(shares_to_trade_list)

        # endregion

        # Here the current trades df is added to 
        # the end of the global_trades df.
        trades_global = trades_global.append(trades)
    
# Here the trades_global df is edited 
# in order to set the entry_date characteristic as the index.
trades_global = trades_global.sort_values(by=['entry_date'])
trades_global.to_csv('SP_trades_raw.csv', sep=';')

# region Portfolio

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
Slots = 10
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
            Filtered_Trades = Filtered_Trades.sort_values(by=['volatility'], ascending=False)
            Portfolio_Trades = Portfolio_Trades.append(Filtered_Trades[0:Slots - Counter], ignore_index=True)
        else :
            Portfolio_Trades = Portfolio_Trades.append(trades_global[trades_global['entry_date'] == Number_of_trades.index.values[i]], ignore_index=True)
    Acum_Opened = sum(1 for x in Portfolio_Trades['entry_date'] if x == Number_of_trades.index.values[i]) + Acum_Opened

# endregion

trades_global = Portfolio_Trades

# endregion

# region Return Table

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
return_table.to_csv('Return Table.csv', sep=';')

# endregion

trades_global = trades_global.sort_values(by=['entry_date'], ignore_index=True)
trades_global.set_index(trades_global['entry_date'], inplace=True)
del trades_global['entry_date']

# region Stats

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
accumulate_y['dates'] = trades_global.index
accumulate_y.set_index(accumulate_y['dates'], drop=True, inplace=True)

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

py.offline.plot(fig, filename = "Analysis_chart.html")

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
for i in range(len(drawdowns)) :
    if drawdowns[i] > max_dd :
        max_dd = drawdowns[i]
        perc_max_dd = drawdowns[i] / dd_piecks[i] * 100

stats.loc[len(stats)] = ['Max drawdown', max_dd]
stats.loc[len(stats)] = ['Max drawdown %', perc_max_dd]

# endregion
# endregion

# Finally stats and trades_global df are exported as .csv files 
# that can be opened with application such as excel for further review.
stats.to_csv('SP_stats.csv', sep=';')
trades_global.to_csv('SP_trades.csv', sep=';')