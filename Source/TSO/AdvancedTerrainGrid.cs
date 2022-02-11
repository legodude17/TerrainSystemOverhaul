using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TSO
{
    public sealed class AdvancedTerrainGrid : IExposable
    {
        private readonly Map map;
        private Dictionary<TerrainLayerDef, TerrainDef[]> grids;

        public AdvancedTerrainGrid(Map map)
        {
            this.map = map;
            ResetGrids();
        }

        public TerrainDef[] TopGrid { get; private set; }

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
                foreach (var grid in grids)
                    ExposeTerrainGrid(grid.Value, grid.Key.defName);
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                ResetGrids();
                for (var xmlNode = Scribe.loader.curXmlParent.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
                {
                    var def = DefDatabase<TerrainLayerDef>.GetNamedSilentFail(xmlNode.Name.Replace("Deflate", ""));
                    if (def is not null) ExposeTerrainGrid(grids[def], def.defName);
                }
            }

            RecacheTop();
        }

        public TerrainDef TerrainAt(int ind) => TopGrid[ind];

        public TerrainDef TerrainAt(IntVec3 c) => TopGrid[map.cellIndices.CellToIndex(c)];
        public TerrainDef TerrainAtLayer(IntVec3 c, TerrainLayerDef layer) => grids[layer][map.cellIndices.CellToIndex(c)];

        public TerrainDef UnderTerrainAt(int ind) => grids[TerrainLayerDefOf.Base][ind];

        public TerrainDef UnderTerrainAt(IntVec3 c) => grids[TerrainLayerDefOf.Base][map.cellIndices.CellToIndex(c)];
        public IEnumerable<TerrainDef> TerrainsAt(int ind) => grids.OrderBy(kv => kv.Key.order).Select(kv => kv.Value[ind]).Where(v => v is not null);
        public IEnumerable<TerrainDef> TerrainsAt(IntVec3 c) => TerrainsAt(map.cellIndices.CellToIndex(c));

        public void SetTerrain(IntVec3 c, TerrainDef newTerr)
        {
            var layers = newTerr.GetModExtension<TerrainExtension>().layers;
            var ind = map.cellIndices.CellToIndex(c);
            foreach (var layer in layers) grids[layer][ind] = newTerr;
            RecacheTop(ind);
            map.terrainGrid.DoTerrainChangedEffects(c);
        }

        public string GetTerrainListString(IntVec3 c)
        {
            return TerrainsAt(c).Select(terrain => terrain.LabelCap.Resolve()).Reverse().ToLineList();
        }

        public TerrainDef FirstBlockingTerrain(IntVec3 c, TerrainDef newTerr)
        {
            var layers = newTerr.GetModExtension<TerrainExtension>().layers;
            var ind = map.cellIndices.CellToIndex(c);
            foreach (var layer in layers)
                if (grids[layer][ind] is { } blocking)
                    return blocking;

            return null;
        }

        public void RemoveTerrain(IntVec3 c, TerrainDef toRemove, bool doLeavings = true)
        {
            var ind = map.cellIndices.CellToIndex(c);
            if (doLeavings) GenLeaving.DoLeavingsFor(toRemove, c, map);

            foreach (var grid in grids.Where(grid => grid.Value[ind] == toRemove))
                grid.Value[ind] = null;

            RecacheTop(ind);
            map.terrainGrid.DoTerrainChangedEffects(c);
        }

        public void RemoveTopLayer(IntVec3 c, bool doLeavings = true)
        {
            RemoveTerrain(c, TopGrid[map.cellIndices.CellToIndex(c)], doLeavings);
        }

        public bool CanRemoveTopLayerAt(IntVec3 c) => TopGrid[map.cellIndices.CellToIndex(c)].Removable;

        private void ExposeTerrainGrid(TerrainDef[] grid, string label, TerrainDef fallbackTerrain = null)
        {
            var terrainDefsByShortHash = DefDatabase<TerrainDef>.AllDefs.ToDictionary(terrainDef => terrainDef.shortHash);

            Func<IntVec3, ushort> shortReader = delegate(IntVec3 c)
            {
                var terrainDef2 = grid[map.cellIndices.CellToIndex(c)];
                return terrainDef2?.shortHash ?? 0;
            };
            Action<IntVec3, ushort> shortWriter = delegate(IntVec3 c, ushort val)
            {
                var terrainDef2 = terrainDefsByShortHash.TryGetValue(val);
                if (terrainDef2 == null && val != 0)
                {
                    var terrainDef3 = BackCompatibility.BackCompatibleTerrainWithShortHash(val);
                    if (terrainDef3 == null)
                    {
                        Log.Error($"Did not find terrain def with short hash {val} for cell {c}.");
                        terrainDef3 = TerrainDefOf.Soil;
                    }

                    terrainDef2 = terrainDef3;
                    terrainDefsByShortHash.Add(val, terrainDef3);
                }

                if (terrainDef2 == null && fallbackTerrain != null)
                {
                    Log.ErrorOnce($"Replacing missing terrain with {fallbackTerrain}", Gen.HashCombine(8388383, fallbackTerrain.shortHash));
                    terrainDef2 = fallbackTerrain;
                }

                grid[map.cellIndices.CellToIndex(c)] = terrainDef2;
            };
            MapExposeUtility.ExposeUshort(map, shortReader, shortWriter, label);
        }

        private void RecacheTop()
        {
            TopGrid = new TerrainDef[map.cellIndices.NumGridCells];
            for (var i = 0; i < TopGrid.Length; i++) RecacheTop(i);
        }

        private void RecacheTop(int index)
        {
            foreach (var pair in grids.OrderByDescending(kv => kv.Key.order))
                if (pair.Value[index] is { } def)
                {
                    TopGrid[index] = def;
                    break;
                }
        }

        public void ResetGrids()
        {
            grids = DefDatabase<TerrainLayerDef>.AllDefs.ToDictionary(def => def, _ => new TerrainDef[map.cellIndices.NumGridCells]);
            RecacheTop();
        }
    }
}