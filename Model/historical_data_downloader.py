# region LIBRARIES

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

# endregion

# Here we create a list in which are going to be saved 
# those tickers from which we couldn't get historical data.
unavailable_tickers = []

Start_Date = '1996-01-02'

# endregion

print(f"The current working directory is {os.getcwd()}")

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

print('------------------------------------------------------------')
print("Survivorship Bias done!")

# endregion

asset_count = 1
# For every ticker in the tickers_directory :
for ticker in tickers_directory.keys() :

    # This section is only for front-end purposes,
    # in order to show the current progress of the program when is running.
    print('------------------------------------------------------------')
    print(f"{asset_count}/{len(tickers_directory.keys())}")
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

        if tickers_directory[ticker][current_start_date + 1] == cleaned_tickers['end_date'].values[-1] :
            print(f"{ticker}{a} is currently listed!")
            continue

        # Yahoo Finance has improper data from TIE, 
        # that's why we are skiping this data provider.
        if ticker == 'TIE' : count = 1
        else : count = 0

        # We create a loop in which all 3 data providers are going to be evaluated 
        # in order to get the data, strating with YF, then SHARADAR and finally WIKI.
        is_downloaded = False
        while count < 3 :
            # region YF
            if count == 0 :
                try :
                    # start_date and end_date is a timestamp and causes problems 
                    # when an object of this type is given as an argument to the yf.download(), 
                    # that's why we have to cast that value as a string and 
                    # then get the first 10 characters that are the date itself.
                    start_a = str(tickers_directory[ticker][current_start_date])
                    start = start_a[:10]

                    end_a = tickers_directory[ticker][current_start_date + 1] + np.timedelta64(1,'D')
                    end_a = str(end_a)
                    end = end_a[:10]

                    # Then download the information from Yahoo Finance 
                    # and rename the columns for standarizing data.
                    df = yf.download(str(ticker), start=start, end=end)
                    df.columns = ['open', 'high', 'low', 'close', 'adj close', 'volume']
                    df.index.names = ['date']
                # If that's not possible :
                except :
                    # If that's not possible, raise an error, 
                    # save that ticker in unavailable tickers list 
                    # and skip this ticker calculation.
                    count += 1
                    print(f"ERROR: Not available data for {ticker} in YF.")
                    continue

                if df.empty :
                    # Raise an error, 
                    # save that ticker in unavailable tickers list 
                    # and skip this ticker calculation.
                    count += 1
                    print(f"ERROR: Not available data for {ticker} in YF.")
                    continue
                else : 
                    is_downloaded = True
                    break
            # endregion

            # region SHARADAR
            elif count == 1 :
                # Try to get the data from SHARADAR data provider.
                try :
                    # Get the df and standarize the data.
                    df = quandl.get_table('SHARADAR/SEP', ticker=str(ticker), date={'gte': tickers_directory[ticker][current_start_date], 'lte': tickers_directory[ticker][current_start_date + 1]})
                    df.drop(['closeunadj', 'lastupdated'], axis=1, inplace=True)
                    df.sort_values(by=['date'], ignore_index=True, inplace=True)
                    df.set_index(df['date'], inplace=True)
                # If that's not possible :
                except :
                    # If that's not possible, raise an error, 
                    # save that ticker in unavailable tickers list 
                    # and skip this ticker calculation.
                    count += 1
                    print(f"ERROR: Not available data for {ticker} in SHARADAR.")
                    continue

                if df.empty :
                    # Raise an error, 
                    # save that ticker in unavailable tickers list 
                    # and skip this ticker calculation.
                    count += 1
                    print(f"ERROR: Not available data for {ticker} in SHARADAR.")
                    continue
                else :
                    is_downloaded = True 
                    break
            # endregion
            
            # region WIKI
            elif count == 2 :
                # Try to get the data from WIKI data provider.
                try :
                    # Get the df and standarize the data.
                    df = quandl.get('WIKI/' + str(ticker), start_date=tickers_directory[ticker][current_start_date], end_date=tickers_directory[ticker][current_start_date + 1])
                    df.drop(['Ex-Dividend', 'Split Ratio', 'Adj. Open', 'Adj. High', 'Adj. Low', 'Adj. Close', 'Adj. Volume'], axis=1, inplace=True)
                    df.columns = ['open', 'high', 'low', 'close', 'volume']
                    df.index.names = ['date']
                # If none of the above were possible :
                except :
                    # If that's not possible, raise an error, 
                    # save that ticker in unavailable tickers list 
                    # and skip this ticker calculation.
                    count += 1
                    print(f"ERROR: Not available data for {ticker} in WIKI.")
                    continue

                if df.empty :
                    # Raise an error, 
                    # save that ticker in unavailable tickers list 
                    # and skip this ticker calculation.
                    count += 1
                    print(f"ERROR: Not available data for {ticker} in WIKI.")
                    continue
                else :
                    is_downloaded = True 
                    break
            # endregion
        
        if not is_downloaded : 
            unavailable_tickers.append(f"{ticker}{a}")
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

        # endregion

unavailable_df = pd.DataFrame()
unavailable_df['ticker'] = np.array(unavailable_tickers)
unavailable_df.to_csv("unavailable_tickers.csv", sep=';')