namespace MarsHandbook.Client.Models;

public record ReadingStep(string Label, string Detail, int? Value = null);
public record ReadingGuide(string Opening, string Figure, string Context, ReadingStep[] Steps,
    string Question, string[] Answers, int Correct, string Explanation, string NextQuestion, string NextSlug, bool Branches = false);

public static class LivingReadingGuides
{
    public static readonly IReadOnlyDictionary<string, ReadingGuide> BySlug = new Dictionary<string, ReadingGuide>
    {
        ["staging-water-loop"] = new(
            "After exercise, moisture from your breath hangs in the cabin air. Where could that water go next?",
            "From cabin air to usable water", "ISS water recovery · reference for a Mars habitat",
            [
                new("Cabin air", "Breathing and sweating add moisture."),
                new("Condensation", "Cooling the air collects droplets."),
                new("Treatment", "The water processor cleans the collected liquid."),
                new("Clean supply", "Water that meets quality checks can be used.")
            ],
            "The collection tank is full, but the processor has stopped. Which supply can you count on for drinking?",
            ["All the collected water", "Only water already treated and cleared for use"], 1,
            "The tank measures what has been collected. Treatment and quality checks determine how much is usable.",
            "How does some of that clean water become breathing oxygen?", "water-to-breathing-oxygen"),
        ["water-to-breathing-oxygen"] = new(
            "You have clean water and a cabin that needs oxygen. What else has to reach the generator?",
            "Water’s path through the oxygen generator", "International Space Station · oxygen generation",
            [
                new("Clean supply", "Treated water feeds the generator."),
                new("Electrolysis", "Electricity splits water into hydrogen and oxygen."),
                new("Cabin air", "The oxygen enters the breathing supply.")
            ],
            "The generator loses electrical power. Would a full water tank alone keep oxygen production going?",
            ["No, electrolysis needs electricity", "Yes, water pressure can drive the reaction"], 0,
            "Electrolysis uses electrical energy to split water molecules. A full tank supplies only one input.",
            "Could an oxygen generator use Martian air as its starting material?", "oxygen-from-martian-air"),
        ["oxygen-from-martian-air"] = new(
            "Outside Perseverance is a source of oxygen atoms. How did MOXIE get them out of the Martian air?",
            "From atmosphere to oxygen sample", "Mars · MOXIE aboard Perseverance",
            [
                new("Intake", "Martian air supplies carbon dioxide."),
                new("Separation", "MOXIE removes oxygen from carbon dioxide."),
                new("Output", "Sixteen runs yielded 122 grams of oxygen in total.")
            ],
            "Can you use 122 grams as MOXIE’s hourly production rate?",
            ["Yes, each run lasted one hour", "No, it is the combined output of sixteen runs"], 1,
            "A cumulative mass tells you how much was made. A rate also requires an operating time.",
            "What can a habitat recover from the air its crew has already breathed?", "staging-water-loop"),
        ["rooms-for-four"] = new(
            "One crewmate is going to sleep while another starts a workout. Where should their paths cross?",
            "Two routes through one habitat", "CHAPEA · illustrative routes in an Earth analog",
            [
                new("Shared kitchen", "Crew members gather here for meals."),
                new("Private quarters", "A person ending a shift can retreat here."),
                new("Exercise area", "A separate destination for an active crewmate.")
            ],
            "What would help you judge whether exercise will disturb a sleeping crewmate?",
            ["Trace the routes and uses of adjacent rooms", "Count only the total floor area"], 0,
            "A floor-area total hides which activities share a wall or a passage. The crew’s routes reveal potential interruptions.",
            "How can a spacecraft gain usable room after launch?", "expandable-room", Branches: true),
        ["expandable-room"] = new(
            "A room is too large to launch at its working size. When could it take its final shape?",
            "BEAM’s trip from cargo to room", "International Space Station · expandable module",
            [
                new("Launch", "The module travels compactly."),
                new("Attach", "The crew’s station receives the module."),
                new("Expand", "The pressurized space becomes accessible.")
            ],
            "Which design constraint did launching BEAM compactly address?",
            ["The amount of launch volume available", "The amount of dust at a Mars landing site"], 0,
            "The demonstration addressed the room needed during transport. Surface dust presents a separate design problem.",
            "Once the space exists, how should four people share it?", "rooms-for-four"),
        ["packing-the-last-meal"] = new(
            "Dinner is still months away when its ingredients leave Earth. What should the crew pack for the last week?",
            "Read the menu across the mission", "Future Mars expedition · planning example",
            [
                new("At packing", "Record how long each food keeps."),
                new("Early meals", "Use shorter-lived items while suitable."),
                new("Late meals", "Check the remaining menu for nutrition and variety.")
            ],
            "A favourite food keeps for only half the mission. How should it enter the menu?",
            ["Reserve it for the final week", "Schedule it earlier and plan a suitable later substitute"], 1,
            "A favourite may improve a meal only while it remains suitable to eat. Later weeks need their own acceptable choices.",
            "Which fresh ingredient has a crew already grown in orbit?", "veggie-salad"),
        ["veggie-salad"] = new(
            "Tonight’s salad is growing a few steps from the galley. How do its roots get both water and air?",
            "Inside the Veggie garden", "International Space Station · Veggie",
            [
                new("Root pillow", "The growing medium holds roots and nutrients."),
                new("Water and air", "Both must reach the root zone."),
                new("LED lighting", "Light reaches the leaves as the crop grows."),
                new("Harvest", "Fresh leaves supplement the stored menu.")
            ],
            "Leaves are still growing when dinner begins. What can supply tonight’s meal?",
            ["The provisions already packed for the crew", "The projected yield of the next harvest"], 0,
            "A crop estimate represents future food. A stored meal is available even when the harvest is late.",
            "How do you plan meals for the months between deliveries?", "packing-the-last-meal"),
        ["dust-and-solar-budget"] = new(
            "Yesterday’s task list assumes clean panels. What happens to that list after dust cuts the day’s electricity supply?",
            "InSight’s energy per Martian day", "Mars · approximate energy per sol, on the same scale",
            [
                new("After landing", "5,000 Wh/sol", 5000),
                new("Spring 2022", "500 Wh/sol", 500)
            ],
            "A task needs 1,000 Wh in a sol. Could the later solar output alone cover it?",
            ["Yes, with about 500 Wh left", "No, it falls short by about 500 Wh"], 1,
            "At about 500 Wh per sol, the panels supply roughly half of this hypothetical task’s 1,000 Wh requirement.",
            "How does Perseverance get electricity after sunset?", "perseverance-heat-and-power"),
        ["perseverance-heat-and-power"] = new(
            "The Sun has set at Jezero Crater. Where does Perseverance get energy to keep its equipment warm?",
            "Two uses for the generator’s heat", "Mars · Perseverance rover",
            [
                new("Heat source", "Natural radioactive decay releases heat."),
                new("Electrical path", "The generator converts part of the heat into electricity for the batteries."),
                new("Thermal path", "Heat also helps keep rover equipment warm.")
            ],
            "Which two entries belong in a budget for this generator?",
            ["Electricity for the batteries and useful heat", "Sunlight collected by day and reflected at night"], 0,
            "Electrical output and heat delivered to equipment meet different rover needs. Both come from the generator.",
            "What happened to the energy budget of a solar-powered Mars lander?", "dust-and-solar-budget", Branches: true),
        ["separating-toilet-waste"] = new(
            "Two waste streams leave the station toilet. Which one can head toward water recovery?",
            "Separate routes after collection", "International Space Station · waste collection",
            [
                new("Collection", "Airflow helps move waste away from the user."),
                new("Liquid route", "Collected urine can enter the water recovery system."),
                new("Solid route", "Solid material goes into a separate container.")
            ],
            "Solid storage is nearly full. Would extra capacity in the urine processor fix that?",
            ["Yes, both streams share one destination", "No, the two streams have different destinations"], 1,
            "Increasing liquid-processing capacity leaves the solid container with the same amount of room.",
            "How can a crew recover storage space from discarded packaging?", "trash-storage-tiles", Branches: true),
        ["trash-storage-tiles"] = new(
            "The storage locker is full of wrappers. Can a crew reclaim space while keeping the discarded material on board?",
            "From loose packaging to a stowable tile", "Earth · NASA Heat Melt Compactor testing",
            [
                new("Loose trash", "Suitable discarded items occupy storage."),
                new("Compaction", "Heat and pressure consolidate them."),
                new("Tile storage", "The denser shape takes up less room.")
            ],
            "What should a crew record after making a tile?",
            ["Space freed and the tile still needing storage", "All the original material as having left the habitat"], 0,
            "A smaller package frees volume. The resulting tile still needs a place in the inventory.",
            "Why does a habitat collect liquid and solid toilet waste separately?", "separating-toilet-waste"),
        ["waiting-for-a-reply"] = new(
            "Your shift ends before mission control can reply. What information does the next crew need from you?",
            "How a question survives a shift change", "CHAPEA · communications exercise on Earth",
            [
                new("Observe", "Describe the issue and the evidence."),
                new("Hand over", "Record actions tried and the decision still open."),
                new("Reconnect", "Compare the reply with the latest crew observations.")
            ],
            "What makes a handover useful during a communications outage?",
            ["A note saying only that Earth has been contacted", "Observations, attempted actions and the open decision"], 1,
            "The incoming crew can continue the work before a reply arrives and can brief mission control when contact resumes.",
            "How did an isolated crew spend time together after a shift?", "time-off-in-isolation"),
        ["time-off-in-isolation"] = new(
            "One crewmate wants a quiet evening and the others want company. How could both fit into tonight’s plan?",
            "Planning an evening in a shared habitat", "CHAPEA · example inspired by crew accounts",
            [
                new("End of shift", "Identify time available after shared duties."),
                new("Common space", "Choose a place for a group activity."),
                new("Private space", "Leave a quiet option for individual downtime.")
            ],
            "Which plan accommodates both preferences?",
            ["Offer a shared activity and a quiet place at the same time", "Make the whole crew follow one leisure schedule"], 0,
            "People can recover from a workday in different ways. Space and timing determine whether both choices are practical.",
            "How does the group keep work moving when Earth is slow to reply?", "waiting-for-a-reply", Branches: true),
    };
}
