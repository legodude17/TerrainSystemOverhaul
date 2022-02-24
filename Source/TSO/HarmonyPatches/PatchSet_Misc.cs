using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TSO
{
    public class PatchSet_Misc : PatchSet
    {
        public PatchSet_Misc(Harmony harm)
        {
            harm.Patch(AccessTools.Method(typeof(WealthWatcher), nameof(WealthWatcher.CalculateWealthFloors)), new HarmonyMethod(GetType(), nameof(CalculateWealthFloors)));
            harm.Patch(AccessTools.Method(typeof(PathFinder), nameof(PathFinder.FindPath),
                    new[] {typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode), typeof(PathFinderCostTuning)}),
                transpiler: new HarmonyMethod(GetType(), nameof(ReplaceTopGrid)));
            harm.Patch(AccessTools.Method(typeof(RegionCostCalculator), nameof(RegionCostCalculator.GetCellCostFast)),
                transpiler: new HarmonyMethod(GetType(), nameof(ReplaceTopGrid)));
            harm.Patch(AccessTools.Method(typeof(SectionLayer_TerrainScatter), nameof(SectionLayer_TerrainScatter.Regenerate)),
                transpiler: new HarmonyMethod(GetType(), nameof(ReplaceTopGrid)));
            harm.Patch(AccessTools.Method(typeof(TerrainDef), nameof(TerrainDef.SpecialDisplayStats)), postfix: new HarmonyMethod(GetType(), nameof(LayerStat)));
            harm.Patch(AccessTools.Method(typeof(MouseoverReadout), nameof(MouseoverReadout.MouseoverReadoutOnGUI)),
                transpiler: new HarmonyMethod(GetType(), nameof(BetterTerrainInfo)));
            harm.Patch(AccessTools.Method(typeof(SectionLayer_BridgeProps), nameof(SectionLayer_BridgeProps.ShouldDrawPropsBelow)),
                new HarmonyMethod(GetType(), nameof(ShouldDrawPropsBelow)));
        }

        public static bool CalculateWealthFloors(WealthWatcher __instance, ref float __result)
        {
            var atg = TSOMod.Grids[__instance.map];
            var fogGrid = __instance.map.fogGrid.fogGrid;
            var size = __instance.map.Size;
            __result = 0f;
            var i = 0;
            var num2 = size.x * size.z;
            while (i < num2)
            {
                if (!fogGrid[i]) __result += atg.TerrainsAt(i).Sum(terrain => WealthWatcher.cachedTerrainMarketValue[terrain.index]);
                i++;
            }

            return false;
        }

        public static IEnumerable<CodeInstruction> ReplaceTopGrid(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();
            var info = AccessTools.Field(typeof(TerrainGrid), nameof(TerrainGrid.topGrid));
            var idx = list.FindIndex(ins => ins.LoadsField(info));
            list.RemoveRange(idx - 1, 2);
            list.Insert(idx - 1, CodeInstruction.Call(typeof(Utils), nameof(Utils.TopGrid)));
            return list;
        }

        public static IEnumerable<StatDrawEntry> LayerStat(IEnumerable<StatDrawEntry> stats, StatRequest req) => stats.Append(new StatDrawEntry(StatCategoryDefOf.Terrain,
            "Layer".Translate(), req.Def.GetModExtension<TerrainExtension>().layer.LabelCap, "", 3000));

        public static IEnumerable<CodeInstruction> BetterTerrainInfo(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();
            var info = AccessTools.Method(typeof(GridsUtility), nameof(GridsUtility.GetTerrain));
            var idx = list.FindIndex(ins => ins.Calls(info)) - 2;
            var idx2 = list.FindIndex(idx, ins => ins.opcode == OpCodes.Stloc_1);
            list.RemoveRange(idx, idx2 - idx + 1);
            list.InsertRange(idx, new[]
            {
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloca, 1),
                CodeInstruction.Call(typeof(Utils), nameof(Utils.DrawTerrainInfo))
            });
            return list;
        }

        public static bool ShouldDrawPropsBelow(IntVec3 c, TerrainGrid terrGrid, ref bool __result)
        {
            var grid = TSOMod.Grids[terrGrid.map];
            if (grid.GetBridgeAdvanced(c) is not {bridge: true})
                __result = false;
            else
            {
                var c2 = c;
                c2.z--;
                var map = terrGrid.map;
                __result = c2.InBounds(map) && grid.GetBridgeAdvanced(c2) is not {bridge: true} &&
                           (grid.TerrainAt(c).passability == Traversability.Impassable ||
                            c2.SupportsStructureType(map, TerrainDefOf.Bridge.terrainAffordanceNeeded));
            }

            return false;
        }
    }
}