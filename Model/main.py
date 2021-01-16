import pandas as pd
import numpy as np
from indicators import *

#Read historical exchange rate file (extracted from dukascopy.com)
df = pd.read_csv("EURUSD.csv")

#Rename df columns for cleaning porpuses
df.columns = ['date', 'open', 'high', 'low', 'close', 'volume']

#Reformat the date column
df.date = pd.to_datetime(df.date)

#Set the date column as the index
df = df.set_index(df.date)

#Delete the old date column
del df['date']

#Drop duplicates leaving only the first value
df = df.drop_duplicates(subset = "close", keep = "first")

print(df.head())

#Get all indicator lists
SMA20  = SMA(df, 20)
SMA50  = SMA(df, 50)
SMA200 = SMA(df, 200)
ATR = ATR(df, 100)

#Attach those lists to columns
df['SMA20']  = np.array(SMA20)
df['SMA50']  = np.array(SMA50)
df['SMA200'] = np.array(SMA200)
df['ATR100'] = np.array(ATR)

print(df.head(20))

#Save the edited dataframe as a new .csv file
df.to_csv('Final_frame.csv')