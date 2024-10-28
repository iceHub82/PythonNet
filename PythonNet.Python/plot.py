import pandas as pd
import matplotlib.pyplot as plt

def hello_world(name: str) -> str:
    return f"Hello World User: {name}!"

def read_csv_plot_and_save(ticker: str, title: str, file_name: str, start_date: str, end_date: str, open: str, close: str):
    df = pd.read_csv(f'Data/{file_name}', parse_dates=["Date"])
    df = df[(df["Date"] >= start_date) & (df["Date"] <= end_date)]
    df = df.sort_values(by="Date", ascending=True).set_index("Date")
    plt.figure(figsize=(10, 6))
    
    if open is not None:
        plt.plot(df['Open'], label=f'{ticker} Open Price')
    if close is not None:
        plt.plot(df['Close'], label=f'{ticker} Close Price')

    plt.title(f'{title} Stock Price (2021-2022)')
    plt.xlabel('Date')
    plt.ylabel(f'{title} Price (USD)')
    plt.legend()

    plt.savefig(f'wwwroot/plots/plot_{ticker}.png', format="png")
    plt.close()