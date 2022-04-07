using HarmonyLib;
using Verse;

namespace TSO
{
    public class PatchSet_Grid : PatchSet
    {
        public PatchSet_Grid(Harmony harm)
        {
            var type = typeof(TerrainGrid);
            harm.Patch(AccessTools.Constructor(type, new[] {typeof(Map)}), postfix: new HarmonyMethod(GetType(), nameof(MakeAdvGrid)));
            harm.Patch(AccessTools.Method(type, nameof(TerrainGrid.ExposeData)), new HarmonyMethod(GetType(), nameof(ExposeGrid)));
            harm.Patch(AccessTools.Method(type, nameof(TerrainGrid.TerrainAt), new[] {typeof(int)}), new HarmonyMethod(GetType(), nameof(TerrainAtInt)));
            harm.Patch(AccessTools.Method(type, nameof(TerrainGrid.TerrainAt), new[] {typeof(IntVec3)}), new HarmonyMethod(GetType(), nameof(TerrainAtVec)));
            harm.Patch(AccessTools.Method(type, nameof(TerrainGrid.UnderTerrainAt), new[] {typeof(int)}), new HarmonyMethod(GetType(), nameof(UnderTerrainAtInt)));
            harm.Patch(AccessTools.Method(type, nameof(TerrainGrid.UnderTerrainAt), new[] {typeof(IntVec3)}), new HarmonyMethod(GetType(), nameof(UnderTerrainAtVec)));
            harm.Patch(AccessTools.Method(type, nameof(TerrainGrid.SetTerrain)), new HarmonyMethod(GetType(), nameof(SetTerrain)) {priority = Priority.Last});
            harm.Patch(AccessTools.Method(type, nameof(TerrainGrid.SetUnderTerrain)), new HarmonyMethod(GetType(), nameof(SetUnderTerrain)));
            harm.Patch(AccessTools.Method(type, nameof(TerrainGrid.RemoveTopLayer)), new HarmonyMethod(GetType(), nameof(RemoveTopLayer)));
            harm.Patch(AccessTools.Method(type, nameof(TerrainGrid.CanRemoveTopLayerAt)), new HarmonyMethod(GetType(), nameof(CanRemoveTopLayerAt)));
        }

        public static void MakeAdvGrid(TerrainGrid __instance, Map map)
        {
            TSOMod.Grids[map] = new AdvancedTerrainGrid(map);
        }

        public static bool ExposeGrid(TerrainGrid __instance)
        {
            if (!TSOMod.Grids.TryGetValue(__instance.map, out var atg)) atg = new AdvancedTerrainGrid(__instance.map);
            if (Scribe.EnterNode("advancedTerrainGrid"))
                try
                {
                    atg.ExposeData();
                }
                finally
                {
                    Scribe.ExitNode();
                }
            else
                atg.ExposeDataBackCompat();

            TSOMod.Grids.SetOrAdd(__instance.map, atg);

            return false;
        }

        public static bool TerrainAtInt(Map ___map, int ind, ref TerrainDef __result)
        {
            __result = TSOMod.Grids[___map].TerrainAt(ind);
            return false;
        }

        public static bool TerrainAtVec(Map ___map, IntVec3 c, ref TerrainDef __result)
        {
            __result = TSOMod.Grids[___map].TerrainAt(c);
            return false;
        }

        public static bool UnderTerrainAtInt(Map ___map, int ind, ref TerrainDef __result)
        {
            __result = TSOMod.Grids[___map].UnderTerrainAt(ind);
            return false;
        }

        public static bool UnderTerrainAtVec(Map ___map, IntVec3 c, ref TerrainDef __result)
        {
            __result = TSOMod.Grids[___map].UnderTerrainAt(c);
            return false;
        }

        public static bool SetTerrain(Map ___map, IntVec3 c, TerrainDef newTerr)
        {
            TSOMod.Grids[___map].SetTerrain(c, newTerr);
            return false;
        }

        public static bool SetUnderTerrain(Map ___map, IntVec3 c, TerrainDef newTerr)
        {
            var grid = TSOMod.Grids[___map];
            grid.ReplaceTerrain(c, grid.UnderTerrainAt(c), newTerr);
            return false;
        }

        public static bool RemoveTopLayer(Map ___map, IntVec3 c, bool doLeavings)
        {
            TSOMod.Grids[___map].RemoveTopLayer(c, doLeavings);
            return false;
        }

        public static bool CanRemoveTopLayerAt(Map ___map, IntVec3 c, ref bool __result)
        {
            __result = TSOMod.Grids[___map].CanRemoveTopLayerAt(c);
            return false;
        }
    }
}