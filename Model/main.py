import pandas as pd
import numpy as np
from indicators import *

#Read historical exchange rate file (extracted from dukascopy.com)
df = pd.read_csv("EURUSD.csv")

#Rename df columns for cleaning porpuses
df.columns = ['date', 'open', 'high', 'low', 'close', 'volume']

#df.date = pd.to_datetime(df.date, format='%d. %m. %Y %H:%M:%S:%f')

df.set_index(df.date)

SMA = SMA(df, 50)