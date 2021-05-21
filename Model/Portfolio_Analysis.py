import pandas as pd
import numpy as np

trades_global = pd.read_csv("SP_trades.csv", sep=';') 

# Here is build a df with the number of trades by every date.
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
""" Number_of_trades.to_csv('Number_of_trades.csv', sep=';') """

# Here is build a df just with the trades that can be entered
# taking into account the portfolio rules
Portfolio_Trades = pd.DataFrame()
Newest_closes = [0]
Closed_Trades_per_Date = 0
Acum_Opened = Number_of_trades['# of trades'].values[0]
Acum_Total = 0
Acum_Closed = 0
Counter = 0
Slots = 10
for i in range(len(Number_of_trades)) :
    if i != 0 :
        Acum_Closed = sum(1 for x in Portfolio_Trades['exit_date'] if x <= Number_of_trades.index.values[i])     
        Closed_Trades_per_Date = Acum_Closed - sum(Newest_closes)
        Newest_closes.append(Closed_Trades_per_Date)   
        Acum_Opened = sum(1 for x in Portfolio_Trades['entry_date'] if x == Number_of_trades.index.values[i-1]) + Acum_Opened     
    Counter = Acum_Total - Closed_Trades_per_Date
    #Acum_Opened = Number_of_trades['# of trades'].values[i] + Acum_Opened
    Acum_Total = Acum_Opened - Acum_Closed
    if Counter <= Slots :
        if Number_of_trades['# of trades'].values[i] > Slots - Counter :
        #print(Number_of_trades.index.values[i])
        #print(trades_global['entry_date'])
        #print(trades_global[trades_global['entry_date'] == Number_of_trades.index.values[i]])
            Portfolio_Trades = Portfolio_Trades.append(trades_global[trades_global['entry_date'] == Number_of_trades.index.values[i]][0:Slots - Counter], ignore_index=True)
        else :
            Portfolio_Trades = Portfolio_Trades.append(trades_global[trades_global['entry_date'] == Number_of_trades.index.values[i]], ignore_index=True)

            """ Portfolio_Trades['# of closed trades'] = [] """
            """ Portfolio_Trades['# of closed trades'] = sum(1 for x in Portfolio_Trades['exit_date'][0:i-1] if x <= Number_of_trades.index.values[i]) - sum(Portfolio_Trades['# of closed trades']) """
            """ Trading_dates = []
            Number_Trades_per_Date = []
            Number_Closed_Trades_per_Date = []
            Trades_per_date = 0
            last_date = Portfolio_Trades['entry_date'].values[0]
            for i in range(len(Portfolio_Trades)) :
                if Portfolio_Trades['entry_date'].values[i] == last_date :
                    Trades_per_date += 1
                else : 
                    Closed_Trades_per_Date = sum(1 for x in Portfolio_Trades['exit_date'][0:i-1] if x <= last_date) - sum(Number_Closed_Trades_per_Date)
                    Trading_dates.append(last_date)
                    Number_Trades_per_Date.append(Trades_per_date)
                    Number_Closed_Trades_per_Date.append(Closed_Trades_per_Date)
                    last_date = trades_global['entry_date'].values[i]
                    Trades_per_date = 1 """
""" else :
        Portfolio_Trades = Portfolio_Trades.append(trades_global[trades_global['entry_date'] == Number_of_trades.index.values[i]], ignore_index=True) """

#print(Portfolio_Trades)
Portfolio_Trades.to_csv('Portfolio_Trades.csv', sep=';')