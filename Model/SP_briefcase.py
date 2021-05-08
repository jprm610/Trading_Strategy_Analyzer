import pandas as pd
import numpy as np
import yfinance as yf
from csv import writer
from indicators import *
pd.options.mode.chained_assignment = None

sp500 = pd.read_html('https://en.wikipedia.org/wiki/List_of_S%26P_500_companies')
actives = sp500[0]
tickers = actives['Symbol'].to_list()
tickers = [i.replace('.','-') for i in tickers]

trades_global = pd.DataFrame()

for asset in tickers :
    # region DATA CLEANING
    #Read historical exchange rate file (extracted from dukascopy.com)
    try :
        df = yf.download(asset,'2019-01-01')
    except ValueError :
        continue
    
    if len(df) == 0 :
        continue

    print(asset)

    #Rename df columns for cleaning porpuses
    df.columns = ['open', 'high', 'low', 'close', 'adj close', 'volume']

    #Drop duplicates leaving only the first value
    df = df.drop_duplicates(keep=False)
    trades = pd.DataFrame()

    #df = df[:2000].copy()

    # endregion

    # region PARAMETERS
    IncipientTrendFactor = 3
    distance_to_BO = 0.0001
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

    iSMA1 = SMA(df, 200)

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

    iRSI = RSI(df, 10)

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

    # region TRADE_EMULATION
    dates = []
    trade_type = []
    stock = []

    entry_close = []
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
                    max_income = df.high[i]
                    dates.append(df.index[i])
                    trade_type.append("Long")
                    stock.append(asset)
                    entry_close.append(df.close[i])
                    entry_candle = i

                    iSMA1_ot.append(iSMA1[i])
                    iRSI_ot.append(iRSI[i])
        # endregion

        # region TRADE MANAGEMENT
        else :
            if len(trade_type) == 0 : continue

            if df.high[i] > max_income :
                    max_income = df.high[i]

            if iRSI[i] < 40 or i == entry_candle + 10 :
                on_trade = False
                outcome = df.close[i] - entry_close[-1]
                
                y.append(outcome)
                y_index.append(df.index[i])
                y2.append(max_income - entry_close[-1])

        if i == len(df) - 1 and on_trade :
            y.append(-1)
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

    trades['entry_close'] = np.array(entry_close)
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

    if len(y2) != len(y) : y2.append(-1)
    
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

    #Save the edited dataframe as a new .csv file
    #df.to_csv(r'SP\_' + str(asset) + '_data.csv', sep=';')
    trades_global = trades_global.append(trades)
    
trades_global.to_csv('SP_trades.csv', sep=';')