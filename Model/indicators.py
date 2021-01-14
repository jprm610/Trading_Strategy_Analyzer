import pandas as pd
import numpy as np

class holder :
    1

#Simple Moving Average (SMA)
def SMA(prices, periods) :
    
    results = holder()

    for i in range(0, len(periods)) :
        if (i <= periods) :
