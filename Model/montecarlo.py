# region LIBRARIES

# Pandas and numpy are the base libraries in order to manipulate
# all data for analysis and strategies development.
import pandas as pd
import numpy as np

# Plotly allows us to create graphs needed to do analysis.
import plotly as py
from plotly.subplots import make_subplots
import plotly.graph_objects as go
from plotly.graph_objs import *

# endregion

Iterations = 500
Account_Size = 10000
Trade_Slots = 10
Number_Of_Years = 10

trades_global = pd.read_csv('Model/Files/SP_trades_raw.csv', sep=';')

trades_global['entry_date'] = pd.to_datetime(trades_global['entry_date'], format='%Y-%m-%d')
trades_global['exit_date'] = pd.to_datetime(trades_global['exit_date'], format='%Y-%m-%d')

print('Downloaded!')

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

print('Number of trades done!')

# endregion

cagrs = []
for i in range(Iterations) :
    print(str(i + 1) + '/' + str(Iterations))

    # region Portfolio

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
                Filtered_Trades = Filtered_Trades.sample(frac=1)
                Portfolio_Trades = Portfolio_Trades.append(Filtered_Trades[: Slots - Counter], ignore_index=True)
            else :
                Portfolio_Trades = Portfolio_Trades.append(trades_global[trades_global['entry_date'] == Number_of_trades.index.values[i]], ignore_index=True)
        Acum_Opened = sum(1 for x in Portfolio_Trades['entry_date'] if x == Number_of_trades.index.values[i]) + Acum_Opened

    print('Portfolio done!')

    # endregion

    starting_value = Account_Size
    ending_value = Portfolio_Trades['y'].sum() + starting_value

    # Formula:
    #    CAGR = (EV / SV)^(1 / n) - 1
    cagr = ((ending_value / starting_value) ** (1 / Number_Of_Years)) - 1
    cagr *= 100

    cagrs.append(cagr)

cagr_df = pd.DataFrame()
cagr_df['cagrs'] = np.array(cagrs)
cagr_df.sort_values(by=['cagrs'], ignore_index=True, inplace=True)

# region Chart

# With this information an analysis chart can be printed 
# showing the strategy evolution through time.
fig = make_subplots(rows=1, cols=1, shared_xaxes=True)

fig.add_trace(
	go.Scatter(x=cagr_df.index, y=cagr_df['cagrs'], 
    line=dict(color='rgba(26,148,49)', width=1),
    fill='tozeroy'),
	row=1, col=1
)

fig.update_layout(paper_bgcolor='rgba(0,0,0)', plot_bgcolor='rgba(0,0,0)')

py.offline.plot(fig, filename = "Model/Montecarlo_chart.html")

# endregion

cagr_df.to_csv('Model/Montecarlo.csv', sep=';')