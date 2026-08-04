import os

import pandas as pd
import pytest

import util

DATA_DIR = os.path.join(os.path.dirname(__file__), "..", "PythonNet.App.Web")


@pytest.fixture
def prices():
    return pd.DataFrame(
        {
            "Date": pd.to_datetime(
                ["2024-10-27", "2024-10-24", "2024-10-26", "2024-10-25"]
            ),
            "Open": [4.0, 1.0, 3.0, 2.0],
            "Close": [4.5, 1.5, 3.5, 2.5],
        }
    )


def test_date_filter_keeps_only_the_range(prices):
    out = util.date_filter(prices, "2024-10-25", "2024-10-26")

    assert len(out) == 2
    assert sorted(d.strftime("%Y-%m-%d") for d in out["Date"]) == [
        "2024-10-25",
        "2024-10-26",
    ]


def test_date_filter_bounds_are_inclusive(prices):
    out = util.date_filter(prices, "2024-10-24", "2024-10-27")

    assert len(out) == 4


def test_date_filter_empty_range(prices):
    assert util.date_filter(prices, "2025-01-01", "2025-01-31").empty


def test_sort_data_ascending_indexes_by_column(prices):
    out = util.sort_data(prices, "Date", True)

    assert out.index.name == "Date"
    assert list(out["Open"]) == [1.0, 2.0, 3.0, 4.0]


def test_sort_data_descending(prices):
    out = util.sort_data(prices, "Date", False)

    assert list(out["Open"]) == [4.0, 3.0, 2.0, 1.0]


@pytest.mark.parametrize(
    "file_name",
    [
        "aapl_27.10.24-24.10.24.csv",
        "msft_27.10.24-24.10.24.csv",
        "ibm_27.10.24-24.10.24.csv",
    ],
)
def test_read_csv_parses_dates(monkeypatch, file_name):
    # read_csv resolves 'Data/<file>' relative to the process cwd, which in production is the web app.
    monkeypatch.chdir(DATA_DIR)

    df = util.read_csv(file_name)

    assert not df.empty
    assert {"Date", "Open", "Close"} <= set(df.columns)
    assert pd.api.types.is_datetime64_any_dtype(df["Date"])


def test_read_csv_missing_file_raises(monkeypatch):
    monkeypatch.chdir(DATA_DIR)

    with pytest.raises(FileNotFoundError):
        util.read_csv("nope.csv")


def test_save_plot_writes_and_closes(tmp_path):
    import matplotlib

    matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    plt.figure()
    plt.plot([1, 2, 3])

    util.save_plot(plt, f"{tmp_path}{os.sep}", "chart", "png")

    written = tmp_path / "chart.png"
    assert written.exists() and written.stat().st_size > 0
    assert plt.get_fignums() == []
