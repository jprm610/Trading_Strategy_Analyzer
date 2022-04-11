# region LIBRARIES

# Pandas and numpy are the base libraries in order to manipulate
# all data for analysis and strategies development.
import pandas as pd
import numpy as np
import math

# Thanks to Yahoo Finance library we can get historical data
# for all tickers without any cost.
import yfinance as yf

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

import win10toast

import warnings
warnings.filterwarnings('ignore')

# endregion

# region PARAMETERS

Use_Pre_Charged_Data = True
# Indicators
SMA1_Period = 100
SMA2_Period = 5
MSD_Period = 100
ATR_Period = 10
Tradepoint_Factor = 0.5

SPY_SMA_Period = 200

Consecutive_Lower_Lows = 2

# Overall backtesting parameters
Start_Date = '2021-01-01'
Risk_Unit = 100
Perc_In_Risk = 2.3
Trade_Slots = 10
Commission_Perc = 0.1
Account_Size = 10000
Filter_Mode = 'volatility'

# endregion

# First the Start_Date parameter is formatted and standarized.
Start_Date = pd.to_datetime(Start_Date)

def main() :

    # Here we create a df in which all trades are going to be saved.
    trades_global = pd.DataFrame()
    unavailable_tickers = []

    print(f"The current working directory is {os.getcwd()}")

    SPY_global, iSPY_SMA_global = SPY_df(SPY_SMA_Period)

    tickers_directory, cleaned_tickers = Survivorship()
    
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
        # then when the quiantity of elements is divided by 2, 
        # it gives us the number of periods of that ticker.
        its = int(len(tickers_directory[ticker]) / 2)
        for a in range(its) :

            returned_tuple = Get_Data_A(tickers_directory, cleaned_tickers, ticker, a, iSPY_SMA_global, SPY_global, unavailable_tickers, max_period_indicator, Start_Date, Use_Pre_Charged_Data)
            if not isinstance(returned_tuple, tuple) :
                continue
            
            df, SPY, iSPY_SMA, unavailable_tickers = returned_tuple

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
            trades['stock'] = np.array(stock)
            trades['shares_to_trade'] = np.array(shares_to_trade_list)
            trades['entry_price'] = np.array(entry_price)
            trades['volatility'] = np.array(volatity_ot)
            trades['is_signal'] = np.array(is_signal)
            trades['close_tomorrow'] = np.array(close_tomorrow)
            trades['trade_type']  = np.array(trade_type)
            trades['exit_price'] = np.array(exit_price)
            trades['y']  = np.array(y)
            trades['y_raw'] = np.array(y_raw)
            trades['y%'] = np.array(y_perc)
            trades[f"iSMA{SMA1_Period}"] = np.array(iSMA1_ot)
            trades[f"iSMA{SMA2_Period}"] = np.array(iSMA2_ot)
            trades[f"iMSD{MSD_Period}"] = np.array(iMSD_ot)
            trades[f"iATR{ATR_Period}"] = np.array(iATR_ot)
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
        os.mkdir('Files')
        print('Created!')
    except :
        print('Created!')

    # Here the trades_global df is edited 
    # in order to set the entry_date characteristic as the index.
    trades_global.drop_duplicates(keep='first', inplace=True)
    trades_global = trades_global.sort_values(by=['entry_date'], ignore_index=True)
    trades_global.to_csv(f'Files/SP_trades_raw{str(datetime.date.today())}.csv')

    unavailable_tickers_df = pd.DataFrame()
    unavailable_tickers_df['ticks'] = np.array(unavailable_tickers)
    unavailable_tickers_df.to_csv('Files/SP_unavailable_tickers.csv')

    notification = win10toast.ToastNotifier()
    notification.show_toast('Alert!', 'Backtesting finished.')

def SPY_df(SPY_SMA_Period) :

    # Here the SPY data is downloaded from Yahoo Finance
    print('SPY_Download')
    SPY_global = yf.download('SPY')

    # Rename df columns for cleaning porpuses 
    # and applying recommended.
    SPY_global.columns = ['open', 'high', 'low', 'close', 'adj close', 'volume']
    SPY_global.index.names = ['date']

    # Drop duplicates leaving only the first value,
    # this due to the resting entry_dates in which the market is not moving.
    SPY_global = SPY_global.drop_duplicates(keep=False)

    # region Date_Range

    # In this region, the SPY df is cut in order to get only the necessary
    # data for the backtesting.

    SPY_global.reset_index(inplace=True)

    if SPY_global.empty :
        # Raise an error, 
        # save that ticker in unavailable tickers list 
        # and skip this ticker calculation.
        print(f"ERROR: Not available data for SPY in YF.")
        return -1
    # endregion
    
    SPY_global.set_index(SPY_global['date'], inplace=True)
    del SPY_global['date']

    # Here the SPY SMA is calculated for the Regime filter process.
    SPY_SMA = TA.SMA(SPY_global, SPY_SMA_Period)

    # The SPY SMA is then saved into a dataframe with it's date, 
    # this in order to "cut" the SMA data needed for a ticker in the backtesting process.
    iSPY_SMA_global = pd.DataFrame({'date' : SPY_global.index, 'SMA' : SPY_SMA})
    iSPY_SMA_global.set_index(iSPY_SMA_global['date'], inplace=True)

    return SPY_global, iSPY_SMA_global

def Survivorship() :
    
    print('Survivorship Bias Process!')

    # region SP500_historical
    # In this region, the SP500 historical constitutents file is cleaned, 
    # this in order to avoid the survivorship bias problem.

    # First the Start_Date parameter is formatted and standarized.
    start_date = pd.to_datetime(Start_Date)

    # Here the SP500 historical constitutents file is read, 
    # then the dates are standarized to avoid dates missinterpretation.
    SP500_historical = pd.read_csv("SP500_historical.csv")
    SP500_historical['DATE'] = pd.to_datetime(SP500_historical['DATE'], format='%m/%d/%Y')
    SP500_historical = SP500_historical[-2:]

    # region FIRST CLEANING
    # A first cleaning process is done, creating a new df with every list of tickers and its changes.

    # The format is:
    #   start_date; end_date; symbols

    # Every row means that that list didn't change through that period.
    tickers_df1 = pd.DataFrame(columns=['start_date', 'end_date', 'symbols'])
    for i in range(len(SP500_historical)) :

        # For every row, a tickers directory is crated, in order to save start_date, 
        # end date and the list of symbols for that time period.
        tickers = {}

        # The start_date is defined simply as the row date.
        tickers['start_date'] = SP500_historical['DATE'].values[i]

        # The end_date is defined here.
        # If the last row is being evaluated,
        # then the end_date is going to be today date.
        if i == len(SP500_historical) - 1 :
            tickers['end_date'] = datetime.datetime.today().strftime('%Y-%m-%d')
        # If not, is defined as the next row date.
        else :
            tickers['end_date'] = SP500_historical['DATE'].values[i]

        # Then all symbols that are presented in the current row as separated columns, 
        # are saved into a simplified list.
        constitutents = SP500_historical['CONSTITUTENTS'].values[i]
        constitutents = constitutents.replace('[',"")
        constitutents = constitutents.replace(']',"")
        constitutents = constitutents.replace("'","")
        constitutents = constitutents.split(', ')
        constitutents = [i.replace('.','-') for i in constitutents]
        tickers['symbols'] = constitutents

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
    cleaned_tickers = tickers_df2.loc[tickers_df2['start_date'] >= start_date]

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

    print('------------------------------------------------------------')
    print("Survivorship Bias done!")

    return tickers_directory, cleaned_tickers

def Get_Data_A(tickers_directory, cleaned_tickers, ticker, a, iSPY_SMA_global, SPY_global, unavailable_tickers, max_period_indicator, Start_Date, Use_Pre_Charged_Data) :

    ticker = ticker.replace('.','-')
    
    # Determine the current start date, 
    # reversing the previous operation.
    current_start_date = a * 2

    if (tickers_directory[ticker][current_start_date + 1] != cleaned_tickers['end_date'].values[-1] or
        Use_Pre_Charged_Data) :
        
        if f"{ticker}.csv" not in os.listdir("SP_data") :
            print(f"ERROR: Not available data for {ticker}.")
            unavailable_tickers.append(f"{ticker}_({str(tickers_directory[ticker][current_start_date])[:10]}) ({str(tickers_directory[ticker][current_start_date + 1])[:10]})")
            return -1
        
        # Then try to get the .csv file of the ticker.
        try :
            df = pd.read_csv(f"SP_data/{ticker}.csv", sep=';')
            is_downloaded = False
        # If that's not possible, raise an error, 
        # save that ticker in unavailable tickers list 
        # and skip this ticker calculation.
        except :
            print(f"ERROR: Not available data for {ticker}.")
            unavailable_tickers.append(f"{ticker}_({str(tickers_directory[ticker][current_start_date])[:10]}) ({str(tickers_directory[ticker][current_start_date + 1])[:10]})")
            return -1
        
        if df.empty :
            print(f"ERROR: Not available data for {ticker}.")
            unavailable_tickers.append(f"{ticker}_({str(tickers_directory[ticker][current_start_date])[:10]}) ({str(tickers_directory[ticker][current_start_date + 1])[:10]})")
            return -1

        # Reformat the df, standarizing dates and index.
        df['date'] = pd.to_datetime(df['date'], format='%Y-%m-%d')
    else :
        # If we want to downloaded new data :
        try :
            # Then download the information from Yahoo Finance 
            # and rename the columns for standarizing data.
            df = yf.download(ticker)
            df.columns = ['open', 'high', 'low', 'close', 'adj close', 'volume']
            df.index.names = ['date']
            is_downloaded = True
        # If that's not possible :
        except :
            # If that's not possible, raise an error, 
            # save that ticker in unavailable tickers list 
            # and skip this ticker calculation.
            print(f"ERROR: Not available data for {ticker} in YF.")
            unavailable_tickers.append(f"{ticker}_({str(tickers_directory[ticker][current_start_date])[:10]}) ({str(tickers_directory[ticker][current_start_date + 1])[:10]})")
            return -1
        
        # If the ticker df doesn't have any information skip it.
        if df.empty : 
            print(f"ERROR: Not available data for {ticker}.")
            unavailable_tickers.append(f"{ticker}_({str(tickers_directory[ticker][current_start_date])[:10]}) ({str(tickers_directory[ticker][current_start_date + 1])[:10]})")
            return -1

        df.reset_index(inplace=True)

    start_date = max(df.date[0], Start_Date)

    while True :
        if start_date in df['date'].tolist() or start_date > pd.to_datetime('today').normalize() : break
        start_date = start_date + datetime.timedelta(days=1)

    if start_date > pd.to_datetime('today').normalize() :
        print(f"ERROR: Not available data for {ticker} in YF.")
        unavailable_tickers.append(f"{ticker}_({str(tickers_directory[ticker][current_start_date])[:10]}) ({str(tickers_directory[ticker][current_start_date + 1])[:10]})")
        return -1

    start_index = df.index[df['date'] == start_date].to_list()[0]
    if start_index - max_period_indicator < 0 :
        start_index = 0
    else :
        start_index -= max_period_indicator

    start_date = df['date'].values[start_index]

    df.set_index(df['date'], inplace=True)
    del df['date']
    
    df = df.loc[df.index >= start_date]

    if df.empty :
        # Raise an error, 
        # save that ticker in unavailable tickers list 
        # and skip this ticker calculation.
        print(f"ERROR: Not available data for {ticker} in YF.")
        unavailable_tickers.append(f"{ticker}_({str(tickers_directory[ticker][current_start_date])[:10]}) ({str(tickers_directory[ticker][current_start_date + 1])[:10]})")
        return -1

    if Use_Pre_Charged_Data :
        print('Charged!')
    else :
        if is_downloaded :
            # Try to create a folder to save all the data, 
            # if there isn't one available yet.
            try :
                # Create dir.
                os.mkdir('SP_data')
                df.to_csv(f"SP_data/{ticker}.csv", sep=';')
            except :
                # Save the data.
                df.to_csv(f"SP_data/{ticker}.csv", sep=';')
        print('Downloaded!')

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

    return df, SPY, iSPY_SMA, unavailable_tickers

main()