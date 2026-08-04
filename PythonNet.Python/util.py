import yfinance as yf
import pandas as pd
from datetime import datetime

def read_csv(file_name: str):
    return pd.read_csv(f'Data/{file_name}', parse_dates=["Date"])

def date_filter(df, start_date: str, end_date: str):
    return df[(df["Date"] >= start_date) & (df["Date"] <= end_date)]

def sort_data(df, column: str, dir: bool):
    return df.sort_values(by=column, ascending=dir).set_index(column)

def save_plot(plt, path: str, file: str, format: str):
    plt.savefig(f'{path}{file}.{format}', format=format)
    plt.close()

def download_data(ticker: str, start_date: str, end_date: str):
    try:
        df = yf.download(
            ticker,
            start=start_date,
            end=end_date,
            progress=False,
            auto_adjust=False
        )

        if df.empty:
            raise ValueError(
                f"No data returned for {ticker} between {start_date} and {end_date}"
            )

        print(f"Downloaded {len(df)} rows of data for {ticker}")
        return df

    except Exception as e:
        print(f"Error downloading data: {e}")
        return None