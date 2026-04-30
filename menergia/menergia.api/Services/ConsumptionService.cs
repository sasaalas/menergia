using menergia.api.Models;
using System.Text.Json;
using menergiabase.Services;
using menergiabase.Models;

namespace menergia.api.Services;

public class ConsumptionService
{
    private readonly MinunEnergiaLoginService _loginService;
    private ConsumptionDataModel? _consumptionData;

    public ConsumptionService(MinunEnergiaLoginService loginService)
    {
        _loginService = loginService;
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            var success = await _loginService.LoginAsync();
            if (!success)
            {
                Console.WriteLine("Login failed - cannot initialize consumption service");
                return false;
            }

            // Try to fetch data from the initial data endpoint (which loads automatically after login)
            var html = await _loginService.GetInitialDataAsync();
            if (!string.IsNullOrEmpty(html))
            {
                // Save the HTML response for debugging
                await File.WriteAllTextAsync("consumption_response.html", html);

                // Try to parse the consumption model from the HTML
                _consumptionData = await ParseConsumptionDataFromHtml(html);
            }

            if (_consumptionData == null)
            {
                // Fallback to file if API call fails
                Console.WriteLine("API call failed or returned no data, trying to load from file...");
                _consumptionData = await _loginService.LoadConsumptionDataModelFromFileAsync("../extracted_model.json");
            }

            if (_consumptionData != null)
            {
                Console.WriteLine($"Successfully initialized consumption service with data");
                return true;
            }
            else
            {
                Console.WriteLine("Failed to load consumption data from both API and file");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during initialization: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    private async Task<ConsumptionDataModel?> ParseConsumptionDataFromHtml(string html)
    {
        try
        {
            // Extract JSON from JavaScript variable using regex similar to MinunEnergiaLoginService
            var match = System.Text.RegularExpressions.Regex.Match(html, @"var\s+model\s*=\s*(\{[\s\S]*?\});", System.Text.RegularExpressions.RegexOptions.Multiline);

            if (!match.Success)
            {
                Console.WriteLine("Could not find 'var model = {...}' in response");
                return null;
            }

            var json = match.Groups[1].Value;

            // Clean up JavaScript Date constructors
            json = System.Text.RegularExpressions.Regex.Replace(json, @"new\s+Date\((-?\d+)\)", 
                m => {
                    var timestamp = long.Parse(m.Groups[1].Value);
                    var dt = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
                    return $"\"{dt:O}\"";
                });

            // Save extracted JSON for debugging
            await File.WriteAllTextAsync("extracted_model.json", json);
            Console.WriteLine("Extracted JSON saved to extracted_model.json");

            // Deserialize to model
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var model = JsonSerializer.Deserialize<ConsumptionDataModel>(json, options);
            Console.WriteLine($"Successfully deserialized consumption data");
            return model;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing consumption data from HTML: {ex.Message}");
            return null;
        }
    }

    public ConsumptionResponse? GetCurrentMonthConsumption()
    {
        if (_consumptionData == null) return null;

        var periodStart = new DateTime(2026, 4, 1);
        var periodEnd = new DateTime(2026, 4, 29, 23, 59, 59);

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

        var temperatureValues = GetSeriesValuesInRange(_consumptionData.Hours?.Temperature, periodStart, periodEnd);
        if (!temperatureValues.Any())
        {
            temperatureValues = GetSeriesValuesInRange(_consumptionData.Days?.Temperature, periodStart, periodEnd);
        }

        return new ConsumptionResponse
        {
            Period = $"{periodStart:d.M.yyyy} - {periodEnd:d.M.yyyy}",
            TotalConsumption = totalConsumption,
            Unit = _consumptionData.PowerUnit,
            TotalCost = totalCost,
            PreviousYearComparison = new ConsumptionComparison
            {
                PreviousYearConsumption = previousYearConsumption,
                Difference = consumptionDifference,
                DifferenceText = comparisonText
            },
            Temperature = temperatureValues.Any() ? new TemperatureData
            {
                Minimum = temperatureValues.Min(),
                Average = temperatureValues.Average(),
                Maximum = temperatureValues.Max()
            } : null
        };
    }

    private List<double> GetSeriesValuesInRange(SeriesData? series, DateTime start, DateTime end)
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

    private double CalculateHeatingCost(double consumption, DateTime periodStart, DateTime periodEnd)
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

    private static bool TryGetTimestamp(object? value, out long timestamp)
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

    private static bool TryGetDouble(object? value, out double result)
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
}
