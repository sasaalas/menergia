namespace menergia.api.Models;

public class ConsumptionResponse
{
    public string Period { get; set; } = string.Empty;
    public double TotalConsumption { get; set; }
    public string Unit { get; set; } = string.Empty;
    public double TotalCost { get; set; }
    public string Currency { get; set; } = "€";
    public ConsumptionComparison? PreviousYearComparison { get; set; }
    public TemperatureData? Temperature { get; set; }
}

public class ConsumptionComparison
{
    public double PreviousYearConsumption { get; set; }
    public double Difference { get; set; }
    public string DifferenceText { get; set; } = string.Empty;
}

public class TemperatureData
{
    public double Minimum { get; set; }
    public double Average { get; set; }
    public double Maximum { get; set; }
    public string Unit { get; set; } = "°C";
}
