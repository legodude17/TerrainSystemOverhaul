using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TSO
{
    public static class AdvancedTerrainGenerator
    {
        public static void Generate(GenStep_Terrain genStep, Map map, GenStepParams parms)
        {
            BeachMaker.Init(map);
            var riverMaker = genStep.GenerateRiver(map);
            var roofNoCollapseLocs = new List<IntVec3>();
            var elevation = MapGenerator.Elevation;
            var fertility = MapGenerator.Fertility;
            var caves = MapGenerator.Caves;
            var grid = TSOMod.Grids[map];
            // Step 1: Generate base stone grid
            foreach (var cell in map.AllCells) grid.SetTerrain(cell, GenStep_RocksFromGrid.RockDefAt(cell).building.naturalTerrain);
            // Step 2: Generate natural floor (dirt, gravel, sand, etc.)
            foreach (var cell in map.AllCells)
            {
                if (cell.GetEdifice(map) is {def: {Fillage: FillCategory.Full}} || caves[cell] > 0f) continue;
                var lev = elevation[cell];
                if (lev is > 0.55f and < 0.61f) grid.SetTerrain(cell, TerrainDefOf.Gravel);
                if (BeachMaker.BeachTerrainAt(cell, map.Biome) is {IsWater: false} terrain1) grid.SetTerrain(cell, terrain1);
                if (TerrainThreshold.TerrainAtValue(map.Biome.terrainsByFertility, fertility[cell]) is { } terrain3) grid.SetTerrain(cell, terrain3);
                foreach (var patchMaker in map.Biome.terrainPatchMakers)
                    if (patchMaker.TerrainAt(cell, map, fertility[cell]) is { } terrain2)
                        grid.SetTerrain(cell, terrain2);
            }


            // Step 3: Generate water
            foreach (var cell in map.AllCells)
            {
                var river = riverMaker?.TerrainAt(cell, true);
                var ocean = BeachMaker.BeachTerrainAt(cell, map.Biome);
                var current = grid.TerrainAt(cell);
                if (river is not {IsWater: true} && ocean is not {IsWater: true}) continue;
                if (current != TerrainDefOf.Sand) grid.SetTerrain(cell, TerrainDefOf.Sand);
                if (current.IsWater) grid.RemoveTopLayer(cell, false);
                if (ocean == TerrainDefOf.WaterOceanDeep) grid.SetTerrain(cell, ocean);
                else if (river is {IsRiver: true}) grid.SetTerrain(cell, river);
                else if (ocean is {IsWater: true}) grid.SetTerrain(cell, ocean);
                else if (river is {IsWater: true}) grid.SetTerrain(cell, river);
            }

            // Step 4: Remove buildings on top of rivers
            foreach (var cell in map.AllCells)
                if (grid.TerrainAt(cell).IsRiver && cell.GetEdifice(map) is { } edifice)
                {
                    roofNoCollapseLocs.Add(edifice.Position);
                    edifice.Destroy();
                }

            RoofCollapseCellsFinder.RemoveBulkCollapsingRoofs(roofNoCollapseLocs, map);

            // Step 5: Validate river
            riverMaker?.ValidatePassage(map);

            // Step 6: Remove Islands
            genStep.RemoveIslands(map);

            // Step 7: Cleanup
            BeachMaker.Cleanup();
            foreach (var patchMaker in map.Biome.terrainPatchMakers) patchMaker.Cleanup();
        }
    }
}