using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Figgle;
using Microsoft.Extensions.Configuration;

namespace TickerConsole;

class Program
{
	private static readonly HttpClient HttpClient = new();
	private static string _lastDisplayedPrice = string.Empty;

	static async Task Main()
	{
		var config = new ConfigurationBuilder()
			.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			.Build();

		string apiKey = config["ApiKey"]!;
		string currency = config["Currency"]!;
		string apiUrl = config["ApiUrl"]!;
		int updateFrequency = int.TryParse(config["UpdateFrequency"], out var delay) ? delay : 1000;

		if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(currency) || string.IsNullOrEmpty(apiUrl))
		{
			Console.WriteLine("Missing configuration in appsettings.json");
			return;
		}

		HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

		Console.CursorVisible = false;

		while (true)
		{
			try
			{
				var response = await HttpClient.GetAsync(apiUrl);
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var options = new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					NumberHandling = JsonNumberHandling.AllowReadingFromString
				};

				var tickers = JsonSerializer.Deserialize<List<TickerModel>>(content, options);
				if (tickers != null)
				{
					var ticker = tickers.Find(t => t.SourceCurrency == "BTC" && t.TargetCurrency == currency);
					if (ticker?.Amount != null)
					{
						var formattedPrice = "$" + ticker.Amount.Value.ToString("N0", new CultureInfo("en-US")).Replace(",", " ");
						UpdateDisplayedPrice(formattedPrice);
					}
				}
			}
			catch (Exception ex)
			{
				Console.Clear();
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"Error: {ex.Message}");
				Console.ResetColor();
			}

			await Task.Delay(updateFrequency);
		}
	}

	static void UpdateDisplayedPrice(string newPrice)
	{
		if (newPrice == _lastDisplayedPrice)
		{
			return;
		}

		var asciiArt = FiggleFonts.Big.Render(newPrice);
		var asciiLines = asciiArt.Split('\n');

		Console.Clear();
		int topPadding = (Console.WindowHeight - asciiLines.Length) / 2;

		for (int i = 0; i < topPadding; i++)
		{
			Console.WriteLine();
		}

		Console.ForegroundColor = ConsoleColor.Green;
		foreach (var line in asciiLines)
		{
			int leftPadding = (Console.WindowWidth - line.Length) / 2;
			Console.WriteLine(new string(' ', Math.Max(0, leftPadding)) + line);
		}
		Console.ResetColor();

		_lastDisplayedPrice = newPrice;
	}
}

public class TickerModel
{
	public TickerModel(string? sourceCurrency, string? targetCurrency, decimal? amount)
	{
		SourceCurrency = sourceCurrency;
		TargetCurrency = targetCurrency;
		Amount = amount;
	}

	public string? SourceCurrency { get; }
	public string? TargetCurrency { get; }
	public decimal? Amount { get; }
}