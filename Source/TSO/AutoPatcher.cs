using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace TSO
{
    [StaticConstructorOnStartup]
    public static class AutoPatcher
    {
        private static readonly HashSet<string> BridgeExtensions = new()
        {
            "TerrainExtension_Foundation",
            "SimplyMoreBridgesModExt"
        };

        static AutoPatcher()
        {
            var toAdd = new List<TerrainDef>();
            foreach (var terrain in DefDatabase<TerrainDef>.AllDefs)
            {
                if (terrain.GetModExtension<TerrainExtension>() is null)
                {
                    var terrainExtension = new TerrainExtension
                    {
                        layer = ImpliedLayer(terrain)
                    };

                    foreach (var error in terrainExtension.ConfigErrors()) Log.Error($"[TSO] Config Error in TerrainExtension of {terrain}: {error}");

                    terrain.modExtensions ??= new List<DefModExtension>();
                    terrain.modExtensions.Add(terrainExtension);
                }

                if (terrain.MadeFromStuff) toAdd.AddRange(ImpliedFromStuff(terrain));
            }
        }

        private static TerrainLayerDef ImpliedLayer(TerrainDef terrain)
        {
            if (terrain.bridge || terrain.modExtensions is not null && terrain.modExtensions.Any(ext => BridgeExtensions.Contains(ext.GetType().Name)) ||
                terrain.terrainAffordanceNeeded is {defName: "Bridgeable"} or {defName: "BridgeableDeep"})
                return TerrainLayerDefOf.Bridge;
            if (terrain.IsCarpet) return TerrainLayerDefOf.Carpet;
            if (terrain.IsRoad || terrain.HasTag("Floor") || terrain.IsFine) return TerrainLayerDefOf.Floor;
            if (terrain.IsWater || terrain.IsSoil || terrain.IsRiver || terrain.generatedFilth is not null || terrain.defName.EndsWith("_Rough") ||
                terrain.defName.EndsWith("_RoughHewn") || terrain.defName.EndsWith("_Smooth")) return TerrainLayerDefOf.Base;
            if (terrain.defName.Contains("Burned"))
                return ImpliedLayer(DefDatabase<TerrainDef>.AllDefs.FirstOrDefault(def => def.burnedDef == terrain));
            return null;
        }

        private static IEnumerable<TerrainDef> ImpliedFromStuff(TerrainDef terrain)
        {
            var group = new DesignatorDropdownGroupDef
            {
                defName = terrain.defName,
                label = terrain.label,
                description = terrain.description
            };
            DefGenerator.AddImpliedDef(group);
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
                if (thingDef.IsStuff && thingDef.stuffProps.CanMake(terrain))
                    yield return StuffedTerrain(terrain, thingDef, group);

            terrain.designationCategory = null;
        }

        private static TerrainDef StuffedTerrain(TerrainDef terrain, ThingDef stuff, DesignatorDropdownGroupDef group)
        {
            var type = terrain.GetType();
            var newTerr = (TerrainDef) Activator.CreateInstance(type);
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) field.SetValue(newTerr, field.GetValue(terrain));

            newTerr.label = "ThingMadeOfStuffLabel".Translate(stuff.LabelAsStuff, terrain.label);
            newTerr.color = stuff.stuffProps.color;
            newTerr.constructEffect = stuff.stuffProps.constructEffect ?? terrain.constructEffect;
            newTerr.designatorDropdown = group;
            foreach (var factor in stuff.stuffProps.statFactors)
            {
                var stat = newTerr.statBases.FirstOrDefault(mod => mod.stat == factor.stat);
                if (stat is not null)
                    stat.value *= factor.value;
                else
                    newTerr.statBases.Add(new StatModifier
                    {
                        stat = factor.stat,
                        value = factor.stat.defaultBaseValue * factor.value
                    });
            }

            foreach (var offset in stuff.stuffProps.statOffsets)
            {
                var stat = newTerr.statBases.FirstOrDefault(mod => mod.stat == offset.stat);
                if (stat is not null)
                    stat.value += offset.value;
                else
                    newTerr.statBases.Add(new StatModifier
                    {
                        stat = offset.stat,
                        value = offset.value
                    });
            }

            DefGenerator.AddImpliedDef(newTerr);

            return newTerr;
        }
    }
}