# region LIBRARIES

# Pandas and numpy are the base libraries in order to manipulate
# all data for analysis and strategies development.
import pandas as pd
import numpy as np
from sympy import symbols

# Thanks to Yahoo Finance library we can get historical data
# for all tickers without any cost.
import yfinance as yf

# Statistics library allows us to use some needed operations for analysis.
import statistics

# Plotly allows us to create graphs needed to do analysis.
import plotly as py
from plotly.subplots import make_subplots
import plotly.graph_objects as go
from plotly.graph_objs import *

# TA libraty from finta allows us to use their indicator 
# functions that have proved to be correct.
from finta import TA

# os library allows us to create directories, 
# will be useful in the Get Data region.
import os

# datetime library allows us to standarize dates format 
# for comparisons between them while calculating trades.
import datetime

import math

import binance as bnb
client = bnb.Client('yp4IdJHY5YdmWMM3KF6fmW8wynwqet3t8PGP4pxkXKJrmomL2Odxo3EUbmLzxVb4', 'mq8rq16gFB132Xd1wkwjya6XMt9UzBZFWhnb2cKiAoWlNV3oikD1Hi7UY1OJarL2')

# endregion