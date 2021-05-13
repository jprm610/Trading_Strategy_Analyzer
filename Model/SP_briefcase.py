import pandas as pd
import numpy as np
import yfinance as yf
import statistics
import plotly as py
from plotly.subplots import make_subplots
import plotly.graph_objects as go
from plotly.graph_objs import *
from finta import TA
from indicators import *
pd.options.mode.chained_assignment = None

sp500 = pd.read_html('https://en.wikipedia.org/wiki/List_of_S%26P_500_companies')
actives = sp500[0]
tickers = actives['Symbol'].to_list()
tickers = [i.replace('.','-') for i in tickers]

trades_global = pd.DataFrame()
risk_unit_dir = pd.read_csv('stock_means.csv', sep=';')

# region PARAMETERS
risk_unit = 100
perc_in_risk = 4
# endregion

ti = tickers[0:10].copy()

asset_count = 1
for asset in ti :
    print(str(asset_count) + '/' + str(len(tickers)))
    print(asset)
    asset_count += 1

    # region DATA CLEANING
    #Read historical exchange rate file (extracted from dukascopy.com)
    try :
        df = yf.download(asset,'2011-01-01')
    except ValueError :
        continue
    
    if len(df) == 0 : continue

    #Rename df columns for cleaning porpuses
    df.columns = ['open', 'high', 'low', 'adj close', 'close', 'volume']

    #Drop duplicates leaving only the first value
    df = df.drop_duplicates(keep=False)
    trades = pd.DataFrame()
    # endregion

    # region INDICATOR CALCULATIONS
    #Get all indicator lists
    iSMA1 = TA.SMA(df, 200)
    iRSI = TA.RSI(df, 10)
    # endregion

    # region TRADE_EMULATION
    dates = []
    trade_type = []
    stock = []

    entry_price = []
    exit_price = []
    shares_to_trade_list = []

    iSMA1_ot = []
    iRSI_ot = []
    #Dimensions
    """
    recoil_x = []
    init_x = []

    iTI_ot = []
    iVI_ot = []
    iRI_ot = []
    iOBV_ot = []
    """

    y = []
    y2 = []
    y_index = []

    on_trade = False
    for i in range(len(df)) :

        if i == 0 : continue

        if iSMA1[i] == -1 or iRSI[i] == -1 : continue
        
        # region TRADE CALCULATION biennnn, pense que iba a ser peor
        if not on_trade :
            if df.close[i] > iSMA1[i] :
                if iRSI[i] < 30 :
                    current_avg_lose = df.close[i] * (perc_in_risk / 100)
                    shares_to_trade = round(abs(risk_unit / current_avg_lose))

                    if shares_to_trade == 0 : continue

                    on_trade = True
                    dates.append(df.index[i])
                    trade_type.append("Long")
                    stock.append(asset)
                    entry_candle = i
                    shares_to_trade_list.append(shares_to_trade)
                    iSMA1_ot.append(iSMA1[i])
                    iRSI_ot.append(iRSI[i])

                    if i == len(df) - 1 :
                        max_income = df.high[i]
                        entry_price.append(df.close[i])
                    else :
                        max_income = df.high[i + 1]
                        entry_price.append(df.open[i + 1])
        # endregion

        # region TRADE MANAGEMENT
        else :
            if len(trade_type) == 0 : continue

            if df.high[i] > max_income :
                    max_income = df.high[i]

            if iRSI[i] > 40 or i == entry_candle + 10 :
                on_trade = False

                if i == len(df) - 1 :
                    outcome = (df.close[i] - entry_price[-1]) * shares_to_trade

                    y_index.append(df.index[i])
                    exit_price.append(df.close[i])
                else :
                    outcome = (df.open[i + 1] - entry_price[-1]) * shares_to_trade

                    y_index.append(df.index[i + 1])
                    exit_price.append(df.open[i + 1])

                y.append(outcome)
                y2.append((max_income - entry_price[-1]) * shares_to_trade)

        if i == len(df) - 1 and on_trade :
            outcome = (df.close[i] - entry_price[-1]) * shares_to_trade

            y2.append((max_income - entry_price[-1]) * shares_to_trade)
            y.append(outcome)
            y_index.append(df.index[i])
            exit_price.append(df.close[i])
        # endregion
    # endregion

    # region TRADES_DF
    trades = pd.DataFrame()

    #Añadir reference swing y close de entrada
    trades['entry_date'] = np.array(dates)
    trades['exit_date'] = np.array(y_index)
    trades['trade_type']  = np.array(trade_type)
    trades['stock'] = np.array(stock)

    trades['entry_price'] = np.array(entry_price)
    trades['exit_price'] = np.array(exit_price)

    trades['iSMA1']  = np.array(iSMA1_ot)
    trades['iRSI'] = np.array(iRSI_ot)

    trades['y']  = np.array(y)
    trades['y2'] = np.array(y2)
    trades['shares_to_trade'] = np.array(shares_to_trade_list)
    # endregion

    #Save the edited dataframe as a new .csv file
    trades_global = trades_global.append(trades)
    
trades_global = trades_global.sort_values(by=['entry_date'])

# region Stats
stats = pd.DataFrame(columns=['stat', 'value'])

global_wins = [i for i in trades_global['y'] if i >= 0]
global_loses = [i for i in trades_global['y'] if i < 0]

stats.loc[len(stats)] = ['Start date', trades_global['entry_date'].values[0]]
stats.loc[len(stats)] = ['End date', trades_global['exit_date'].values[-1]]

stats.loc[len(stats)] = ['Net profit', trades_global['y'].sum()]
stats.loc[len(stats)] = ['Total # of trades', len(trades_global)]
stats.loc[len(stats)] = ['# of winning trades', len(global_wins)]
stats.loc[len(stats)] = ['# of losing trades', len(global_loses)]

total_win = sum(global_wins)
total_lose = sum(global_loses)
stats.loc[len(stats)] = ['Total win', total_win]
stats.loc[len(stats)] = ['Total lose', total_lose]

stats.loc[len(stats)] = ['Profit factor', total_win / abs(total_lose)]

profitable = len(global_wins) / len(trades_global)
stats.loc[len(stats)] = ['Winning Weight', profitable * 100]

avg_win = statistics.mean(global_wins)
avg_lose = statistics.mean(global_loses)
stats.loc[len(stats)] = ['Avg win', avg_win]
stats.loc[len(stats)] = ['Avg lose', avg_lose]

stats.loc[len(stats)] = ['Reward to risk ratio', avg_win / abs(avg_lose)]
stats.loc[len(stats)] = ['Best trade', max(global_wins)]
stats.loc[len(stats)] = ['Worst trade', min(global_loses)]
stats.loc[len(stats)] = ['Expectancy', (profitable * avg_win) - ((1 - profitable) * -avg_lose)]

# region Streaks
winning_streaks = []
win_streak = 0
for i in range(len(trades_global)) :
    if trades_global['y'].values[i] >= 0 :
        win_streak += 1
    else : 
        winning_streaks.append(win_streak)
        win_streak = 0
stats.loc[len(stats)] = ['Best streak', max(winning_streaks)]

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
accumulate_y = pd.DataFrame()
accumulate_y['acc_y'] = np.cumsum(trades_global['y'])
accumulate_y.reset_index(drop=True, inplace=True)

fig = make_subplots(rows=1, cols=1, shared_xaxes=True)

fig.add_trace(
	go.Scatter(x=accumulate_y.index, y=accumulate_y['acc_y'], 
    line=dict(color='rgba(26,148,49)', width=1),
    fill='tozeroy'),
	row=1, col=1
)

fig.update_layout(paper_bgcolor='rgba(0,0,0)', plot_bgcolor='rgba(0,0,0)')

py.offline.plot(fig, filename = "Accumulate.html")
# endregion

# region Drawdown
piecks = []
drawdowns = []
for i in range(len(trades_global)) :
    if i == 0 : 
        piecks.append(accumulate_y['acc_y'].values[i])
        drawdowns.append(0)
        continue
    
    piecks.append(max(accumulate_y['acc_y'].values[i], piecks[i - 1]))

    if accumulate_y['acc_y'].values[i] < piecks[i] : 
        drawdowns.append(piecks[i] - accumulate_y['acc_y'].values[i])

stats.loc[len(stats)] = ['Max drawdown', max(drawdowns)]
# endregion
# endregion

#Define the portfolio
#Add raw profit

stats.to_csv('SP_stats.csv', sep=';')
trades_global.to_csv('SP_trades.csv', sep=';')