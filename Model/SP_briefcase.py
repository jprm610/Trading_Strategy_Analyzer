import pandas as pd
import numpy as np
import yfinance as yf
import statistics
import plotly as py
from plotly.subplots import make_subplots
import plotly.graph_objects as go
from plotly.graph_objs import *
import finta
from finta import TA
from indicators import *
pd.options.mode.chained_assignment = None

sp500 = pd.read_html('https://en.wikipedia.org/wiki/List_of_S%26P_500_companies')
actives = sp500[0]
tickers = actives['Symbol'].to_list()
tickers = [i.replace('.','-') for i in tickers]

trades_global = pd.DataFrame()
risk_unit_dir = pd.read_csv('stock_means.csv', sep=';')
avg_df = pd.DataFrame(columns=['stock', 'avg_win', 'avg_lose'])

# region PARAMETERS
risk_unit = 100
# endregion

asset_count = 1
for asset in tickers :
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
    df.columns = ['open', 'high', 'low', 'close', 'adj close', 'volume']

    #Drop duplicates leaving only the first value
    df = df.drop_duplicates(keep=False)
    trades = pd.DataFrame()
    # endregion

    # region INDICATOR CALCULATIONS
    #Get all indicator lists
    """
    iSwing1 = MySwing(4)
    iSwing1.swingHigh, iSwing1.swingLow = MySwing.Swing(MySwing, df, iSwing1.strength)

    iATR = ATR(df, 100)

    iSMA3  = SMA(df, 20)
    iSMA2  = SMA(df, 50)
    """

    iSMA1 = TA.SMA(df, 200)

    """
    iEMA3  = EMA(df, 20)
    iEMA2  = EMA(df, 50)
    iEMA1 = EMA(df, 200)

    iMACD1 = MACD(df)

    iBB_Upper_1, iBB_Lower_1 = Bollinger_Bands(df, 20)

    iROC3 = ROC(df, 20)
    iROC2 = ROC(df, 50)
    iROC1 = ROC(df, 200)
    """

    iRSI = TA.RSI(df, 10)

    """
    iTIH = TI(df, 10, True)
    iTIL = TI(df, 10, False)
    iRIH = RI(df, 10, True)
    iRIL = RI(df, 10, False)
    iVIH = VI(df, 10, True)
    iVIL = VI(df, 10, False)
    iOBV = OBV(df, 10)
    """
    # endregion
    
    """
    historical_win_avg = risk_unit_dir[]

    shares_to_trade = risk_unit / avg_loss
    """

    # region TRADE_EMULATION
    dates = []
    trade_type = []
    stock = []

    entry_price = []
    #reference_swing_entry = []

    #iSMA3_ot = []
    #iSMA2_ot = [] 
    iSMA1_ot = []

    #iATR_ot = []

    #iMACD1_ot = []

    #iBB_Upper_1_ot = []
    #iBB_Lower_1_ot = []

    """
    iROC3_ot = []
    iROC2_ot = []
    iROC1_ot = []

    iEMA3_ot = []
    iEMA2_ot = []
    iEMA1_ot = []
    """

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

    #is_upward = False
    #is_downward = False
    on_trade = False
    trade_point_long = -1
    trade_point_short = -1

    for i in range(len(df)) :

        if i == 0 : continue

        #if iSwing1.swingHigh[i] == 0 or iSwing1.swingLow[i] == 0 : continue

        #if iSwing1.Swing_Bar(iSwing1.swingHigh[0:i], 1, iSwing1.strength) == -1 or iSwing1.Swing_Bar(iSwing1.swingLow[0:i], 1, iSwing1.strength) == -1 : continue

        #if iSMA1[i] == -1 or iSMA2[i] == -1 or iSMA3[i] == -1 or iATR[i] == -1 or iRSI[i] == -1 : continue

        if iSMA1[i] == -1 or iRSI[i] == -1 : continue

        #trade_point_short = -1
        #trade_point_long = -1

        #current_stop = iATR[i] * 3
        
        # region TRADE CALCULATION
        if not on_trade :
            if df.close[i] > iSMA1[i] :
                if iRSI[i] < 30 :
                    on_trade = True
                    dates.append(df.index[i])
                    trade_type.append("Long")
                    stock.append(asset)
                    entry_candle = i
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
                    outcome = (df.close[i] - entry_price[-1])

                    y_index.append(df.index[i])
                else :
                    outcome = (df.open[i + 1] - entry_price[-1])

                    y_index.append(df.index[i + 1])

                y.append(outcome)
                y2.append(max_income - entry_price[-1])

        if i == len(df) - 1 and on_trade :
            outcome = df.close[i] - entry_price[-1]

            y2.append(max_income - entry_price[-1])
            y.append(outcome)
            y_index.append(df.index[i])
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
    #trades['reference_swing'] = np.array(reference_swing_entry)

    """
    trades['iATR']  = np.array(iATR_ot)

    trades['iSMA3']  = np.array(iSMA3_ot)
    trades['iSMA2']  = np.array(iSMA2_ot)
    """

    trades['iSMA1']  = np.array(iSMA1_ot)

    """
    trades['iROC3'] = np.array(iROC3_ot)
    trades['iROC2'] = np.array(iROC2_ot)
    trades['iROC1'] = np.array(iROC1_ot)

    trades['iEMA3'] = np.array(iEMA3_ot)
    trades['iEMA2'] = np.array(iEMA2_ot)
    trades['iEMA1'] = np.array(iEMA1_ot)

    trades['iBBUpper20'] = np.array(iBB_Upper_1_ot)
    trades['iBBLower20'] = np.array(iBB_Lower_1_ot)

    trades['iMACD1226'] = np.array(iMACD1_ot)
    """

    trades['iRSI'] = np.array(iRSI_ot)

    """
    trades['recoil_x'] = np.array(recoil_x)
    trades['init_x'] = np.array(init_x)

    trades['iOBV'] = np.array(iOBV_ot)
    trades['iRI'] = np.array(iRI_ot)
    trades['iVI'] = np.array(iVI_ot)
    trades['iTI'] = np.array(iTI_ot)
    """

    trades['y']  = np.array(y)
    trades['y2'] = np.array(y2)
    # endregion

    # region BUILD NEW DATASET
    #Attach those lists to columns
    """
    df['ATR100'] = np.array(iATR)

    df['SMA20']  = np.array(iSMA3)
    df['SMA50']  = np.array(iSMA2)
    """

    df['SMA200'] = np.array(iSMA1)

    """
    df['ROC20'] = np.array(iROC3)
    df['ROC50'] = np.array(iROC2)
    df['ROC200'] = np.array(iROC1)

    df['EMA20'] = np.array(iEMA3)
    df['EMA50'] = np.array(iEMA2)
    df['EMA200'] = np.array(iEMA1)

    df['MACD1226'] = np.array(iMACD1)

    df['BBLower20'] = np.array(iBB_Lower_1)
    df['BBUpper20'] = np.array(iBB_Upper_1)

    df['swing4high'] = np.array(iSwing1.swingHigh)
    df['swing4low'] = np.array(iSwing1.swingLow)
    """

    df["RSI"] = np.array(iRSI)

    """
    df['TIH'] = np.array(TIH)
    df['TIL'] = np.array(TIL)
    df['RIH'] = np.array(RIH)
    df['RIL'] = np.array(RIL)
    df['VIH'] = np.array(VIH)
    df['VIL'] = np.array(VIL)
    df['MACD']   = np.array(MACD)
    df["BB_Upper"] = np.array(BB_Upper)
    df["BB_Lower"] = np.array(BB_Lower)
    df["OBV"] = np.array(OBV)
    """
    # endregion

    wins = [e for e in trades['y'] if e >= 0]
    loses = [e for e in trades['y'] if e < 0]

    if len(wins) == 0 : avg_win = 0
    else : avg_win = statistics.mean(wins)

    if len(loses) == 0 : avg_lose = 0
    else : avg_lose = statistics.mean(loses)

    avg_df.loc[len(avg_df)] = [str(asset), avg_win, avg_lose]

    avg_df.set_index(avg_df['stock'], drop=True, inplace=True)

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
	go.Scatter(x=accumulate_y.index, y=accumulate_y['acc_y'], line=dict(color='green', width=1)),
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

#adj close instead of close
#Define the portfolio
#avg lose for each stock and risk unit parameter

stats.to_csv('SP_stats.csv', sep=';')
trades_global.to_csv('SP_trades.csv', sep=';')
avg_df.to_csv('stock_means.csv', sep=';')