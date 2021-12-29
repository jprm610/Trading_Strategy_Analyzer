# region LIBRARIES

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

# qunadl library allows us to access the api 
# from where the tickers data is extracted, 
# either from SHARADAR or WIKI.
import quandl
quandl.ApiConfig.api_key = "RA5Lq7EJx_4NPg64UULv"

# endregion

# Here we create a df in which all trades are going to be saved.
trades_global = pd.DataFrame()

# Here we create a list in which are going to be saved 
# those tickers from which we couldn't get historical data.
unavailable_tickers = []

# region PARAMETERS

Use_Pre_Charged_Data = False
# Indicators
SMA1_Period = 100
SMA2_Period = 5
MSD_Period = 100
ATR_Period = 10
Tradepoint_Factor = 0.5

SPY_SMA_Period = 200

Consecutive_Lower_Lows = 2

# Overall backtesting parameters
Start_Date = '2011-01-01'
Risk_Unit = 100
Perc_In_Risk = 2.3
Trade_Slots = 10
Commission_Perc = 0.1
Account_Size = 10000

# endregion

max_period_indicator = max(SMA1_Period, SMA2_Period, MSD_Period, ATR_Period, SPY_SMA_Period)

print(f"The current working directory is {os.getcwd()}")

# region SPY df

# Here the SPY data is downloaded 
print('SPY')
SPY_global = yf.download('SPY')

# Rename df columns for cleaning porpuses 
# and applying recommended.
SPY_global.columns = ['open', 'high', 'low', 'close', 'adj close', 'volume']

# Drop duplicates leaving only the first value,
# this due to the resting entry_dates in which the market is not moving.
SPY_global = SPY_global.drop_duplicates(keep=False)

# Here the SPY SMA is calculated for the Regime filter process,
# establishing -1 as a value when the SMA is not calculated completely.
SPY_SMA = TA.SMA(SPY_global, SPY_SMA_Period)

# The SPY SMA is then saved into a dataframe with it's date, 
# this in order to "cut" the SMA data needed for a ticker in the backtesting process.
iSPY_SMA_global = pd.DataFrame({'date' : SPY_global.index, 'SMA' : SPY_SMA})
iSPY_SMA_global.set_index(iSPY_SMA_global['date'], inplace=True)

# endregion

# region SURVIVORSHIP BIAS

# region Tickers_df
# In this region, the SP500 historical constitutents file is cleaned, 
# this in order to avoid the survivorship bias problem.

# First the Start_Date parameter is formatted and standarized.
Start_Date = pd.to_datetime(Start_Date)

# Here the SP500 historical constitutents file is read, 
# then the dates are standarized to avoid dates missinterpretation.
tickers_df = pd.read_csv("Model/SP500_historical.csv", sep=';')
tickers_df['date'] = [i.replace('/','-') for i in tickers_df['date']]
tickers_df['date'] = pd.to_datetime(tickers_df['date'], format='%d-%m-%Y')

# region FIRST CLEANING
# A first cleaning process is done, creating a new df with every list of tickers and its changes.

# The format is:
#   start_date; end_date; symbols

# Every row means that that list didn't change through that period.
tickers_df1 = pd.DataFrame(columns=['start_date', 'end_date', 'symbols'])
for i in range(len(tickers_df)) :

    # For every row, a tickers directory is crated, in order to save start_date, 
    # end date and the list of symbols for that time period.
    tickers = {}

    # Also a symbols list is created in order to save all symbols columns 
    # as a simplified list.
    symbols = []

    # The start_date is defined simply as the row date.
    tickers['start_date'] = tickers_df.date[i]

    # The end_date is defined here.
    # If the last row is being evaluated,
    # then the end_date is going to be today date.
    if i == len(tickers_df) - 1 :
        tickers['end_date'] = datetime.datetime.today().strftime('%Y-%m-%d')
    # If not, is defined as the next row date.
    else :
        tickers['end_date'] = tickers_df.date[i]

    # Then all symbols that are presented in the current row as separated columns, 
    # are saved into a simplified list.
    for j in tickers_df.columns[1:] :
        symbols.append(tickers_df[j].values[i])

    # Here the symbols are filtered, filtering "None" values, 
    # recalling that "None" != "None".
    # Then "." are replaced by "_" correcting the symbol writing.
    # Finally they're sorted and saved in the dictionary.
    symbols = [x for x in symbols if x == x]
    symbols = [x.replace('.','-') for x in symbols]
    symbols.sort()
    tickers['symbols'] = symbols

    # Lastly, a new row is appended with all 3 different column values.
    tickers_df1.loc[len(tickers_df1)] = [tickers['start_date'], tickers['end_date'], tickers['symbols']]

# endregion

# region LAST CLEANING

# Here are eliminated the continouos periods, cleaning the data.
# For example: if a period is 01-01-2020 - 02-01-2020,
#              and the next one is 02-01-2020 - 03-02-2020
#              This period is really 01-01-2020 - 03-02-2020.
tickers_df2 = pd.DataFrame(columns=['start_date', 'symbols'])

# The real work here is to get the end_dates, 
# that's why we only create a list for that instance.
end_dates = []
for i in range(len(tickers_df1)) :

    # For the first row, we copy the same information from the tickers_df1 df.
    if i == 0 : tickers_df2.loc[len(tickers_df2)] = tickers_df1.iloc[i]
    # For the last row, today date is appended as end date.
    elif i == len(tickers_df1) - 1 :
        end_dates.append(datetime.datetime.today().strftime('%Y-%m-%d'))
    # For the rest:
    else :
        # If to continous list of tickers are different, 
        # add a new row and append the current end_date.
        if tickers_df2['symbols'].values[-1] != tickers_df1['symbols'].values[i] :
            tickers_df2.loc[len(tickers_df2)] = tickers_df1.iloc[i]
            end_dates.append(tickers_df1['end_date'].values[i] - datetime.timedelta(days=1))

# Append the new end_date list in the df.
tickers_df2['end_date'] = np.array(end_dates)

# Only the tickers in the selected period 
# are saved in the cleaned_tickers df.
cleaned_tickers = tickers_df2.loc[tickers_df2['start_date'] >= Start_Date]

# endregion

# endregion

# region Tickers_Periods
# This is the last step done to avoid Survivorship Bias.
# Here a tickers directory is created in order to save 
# each ticker with their periods when they were SP500 constistutents.

# Here the first ticker directory is created.
tickers_directory = {}
for i in range(len(cleaned_tickers)) :
    # For every row in cleaned_tickers df :
    for ticker in cleaned_tickers['symbols'].values[i] :
        # The directory saves every ticker as a key giving it a list, in case is not a key yet.
        if ticker not in tickers_directory :
            tickers_directory[ticker] = []

        # In that ticker list is saved a tuple with it's start_date and end_date.
        tickers_directory[ticker].append(tuple((cleaned_tickers['start_date'].values[i], cleaned_tickers['end_date'].values[i])))

# Finally the tickers directory is cleaned and simplified.

# For every ticker in tickers_directory :
for e in tickers_directory.keys() :
    # Create a list in which cleaned and simplified dates are going to be saved, 
    # replacing the previous list.
    clean_dates = []

    # For every element in that previous ticker list :
    for i in range(len(tickers_directory[e])) :

        # If there's only 1 tuple, save the start_date as the first element 
        # and it's end_date as the second element, 
        # completing the period of that ticker.
        if len(tickers_directory[e]) == 1 :
            clean_dates.append(tickers_directory[e][i][0])
            clean_dates.append(tickers_directory[e][i][1])
        # Or if it's the first iteration, save the first 
        # element of the tuple as the start_date.
        elif i == 0 : clean_dates.append(tickers_directory[e][i][0])
        # Or if it's the last iteration, save the second 
        # element of the tuple as the end_date.
        elif i == len(tickers_directory[e]) - 1 : clean_dates.append(tickers_directory[e][i][1])
        # For everything else :
        else :
            # Check wheter the previous tuple end_date 
            # is not contiguous to the current tuple start_date.
            if tickers_directory[e][i - 1][1] != tickers_directory[e][i][0] - np.timedelta64(1,'D') :
                # If so, close the previous period and open a new one.
                # NOTE: This process is very similar 
                # to the tickers_df2 cleaning process.
                clean_dates.append(tickers_directory[e][i - 1][1])
                clean_dates.append(tickers_directory[e][i][0])
    
    # Finally the old list of dates is replaced with the new one.
    tickers_directory[e] = clean_dates

# endregion

print("Survivorship Bias done!")
# endregion

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
    # then when the quiantity of elements is divided by 2, 
    # it gives us the number of periods of that ticker.
    its = int(len(tickers_directory[ticker]) / 2)
    for a in range(its) :
        # region GET DATA
        
        # Determine the current start date, 
        # reversing the previous operation.
        current_start_date = a * 2

        if (tickers_directory[ticker][current_start_date + 1] != cleaned_tickers['end_date'].values[-1] or
            Use_Pre_Charged_Data) :
            
            if f"{ticker}.csv" not in os.listdir("Model/SP_data") :
                print(f"ERROR: Not available data for {ticker}.")
                continue
            
            # Then try to get the .csv file of the ticker.
            try :
                df = pd.read_csv(f"Model/SP_data/{ticker}.csv", sep=';')
            # If that's not possible, raise an error, 
            # save that ticker in unavailable tickers list 
            # and skip this ticker calculation.
            except :
                print(f"ERROR: Not available data for {ticker}.")
                unavailable_tickers.append(ticker)
                continue
            
            if df.empty :
                print(f"ERROR: Not available data for {ticker}.")
                continue

            # Reformat the df, standarizing dates and index.
            df['date'] = pd.to_datetime(df['date'], format='%Y-%m-%d')

            start_date = max(df.date[0], pd.to_datetime(tickers_directory[ticker][current_start_date]), Start_Date)

            start_index = df.index[df['date'] == start_date].to_list()[0]
            if start_index - max_period_indicator < 0 :
                start_index = 0
            else :
                start_index -= max_period_indicator

            start_date = df['date'].values[start_index]

            while True :
                if start_date in df['date'].tolist() or start_date > pd.to_datetime('today').normalize() : break
                start_date = start_date + datetime.timedelta(days=1)

            if start_date > pd.to_datetime('today').normalize() : continue

            df.set_index(df['date'], inplace=True)
            del df['date']

            df = df.loc[df.index >= start_date]
            df = df.loc[df.index <= tickers_directory[ticker][current_start_date + 1]]

            # If the ticker df doesn't have any information skip it.
            if df.empty : 
                print(f"ERROR: Not available data for {ticker}.")
                continue

            print('Charged!')
        else :
            # If we want to downloaded new data :
            try :
                # Then download the information from Yahoo Finance 
                # and rename the columns for standarizing data.
                df = yf.download(ticker)
                df.columns = ['open', 'high', 'low', 'close', 'adj close', 'volume']
                df.index.names = ['date']
            # If that's not possible :
            except :
                # If that's not possible, raise an error, 
                # save that ticker in unavailable tickers list 
                # and skip this ticker calculation.
                print(f"ERROR: Not available data for {ticker} in YF.")
                unavailable_tickers.append(ticker)
                continue
            
            # If the ticker df doesn't have any information skip it.
            if df.empty : 
                print(f"ERROR: Not available data for {ticker}.")
                continue

            df.reset_index(inplace=True)
            start_date = max(df.date[0], pd.to_datetime(tickers_directory[ticker][current_start_date]), Start_Date)

            while True :
                if start_date in df['date'].tolist() or start_date > pd.to_datetime('today').normalize() : break
                start_date = start_date + datetime.timedelta(days=1)

            if start_date > pd.to_datetime('today').normalize() : continue

            start_index = df.index[df['date'] == start_date].to_list()[0]
            if start_index - max_period_indicator < 0 :
                start_index = 0
            else :
                start_index -= max_period_indicator

            start_date = df['date'].values[start_index]

            df.set_index(df['date'], inplace=True)
            del df['date']

            df = df.loc[df.index >= start_date]
            df = df.loc[df.index <= tickers_directory[ticker][current_start_date + 1]]

            if df.empty :
                # Raise an error, 
                # save that ticker in unavailable tickers list 
                # and skip this ticker calculation.
                print(f"ERROR: Not available data for {ticker} in YF.")
                unavailable_tickers.append(ticker)
                continue
            
            print('Downloaded!')

            # Try to create a folder to save all the data, 
            # if there isn't one available yet.
            try :
                # Create dir.
                os.mkdir('Model/SP_data')
            except :
                # Save the data.
                df.to_csv(f"Model/SP_data/{ticker}{a}.csv", sep=';')

        # Here both SPY_SMA and SPY information is cut,
        # in order that the data coincides with the current df period.
        # This check is done in order that the SPY 
        # information coincides with the current ticker info.
        iSPY_SMAa = iSPY_SMA_global.loc[iSPY_SMA_global.index >= df.index[0]]
        iSPY_SMA = iSPY_SMAa.loc[iSPY_SMAa.index <= df.index[-1]]

        SPYa = SPY_global.loc[SPY_global.index >= df.index[0]]
        SPY = SPYa.loc[SPYa.index <= df.index[-1]]

        # endregion

        # region INDICATOR CALCULATIONS

        # Get all indicator lists,
        # for this strategy we only need SMA 200 and RSI 10.
        iSMA1 = TA.SMA(df, SMA1_Period)
        iSMA2 = TA.SMA(df, SMA2_Period)
        iMSD = TA.MSD(df, MSD_Period)
        iATR = TA.ATR(df, ATR_Period)

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

        iSMA1_ot = []
        iSMA2_ot = []
        iMSD_ot = []
        iATR_ot = []
        volatity_ot = []

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

        # Here occurs the OnBarUpdate() in which all strategie calculations happen.
        on_trade = False
        for i in range(len(df)) :

            # region CHART INITIALIZATION

            # Here we make sure that we have enough info to work with.
            if i == 0 : continue

            if math.isnan(iSPY_SMA['SMA'].values[i]) : continue

            if (math.isnan(iSMA1[i]) or math.isnan(iSMA2[i]) or math.isnan(iMSD[i]) or 
                math.isnan(iATR[i])) : 
                continue

            # endregion
            
            # region TRADE CALCULATION

            # If there isn't a trade in progress:
            if not on_trade :
                # Here the Regime Filter is done, 
                # checking that the current SPY close is above the SPY's SMA.
                if SPY.close[i] > iSPY_SMA['SMA'].values[i] :
                    # Check if the current price is above the SMA1,
                    # this in purpose of determining whether the stock is in an up trend
                    # and if that same close is below the SMA2, 
                    # reflecting a recoil movement.
                    if df.close[i] > iSMA1[i] and df.close[i] < iSMA2[i] :

                        # Review wheter there are the required consecutive lower lows,
                        # first checking the given parameter.
                        if Consecutive_Lower_Lows <= 0 : is_consecutive_check = True
                        else :
                            is_consecutive_check = True
                            for j in range(Consecutive_Lower_Lows) :
                                if df.low[i - j] > df.low[i - j - 1] :
                                    is_consecutive_check = False
                                    break

                        if is_consecutive_check :
                            # region Limit Operation

                            # Here the tradepoint for the Limit Operation is calculated, 
                            # taking into account the ATR.
                            tradepoint = df.close[i] - Tradepoint_Factor * iATR[i]

                            # If there is going to be a signal for the next day.
                            if i == len(df) - 1 :
                                # region Signal Mode

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
                                is_signal.append(True)
                                trade_type.append("Long")
                                stock.append(ticker)
                                shares_to_trade_list.append(shares_to_trade)
                                iSMA1_ot.append(iSMA1[i])
                                iSMA2_ot.append(iSMA2[i])
                                iMSD_ot.append(iMSD[i])
                                iATR_ot.append(iATR[i])
                                volatity_ot.append(iMSD[i] / df.close[i] * 100)
                                
                                # Here all y variables are set to 0, 
                                # in order to differentiate the signal operation in the trdes df.
                                entry_dates.append(df.index[i])
                                entry_price.append(round(tradepoint, 2))
                                y.append(0)
                                y2.append(0)
                                y3.append(0)
                                y_raw.append(0)
                                y_perc.append(0)
                                y2_raw.append(0)
                                y3_raw.append(0)
                                y_index.append(df.index[i])
                                exit_price.append(round(tradepoint, 2))
                                close_tomorrow.append(False)
                                break
                                
                                # endregion
                            else :
                                # region Backtest_Mode

                                # Here is reviewed if the tradepoint is triggered.
                                if df.low[i + 1] <= tradepoint :
                                    
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
                                    on_trade = True
                                    is_signal.append(False)
                                    trade_type.append("Long")
                                    stock.append(ticker)
                                    entry_candle = i + 1
                                    shares_to_trade_list.append(shares_to_trade)
                                    iSMA1_ot.append(iSMA1[i])
                                    iSMA2_ot.append(iSMA2[i])
                                    iMSD_ot.append(iMSD[i])
                                    iATR_ot.append(iATR[i])
                                    volatity_ot.append(iMSD[i] / df.close[i] * 100)
                                    
                                    # To simulate that the order is executed in the next day, 
                                    # the entry price is taken in the next candle open. 
                                    # Nevertheless, when we are in the last candle that can't be done, 
                                    # that's why the current close is saved in that case.
                                    entry_dates.append(df.index[i + 1])
                                    max_income = df.high[i + 1]
                                    min_income = df.low[i + 1]
                                    entry_price.append(round(tradepoint, 2))
                                
                                #endregion
                            # endregion
            # endregion

            # region TRADE MANAGEMENT
            # If there is a trade in progress.
            else :
                # Ordinary check to avoid errors.
                if len(trade_type) == 0 : continue
                
                # Here the max_income variable is updated.
                if df.high[i] > max_income and i >= entry_candle :
                    max_income = df.high[i]

                # Here the min_income variable is updated.
                if df.low[i] < min_income and i >= entry_candle :
                    min_income = df.low[i]

                # If the current close is above the last close:
                if df.close[i] > df.close[i - 1] and i >= entry_candle :

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
                        exit_price.append(round(df.close[i], 2))

                        close_tomorrow.append(True)
                    else :
                        outcome = ((df.open[i + 1] * (1 - (Commission_Perc / 100))) - entry_price[-1]) * shares_to_trade

                        y_index.append(df.index[i + 1])
                        exit_price.append(round(df.open[i + 1], 2))

                        close_tomorrow.append(False)

                    # Saving all missing trade characteristics.
                    y_raw.append(exit_price[-1] - entry_price[-1])
                    y_perc.append(round(y_raw[-1] / entry_price[-1] * 100, 2))
                    y2_raw.append(max_income - entry_price[-1])
                    y3_raw.append(min_income - entry_price[-1])
                    y.append(outcome)
                    y2.append(y2_raw[-1] * shares_to_trade)
                    y3.append(y3_raw[-1] * shares_to_trade)

            # If a trade is in progress in the last candle:
            if i == len(df) - 1 and on_trade :

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

        trades[f"iSMA{SMA1_Period}"] = np.array(iSMA1_ot)
        trades[f"iSMA{SMA2_Period}"] = np.array(iSMA2_ot)
        trades[f"iMSD{MSD_Period}"] = np.array(iMSD_ot)
        trades[f"iATR{ATR_Period}"] = np.array(iATR_ot)
        trades['volatility'] = np.array(volatity_ot)

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
trades_global = trades_global.sort_values(by=['entry_date'], ignore_index=True)
trades_global.to_csv('Model/Files/SP_trades_raw.csv', sep=';')

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
            Filtered_Trades = Filtered_Trades.sort_values(by=['volatility'], ignore_index=True)
            Portfolio_Trades = Portfolio_Trades.append(Filtered_Trades[: Slots - Counter], ignore_index=True)
        else :
            Portfolio_Trades = Portfolio_Trades.append(trades_global[trades_global['entry_date'] == Number_of_trades.index.values[i]], ignore_index=True)
    Acum_Opened = sum(1 for x in Portfolio_Trades['entry_date'] if x == Number_of_trades.index.values[i]) + Acum_Opened

# endregion

trades_global = Portfolio_Trades

print('Portfolio done!')

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
return_table.to_csv('Model/Files/Return Table.csv', sep=';')

print('Return Table done!')

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

py.offline.plot(fig, filename = "Model/Files/Analysis_chart.html")

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
stats.to_csv('Model/Files/SP_stats.csv', sep=';')
trades_global.to_csv('Model/Files/SP_trades.csv', sep=';')

un = pd.DataFrame()
un['ticks'] = np.array(unavailable_tickers)
un.to_csv('Model/Files/unavailable_tickers.csv', sep=';')