namespace StockSharp.Backtester;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Compilation;
using Ecng.Logging;
using Ecng.Reflection;

using StockSharp.Algo.Compilation;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

/// <summary>
/// Backtester console application for testing trading strategies against historical market data.
/// </summary>
/// <remarks>
/// This tool provides a command-line interface for:
/// - Dynamic compilation of C# strategy source files
/// - Historical market data simulation
/// - Portfolio backtesting with realistic order execution
/// - Performance analysis and logging
///
/// Usage: Backtester path/to/strategy.cs
///
/// The strategy file must contain a class that inherits from StockSharp.Algo.Strategies.Strategy
/// and implements the required lifecycle methods (OnStarted, etc.).
/// </remarks>
static class Program
{
	/// <summary>
	/// Main entry point for the Backtester application.
	/// </summary>
	/// <param name="args">Command-line arguments. First argument should be the path to a strategy .cs file.</param>
	/// <returns>Async task that completes when the backtest finishes or an error occurs.</returns>
	public static async Task Main(string[] args)
	{
		// Validate command-line arguments
		if (args.Length == 0)
		{
			Console.WriteLine("Usage: Backtester <strategy.cs>");
			return;
		}

		var strategyPath = args[0];
		if (!File.Exists(strategyPath))
		{
			Console.WriteLine($"File not found: {strategyPath}");
			return;
		}

		// Initialize logging system with dual output (file + console)
		// This allows both real-time monitoring and post-analysis of backtest execution
		var logManager = new LogManager();
		logManager.Listeners.Add(new FileLogListener("backtest.log"));  // Persistent log file
		logManager.Listeners.Add(new ConsoleLogListener());             // Real-time console output

		var token = CancellationToken.None;

		Console.WriteLine("Initializing compilation environment...");

		// Initialize the Roslyn-based compilation environment
		// This sets up the necessary references and compilation context
		await CompilationExtensions.Init(logManager.Application, [], token);

		// Create a CodeInfo object containing the strategy source code
		// This encapsulates the code text and metadata needed for compilation
		var code = new CodeInfo
		{
			Name = Path.GetFileNameWithoutExtension(strategyPath),  // Assembly name derived from file
			Text = File.ReadAllText(strategyPath),                  // Raw C# source code
		};

		Console.WriteLine($"Compiling strategy from {strategyPath}...");

		// Compile the strategy code dynamically using Roslyn
		// The type validator ensures the compiled code contains a class that inherits from Strategy
		var errors = await code.CompileAsync(t => t.IsRequiredType<Strategy>(), code.Name, token);

		// Check for compilation errors and report them
		if (errors.HasErrors())
		{
			foreach (var err in errors)
				Console.WriteLine(err);

			return;
		}

		Console.WriteLine("Compilation successful.");

		// Configure the security (trading instrument) to backtest
		// The security ID comes from application configuration (e.g., "AAPL@NASDAQ")
		var secId = Paths.HistoryDefaultSecurity;
		var security = new Security { Id = secId };

		// Set up the storage registry to access historical market data
		// The LocalMarketDataDrive reads data from the file system in StockSharp format
		var storageRegistry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(Paths.HistoryDataPath) };

		// Define the time period for the backtest
		// These dates determine which historical data will be loaded and replayed
		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		// Create a simulated portfolio with initial capital
		// This portfolio tracks positions, cash balance, and P&L during the backtest
		var pf = Portfolio.CreateSimulator();
		pf.CurrentValue = 1000000;  // Starting capital: $1,000,000

		// Initialize the historical emulation connector
		// This component orchestrates the backtest by:
		// - Loading historical data from storage
		// - Replaying market events in chronological order
		// - Simulating order execution and fills
		// - Tracking portfolio state changes
		var connector = new HistoryEmulationConnector([security], [pf], storageRegistry)
		{
			HistoryMessageAdapter =
			{
				StartDate = startTime,  // Begin backtest at this date/time
				StopDate = stopTime,    // End backtest at this date/time
			}
		};

		// Instantiate the compiled strategy using reflection
		// This creates a new instance of the Strategy-derived class from the compiled assembly
		var strategy = code.ObjectType.CreateInstance<Strategy>();

		// Configure the strategy with required dependencies
		strategy.Portfolio = pf;        // Assign the simulated portfolio
		strategy.Security = security;   // Assign the trading instrument
		strategy.Connector = connector; // Assign the market data connector
		strategy.Volume = 1;            // Set default order volume

		// Register the connector and strategy as log sources
		// This routes their log messages through the LogManager to file and console
		logManager.Sources.Add(connector);
		logManager.Sources.Add(strategy);

		// Connect to the historical data source (async operation)
		// This initializes the connector and prepares it to start data playback
		await connector.ConnectAsync(token);

		// Start the strategy execution (runs until completion or stopped)
		// The strategy's OnStarted method will be called, and it will begin processing market data
		var task1 = strategy.ExecAsync(null, token);

		// Start the connector's data playback
		// This begins feeding historical market data to the strategy
		connector.Start();

		// Wait for either the backtest to complete or user to press Enter to stop
		// This allows manual termination while providing a clean exit path
		var task2 = Task.Run(Console.ReadLine, token);
		Task.WaitAny(task1.AsTask(), task2);

		// Report the final profit/loss result
		// PnL is calculated as: (Current Portfolio Value + Unrealized P&L) - Initial Capital
		Console.WriteLine($"Backtest finished. PnL: {strategy.PnL}");
	}
}
