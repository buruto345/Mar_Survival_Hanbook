using System.ComponentModel.DataAnnotations;

namespace Mars_Survival_Hanbook.Contracts;

public record Stock(decimal Initial, decimal Capacity, string Unit);
public record Facility(int DefaultCount, int MaxCount, string OperatingMode, Flow[] Flows);
public record Flow(string Resource, string Direction, string Phase, string Parameter);
public record Parameter(decimal Value, string Unit, string Basis, string AssumptionNote,
    Guid? SourceRevisionId = null, string? SourceLocator = null, decimal? OriginalValue = null, string? OriginalBasis = null,
    string? SourceUrl = null, string EvidenceType = "Assumption", string TestEnvironment = "Simulation")
{
    public decimal NormalizedValue => Value;
    public string NormalizedUnit => Unit;
    public string NormalizedBasis => Basis;
    public string OriginalUnit => Unit;
}
public record ScenarioPack(string ScenarioKey, int Version, string EngineVersion, string LanguageCode,
    string Title, int DefaultCrewSize, int DurationSols, int MaxCrewSize, int MaxDurationSols,
    Dictionary<string, Stock> Stocks, Dictionary<string, Facility> Facilities,
    string[] PowerPriority, Dictionary<string, decimal> ActivityHoursPerPerson,
    Dictionary<string, Parameter> Parameters, string[] SolRules)
{
    public decimal MinCropArea => 0;
    public decimal MaxCropArea => 100;
}
public record InitialStock(decimal Quantity, string Unit);
public record PreviewRequest([Required] string ScenarioKey, [Range(1, int.MaxValue)] int ScenarioVersion,
    int? CrewSize = null, int? DurationSols = null, Dictionary<string, InitialStock>? InitialStocks = null,
    Dictionary<string, int>? Facilities = null, string[]? PowerPriority = null,
    Dictionary<string, decimal>? CrewAllocations = null, int HousingToBuild = 0, decimal CropArea = 0,
    string? LanguageCode = null, string? Language = null);
public record Demand(string Key, string Unit, decimal Requested, decimal Supplied)
{
    public decimal Unmet => Requested - Supplied;
}
public record Shortage(int Sol, Demand Need, string Code = "UnmetDemand");
public record Limit(string Code, string Unit, decimal Requested, decimal Available);
public record Operation(string Key, string Unit, decimal Requested, decimal Processed, decimal PowerRequested,
    decimal PowerSupplied, decimal Unrecovered, Limit[] LimitingFactors)
{
    public string? Message => LimitingFactors.Any(f => f.Code == "InsufficientPower")
        ? FormattableString.Invariant($"The {Key} received {PowerSupplied:G29} of the {PowerRequested:G29} kWh it requested.") : null;
}
public sealed class SolResourceBalance(decimal opening, string unit)
{
    public string Unit { get; } = unit;
    public decimal Opening { get; } = opening;
    public decimal Received { get; internal set; }
    public decimal Supplied { get; internal set; }
    public decimal Discarded { get; internal set; }
    public decimal Unmet { get; internal set; }
    public decimal Closing => Opening + Received - Supplied - Discarded;
    public string? Warning => Closing == 0 ? "EmptyStock" : null;
}
public record SolResult(int Sol, Dictionary<string, SolResourceBalance> Balances, List<Demand> Needs,
    List<Operation> Facilities, decimal GeneratedPower, decimal BatteryDischarged, decimal BatteryCharged,
    decimal SurplusPower, decimal WastewaterCollected, decimal UncollectedWater, int ActiveHousing,
    decimal BuildHoursRemaining, decimal CropGrowthSols, decimal Harvest);
public record PreviewResult(string EngineVersion, int ScenarioVersion, Shortage? FirstShortage, List<SolResult> Sols);
