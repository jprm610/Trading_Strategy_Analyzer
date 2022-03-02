# region LIBRARIES

from My_Library import *

# Pandas and numpy are the base libraries in order to manipulate
# all data for analysis and strategies development.
import pandas as pd
import numpy as np

# Thanks to Yahoo Finance library we can get historical data
# for all tickers without any cost.
import yfinance as yf

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

import warnings
warnings.filterwarnings('ignore')

# endregion

# Here we create a list in which are going to be saved 
# those tickers from which we couldn't get historical data.
unavailable_tickers = []

Start_Date = '1950-01-01'

# endregion

print(f"The current working directory is {os.getcwd()}")

tickers_directory, cleaned_tickers = Survivorship_Bias(Start_Date)

tickers_dict = pd.DataFrame()
tickers_dict['tickers'] = list(tickers_directory.keys())
tickers_dict['periods'] = list(tickers_directory.values())
tickers_dict.to_csv("Model/tickers_periods.csv")

asset_count = 1
# For every ticker in the tickers_directory :
for ticker in tickers_directory.keys() :

    # This section is only for front-end purposes,
    # in order to show the current progress of the program when is running.
    print('------------------------------------------------------------')
    print(f"{asset_count}/{len(tickers_directory.keys())}")
    print(ticker)
    asset_count += 1

    # We create a loop in which all 3 data providers are going to be evaluated 
    # in order to get the data, strating with YF, then SHARADAR and finally WIKI.
    
    if tickers_directory[ticker][-1] == cleaned_tickers['end_date'].values[-1] :
        print('Listed!')
        continue

    count = 0
    is_downloaded = False
    while count < 2 :
        # region YF
        if count == 0 :
            try :
                print('YF')
                # Then download the information from Yahoo Finance 
                # and rename the columns for standarizing data.
                yf_df = yf.download(str(ticker))
                yf_df.columns = ['open', 'high', 'low', 'close', 'adj close', 'volume']
                yf_df.index.names = ['date']
            # If that's not possible :
            except :
                yf_df = pd.DataFrame()
                print(f"ERROR: Not available data for {ticker} in YF.")
        # endregion
        
        # region WIKI
        elif count == 1 :
            # Try to get the data from WIKI data provider.
            try :
                print('WIKI')
                # Get the df and standarize the data.
                wiki_df = quandl.get(f'WIKI/{ticker}')
                wiki_df.drop(['Ex-Dividend', 'Split Ratio', 'Adj. Open', 'Adj. High', 'Adj. Low', 'Adj. Close', 'Adj. Volume'], axis=1, inplace=True)
                wiki_df.columns = ['open', 'high', 'low', 'close', 'volume']
                wiki_df.index.names = ['date']
            # If none of the above were possible :
            except :
                wiki_df = pd.DataFrame()
                print(f"ERROR: Not available data for {ticker} in WIKI.")

        count += 1
        # endregion

    if yf_df.empty and wiki_df.empty : 
        unavailable_tickers.append(f"{ticker}")
        print('No data!')
        continue

    if not yf_df.empty and yf_df.index[0] > tickers_directory[ticker][-1] :
        yf_df = pd.DataFrame()
    
    if not yf_df.empty :
        data_from = 'YF'
        df = yf_df
    else :
        data_from = 'WIKI'
        df = wiki_df

    if df.empty :
        unavailable_tickers.append(f"{ticker}")
        print('No data!')
        continue

    print(f'Downloaded from {data_from}!')

    # Try to create a folder to save all the data, 
    # if there isn't one available yet.
    try :
        # Create dir.
        os.mkdir('Model/SP_data')
        # Save the data.
        df.to_csv(f"Model/SP_data/{ticker}.csv", sep=';')
    except :
        # Save the data.
        df.to_csv(f"Model/SP_data/{ticker}.csv", sep=';')

unavailable_df = pd.DataFrame()
unavailable_df['ticker'] = np.array(unavailable_tickers)
unavailable_df.to_csv("Model/unavailable_tickers.csv")
