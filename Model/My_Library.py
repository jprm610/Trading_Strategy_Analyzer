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

def Survivorship_Bias(Start_Date) :

    print('Survivorship Bias Process!')

    # region SP500_historical
    # In this region, the SP500 historical constitutents file is cleaned, 
    # this in order to avoid the survivorship bias problem.

    # First the Start_Date parameter is formatted and standarized.
    start_date = pd.to_datetime(Start_Date)

    # Here the SP500 historical constitutents file is read, 
    # then the dates are standarized to avoid dates missinterpretation.
    SP500_historical = pd.read_csv("Model/SP500_historical.csv")
    SP500_historical['DATE'] = pd.to_datetime(SP500_historical['DATE'], format='%m/%d/%Y')

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

def Portfolio(trades_global, Trade_Slots, filter_mode, is_asc=False) :

    print('Defining Portfolio!')
     
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
                
                if filter_mode == 'random' :
                    Filtered_Trades = Filtered_Trades.sample(frac=1)
                else :
                    Filtered_Trades = Filtered_Trades.sort_values(by=[f'{filter_mode}'], ascending=is_asc)

                Portfolio_Trades = Portfolio_Trades.append(Filtered_Trades[: Slots - Counter], ignore_index=True)
            else :
                Portfolio_Trades = Portfolio_Trades.append(trades_global[trades_global['entry_date'] == Number_of_trades.index.values[i]], ignore_index=True)
        Acum_Opened = sum(1 for x in Portfolio_Trades['entry_date'] if x == Number_of_trades.index.values[i]) + Acum_Opened

    # endregion

    return Portfolio_Trades

def Return_Table(trades_global) :

    print('Constructing Return Table!')

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

def Stats(trades_global, Account_Size) :
    
    print('Calculating Stats!')

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

    py.offline.plot(fig, filename = "Model/Files/SP_Analysis_chart.html")

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

def Files_Return(trades_global, return_table, stats, unavailable_tickers) :

    print('Returning Files!')

    trades_global.to_csv('Model/Files/SP_trades.csv')
    return_table.to_csv('Model/Files/SP_Return_Table.csv')
    stats.to_csv('Model/Files/SP_stats.csv')

    unavailable_tickers_df = pd.DataFrame()
    unavailable_tickers_df['ticks'] = np.array(unavailable_tickers)
    unavailable_tickers_df.to_csv('Model/Files/SP_unavailable_tickers.csv')
