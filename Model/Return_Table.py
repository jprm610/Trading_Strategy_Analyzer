import pandas as pd
import numpy as np

trades_global = pd.read_csv("SP_trades.csv", sep=';') 

# Here the trades_global df is edited 
# in order to sart the df by exit_date.
trades_global['exit_date'] =  pd.to_datetime(trades_global['exit_date'], format='%d/%m/%Y')
trades_global = trades_global.sort_values(by=['exit_date'])
trades_global.set_index(trades_global['entry_date'], drop=True, inplace=True)
del trades_global['entry_date']
""" print(trades_global) """

# Here is build a df with the number of trades by every date.
Profit_perc = []
for i in range(len(trades_global)) :
    Profit_perc.append(round(((trades_global['y_raw'].values[i] / trades_global['entry_price'].values[i])/10)*100,2))
trades_global['Profit_perc'] = np.array(Profit_perc)
trades_global['year'] = pd.DatetimeIndex(trades_global['exit_date']).year
trades_global['month'] = pd.DatetimeIndex(trades_global['exit_date']).month
""" print(trades_global) """

""" trades_global['Profit_perc'] = trades_global['Profit_perc'] + 1 """

df = pd.DataFrame()
df1 = pd.DataFrame()
df = trades_global.groupby([trades_global['year'], trades_global['month']])['Profit_perc'].sum().unstack(fill_value=0)
df1 = df/100 + 1
df1.columns = df.columns.map(str)
df['Y%'] = round(((df1['1'] * df1['2'] * df1['3'] * df1['4'] * df1['5'] * df1['6'] *df1['7'] * df1['8'] * df1['9'] * df1['10'] * df1['11'] * df1['12'])-1)*100,2)
""" print(df) """
df.to_csv('Return Table.csv', sep=';')