import matplotlib

matplotlib.use("Agg")

import matplotlib.pyplot as plt
import pandas as pd
import pytest

import plot as plot_module


@pytest.fixture(autouse=True)
def no_leftover_figures():
    plt.close("all")
    yield
    plt.close("all")


@pytest.fixture
def prices():
    return pd.DataFrame(
        {"Open": [1.0, 2.0, 3.0], "Close": [1.5, 2.5, 3.5]},
        index=pd.to_datetime(["2024-10-24", "2024-10-25", "2024-10-26"]),
    )


def axes_of(returned):
    assert returned is plt  # plot() hands the module back for util.save_plot
    return plt.gcf().axes[0]


def test_plot_draws_both_series(prices):
    ax = axes_of(
        plot_module.plot(
            prices, "AAPL", "Apple", "on", "on", "2024-10-24", "2024-10-26", "light"
        )
    )

    labels = [line.get_label() for line in ax.get_lines()]
    assert labels == ["AAPL Open Price", "AAPL Close Price"]
    assert list(ax.get_lines()[0].get_ydata()) == [1.0, 2.0, 3.0]
    assert list(ax.get_lines()[1].get_ydata()) == [1.5, 2.5, 3.5]


def test_plot_omits_unchecked_series(prices):
    ax = axes_of(
        plot_module.plot(
            prices, "AAPL", "Apple", None, "on", "2024-10-24", "2024-10-26", "light"
        )
    )

    assert [line.get_label() for line in ax.get_lines()] == ["AAPL Close Price"]


def test_plot_titles_and_labels_include_the_range(prices):
    ax = axes_of(
        plot_module.plot(
            prices, "MSFT", "Microsoft", "on", "on", "2024-10-24", "2024-10-26", "light"
        )
    )

    assert ax.get_title() == "Microsoft Stock Price (2024-10-24 - 2024-10-26)"
    assert ax.get_xlabel() == "Date"
    assert ax.get_ylabel() == "Microsoft Price (USD)"


@pytest.mark.parametrize(
    "theme,expected_dark", [("light", False), ("dark", True)]
)
def test_plot_theme_switches_the_style(prices, theme, expected_dark):
    plot_module.plot(
        prices, "IBM", "IBM", "on", "on", "2024-10-24", "2024-10-26", theme
    )

    # dark_background paints the figure near-black; the default style paints it white.
    is_dark = plt.gcf().get_facecolor()[:3] == (0.0, 0.0, 0.0)
    assert is_dark == expected_dark
