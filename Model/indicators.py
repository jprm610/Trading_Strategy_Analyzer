import pandas as pd
import numpy as np

class holder :
    1

#Simple Moving Average (SMA)
def SMA(prices, periods) :

    results = holder()

    moving_avg = {}

    moving_avg = prices.close.rolling(center=False, window=periods)

    results = moving_avg
    
    return results

