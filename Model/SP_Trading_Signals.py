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
Start_Date = pd.to_datetime('2020-01-01')
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

    tickers = pd.read_csv('Model/SP500_historical.csv')
    tickers = tickers['CONSTITUENTS'].values[-1]
    tickers = tickers.replace('[', '')
    tickers = tickers.replace(']', '')
    tickers = tickers.replace("'", '')
    tickers = tickers.split(',')

    max_period_indicator = max(SMA1_Period, SMA2_Period, MSD_Period, ATR_Period)

    print('------------------------------------------------------------')
    print("Trade Calculation!")
    asset_count = 1

    # For every ticker in the tickers_directory :
    for ticker in tickers :

        # This section is only for front-end purposes,
        # in order to show the current progress of the program when is running.
        print('------------------------------------------------------------')
        print(f"{asset_count}/{len(tickers)}")
        print(ticker)
        asset_count += 1

        # region GET_DATA

        returned = Get_Data(ticker, Start_Date, End_Date, Use_Pre_Charged_Data, max_period_indicator)
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
    trades_global.to_csv('Model/SP_trades_raw.csv')

    notification = win10toast.ToastNotifier()
    notification.show_toast('Alerta!', 'Finalizado.')

class Trade :
    def __init__(self, Entry_Price, Entry_Date, Shares_To_Trade, Trade_Type, Stock, Max_Price, Min_Price, Commission_Perc) :
        """
        Enters a trade saving all entry information.

        Parameters: \\
            1) Entry_Price (float): Price in which the trade is positioned. \\
            2) Entry_Date (float): Date when the trade is positioned. \\
            3) Shares_To_Trade (float): The amount of shares to be positioned. \\
            4) Trade_Type (string): Is it "Long" or "Short"? \\
            5) Stock (string): Stock in which the trade is going to be executed. \\
            6) Max_Price (float): Price to start the Max_Price variable. \\
            7) Min_Price (float): Price to start the Min_Price variable. \\
            8) Commission_Perc (float): Commission charged for the trade. \\
        """
        
        # Save all parameters into the object.
        self.entry_price = round(Entry_Price, 2)
        self.entry_date = Entry_Date
        self.shares_to_trade = round(Shares_To_Trade, 2)
        self.trade_type = Trade_Type
        self.stock = Stock
        self.max_price = round(Max_Price, 2)
        self.min_price = round(Min_Price, 2)
        self.commission_percentage = Commission_Perc

    def Close(self, Trades: pd.DataFrame, Exit_Price, Exit_Date, Is_Signal=False, Close_Tomorrow=False) :
        """ 
        Closes the trade filling all missing trade characteristics.

        Parameters: \\
            1) Trades (DataFrame): Dataframe populated with all trades done in the current ticker. \\
            2) Exit_Price (float): Price in which the trade exited. \\
            3) Exit_Date (Timestamp): Date in which the trade exited. \\
            4) Is_Signal (False) (bool): Is the trade a signal? \\
            5) Close_Tomorrow (False) (bool): Is this trade closing tomorrow? \\

        Returns: \\
            1) Trades (DataFrame): Updated Trades dataframe with the new trade added at the end with all characteristics.
        """

        # Round all inputs to 2 decimal points.
        Exit_Price = round(Exit_Price, 2)

        if Is_Signal :
            # Fill the trade information with the default characteristics.
            self.is_signal = Is_Signal
            self.close_tomorrow = Close_Tomorrow
            self.exit_price = self.entry_price
            self.exit_date = self.entry_date
            self.y_raw = 0
            self.y_perc = 0
            self.y2_raw = 0
            self.y3_raw = 0
            self.y = 0
            self.y2 = 0
            self.y3 = 0
        else :
            # Calculate the outcome taking into consideration the Commission Percentage charged.
            outcome = ((Exit_Price * (1 - (self.commission_percentage / 100))) - self.entry_price) * self.shares_to_trade

            # Update the max and min price variables.
            if Exit_Price > self.max_price : self.max_price = Exit_Price
            if Exit_Price < self.min_price : self.min_price = Exit_Price

            self.is_signal = Is_Signal
            self.close_tomorrow = Close_Tomorrow
            self.exit_price = Exit_Price
            self.exit_date = Exit_Date
            self.y_raw = self.exit_price - self.entry_price
            self.y2_raw = self.max_price - self.entry_price
            self.y3_raw = self.min_price - self.entry_price
            self.y = round(outcome, 2)
            self.y_perc = round((self.y_raw/self.entry_price) * 100, 2)
            self.y2 = round(self.y2_raw * self.shares_to_trade, 2)
            self.y3 = round(self.y3_raw * self.shares_to_trade, 2)

        # Add the new trade at the end of the trades dataframe with all its characteristics.
        Trades.loc[len(Trades)] = [self.entry_date, self.exit_date, self.trade_type, self.stock, 
                                    self.is_signal, self.entry_price, self.exit_price, self.y, 
                                    self.y_raw, self.y_perc, self.shares_to_trade, self.close_tomorrow, 
                                    self.max_price, self.min_price, self.y2, self.y3, self.y2_raw, self.y3_raw]

        return Trades

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

    print('------------------------------------------------------------')

    return SPY_global, iSPY_SMA_global

def Get_Data(Ticker, Start, End, Load_Data, Pre_Start_Period=0, Directory_Path="Model/SP_data") :
    """
    Retrieves ticker data from Yahoo Finance in dataframe format.
    If the information was downloaded (new information), saves it into
    the selected directory so the data repository is updated.

    Parameters:
    1) Ticker (str): Ticker for which the data is going to be extracted.
    2) Start (Timestamp): Start date for data extraction.
    3) End (Timestamp): End date for data extraction.
    4) Load_Data (bool): False => Download data from Yahoo Finace. True => Charge data from local directory.
    5) Pre_Start_Period (0) (int): The additional days needed previous the Start, so the indicators can be well calculated.
    6) Directory_Path ("Model/SP_data") (str): Directory to download and charge data.

    Returns:
    -1 (int): If the data couldn't be retrieved or there wasn't available information.
    df (dataframe): pandas dataframe with the ticker data. (Columns: date (as index), open, high, low, close, adj close, volume))
    """

    # For front-end purposes.
    print(f"({str(Start)[:10]}) ({str(End)[:10]})")
    error_string = f"ERROR: Not available data for {Ticker}."

    # DATA EXTRACTION
    # Retrieve data from local directory:
    if Load_Data :
        # Check if the ticker data exists in the directory.
        if f"{Ticker}.csv" not in os.listdir(Directory_Path) :
            print(error_string)
            return -1
        
        # Then try to get the .csv file of the Ticker.
        try :
            df = pd.read_csv(f"{Directory_Path}/{Ticker}.csv", sep=';')
        except :
            print(error_string)
            return -1
        
        # If empty, skip the ticker.
        if df.empty :
            print(error_string)
            return -1

        # Reformat the df, standarizing dates and index.
        df['date'] = pd.to_datetime(df['date'], format='%Y-%m-%d')
    # Retrieve data from Yahoo Finance:
    else :
        try :
            # Download the information from Yahoo Finance 
            # and rename the columns for standarizing data, setting the dates as index
            df = yf.download(Ticker)
            df.columns = ['open', 'high', 'low', 'close', 'adj close', 'volume']
            df.index.names = ['date']
        # If that's not possible :
        except :
            print(error_string)
            return -1
        
        # If empty, skip the ticker.
        if df.empty : 
            print(error_string)
            return -1

        # Reset index for next process.
        df.reset_index(inplace=True)

    # ADDITIONAL TIME RANGE
    # Define the start date for the current ticker.
    start_date = max(df.date[0], Start)
    
    # Find the real start date in the df, this by adding one day at a time,
    # until it is found or it's 'tomorrow' date.
    while True :
        if start_date in df['date'].tolist() or start_date > pd.to_datetime('today').normalize() : break
        start_date = start_date + pd.Timedelta(1, unit='D')
    # If it's tomorrow date, means the start date didn't exist in the df,
    # so the ticker is skiped.
    if start_date > pd.to_datetime('today').normalize() :
        print(error_string)
        return -1

    # Find the start_date index,
    start_index = df.index[df['date'] == start_date].to_list()[0]
    # and redefine the start index according to the Pre_Start_Period parameter.
    if start_index - Pre_Start_Period < 0 :
        start_index = 0
    else :
        start_index -= Pre_Start_Period

    # Get the real start date
    start_date = df['date'].values[start_index]

    # Set date column as index.
    df.set_index(df['date'], inplace=True)
    del df['date']

    # The df is going to be cutted for the backtesting,
    # so a copy of the full df is saved.
    download_df = df.copy()

    # Cut the df.
    df = df.loc[df.index >= start_date]
    df = df.loc[df.index <= End]

    # If after the cut there's no data, skip it,
    # meaning that there wasn't data for the time range requested.
    if df.empty :
        print(error_string)
        return -1

    if Load_Data :
        print('Charged!')
    # If the data was downloaded from Yahoo Finance:
    else :
        # Try to create a folder to save all the data, 
        # if there isn't one available yet.
        try :
            # Create dir.
            os.mkdir(f"{Directory_Path}")
            download_df.to_csv(f"{Directory_Path}/{Ticker}.csv", sep=';')
        except :
            # Save the data.
            download_df.to_csv(f"{Directory_Path}/{Ticker}.csv", sep=';')
        print('Downloaded!')

    return df

main()
