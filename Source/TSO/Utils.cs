using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TSO
{
    public static class Utils
    {
        public static TerrainDef[] TopGrid(this Map map) => TSOMod.Grids[map].TopGrid;

        public static void DrawTerrainInfo(Rect rect, IntVec3 c, MouseoverReadout readout, ref float num)
        {
            var grid = TSOMod.Grids[Find.CurrentMap];
            var terrain = grid.TerrainAt(c);
            if (terrain != readout.cachedTerrain)
            {
                var t = terrain.fertility > 0.0001 ? " " + "FertShort".TranslateSimple() + " " + terrain.fertility.ToStringPercent() : "";
                readout.cachedTerrainString = terrain.passability != Traversability.Impassable
                    ? "(" + "WalkSpeed".Translate(readout.SpeedPercentString(terrain.pathCost)) + t + ")"
                    : null;
                readout.cachedTerrain = terrain;
            }

            var text = grid.GetTerrainListString(c) + (readout.cachedTerrainString is null ? "" : "\n" + readout.cachedTerrainString);
            var height = Text.CalcHeight(text, 999f);
            num += height;
            rect.y -= height - 19f;
            Widgets.Label(rect, text);
        }

        // DO NOT CHANGE, USED FOR INTEROP
        public static TerrainDef GetBridge(this TerrainGrid terrGrid, IntVec3 c) => TSOMod.Grids[terrGrid.map].GetBridgeAdvanced(c);
        public static TerrainDef GetBridgeNoNull(this TerrainGrid terrGrid, IntVec3 c) => TSOMod.Grids[terrGrid.map].GetBridgeAdvanced(c) ?? new TerrainDef();

        public static TerrainDef GetBridgeAdvanced(this AdvancedTerrainGrid grid, IntVec3 c) => grid.TerrainsAt(c)
            .FirstOrFallback(terr => terr.GetModExtension<TerrainExtension>().type == TerrainTypeDefOf.Bridge);

        public static void Place<T>(this IList<T> source, int idx, T item)
        {
            if (idx >= source.Count)
            {
                while (idx > source.Count) source.Add(default);

                source.Add(item);
            }
            else source.Insert(Mathf.Clamp(0, idx, source.Count - 1), item);
        }

        public static T Get<T>(this IList<T> source, int idx)
        {
            if (idx >= 0 && idx < source.Count) return source[idx];
            return default;
        }

        public static void ReplaceTerrain(this TerrainGrid grid, IntVec3 c, TerrainDef newTerr)
        {
            TSOMod.Grids[grid.map].ReplaceTerrain(c, newTerr);
        }
    }
}