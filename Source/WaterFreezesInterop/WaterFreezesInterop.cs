using System.Linq;
using HarmonyLib;
using TSO;
using Verse;
using WF;

namespace WaterFreezesInterop
{
    [StaticConstructorOnStartup]
    public static class WaterFreezesInterop
    {
        static WaterFreezesInterop()
        {
            var harm = new Harmony("legodude17.tso.wf");
            Log.Message("[TSO] Activating compatibility patch for: Water Freezes");
            harm.Patch(AccessTools.Method(typeof(MapComponent_WaterFreezes), nameof(MapComponent_WaterFreezes.UpdateIceStage)),
                new HarmonyMethod(typeof(WaterFreezesInterop), nameof(UpdateIceStage_Prefix)));
            harm.Patch(AccessTools.Method(typeof(MapComponent_WaterFreezes), nameof(MapComponent_WaterFreezes.InitNaturalWaterGrid)),
                new HarmonyMethod(typeof(WaterFreezesInterop), nameof(InitNaturalTerrainGrid_Prefix)));
        }

        public static bool UpdateIceStage_Prefix(MapComponent_WaterFreezes __instance, IntVec3 cell, TerrainExtension_WaterStats extension = null)
        {
            var num = __instance.map.cellIndices.CellToIndex(cell);
            var iceDepth = __instance.IceDepthGrid[num];
            var waterDepth = __instance.WaterDepthGrid[num];
            var waterTerrain = __instance.AllWaterTerrainGrid[num];
            var grid = TSOMod.Grids[__instance.map];
            var list = grid.TerrainListAt(num);
            TerrainDef water = null, ice = null, bridge = null;
            for (var i = 0; i < list.Count; i++)
            {
                var t = list[i];
                if (t.IsWater) water = t;

                if (t.IsThawableIce()) ice = t;

                if (t.IsBridge()) bridge = t;
            }

            Log.Message($"UpdateIceStage: iceDepth={iceDepth}, waterDepth={waterDepth}, water={water}, ice={ice}, bridge={bridge}");

            grid.BeginBatch();

            if (waterDepth > 0)
            {
                if (water is null)
                {
                    if (ice is not null) grid.TryInsertTerrain(cell, ice, waterTerrain);
                    else if (!grid.TryInsertTerrain(cell, list.Get(2), waterTerrain, 1)) grid.SetTerrain(cell, waterTerrain);
                }
                else if (water != waterTerrain) grid.ReplaceTerrain(cell, water, waterTerrain);
            }
            else if (water is not null) grid.RemoveTerrain(cell, water, false);

            if (iceDepth > 0)
            {
                var desiredIce = __instance.GetAppropriateTerrainFor(waterTerrain, waterDepth, iceDepth, extension);
                if (desiredIce is not null && desiredIce.IsThawableIce())
                {
                    if (ice is null)
                    {
                        if (water is not null) grid.TryInsertTerrain(cell, water, desiredIce, 1);
                        else if (!grid.TryInsertTerrain(cell, list.Get(2), desiredIce, 1)) grid.SetTerrain(cell, desiredIce);
                    }
                    else if (ice != desiredIce) grid.ReplaceTerrain(cell, ice, desiredIce);
                }
            }
            else if (ice is not null) grid.RemoveTerrain(cell, ice, false);

            grid.EndBatch();

            __instance.CheckAndRefillCell(cell, extension);
            if (bridge is null) __instance.BreakdownOrDestroyBuildingsInCellIfInvalid(cell);
            return false;
        }

        public static bool InitNaturalTerrainGrid_Prefix(MapComponent_WaterFreezes __instance)
        {
            var map = __instance.map;
            var grid = TSOMod.Grids[map];
            __instance.NaturalWaterTerrainGrid = new TerrainDef[map.cellIndices.NumGridCells];
            for (var i = 0; i < map.cellIndices.NumGridCells; ++i)
                foreach (var terrainDef in grid.TerrainsAt(i).Reverse())
                    if (terrainDef.IsFreezableWater())
                    {
                        __instance.NaturalWaterTerrainGrid[i] = terrainDef;
                        __instance.WaterDepthGrid[i] = WaterFreezesStatCache.GetExtension(terrainDef).MaxWaterDepth;
                        break;
                    }

            return false;
        }
    }
}