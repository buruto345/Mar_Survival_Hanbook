using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Living;
using Xunit;

namespace Mars_Survival_Hanbook.Tests;

public class LivingSimulationTests
{
    public static PreviewRequest WaterFixture => new("outpost", 1, DurationSols: 1,
        Facilities: new() { ["solar"] = 1, ["habitat"] = 2, ["electrolyzer"] = 0 });

    [Fact] public void WaterLoopMatchesHandCalculation()
    {
        var result = LivingSimulationService.Run(LivingScenarioService.Baseline(), WaterFixture);
        var sol = Assert.Single(result.Sols);
        var machine = sol.Facilities[0];
        Assert.Equal((4m, 2m, .5m), (machine.PowerRequested, machine.Processed, machine.Unrecovered));
        Assert.Equal("InsufficientPower", Assert.Single(machine.LimitingFactors).Code);
        var clean = sol.Balances["water"];
        Assert.Equal((1.5m, 9.5m, .5m, 0m), (clean.Received, clean.Supplied, clean.Unmet, clean.Closing));
        Assert.Equal(2m, sol.Balances["wastewater"].Closing - sol.WastewaterCollected);
        Assert.Equal(.5m, result.FirstShortage!.Need.Unmet);
    }

    [Fact] public void StocksBalanceAndPrioritiesChangeActualSupply()
    {
        var pack = LivingScenarioService.Baseline();
        var r = WaterFixture with { Facilities = new() { ["solar"] = 1 }, DurationSols = 60 };
        var first = LivingSimulationService.Run(pack, r);
        var alternate = LivingSimulationService.Run(pack, r with { PowerPriority = ["electrolyzer", "recycler", "crops"] });
        Assert.True(first.Sols[0].Facilities.Single(f => f.Key == "recycler").Processed >
            alternate.Sols[0].Facilities.Single(f => f.Key == "recycler").Processed);
        Assert.Equal(DiscoveryJson.Write(first), DiscoveryJson.Write(LivingSimulationService.Run(pack, r)));
        foreach (var sol in first.Sols)
        {
            foreach (var (key, balance) in sol.Balances)
            {
                Assert.InRange(balance.Closing, 0, pack.Stocks[key].Capacity);
                if (sol.Sol > 1) Assert.Equal(first.Sols[sol.Sol - 2].Balances[key].Closing, balance.Opening);
            }
            Assert.Equal(sol.GeneratedPower + sol.BatteryDischarged, sol.Facilities.Sum(f => f.PowerSupplied) +
                sol.Needs[0].Supplied + sol.BatteryCharged + sol.SurplusPower);
        }
    }

    [Fact] public void ConstructionAndHarvestTakeEffectAfterMealsAndOnlyOnce()
    {
        var r = new PreviewRequest("outpost", 1, DurationSols: 5, Facilities: new() { ["solar"] = 8, ["electrolyzer"] = 0 },
            InitialStocks: new() { ["water"] = new(100, "kg"), ["food"] = new(0, "kg") }, HousingToBuild: 1, CropArea: 2);
        var sols = LivingSimulationService.Run(LivingScenarioService.Baseline(), r).Sols;
        Assert.Equal(new[] { 1, 1, 1, 2, 2 }, sols.Select(s => s.ActiveHousing));
        Assert.Equal(6, sols.Sum(s => s.Balances["materials"].Supplied));
        Assert.Equal(2m, sols[2].Harvest);
        Assert.Equal(2m, sols.Sum(s => s.Harvest));
        Assert.Equal(0m, sols[2].Needs.Single(n => n.Key == "food").Supplied);
        Assert.Equal(2m, sols[3].Needs.Single(n => n.Key == "food").Supplied);
    }

    [Fact] public void ReportsTiedLimitsAndSpillsWhenStorageIsFull()
    {
        var r = WaterFixture with { InitialStocks = new() { ["wastewater"] = new(2, "kg") } };
        var p = LivingScenarioService.Baseline();
        var sol = LivingSimulationService.Run(p, r).Sols.Single();
        Assert.Equal(new[] { "InsufficientPower", "InsufficientInput" }, sol.Facilities[0].LimitingFactors.Select(x => x.Code));
        r = r with { InitialStocks = new() { ["wastewater"] = new(50, "kg"), ["battery"] = new(20, "kWh") },
            Facilities = new() { ["recycler"] = 0, ["electrolyzer"] = 0, ["solar"] = 8 } };
        sol = LivingSimulationService.Run(p, r).Sols.Single();
        Assert.Equal(sol.WastewaterCollected, sol.Balances["wastewater"].Discarded);
        Assert.Equal(0m, sol.BatteryCharged);
        Assert.Equal(31m, sol.SurplusPower);
    }

    [Fact] public void InvalidConfigurationCannotReachTheEngineOrPublication()
    {
        var p = LivingScenarioService.Baseline();
        Assert.Throws<ApiException>(() => LivingSimulationService.Run(p, WaterFixture with { InitialStocks = new() { ["water"] = new(1, "kWh") } }));
        Assert.Throws<ApiException>(() => LivingSimulationService.Run(p, WaterFixture with { CrewAllocations = new() { ["construction"] = 96 } }));
        p.Parameters.Remove("recyclePower");
        Assert.Contains("recyclePower", Assert.Throws<ApiException>(() => LivingScenarioService.Validate(p)).Message);
    }
}
