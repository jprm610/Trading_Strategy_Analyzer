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
from indicators import *
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
use_last_candle_weakness = True

risk_unit = 100
perc_in_risk = 4
last_candle_weakness = 25
trades_at_the_same_time = 10
# endregion

#tickers = tickers[0:10].copy()

# In this loop the evaluation of all stocks is done.
asset_count = 1
for asset in tickers :
    # This section is only for front-end purposes,
    # in order to show the current progress of the program when is running.
    print(str(asset_count) + '/' + str(len(tickers)))
    print(asset)
    asset_count += 1

    # region DATA CLEANING

    # Here is read the historical data provided by Yahoo Finance,
    # this also has an error handling in case the ticker historical info
    # couldn't be downloaded, in which is skipped.
    try :
        df = yf.download(asset,'2011-01-01')
    except ValueError :
        continue
    
    # If the ticker df doesn't have any information skip it.
    if len(df) == 0 : continue

    # Rename df columns for cleaning porpuses 
    # and applying recommended "adjusted close" info.
    df.columns = ['open', 'high', 'low', 'close', 'adj close', 'volume']

    # Drop duplicates leaving only the first value,
    # this due to the resting dates in which the market is not moving.
    df = df.drop_duplicates(keep=False)

    # endregion

    # region INDICATOR CALCULATIONS

    # Get all indicator lists,
    # for this strategy we only need SMA 200 and RSI 10.
    iSMA1 = TA.SMA(df, 200)
    iRSI = TA.RSI(df, 3)
    iMSD = TA.MSD(df, 100)
    iTR = TA.TR(df)

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
    volatity_ot = []
    candle_weakness_ot = []

    close_ot = []
    low_ot = []

    y_raw = []
    y2_raw = []
    y = []
    y2 = []
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
            # Check if the current price is above the SMA1,
            # this in purpose of determining whether the stock is in an up trend
            # and if the RSI is below 10, enter the trade in the next open.
            if df.close[i] > iSMA1[i] and iRSI[i] < 10 :
                # Review wheter there are 3 consecutive lower lows.
                if df.low[i] < df.low[i - 1] and df.low[i - 1] < df.low[i - 2] :
                    
                    if use_last_candle_weakness :
                        lower_tail = df.close[i] - df.low[i]
                        close_proportion = (lower_tail / iTR[i]) * 100
                    else :
                        close_proportion = last_candle_weakness

                    if close_proportion <= last_candle_weakness :

                        # Before entering the trade,
                        # calculate the shares_to_trade,
                        # in order to risk the perc_in_risk of the stock,
                        # finally make sure that we can afford those shares to trade.
                        if i == len(df) - 1 : current_avg_lose = df.close[i] * (perc_in_risk / 100)
                        else : current_avg_lose = df.open[i + 1] * (perc_in_risk / 100)

                        shares_to_trade = round(abs(risk_unit / current_avg_lose))
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
                            entry_price.append(df.close[i])
                        else :
                            dates.append(df.index[i + 1])
                            max_income = df.high[i + 1]
                            entry_price.append(df.open[i + 1])
        # endregion

        # region TRADE MANAGEMENT
        # If there is a trade in progress.
        else :
            # Ordinary check to avoid errors.
            if len(trade_type) == 0 : continue
            
            # Here the max_income variable is updated.
            if df.high[i] > max_income :
                    max_income = df.high[i]

            # If the RSI is over 50 or 10 days have passed:
            if iRSI[i] > 50 or i == entry_candle + 11 :

                # The trade is exited.
                # First updating the on_trade flag.
                on_trade = False

                # To simulate that the order is executed in the next day, 
                # the entry price is taken in the next candle open. 
                # Nevertheless, when we are in the last candle that can't be done, 
                # that's why the current close is saved in that case.
                if i == len(df) - 1 :
                    outcome = (df.close[i] - entry_price[-1]) * shares_to_trade

                    y_index.append(df.index[i])
                    exit_price.append(df.close[i])
                else :
                    outcome = (df.open[i + 1] - entry_price[-1]) * shares_to_trade

                    y_index.append(df.index[i + 1])
                    exit_price.append(df.open[i + 1])

                # Saving all missing trade characteristics.
                y.append(outcome)
                y2.append((max_income - entry_price[-1]) * shares_to_trade)
                y_raw.append(outcome / shares_to_trade)
                y2_raw.append(y2[-1] / shares_to_trade)

        # If a trade is in progress in the last candle:
        if i == len(df) - 1 and on_trade :

            # All characteristics are saved 
            # as if the trade was exited in this moment.
            outcome = (df.close[i] - entry_price[-1]) * shares_to_trade

            y2.append((max_income - entry_price[-1]) * shares_to_trade)
            y.append(outcome)
            y_raw.append(outcome / shares_to_trade)
            y2_raw.append(y2[-1] / shares_to_trade)
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
   
    trades['volatility'] = np.array(volatity_ot)

    if use_last_candle_weakness : trades['candle_weakness'] = np.array(candle_weakness_ot)

    trades['y']  = np.array(y)
    trades['y2'] = np.array(y2)
    trades['y_raw'] = np.array(y_raw)
    trades['y2_raw'] = np.array(y2_raw)
    trades['shares_to_trade'] = np.array(shares_to_trade_list)

    # endregion

    # Here the current trades df is added to 
    # the end of the global_trades df.
    trades_global = trades_global.append(trades)
    
# Here the trades_global df is edited 
# in order to set the entry_date characteristic as the index.
trades_global = trades_global.sort_values(by=['entry_date'])

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
Newest_closes = [0]
Closed_Trades_per_Date = 0
Acum_Opened = Number_of_trades['# of trades'].values[0]
Acum_Total = 0
Acum_Closed = 0
Counter = 0
Slots = trades_at_the_same_time
for i in range(len(Number_of_trades)) :
    if i != 0 :
        Acum_Closed = sum(1 for x in Portfolio_Trades['exit_date'] if x <= Number_of_trades.index.values[i])
        Closed_Trades_per_Date = Acum_Closed - sum(Newest_closes)
        Newest_closes.append(Closed_Trades_per_Date)
        Acum_Opened = sum(1 for x in Portfolio_Trades['entry_date'] if x == Number_of_trades.index.values[i-1]) + Acum_Opened

    Counter = Acum_Total - Closed_Trades_per_Date
    Acum_Total = Acum_Opened - Acum_Closed
    if Counter <= Slots :
        if Number_of_trades['# of trades'].values[i] > Slots - Counter :
            Filtered_Trades = pd.DataFrame()
            Filtered_Trades = Filtered_Trades.append(trades_global[trades_global['entry_date'] == Number_of_trades.index.values[i]], ignore_index=True)
            Filtered_Trades = Filtered_Trades.sort_values(by=['volatility'], ascending=False)
            Portfolio_Trades = Portfolio_Trades.append(Filtered_Trades[0:Slots - Counter], ignore_index=True)
        else :
            Portfolio_Trades = Portfolio_Trades.append(trades_global[trades_global['entry_date'] == Number_of_trades.index.values[i]], ignore_index=True)

# endregion

trades_global = Portfolio_Trades

# endregion

trades_global.set_index(trades_global['entry_date'], drop=True, inplace=True)
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