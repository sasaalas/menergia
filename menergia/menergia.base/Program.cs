using Microsoft.Extensions.Configuration;
using menergiabase;
using menergiabase.Models;
using System.Text.Json;
using System.Linq;
using menergiabase.Services;

namespace menergiabase;

class Program
{
  private static MinunEnergiaLoginService? _loginService;
  private static ConsumptionDataModel? _consumptionData;

  static async Task Main(string[] args)
  {
    // Load configuration from appsettings.json
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

    // Get credentials from configuration
    var username = configuration["MinunEnergia:Username"] ?? throw new InvalidOperationException("Username not configured");
    var password = configuration["MinunEnergia:Password"] ?? throw new InvalidOperationException("Password not configured");

    // Create login service
    _loginService = new MinunEnergiaLoginService(username, password);

    try
    {
      // Perform login
      Console.WriteLine("Attempting to login to MinunEnergia...");
      bool success = await _loginService.LoginAsync();

      if (success)
      {
        Console.WriteLine("Successfully logged in!");
        Console.WriteLine();

        // Load consumption data from extracted JSON file
        Console.WriteLine("Loading consumption data from extracted_model.json...");
        _consumptionData = await _loginService.LoadConsumptionDataModelFromFileAsync("../extracted_model.json");

        if (_consumptionData != null)
        {
          Console.WriteLine("✓ Consumption data loaded successfully!");
          Console.WriteLine();

          // Show menu options
          ShowMenu();

          // Get user choice
          var choice = Console.ReadLine()?.Trim();

          switch (choice)
          {
            case "1":
              DisplayTwoWeekConsumption();
              break;
            case "2":
              DisplayDailyConsumption();
              break;
            case "3":
              DisplayCurrentMonthConsumption();
              break;
            default:
              Console.WriteLine("Invalid option. Showing two-week consumption by default.");
              DisplayTwoWeekConsumption();
              break;
          }
        }
        else
        {
          Console.WriteLine("Failed to load consumption data.");
        }
      }
      else
      {
        Console.WriteLine("Login failed.");
      }
    }
    finally
    {
      _loginService?.Dispose();
    }
  }

  static void ShowMenu()
  {
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine("              MINUN ENERGIA CONSUMPTION VIEWER");
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine();
    Console.WriteLine("Select an option:");
    Console.WriteLine("1) Show consumption for previous two weeks (16.4.2026 - 29.4.2026)");
    Console.WriteLine("2) Show consumption for a specific day");
    Console.WriteLine("3) Show consumption for current month");
    Console.WriteLine();
    Console.Write("Enter your choice (1, 2 or 3): ");
  }

  static void DisplayTwoWeekConsumption()
  {
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine("              CONSUMPTION DATA MODEL OVERVIEW");
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine();

    if (_consumptionData == null)
    {
      Console.WriteLine("No consumption data available.");
      return;
    }

    // Basic model information
    Console.WriteLine($"✓ Model Valid: {_consumptionData.IsValid}");
    Console.WriteLine($"✓ Power Unit: {_consumptionData.PowerUnit}");
    Console.WriteLine($"✓ Has Temperature Series: {_consumptionData.HasTemperatureSeries}");
    Console.WriteLine($"✓ Has Reactive Power Series: {_consumptionData.HasReactivePowerSeries}");
    Console.WriteLine($"✓ Utility Type: {_consumptionData.UtilityType}");
    Console.WriteLine($"✓ Customer Type: {_consumptionData.CustomerType}");
    Console.WriteLine();

    // Data interval
    if (_consumptionData.DataInterval != null)
    {
      Console.WriteLine("📅 Data Interval:");
      Console.WriteLine($"  Start: {_consumptionData.DataInterval.StartValue:yyyy-MM-dd}");
      Console.WriteLine($"  End: {_consumptionData.DataInterval.StopValue:yyyy-MM-dd}");
      Console.WriteLine($"  Duration: {_consumptionData.DataInterval.Duration}");
      Console.WriteLine($"  Total Days: {_consumptionData.DataInterval.TotalDays:F0}");
      Console.WriteLine();
    }

    // Data availability by resolution
    Console.WriteLine("📊 Data Availability by Resolution:");
    Console.WriteLine();

    DisplayResolutionInfo("15-Minute", _consumptionData.Minutes15);
    DisplayResolutionInfo("Hourly", _consumptionData.Hours);
    DisplayResolutionInfo("Daily", _consumptionData.Days);
    DisplayResolutionInfo("Weekly", _consumptionData.Weeks);
    DisplayResolutionInfo("Monthly", _consumptionData.Months);
    DisplayResolutionInfo("Yearly", _consumptionData.Years);

    // Pricing information
    Console.WriteLine();
    Console.WriteLine("💰 Pricing Information:");
    if (_consumptionData.SalesPriceList != null)
    {
      Console.WriteLine($"  Sales Price List: {_consumptionData.SalesPriceList.PriceListName}");
      Console.WriteLine($"  Energy Prices: {_consumptionData.SalesPriceList.SingleEnergyPrices?.Count ?? 0} entries");
    }
    else
    {
      Console.WriteLine("  Sales Price List: Not available");
    }

    if (_consumptionData.NetworkPriceList != null)
    {
      Console.WriteLine($"  Network Price List: {_consumptionData.NetworkPriceList.PriceListName}");
    }
    else
    {
      Console.WriteLine("  Network Price List: Not available");
    }

    // Tax information
    Console.WriteLine();
    Console.WriteLine("📋 Tax Information:");
    if (_consumptionData.EnergyTaxes != null)
    {
      Console.WriteLine($"  Tax Name: {_consumptionData.EnergyTaxes.Name}");
      Console.WriteLine($"  Tax Periods: {_consumptionData.EnergyTaxes.Taxes?.Count ?? 0}");
    }
    else
    {
      Console.WriteLine("  Energy Taxes: Not available");
    }

    if (_consumptionData.Vat?.Count > 0)
    {
      Console.WriteLine($"  VAT Periods: {_consumptionData.Vat.Count}");
    }
    else
    {
      Console.WriteLine("  VAT Information: Not available");
    }

    // Heating Consumption Data (UtilityTypeId 2)
    Console.WriteLine();
    Console.WriteLine("🔥 HEATING CONSUMPTION DATA (UtilityTypeId: 2 - Kaukolämpö):");
    Console.WriteLine();

    DisplayHeatingConsumptionData();

    Console.WriteLine();
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine("Model structure successfully illustrated!");
  }

  static void DisplayModelInformation()
  {
    DisplayTwoWeekConsumption();
  }

  static void DisplayHeatingConsumptionData()
  {
    if (_consumptionData == null) return;

    // Focus on the previous two weeks: April 16-29, 2026
    var periodStart = new DateTime(2026, 4, 16);
    var periodEnd = new DateTime(2026, 4, 29, 23, 59, 59);

    Console.WriteLine("🔥 HEATING CONSUMPTION DATA (UtilityTypeId: 2 - Kaukolämpö):");
    Console.WriteLine();
    Console.WriteLine($"Valittu jakso: {periodStart:dd.MM.yyyy} - {periodEnd:dd.MM.yyyy}");
    Console.WriteLine();

    var consumptionValues = GetSeriesValuesInRange(_consumptionData.Hours?.Consumption, periodStart, periodEnd);
    if (!consumptionValues.Any())
    {
      consumptionValues = GetSeriesValuesInRange(_consumptionData.Days?.Consumption, periodStart, periodEnd);
    }

    var totalConsumption = consumptionValues.Sum();
    var totalCost = CalculateHeatingCost(totalConsumption, periodStart, periodEnd);

    var previousYearStart = periodStart.AddYears(-1);
    var previousYearEnd = periodEnd.AddYears(-1);

    var previousYearValues = GetSeriesValuesInRange(_consumptionData.Hours?.Consumption, previousYearStart, previousYearEnd);
    if (!previousYearValues.Any())
    {
      previousYearValues = GetSeriesValuesInRange(_consumptionData.Days?.Consumption, previousYearStart, previousYearEnd);
    }

    var previousYearConsumption = previousYearValues.Sum();

    Console.WriteLine($"Kulutus yhteensä: {totalConsumption:F3} {_consumptionData.PowerUnit}.");
    Console.WriteLine($"Kustannukset yhteensä: {totalCost:F2} €.");

    var consumptionDifference = totalConsumption - previousYearConsumption;
    var comparisonText = consumptionDifference >= 0
      ? $"kulutuksenne on {consumptionDifference:F3} {_consumptionData.PowerUnit} enemmän"
      : $"kulutuksenne on {Math.Abs(consumptionDifference):F3} {_consumptionData.PowerUnit} vähemmän";

    Console.WriteLine($"Edellisen vuoden vastaavaan ajankohtaan verrattuna {comparisonText}.");
    Console.WriteLine();

    var temperatureValues = GetSeriesValuesInRange(_consumptionData.Hours?.Temperature, periodStart, periodEnd);
    if (!temperatureValues.Any())
    {
      temperatureValues = GetSeriesValuesInRange(_consumptionData.Days?.Temperature, periodStart, periodEnd);
    }

    if (temperatureValues.Any())
    {
      var minTemp = temperatureValues.Min();
      var maxTemp = temperatureValues.Max();
      var avgTemp = temperatureValues.Average();
      Console.WriteLine($"Alin: {minTemp:F1} °C Keskilämpötila: {avgTemp:F1} °C Ylin: {maxTemp:F1} °C");
    }
    else
    {
      Console.WriteLine("Alin: N/A Keskilämpötila: N/A Ylin: N/A");
    }
  }

  static void DisplayDailyConsumption()
  {
    if (_consumptionData == null) return;

    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine("              DAILY HEATING CONSUMPTION VIEW");
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine();

    Console.Write("Enter date (dd.MM.yyyy): ");
    var dateInput = Console.ReadLine()?.Trim();

    DateTime selectedDate;
    if (string.IsNullOrEmpty(dateInput) || !DateTime.TryParseExact(dateInput, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out selectedDate))
    {
      Console.WriteLine("Invalid date format. Using default date: 29.04.2026");
      selectedDate = new DateTime(2026, 4, 29);
    }

    var dayStart = selectedDate.Date;
    var dayEnd = selectedDate.Date.AddDays(1).AddTicks(-1);

    Console.WriteLine($"🔥 HEATING CONSUMPTION DATA (UtilityTypeId: 2 - Kaukolämpö):");
    Console.WriteLine();
    Console.WriteLine($"Valittu jakso: ({selectedDate:dd.MM.yyyy} - {selectedDate:dd.MM.yyyy})");
    Console.WriteLine();

    var consumptionValues = GetSeriesValuesInRange(_consumptionData.Hours?.Consumption, dayStart, dayEnd);
    if (!consumptionValues.Any())
    {
      consumptionValues = GetSeriesValuesInRange(_consumptionData.Days?.Consumption, dayStart, dayEnd);
    }

    var totalConsumption = consumptionValues.Sum();
    var totalCost = CalculateHeatingCost(totalConsumption, dayStart, dayEnd);

    var previousYearStart = dayStart.AddYears(-1);
    var previousYearEnd = dayEnd.AddYears(-1);
    var previousYearValues = GetSeriesValuesInRange(_consumptionData.Hours?.Consumption, previousYearStart, previousYearEnd);
    if (!previousYearValues.Any())
    {
      previousYearValues = GetSeriesValuesInRange(_consumptionData.Days?.Consumption, previousYearStart, previousYearEnd);
    }

    var previousYearConsumption = previousYearValues.Sum();
    var consumptionDifference = totalConsumption - previousYearConsumption;
    var comparisonText = consumptionDifference >= 0
      ? $"kulutuksenne on {consumptionDifference:F3} {_consumptionData.PowerUnit} enemmän"
      : $"kulutuksenne on {Math.Abs(consumptionDifference):F3} {_consumptionData.PowerUnit} vähemmän";

    Console.WriteLine($"Kulutus yhteensä: {totalConsumption:F3} {_consumptionData.PowerUnit}.");
    Console.WriteLine($"Kustannukset yhteensä: {totalCost:F2} €.");
    Console.WriteLine($"Edellisen vuoden vastaavaan ajankohtaan verrattuna {comparisonText}.");
    Console.WriteLine();

    var temperatureValues = GetSeriesValuesInRange(_consumptionData.Hours?.Temperature, dayStart, dayEnd);
    if (!temperatureValues.Any())
    {
      temperatureValues = GetSeriesValuesInRange(_consumptionData.Days?.Temperature, dayStart, dayEnd);
    }

    if (temperatureValues.Any())
    {
      var minTemp = temperatureValues.Min();
      var maxTemp = temperatureValues.Max();
      var avgTemp = temperatureValues.Average();
      Console.WriteLine($"Alin: {minTemp:F1} °C Keskilämpötila: {avgTemp:F1} °C Ylin: {maxTemp:F1} °C");
    }
    else
    {
      Console.WriteLine("Alin: N/A Keskilämpötila: N/A Ylin: N/A");
    }

    Console.WriteLine();
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
  }

  static void DisplayCurrentMonthConsumption()
  {
    if (_consumptionData == null) return;

    // Based on data availability, use April 1-29, 2026
    var periodStart = new DateTime(2026, 4, 1);
    var periodEnd = new DateTime(2026, 4, 29, 23, 59, 59);

    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine("              CURRENT MONTH CONSUMPTION VIEW");
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine();
    Console.WriteLine("🔥 HEATING CONSUMPTION DATA (UtilityTypeId: 2 - Kaukolämpö):");
    Console.WriteLine();
    Console.WriteLine($"Valittu jakso  ({periodStart:d.M.yyyy} - {periodEnd:d.M.yyyy})");
    Console.WriteLine();

    var consumptionValues = GetSeriesValuesInRange(_consumptionData.Hours?.Consumption, periodStart, periodEnd);
    if (!consumptionValues.Any())
    {
      consumptionValues = GetSeriesValuesInRange(_consumptionData.Days?.Consumption, periodStart, periodEnd);
    }

    var totalConsumption = consumptionValues.Sum();
    var totalCost = CalculateHeatingCost(totalConsumption, periodStart, periodEnd);

    var previousYearStart = periodStart.AddYears(-1);
    var previousYearEnd = periodEnd.AddYears(-1);
    var previousYearValues = GetSeriesValuesInRange(_consumptionData.Hours?.Consumption, previousYearStart, previousYearEnd);
    if (!previousYearValues.Any())
    {
      previousYearValues = GetSeriesValuesInRange(_consumptionData.Days?.Consumption, previousYearStart, previousYearEnd);
    }

    var previousYearConsumption = previousYearValues.Sum();
    var consumptionDifference = totalConsumption - previousYearConsumption;
    var comparisonText = consumptionDifference >= 0
      ? $"kulutuksenne on {consumptionDifference:F3} {_consumptionData.PowerUnit} enemmän"
      : $"kulutuksenne on {Math.Abs(consumptionDifference):F3} {_consumptionData.PowerUnit} vähemmän";

    Console.WriteLine($"Kulutus yhteensä: {totalConsumption:F3} {_consumptionData.PowerUnit}.");
    Console.WriteLine($"Kustannukset yhteensä: {totalCost:F2} €.");
    Console.WriteLine($"Edellisen vuoden vastaavaan ajankohtaan verrattuna {comparisonText}.");
    Console.WriteLine();

    var temperatureValues = GetSeriesValuesInRange(_consumptionData.Hours?.Temperature, periodStart, periodEnd);
    if (!temperatureValues.Any())
    {
      temperatureValues = GetSeriesValuesInRange(_consumptionData.Days?.Temperature, periodStart, periodEnd);
    }

    if (temperatureValues.Any())
    {
      var minTemp = temperatureValues.Min();
      var maxTemp = temperatureValues.Max();
      var avgTemp = temperatureValues.Average();
      Console.WriteLine($"Alin: {minTemp:F1} °C Keskilämpötila: {avgTemp:F1} °C Ylin: {maxTemp:F1} °C");
    }
    else
    {
      Console.WriteLine("Alin: N/A Keskilämpötila: N/A Ylin: N/A");
    }

    Console.WriteLine();
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
  }

  static List<double> GetSeriesValuesInRange(SeriesData? series, DateTime start, DateTime end)
  {
    var values = new List<double>();
    if (series?.Data == null) return values;

    foreach (var dp in series.Data)
    {
      if (dp.Count < 2) continue;
      if (!TryGetTimestamp(dp[0], out var timestamp)) continue;

      var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).LocalDateTime;
      if (dateTime < start || dateTime > end) continue;

      if (!TryGetDouble(dp[1], out var value)) continue;
      values.Add(value);
    }

    return values;
  }

  static double CalculateHeatingCost(double consumption, DateTime periodStart, DateTime periodEnd)
  {
    if (_consumptionData?.SalesPriceList == null || consumption <= 0) return 0.0;

    double totalCost = 0.0;

    // 1. Energy cost from sales price list
    var energyPrices = _consumptionData.SalesPriceList.HeatingEnergyPrices;
    if (energyPrices != null && energyPrices.Count > 0)
    {
      var energyPrice = energyPrices
        .Where(p => p.StartTime <= periodEnd && (p.EndTime == default || p.EndTime >= periodStart))
        .OrderByDescending(p => p.StartTime)
        .FirstOrDefault()
        ?? energyPrices.OrderByDescending(p => p.StartTime).First();

      totalCost += consumption * (double)energyPrice.PriceWithVat;
    }

    // 2. Basic charges from sales price list (prorated for the period)
    var basicPrices = _consumptionData.SalesPriceList.HeatingBasicPrices;
    if (basicPrices != null && basicPrices.Count > 0)
    {
      foreach (var item in basicPrices)
      {
        if (item is JsonElement jsonElement)
        {
          try
          {
            var priceData = JsonSerializer.Deserialize<PriceData>(jsonElement.GetRawText());
            if (priceData != null && 
                priceData.StartTime <= periodEnd && 
                (priceData.EndTime == default || priceData.EndTime >= periodStart))
            {
              // Calculate days in the period - using actual calendar days
              var actualStart = priceData.StartTime > periodStart ? priceData.StartTime : periodStart;
              var actualEnd = priceData.EndTime != default && priceData.EndTime < periodEnd ? priceData.EndTime : periodEnd;
              var daysInPeriod = (actualEnd - actualStart).TotalDays + 1;
              var daysInMonth = DateTime.DaysInMonth(periodStart.Year, periodStart.Month);
              var proration = daysInPeriod / daysInMonth;

              totalCost += (double)priceData.PriceWithVat * proration;
              break;
            }
          }
          catch { }
        }
      }
    }

    // 3. Network prices if available
    if (_consumptionData.NetworkPriceList != null)
    {
      var networkEnergyPrices = _consumptionData.NetworkPriceList.HeatingEnergyPrices;
      if (networkEnergyPrices != null && networkEnergyPrices.Count > 0)
      {
        var networkPrice = networkEnergyPrices
          .Where(p => p.StartTime <= periodEnd && (p.EndTime == default || p.EndTime >= periodStart))
          .OrderByDescending(p => p.StartTime)
          .FirstOrDefault();

        if (networkPrice != null)
        {
          totalCost += consumption * (double)networkPrice.PriceWithVat;
        }
      }

      // Network basic charges
      var networkBasicPrices = _consumptionData.NetworkPriceList.HeatingBasicPrices;
      if (networkBasicPrices != null && networkBasicPrices.Count > 0)
      {
        foreach (var item in networkBasicPrices)
        {
          if (item is JsonElement jsonElement)
          {
            try
            {
              var priceData = JsonSerializer.Deserialize<PriceData>(jsonElement.GetRawText());
              if (priceData != null && 
                  priceData.StartTime <= periodEnd && 
                  (priceData.EndTime == default || priceData.EndTime >= periodStart))
              {
                var actualStart = priceData.StartTime > periodStart ? priceData.StartTime : periodStart;
                var actualEnd = priceData.EndTime != default && priceData.EndTime < periodEnd ? priceData.EndTime : periodEnd;
                var daysInPeriod = (actualEnd - actualStart).TotalDays + 1;
                var daysInMonth = DateTime.DaysInMonth(periodStart.Year, periodStart.Month);
                var proration = daysInPeriod / daysInMonth;

                totalCost += (double)priceData.PriceWithVat * proration;
                break;
              }
            }
            catch { }
          }
        }
      }
    }

    // 4. Energy taxes
    var energyTaxes = _consumptionData.EnergyTaxes;
    if (energyTaxes != null && energyTaxes.Taxes != null && energyTaxes.Taxes.Count > 0)
    {
      var tax = energyTaxes.Taxes
        .Where(t => t.StartDate <= periodEnd && (t.EndDate == null || t.EndDate >= periodStart))
        .OrderByDescending(t => t.StartDate)
        .FirstOrDefault();

      if (tax != null)
      {
        totalCost += consumption * (double)tax.TotalTaxWithVAT;
      }
    }

    return totalCost;
  }

  static bool TryGetTimestamp(object? value, out long timestamp)
  {
    if (value is JsonElement jsonValue)
    {
      if (jsonValue.ValueKind == JsonValueKind.Number && jsonValue.TryGetInt64(out timestamp))
      {
        return true;
      }

      if (jsonValue.ValueKind == JsonValueKind.String)
      {
        var text = jsonValue.GetString();
        if (text != null && long.TryParse(text, out timestamp)) return true;
        if (text != null && DateTime.TryParse(text, out var dt))
        {
          timestamp = new DateTimeOffset(dt).ToUnixTimeMilliseconds();
          return true;
        }
      }
    }
    else if (value is long longValue)
    {
      timestamp = longValue;
      return true;
    }
    else if (value is int intValue)
    {
      timestamp = intValue;
      return true;
    }
    else if (value is string stringValue)
    {
      if (long.TryParse(stringValue, out timestamp)) return true;
      if (DateTime.TryParse(stringValue, out var dt))
      {
        timestamp = new DateTimeOffset(dt).ToUnixTimeMilliseconds();
        return true;
      }
    }

    timestamp = 0;
    return false;
  }

  static bool TryGetDouble(object? value, out double result)
  {
    if (value is JsonElement jsonValue)
    {
      if (jsonValue.ValueKind == JsonValueKind.Number && jsonValue.TryGetDouble(out result)) return true;
      if (jsonValue.ValueKind == JsonValueKind.String)
      {
        var text = jsonValue.GetString();
        if (!string.IsNullOrEmpty(text) && double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result))
        {
          return true;
        }
      }
    }
    else if (value is double doubleValue)
    {
      result = doubleValue;
      return true;
    }
    else if (value is float floatValue)
    {
      result = floatValue;
      return true;
    }
    else if (value is decimal decimalValue)
    {
      result = (double)decimalValue;
      return true;
    }
    else if (value is int intValue)
    {
      result = intValue;
      return true;
    }
    else if (value is long longValue)
    {
      result = longValue;
      return true;
    }
    else if (value is string stringValue)
    {
      if (!string.IsNullOrEmpty(stringValue) && double.TryParse(stringValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result))
      {
        return true;
      }
    }

    result = 0;
    return false;
  }

  static void DisplayResolutionInfo(string name, ResolutionData? resolution)
  {
    if (resolution == null)
    {
      Console.WriteLine($"  {name}: Not available");
      return;
    }

    var features = new List<string>();
    if (resolution.Consumptions?.Count > 0) features.Add($"{resolution.Consumptions.Count} consumption series");
    if (resolution.Productions?.Count > 0) features.Add($"{resolution.Productions.Count} production series");
    if (resolution.Temperature?.HasData == true) features.Add("temperature");
    if (resolution.Consumption?.HasData == true) features.Add("aggregated consumption");
    if (resolution.Production?.HasData == true) features.Add("aggregated production");
    if (resolution.WaterFlow?.HasData == true) features.Add("water flow");
    if (resolution.MeterReadings?.HasData == true) features.Add("meter readings");

    if (features.Count > 0)
    {
      Console.WriteLine($"  {name}: {string.Join(", ", features)}");
    }
    else
    {
      Console.WriteLine($"  {name}: Available (empty data)");
    }
  }
}

