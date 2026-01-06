# StockSharp Backtester

A command-line tool for backtesting algorithmic trading strategies against historical market data. This tool enables developers to validate strategy performance before deploying to live trading environments.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Usage](#usage)
  - [Basic Usage](#basic-usage)
  - [Advanced Configuration](#advanced-configuration)
- [Understanding the Output](#understanding-the-output)
- [Configuration](#configuration)
  - [Historical Data Setup](#historical-data-setup)
  - [Portfolio Settings](#portfolio-settings)
  - [Time Period Selection](#time-period-selection)
- [Architecture](#architecture)
  - [Compilation Pipeline](#compilation-pipeline)
  - [Emulation Environment](#emulation-environment)
  - [Logging System](#logging-system)
- [Examples](#examples)
  - [Testing a Simple MA Crossover Strategy](#testing-a-simple-ma-crossover-strategy)
  - [Running Multiple Backtests](#running-multiple-backtests)
- [Troubleshooting](#troubleshooting)
- [Advanced Topics](#advanced-topics)
  - [Custom Historical Data](#custom-historical-data)
  - [Performance Optimization](#performance-optimization)
  - [Integration with CI/CD](#integration-with-cicd)
- [API Reference](#api-reference)

## Overview

The Backtester is a standalone console application that allows you to:

1. **Compile** C# strategy files dynamically at runtime
2. **Load** historical market data from local storage
3. **Execute** strategies in a simulated trading environment
4. **Analyze** performance metrics including Profit & Loss (PnL)

Unlike the StockSharp Designer which provides a GUI, the Backtester offers a lightweight, scriptable alternative ideal for:
- Automated testing in development workflows
- Continuous integration/deployment pipelines
- Batch processing multiple strategies
- Command-line oriented development environments

## Features

- **Dynamic Compilation**: Compiles strategy C# source files at runtime without requiring pre-built assemblies
- **Historical Simulation**: Replays historical market data through `HistoryEmulationConnector`
- **Realistic Portfolio Simulation**: Simulates a trading portfolio with configurable initial capital
- **Dual Logging**: Outputs to both console and file (`backtest.log`) for analysis
- **Type-Safe Strategy Loading**: Validates that compiled code implements `Strategy` base class
- **Comprehensive Error Reporting**: Clear compilation and runtime error messages
- **Configurable Time Periods**: Test strategies across any historical date range
- **Market Data Flexibility**: Supports multiple securities and data formats via `StorageRegistry`

## Prerequisites

- **.NET 6.0 SDK or later**: Required for compilation and execution
- **StockSharp Libraries**: The following packages must be available:
  - `StockSharp.Algo`
  - `StockSharp.BusinessEntities`
  - `StockSharp.Messages`
  - `StockSharp.Configuration`
  - `Ecng.Common`
  - `Ecng.Compilation`
- **Historical Market Data**: Market data files stored in StockSharp format (see [Historical Data Setup](#historical-data-setup))

## Installation

### Building from Source

```bash
# Navigate to the repository root
cd AlgoTrading

# Build the Backtester project
dotnet build Backtester/Backtester.csproj

# Or build in Release mode for better performance
dotnet build Backtester/Backtester.csproj -c Release
```

### Running without Building

```bash
dotnet run --project Backtester/Backtester.csproj -- <strategy.cs>
```

### Creating a Standalone Executable

```bash
# Publish as a self-contained executable
dotnet publish Backtester/Backtester.csproj -c Release -r linux-x64 --self-contained

# The executable will be in:
# Backtester/bin/Release/net6.0/linux-x64/publish/Backtester
```

## Usage

### Basic Usage

```bash
# Run with dotnet
dotnet run --project Backtester/Backtester.csproj -- path/to/strategy.cs

# Or if you have a built executable
./Backtester/bin/Debug/net6.0/Backtester path/to/strategy.cs

# Example with a strategy from the API directory
dotnet run --project Backtester/Backtester.csproj -- API/0001_Simple_SMA/CS/SimpleSmaStrategy.cs
```

### Output Example

```
Usage example:
$ dotnet run --project Backtester/Backtester.csproj -- API/0001_Simple_SMA/CS/SimpleSmaStrategy.cs

Initializing compilation environment...
Compiling strategy from API/0001_Simple_SMA/CS/SimpleSmaStrategy.cs...
Compilation successful.
2024-01-15 09:30:00 | INFO | Strategy started
2024-01-15 09:35:00 | INFO | Buy order executed: 100 @ $150.25
2024-01-15 10:15:00 | INFO | Sell order executed: 100 @ $152.50
...
Backtest finished. PnL: 225.00
```

### Advanced Configuration

The Backtester reads configuration from the `StockSharp.Configuration.Paths` class. You can customize:

```csharp
// Default values (configurable in Paths.cs or app configuration)
Paths.HistoryDataPath       // Location of historical market data
Paths.HistoryDefaultSecurity // Security identifier (e.g., "AAPL@NASDAQ")
Paths.HistoryBeginDate      // Start date for backtest
Paths.HistoryEndDate        // End date for backtest
```

## Understanding the Output

### Profit & Loss (PnL)

The final line reports the strategy's **net profit or loss** in the portfolio's currency:

```
Backtest finished. PnL: 1234.56
```

- **Positive values**: Strategy made a profit
- **Negative values**: Strategy incurred losses
- **Zero**: Break-even performance

### Log Files

Two logging outputs are generated:

1. **Console Output**: Real-time feedback during execution
2. **backtest.log**: Detailed log file in the working directory with:
   - Connector events (connection, disconnection, data loading)
   - Strategy lifecycle (start, stop, reset)
   - Trade executions
   - Error messages and warnings

Example `backtest.log` entry:
```
2024-01-15 09:30:00.123 | INFO | HistoryEmulationConnector | Connected to historical data source
2024-01-15 09:30:00.456 | INFO | SimpleSmaStrategy | Strategy started at 2024-01-15 09:30:00
2024-01-15 09:35:12.789 | INFO | SimpleSmaStrategy | BuyMarket order registered: Volume=100
```

## Configuration

### Historical Data Setup

The Backtester expects market data in the **StockSharp storage format**:

```
HistoryDataPath/
└── Securities/
    └── AAPL@NASDAQ/
        ├── candles/
        │   ├── TimeFrame_1min/
        │   ├── TimeFrame_5min/
        │   └── TimeFrame_1day/
        └── trades/
            └── 2024/
                └── 01/
```

**To set up historical data:**

1. Download data using StockSharp Designer or Hydra
2. Export to local storage format
3. Configure `Paths.HistoryDataPath` to point to the root directory

### Portfolio Settings

The simulated portfolio is configured in `Program.cs` (line 77-78):

```csharp
var pf = Portfolio.CreateSimulator();
pf.CurrentValue = 1000000;  // Initial capital: $1,000,000
```

**To customize:**
- Edit the `CurrentValue` to match your testing capital requirements
- Consider realistic values based on your strategy's position sizing

### Time Period Selection

Modify the date range by configuring `Paths`:

```csharp
Paths.HistoryBeginDate = new DateTime(2023, 1, 1);
Paths.HistoryEndDate = new DateTime(2023, 12, 31);
```

## Architecture

### Compilation Pipeline

```
Strategy.cs File
      ↓
[Read File Content]
      ↓
[Create CodeInfo Object]
      ↓
[CompileAsync with Strategy Type Validation]
      ↓
[Check for Compilation Errors]
      ↓
[Instantiate Strategy via Reflection]
```

**Key Components:**

1. **CodeInfo** (line 49-53): Encapsulates source code and metadata
2. **CompileAsync** (line 57): Dynamic compilation using Roslyn
3. **Type Validation** (line 57): Ensures compiled type inherits from `Strategy`
4. **Reflection Instantiation** (line 89): Creates strategy instance at runtime

### Emulation Environment

```
HistoryEmulationConnector
      ├── Security Configuration
      ├── Portfolio Simulation
      └── HistoryMessageAdapter
            ├── StartDate
            ├── StopDate
            └── Market Data Playback
```

**Components:**

1. **Security** (line 69-70): Defines the trading instrument
2. **Portfolio** (line 77-78): Simulates account balance and positions
3. **StorageRegistry** (line 72): Manages historical data access
4. **HistoryEmulationConnector** (line 80-87): Coordinates simulation

### Logging System

The Backtester implements a dual-logging architecture:

```csharp
var logManager = new LogManager();
logManager.Listeners.Add(new FileLogListener("backtest.log"));   // File logging
logManager.Listeners.Add(new ConsoleLogListener());              // Console logging
```

**Log Sources:**
- **Connector**: Connection events, data loading progress
- **Strategy**: Lifecycle events, trade decisions, custom logs

## Examples

### Testing a Simple MA Crossover Strategy

```bash
# Strategy file: my_strategy.cs
dotnet run --project Backtester/Backtester.csproj -- my_strategy.cs

# Expected output shows compilation, execution, and final PnL
```

### Running Multiple Backtests

Create a shell script to batch-test strategies:

```bash
#!/bin/bash
# backtest_all.sh

for strategy in API/*/CS/*.cs; do
    echo "Testing $strategy..."
    dotnet run --project Backtester/Backtester.csproj -- "$strategy"
    echo "---"
done
```

### Integration with Optimization

```bash
# Test with different parameter variations
for period in 10 20 30; do
    # Modify strategy parameters programmatically
    sed "s/Period = 20/Period = $period/" template.cs > strategy_$period.cs

    dotnet run --project Backtester/Backtester.csproj -- strategy_$period.cs
done
```

## Troubleshooting

### "File not found" Error

**Problem:**
```
File not found: my_strategy.cs
```

**Solution:**
- Verify the file path is correct (use absolute paths or relative to working directory)
- Check file permissions (must be readable)

### Compilation Errors

**Problem:**
```
error CS0246: The type or namespace name 'Strategy' could not be found
```

**Solution:**
- Ensure your strategy file includes necessary `using` directives:
  ```csharp
  using StockSharp.Algo.Strategies;
  using StockSharp.BusinessEntities;
  ```
- Verify your class inherits from `Strategy`:
  ```csharp
  public class MyStrategy : Strategy
  ```

### No Historical Data

**Problem:**
Strategy runs but no trades execute, PnL is 0.

**Solution:**
- Verify `Paths.HistoryDataPath` points to valid market data
- Check that the date range overlaps with available data
- Ensure the security ID matches the data files

### Out of Memory

**Problem:**
Exception during large historical data backtests.

**Solution:**
- Reduce the date range (`HistoryBeginDate` to `HistoryEndDate`)
- Use higher timeframe candles (e.g., 1-hour instead of 1-minute)
- Increase available memory or use 64-bit runtime

## Advanced Topics

### Custom Historical Data

To use custom data sources, modify the `StorageRegistry` initialization:

```csharp
var storageRegistry = new StorageRegistry
{
    DefaultDrive = new LocalMarketDataDrive("path/to/custom/data")
};
```

### Performance Optimization

**Tips for faster backtests:**

1. **Use Release builds**: `dotnet build -c Release`
2. **Reduce log verbosity**: Modify log levels in `LogManager`
3. **Cache compiled assemblies**: Save compiled DLLs to skip recompilation
4. **Parallel processing**: Run multiple backtests across CPU cores

### Integration with CI/CD

Example GitHub Actions workflow:

```yaml
name: Strategy Backtests

on: [push, pull_request]

jobs:
  backtest:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '6.0.x'
      - name: Run Backtests
        run: |
          dotnet run --project Backtester/Backtester.csproj -- API/0001_Simple_SMA/CS/SimpleSmaStrategy.cs
          # Add assertions on PnL or other metrics
```

## API Reference

### Command-Line Interface

```
Backtester <strategy.cs>
```

**Arguments:**
- `<strategy.cs>`: Path to the C# strategy file to backtest (required)

**Exit Codes:**
- `0`: Backtest completed successfully
- `1`: File not found or compilation error
- `2`: Runtime exception during backtest

### Strategy Requirements

Your strategy must:

1. **Inherit from `Strategy`**:
   ```csharp
   public class MyStrategy : Strategy
   ```

2. **Implement lifecycle methods**:
   ```csharp
   protected override void OnStarted(DateTimeOffset time)
   {
       // Initialization logic
   }
   ```

3. **Use available properties**:
   - `Security`: Trading instrument (auto-assigned by Backtester)
   - `Portfolio`: Simulated portfolio (auto-assigned)
   - `Connector`: Market data connector (auto-assigned)
   - `Volume`: Default order volume (set to 1 by default)

### Customization Points

To customize the Backtester behavior, edit `Program.cs`:

| Line | Component | Description |
|------|-----------|-------------|
| 40-41 | Logging | Add/remove log listeners |
| 69-70 | Security | Change trading instrument |
| 72 | Data Source | Modify historical data location |
| 74-75 | Date Range | Set backtest time period |
| 77-78 | Portfolio | Configure initial capital |
| 94 | Volume | Set default order size |

## Related Tools

- **[StockSharp Designer](https://stocksharp.com/products/designer/)**: GUI-based strategy development and backtesting
- **[Hydra](https://doc.stocksharp.com/topics/hydra.html)**: Market data downloader and manager
- **Test Suite** (`/Tests`): Automated testing framework for all repository strategies

## Contributing

Improvements to the Backtester are welcome! Consider:

- Adding support for multiple securities (portfolio strategies)
- Implementing parameter optimization loops
- Enhanced performance metrics (Sharpe ratio, drawdown, etc.)
- Export results to CSV/JSON for external analysis
- GUI progress indicators for long-running backtests

## License

This tool is part of the StockSharp AlgoTrading repository. See the main repository README for license information.

## Support

- **Issues**: Report bugs at [GitHub Issues](https://github.com/StockSharp/AlgoTrading/issues)
- **Community**: Join the [Telegram Chat](https://t.me/stocksharpchat)
- **Documentation**: [StockSharp Official Docs](https://doc.stocksharp.com)

---

**Last Updated**: 2026-01-06
**Backtester Version**: 1.0 (as of commit `claude/document-api-mk31iu7s0iz822g3-Sfx7N`)
