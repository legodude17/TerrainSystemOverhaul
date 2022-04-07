using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TSO
{
    public sealed class AdvancedTerrainGrid : IExposable
    {
        private readonly HashSet<int> batchModified = new();
        private readonly Map map;
        private List<TerrainDef>[] grid;

        private bool inBatch;

        public AdvancedTerrainGrid(Map map)
        {
            this.map = map;
            ResetGrids();
        }

        public TerrainDef[] TopGrid { get; private set; }

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                var max = grid.Max(list => list.Count);
                for (var layer = 0; layer < max; layer++)
                {
                    var layerGrid = grid.Select(g => g.Get(layer)).ToArray();
                    ExposeTerrainGrid(layerGrid, $"Layer{layer}", layer == 0 ? TerrainDefOf.Soil : null);
                }
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                ResetGrids();

                for (var xmlNode = Scribe.loader.curXmlParent.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
                {
                    var layer = int.Parse(xmlNode.Name.Replace("Layer", "").Replace("Deflate", ""));
                    var layerGrid = new TerrainDef[map.cellIndices.NumGridCells];
                    ExposeTerrainGrid(layerGrid, $"Layer{layer}", layer == 0 ? TerrainDefOf.Soil : null);
                    for (var i = 0; i < layerGrid.Length; i++)
                        if (layerGrid[i] is not null)
                            grid[i].Place(layer, layerGrid[i]);
                }

                RecacheTop();
            }
        }

        public void ExposeDataBackCompat()
        {
            ResetGrids();

            var topGrid = new TerrainDef[map.cellIndices.NumGridCells];
            var underGrid = new TerrainDef[map.cellIndices.NumGridCells];
            map.terrainGrid.ExposeTerrainGrid(topGrid, "topGrid", TerrainDefOf.Soil);
            map.terrainGrid.ExposeTerrainGrid(underGrid, "underGrid", null);

            for (var i = 0; i < underGrid.Length; i++)
                if (underGrid[i] is not null)
                    grid[i].Add(underGrid[i]);

            for (var i = 0; i < topGrid.Length; i++)
                if (topGrid[i] is not null)
                    grid[i].Add(topGrid[i]);

            RecacheTop();
        }

        public void BeginBatch() => inBatch = true;

        public void EndBatch()
        {
            inBatch = false;
            foreach (var ind in batchModified) RecacheTop(ind);
            batchModified.Clear();
        }

        public TerrainDef TerrainAt(int ind) => TopGrid[ind];

        public TerrainDef TerrainAt(IntVec3 c) => TopGrid[map.cellIndices.CellToIndex(c)];

        public TerrainDef UnderTerrainAt(int ind) => grid[ind][0];

        public TerrainDef UnderTerrainAt(IntVec3 c) => grid[map.cellIndices.CellToIndex(c)][0];
        public IEnumerable<TerrainDef> TerrainsAt(int ind) => grid[ind];
        public List<TerrainDef> TerrainListAt(int ind) => grid[ind];
        public List<TerrainDef> TerrainListAt(IntVec3 c) => grid[map.cellIndices.CellToIndex(c)];
        public IEnumerable<TerrainDef> TerrainsAt(IntVec3 c) => grid[map.cellIndices.CellToIndex(c)];

        public void SetTerrain(IntVec3 c, TerrainDef newTerr)
        {
            if (newTerr is null)
            {
                Log.Error("Tried to set terrain at " + c + " to null.");
                return;
            }

            if (Current.ProgramState == ProgramState.Playing) map.designationManager.DesignationAt(c, DesignationDefOf.SmoothFloor)?.Delete();

            var ind = map.cellIndices.CellToIndex(c);
            grid[ind].Add(newTerr);
            RecacheTop(ind);
            map.terrainGrid.DoTerrainChangedEffects(c);
        }

        public void ReplaceTerrain(IntVec3 c, TerrainDef newTerr)
        {
            RemoveTopLayer(c, false);
            SetTerrain(c, newTerr);
        }

        public void ReplaceTerrain(IntVec3 c, TerrainDef oldTerr, TerrainDef newTerr)
        {
            var ind = map.cellIndices.CellToIndex(c);
            var list = grid[ind];
            for (var i = 0; i < list.Count; i++)
                if (list[i] == oldTerr)
                {
                    list[i] = newTerr;
                    RecacheTop(ind);
                    map.terrainGrid.DoTerrainChangedEffects(c);
                    break;
                }
        }

        public string GetTerrainListString(IntVec3 c)
        {
            return TerrainsAt(c).Reverse().Select(terrain => terrain.LabelCap.Resolve()).ToLineList();
        }

        public bool TryInsertTerrain(IntVec3 c, TerrainDef target, TerrainDef toInsert, int offset = 0)
        {
            var ind = map.cellIndices.CellToIndex(c);
            var list = grid[ind];
            var idx = list.IndexOf(target);
            if (idx == -1) return false;
            list.Place(idx + offset, toInsert);
            RecacheTop(ind);
            return true;
        }

        public void RemoveTerrain(IntVec3 c, TerrainDef toRemove, bool doLeavings = true)
        {
            var ind = map.cellIndices.CellToIndex(c);
            if (toRemove == TopGrid[ind])
            {
                RemoveTopLayer(c, doLeavings);
                return;
            }

            if (doLeavings) GenLeaving.DoLeavingsFor(toRemove, c, map);
            grid[ind].Remove(toRemove);
            RecacheTop(ind);
            map.terrainGrid.DoTerrainChangedEffects(c);
        }

        public void RemoveTopLayer(IntVec3 c, bool doLeavings = true)
        {
            var ind = map.cellIndices.CellToIndex(c);
            var list = grid[ind];
            if (doLeavings) GenLeaving.DoLeavingsFor(TopGrid[ind], c, map);
            list.RemoveAt(list.Count - 1);
            grid[ind] = list;
            RecacheTop(ind);
            map.terrainGrid.DoTerrainChangedEffects(c);
        }

        public bool CanRemoveTopLayerAt(IntVec3 c) => TopGrid[map.cellIndices.CellToIndex(c)].Removable();

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
            if (inBatch)
            {
                batchModified.Add(index);
                return;
            }

            var list = grid[index];
            if (list.Count == 0) return;
            var top = list[list.Count - 1].Clone();
            for (var i = 0; i < list.Count; i++)
            {
                var terrain = list[i];
                if (terrain.bridge) top.bridge = true;
                if (top.IsCarpet && i == list.Count - 2) top.pathCost = terrain.pathCost;
            }

            top.ClearCachedData();

            TopGrid[index] = top;
        }

        public void ResetGrids()
        {
            grid = Enumerable.Repeat(0, map.cellIndices.NumGridCells).Select(_ => new List<TerrainDef>()).ToArray();
            RecacheTop();
        }
    }
}