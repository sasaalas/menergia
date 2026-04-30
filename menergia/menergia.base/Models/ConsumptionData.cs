using System.Text.Json.Serialization;

namespace menergiabase.Models;

public class ConsumptionDataModel
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public bool HasTemperatureSeries { get; set; }
    public bool HasReactivePowerSeries { get; set; }
    public bool WaterConsumptionAndReadingsAvailable { get; set; }
    public string PowerUnit { get; set; } = string.Empty;
    public DataInterval DataInterval { get; set; } = new();
    public ResolutionData Minutes15 { get; set; } = new();
    public ResolutionData Hours { get; set; } = new();
    public ResolutionData Days { get; set; } = new();
    public ResolutionData Weeks { get; set; } = new();
    public ResolutionData Months { get; set; } = new();
    public ResolutionData Years { get; set; } = new();
    public object? ElectricityReadingData { get; set; }
    public object? HeatingReadingData { get; set; }
    public object? WaterReadingData { get; set; }
    public object? GasReadingData { get; set; }
    public object? ComparisonGroup { get; set; }
    public object? ProfilingDetail { get; set; }
    public List<object> Notes { get; set; } = new();
    public PriceList? SalesPriceList { get; set; }
    public PriceList? NetworkPriceList { get; set; }
    public EnergyTaxes? EnergyTaxes { get; set; }
    public List<VatInfo> Vat { get; set; } = new();
    public List<object> EnergyConsumptionCorrections { get; set; } = new();
    public bool UseReadings { get; set; }
    public bool HourlyDataAvailable { get; set; }
    public int UtilityType { get; set; }
    public int CustomerType { get; set; }
    public double LowTemperature { get; set; }
    public double HighTemperature { get; set; }
    public bool HasProviderHourlyValuesSeparateLoadingSupport { get; set; }
    public bool HasHourlyValues { get; set; }
    public bool HasMinutes15Values { get; set; }
    public bool FullConsumptionLoaded { get; set; }
    public object? MeteringPoint { get; set; }
    public object? PriceLimitPriceList { get; set; }
    public object? PriceGuaranteedPriceList { get; set; }
    public object? ServiceProductPriceList { get; set; }
    public object? NettedConsumption { get; set; }
    public object? NettedProduction { get; set; }
    public object? EnergyCommunityCalculatedConsumption { get; set; }
    public object? EnergyCommunityCalculatedProduction { get; set; }
    public int ContractTypeId { get; set; }
    public bool HasWaterHourlyValues { get; set; }
    public object? SingleReportConsumer { get; set; }
    public DateTime EarliestContractStartDate { get; set; }
}

public class DataInterval
{
    public string Duration { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime Stop { get; set; }
    public DateTime StartValue { get; set; }
    public DateTime StopValue { get; set; }
    public int TotalYears { get; set; }
    public double TotalDays { get; set; }
    public double TotalHours { get; set; }
}

public class ResolutionData
{
    public MaxValue? PMax { get; set; }
    public MaxValue? QMax { get; set; }
    public TimeStep Step { get; set; } = new();
    public List<TariffData> Consumptions { get; set; } = new();
    public List<TariffData> Productions { get; set; } = new();
    public List<TariffData> SalesConsumptions { get; set; } = new();
    public List<object> PreviousYearMonthlyConsumptions { get; set; } = new();
    public List<object> Prices { get; set; } = new();
    public SeriesData? Consumption { get; set; }
    public SeriesData? NettedConsumption { get; set; }
    public SeriesData? EnergyCommunityCalculatedConsumption { get; set; }
    public SeriesData? EnergyCommunityCalculatedProduction { get; set; }
    public SeriesData? SalesConsumption { get; set; }
    public SeriesData? SalesConsumptionSecondary { get; set; }
    public SeriesData? PeakPower { get; set; }
    public SeriesData? TemperatureCorrectedConsumption { get; set; }
    public SeriesData? DegreeDayValues { get; set; }
    public SeriesData? MeasuredDegreeDayValues { get; set; }
    public SeriesData? Temperature { get; set; }
    public SeriesData? ReactivePower { get; set; }
    public SeriesData? ReactivePowerProduction { get; set; }
    public SeriesData? ActivePower { get; set; }
    public SeriesData? ActivePowerProduction { get; set; }
    public object? HeatingPowerMeterReadingData { get; set; }
    public object? HeatingWaterMeterReadingData { get; set; }
    public object? HeatingReadingBasicPriceData { get; set; }
    public object? HeatingReadingEnergyPriceData { get; set; }
    public SeriesData? WaterFlow { get; set; }
    public SeriesData? WaterFlowInTemperature { get; set; }
    public SeriesData? WaterFlowOutTemperature { get; set; }
    public object? WaterMeterReadingData { get; set; }
    public SeriesData? ConsumptionStatuses { get; set; }
    public SeriesData? Production { get; set; }
    public SeriesData? NettedProduction { get; set; }
    public object? OwnAreaComparisionGroupData { get; set; }
    public object? ForeignAreaComparisionGroupData { get; set; }
    public SeriesData? MeterReadings { get; set; }
    public object? DHMaxPowerCharges { get; set; }
    public object? DHWaterOutCharges { get; set; }
    public SeriesData? JoinedConsumptions { get; set; }
}

public class MaxValue
{
    public DateTime Item1 { get; set; }
    public double Item2 { get; set; }
}

public class TimeStep
{
    public TimeZoneInformation TimeZoneInfo { get; set; } = new();
    public int Type { get; set; }
    public string? StepLength { get; set; }
    public DateTime Start { get; set; }
    public DateTime Stop { get; set; }
    public int StepCount { get; set; }
    public int? Multiplier { get; set; }
}

public class TimeZoneInformation
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string StandardName { get; set; } = string.Empty;
    public string DaylightName { get; set; } = string.Empty;
    public string BaseUtcOffset { get; set; } = string.Empty;
    public List<AdjustmentRule> AdjustmentRules { get; set; } = new();
    public bool SupportsDaylightSavingTime { get; set; }
}

public class AdjustmentRule
{
    public DateTime DateStart { get; set; }
    public DateTime DateEnd { get; set; }
    public string DaylightDelta { get; set; } = string.Empty;
    public TransitionTime DaylightTransitionStart { get; set; } = new();
    public TransitionTime DaylightTransitionEnd { get; set; } = new();
    public string BaseUtcOffsetDelta { get; set; } = string.Empty;
}

public class TransitionTime
{
    public DateTime TimeOfDay { get; set; }
    public int Month { get; set; }
    public int Week { get; set; }
    public int Day { get; set; }
    public int DayOfWeek { get; set; }
    public bool IsFixedDateRule { get; set; }
}

public class TariffData
{
    public int TariffTimeZoneId { get; set; }
    public string TariffTimeZoneName { get; set; } = string.Empty;
    public string TariffTimeZoneDescription { get; set; } = string.Empty;
    public SeriesData Series { get; set; } = new();
}

public class SeriesData
{
    public int ReadingCounter { get; set; }
    public string? Name { get; set; }
    public string Resolution { get; set; } = string.Empty;
    public List<List<object>> Data { get; set; } = new();
    public DateTime Start { get; set; }
    public DateTime Stop { get; set; }
    public TimeStep Step { get; set; } = new();
    public int DataCount { get; set; }
    public string? Type { get; set; }
    public string? Unit { get; set; }
    public bool HasData { get; set; }
}

public class PriceList
{
    public string PriceListName { get; set; } = string.Empty;
    public int ActiveTarificationId { get; set; }
    public List<Tarification> Tarifications { get; set; } = new();
    public List<object> BundlePricePeriods { get; set; } = new();
    public List<PriceData> SingleTariffBasicPrices { get; set; } = new();
    public List<PriceData> SingleEnergyPrices { get; set; } = new();
    public object? SingleEnergyExceededBundlePrices { get; set; }
    public List<object> TimeBasedTariffBasicPrices { get; set; } = new();
    public List<object> TimeBasedEnergyDayPrices { get; set; } = new();
    public List<object> TimeBasedEnergyNightPrices { get; set; } = new();
    public List<object> SeasonTariffBasicPrices { get; set; } = new();
    public List<object> SeasonEnergyWinterdayPrices { get; set; } = new();
    public List<object> SeasonEnergyOtherPrices { get; set; } = new();
    public List<object> PowerTariffBasicPrices { get; set; } = new();
    public List<object> PowerEnergyDayPrices { get; set; } = new();
    public List<object> PowerEnergyOtherTimePrices { get; set; } = new();
    public List<object> HeatingBasicPrices { get; set; } = new();
    public List<PriceData> HeatingEnergyPrices { get; set; } = new();
    public List<object> GasBasicPrices { get; set; } = new();
    public List<object> GasEnergyPrices { get; set; } = new();
    public List<object> CleanWaterBasicPrices { get; set; } = new();
    public List<object> WasteWaterBasicPrices { get; set; } = new();
    public List<object> CleanWaterUsagePrices { get; set; } = new();
    public List<object> WasteWaterUsagePrices { get; set; } = new();
    public List<object> RainWaterBasicPrices { get; set; } = new();
    public List<object> RainWaterUsagePrices { get; set; } = new();
    public List<object> PowerEnergyWinterPrices { get; set; } = new();
    public List<object> PeakPowerPrices { get; set; } = new();
    public List<object> ReactivePowerPrices { get; set; } = new();
    public List<object> FourSeasonBasicPrices { get; set; } = new();
    public List<object> FourSeasonSummerDayPrices { get; set; } = new();
    public List<object> FourSeasonSummerNightPrices { get; set; } = new();
    public List<object> FourSeasonWinterDayPrices { get; set; } = new();
    public List<object> FourSeasonWinterNightPrices { get; set; } = new();
    public List<object> FiftyFiftyFixedPrices { get; set; } = new();
    public object? ProfileCostPrices { get; set; }
    public bool IsFiftyFiftyPriceList { get; set; }
    public bool IsProfileCostPriceList { get; set; }
    public bool IsSpotPriceList { get; set; }
    public bool IsFixedWinterAndSpotSummerPriceList { get; set; }
    public object? SpotPrices { get; set; }
    public object? SpotPriceRanges { get; set; }
    public object? EnergyWeightedSpotPrices { get; set; }
    public object? ProfileCosts { get; set; }
    public object? PriceListSpecialType { get; set; }
    public object? MonthlyConsumptionRangeMinValue { get; set; }
    public object? MonthlyConsumptionRangeMaxValue { get; set; }
    public object? MonthlyConsumptionRangeEnergyUnit { get; set; }
    public object? YearlyConsumptionRangeMinValue { get; set; }
    public object? YearlyConsumptionRangeMaxValue { get; set; }
    public object? YearlyConsumptionRangeEnergyUnit { get; set; }
}

public class Tarification
{
    public int TarificationId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

public class PriceData
{
    public string ProductComponentTypeCode { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal PriceWithVat { get; set; }
    public decimal PriceNoVat { get; set; }
    public object? PriceFormula { get; set; }
    public bool IsProfileCostContract { get; set; }
    public bool IsFiftyFiftyPriceData { get; set; }
}

public class EnergyTaxes
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<TaxPeriod> Taxes { get; set; } = new();
}

public class TaxPeriod
{
    public decimal TotalTaxWithVAT { get; set; }
    public decimal TotalTaxNoVAT { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public long StartDateTicks { get; set; }
    public long EndDateTicks { get; set; }
}

public class VatInfo
{
    public decimal Tax { get; set; }
    public DateTime Start { get; set; }
    public DateTime Stop { get; set; }
}
