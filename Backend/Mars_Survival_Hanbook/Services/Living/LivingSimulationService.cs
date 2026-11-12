using Mars_Survival_Hanbook.Contracts;

namespace Mars_Survival_Hanbook.Services.Living;

public static class LivingSimulationService
{
    public static PreviewResult Run(ScenarioPack pack, PreviewRequest r)
    {
        var crew = r.CrewSize ?? pack.DefaultCrewSize;
        var duration = r.DurationSols ?? pack.DurationSols;
        Check(crew >= 1 && crew <= pack.MaxCrewSize, "crewSize");
        Check(duration >= 1 && duration <= pack.MaxDurationSols, "durationSols");
        Check(r.CropArea >= pack.MinCropArea && r.CropArea <= pack.MaxCropArea, "cropArea");
        var stocks = pack.Stocks.ToDictionary(x => x.Key, x => x.Value.Initial);
        foreach (var (key, value) in r.InitialStocks ?? [])
        {
            Check(pack.Stocks.TryGetValue(key, out var stock) && value != null && value.Unit == stock.Unit &&
                value.Quantity >= 0 && value.Quantity <= stock.Capacity, "initialStocks");
            stocks[key] = value!.Quantity;
        }
        var counts = pack.Facilities.ToDictionary(x => x.Key, x => x.Value.DefaultCount);
        foreach (var (key, count) in r.Facilities ?? [])
        {
            Check(pack.Facilities.TryGetValue(key, out var facility) && count >= 0 && count <= facility.MaxCount, "facilities");
            counts[key] = count;
        }
        Check(r.HousingToBuild >= 0 && r.HousingToBuild <= pack.Facilities["habitat"].MaxCount - counts["habitat"], "housingToBuild");
        var priority = r.PowerPriority ?? pack.PowerPriority;
        Check(priority.Length == pack.PowerPriority.Length && priority.ToHashSet().SetEquals(pack.PowerPriority), "powerPriority");
        decimal P(string key) => pack.Parameters[key].Value;
        var hours = pack.ActivityHoursPerPerson.ToDictionary(x => x.Key, x => x.Value * crew);
        foreach (var (key, allocation) in r.CrewAllocations ?? [])
        {
            Check(hours.ContainsKey(key) && allocation >= 0 && allocation <= crew * P("budget"), "crewAllocations");
            hours[key] = allocation;
        }
        Check(hours.Values.Sum() <= crew * P("budget"), "crewAllocations");
        var toBuild = r.HousingToBuild;
        decimal construction = 0, growth = 0;
        bool harvested = false;
        var sols = new List<SolResult>();
        Shortage? first = null;
        for (int sol = 1; sol <= duration; sol++)
        {
            var balances = stocks.ToDictionary(x => x.Key, x => new SolResourceBalance(x.Value, pack.Stocks[x.Key].Unit));
            decimal Room(string key) => pack.Stocks[key].Capacity - balances[key].Closing;
            void Receive(string key, decimal amount)
            {
                var spill = Math.Max(0, amount - Room(key));
                balances[key].Received += amount;
                balances[key].Discarded += spill;
            }
            decimal Take(string key, decimal requested)
            {
                var supplied = Math.Min(requested, balances[key].Closing);
                balances[key].Supplied += supplied;
                balances[key].Unmet += requested - supplied;
                return supplied;
            }
            var needs = new List<Demand>();
            var operations = new List<Operation>();
            var housing = counts["habitat"];
            var generated = counts["solar"] * P("solar");
            var dischargeLimit = Math.Min(stocks["battery"], P("discharge"));
            var power = generated + dischargeLimit;
            var baseLoad = housing * P("baseLoad");
            var basePaid = Math.Min(power, baseLoad);
            power -= basePaid;
            needs.Add(new("housingPower", "kWh", baseLoad, basePaid));
            var operators = hours["maintenance"];
            foreach (var key in priority)
            {
                bool crop = key == "crops", recycler = key == "recycler";
                var input = recycler ? "wastewater" : "water";
                var output = recycler ? "water" : "oxygen";
                var requested = crop ? (harvested ? 0 : r.CropArea) : counts[key] * P(recycler ? "recycleRate" : "electrolysisRate");
                var unit = crop ? "m2" : "kg";
                var energy = P(crop ? "cropPower" : recycler ? "recyclePower" : "electrolysisPower");
                var labor = P(crop ? "cropLabor" : "operator");
                var water = crop ? P("cropWater") : 1;
                var yield = crop ? 0 : P(recycler ? "recovery" : "oxygenYield");
                var limits = new List<(string Code, string Unit, decimal Available, decimal Rate)> {
                    ("InsufficientPower", "kWh", power, energy),
                    ("InsufficientInput", "kg", balances[input].Closing, water),
                    ("InsufficientLabor", "h", crop ? hours["cultivation"] : operators, labor)
                };
                if (!crop) limits.Add(("InsufficientStorage", "kg", Room(output), yield));
                var processed = limits.Where(l => l.Rate > 0).Aggregate(requested,
                    (n, l) => Math.Min(n, decimal.Round(l.Available / l.Rate, 12, MidpointRounding.ToZero)));
                var factors = limits.Where(l => l.Rate > 0 && l.Available < requested * l.Rate &&
                    decimal.Round(l.Available / l.Rate, 12, MidpointRounding.ToZero) == processed)
                    .Select(l => new Limit(l.Code, l.Unit, requested * l.Rate, l.Available)).ToArray();
                power -= processed * energy;
                Take(input, processed * water);
                if (crop)
                {
                    if (requested > 0 && processed >= requested * P("cropThreshold")) growth += processed / requested;
                }
                else
                {
                    operators -= processed * labor;
                    Receive(output, processed * yield);
                }
                operations.Add(new(key, unit, requested, processed, requested * energy, processed * energy,
                    crop ? 0 : processed * (1 - yield), factors));
            }
            decimal Consume(string need, string key, decimal amount)
            {
                var actual = Take(key, amount);
                needs.Add(new(need, "kg", amount, actual));
                return actual;
            }
            var usedWater = Consume("drinkingWater", "water", crew * P("drink")) +
                Consume("hygieneWater", "water", crew * P("wash"));
            Consume("oxygen", "oxygen", crew * P("oxygen"));
            var eaten = Consume("food", "food", crew * P("ration"));
            Receive("solidWaste", eaten * P("solid"));
            var collected = usedWater * P("collection");
            Receive("wastewater", collected);
            needs.Add(new("housing", "places", crew, Math.Min(crew, housing * P("beds"))));
            foreach (var (key, target) in pack.ActivityHoursPerPerson)
                needs.Add(new(key, "h", crew * target, Math.Min(crew * target, hours[key])));
            if (toBuild > 0 && (construction > 0 || stocks["materials"] >= P("buildMaterials")) && hours["construction"] > 0)
            {
                if (construction == 0) Take("materials", P("buildMaterials"));
                var dailyBuildLimit = P("buildHours") / P("buildDurationSols");
                construction += Math.Min(dailyBuildLimit, Math.Min(hours["construction"], P("buildHours") - construction));
                if (construction == P("buildHours")) { counts["habitat"]++; toBuild--; construction = 0; }
            }
            decimal harvest = 0;
            if (!harvested && growth >= P("cropDays"))
            {
                harvest = r.CropArea * P("cropYield");
                Receive("food", harvest);
                harvested = true;
            }
            var discharged = Take("battery", Math.Max(0, dischargeLimit - power));
            var surplus = Math.Max(0, power - dischargeLimit);
            var charged = Math.Min(surplus, Math.Min(P("charge"), Room("battery")));
            Receive("battery", charged);
            stocks = balances.ToDictionary(x => x.Key, x => x.Value.Closing);
            if (first == null && needs.FirstOrDefault(n => n.Unmet > 0) is Demand deficit) first = new(sol, deficit);
            sols.Add(new(sol, balances, needs, operations, generated, discharged, charged, surplus - charged,
                collected, usedWater - collected, housing, toBuild * P("buildHours") - construction, growth, harvest));
        }
        return new(pack.EngineVersion, pack.Version, first, sols);
    }

    private static void Check(bool valid, string field)
    {
        if (!valid) throw ApiException.Invalid($"Invalid value for {field}.");
    }
}
