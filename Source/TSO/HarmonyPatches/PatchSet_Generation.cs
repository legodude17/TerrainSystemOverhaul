using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TSO
{
    public class PatchSet_Generation : PatchSet
    {
        public PatchSet_Generation(Harmony harm)
        {
            harm.Patch(AccessTools.Method(typeof(GenStep_Terrain), nameof(GenStep_Terrain.Generate)), new HarmonyMethod(GetType(), nameof(OverrideTerrainGen)));
            harm.Patch(AccessTools.Method(typeof(GenStep_Terrain), nameof(GenStep_Terrain.RemoveIslands)), transpiler: new HarmonyMethod(GetType(), nameof(FixRemoveIslands)));
        }

        public static IEnumerable<CodeInstruction> FixRemoveIslands(IEnumerable<CodeInstruction> instructions) =>
            instructions.MethodReplacer(AccessTools.Method(typeof(TerrainGrid), nameof(TerrainGrid.SetTerrain)), AccessTools.Method(typeof(Utils), nameof(Utils.ReplaceTerrain)));

        public static bool OverrideTerrainGen(GenStep_Terrain __instance, Map map, GenStepParams parms)
        {
            AdvancedTerrainGenerator.Generate(__instance, map, parms);
            return false;
        }
    }
}