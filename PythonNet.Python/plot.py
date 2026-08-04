import matplotlib.pyplot as plt

def plot(df, ticker: str, title: str, open: str, close: str, start: str, end: str, theme: str):

    if theme == 'light':
        plt.style.use('default')
    if theme == 'dark':
        plt.style.use('dark_background')

    plt.figure(figsize=(10, 6))
    
    if open is not None:
        plt.plot(df['Open'], label=f'{ticker} Open Price')
    if close is not None:
        plt.plot(df['Close'], label=f'{ticker} Close Price')

    plt.title(f'{title} Stock Price ({start} - {end})')
    plt.xlabel('Date')
    plt.ylabel(f'{title} Price (USD)')
    plt.legend()

    return plt
