﻿import pandas as pd

def read_csv(file_name: str):
    return pd.read_csv(f'Data/{file_name}', parse_dates=["Date"])

def date_filter(df, start_date: str, end_date: str):
    return df[(df["Date"] >= start_date) & (df["Date"] <= end_date)]

def save_plot(plt, path: str, file: str, format: str):
    plt.savefig(f'{path}{file}.{format}', format=format)
    plt.close()