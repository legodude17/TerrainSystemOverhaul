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

        public static TerrainDef GetBridge(this TerrainGrid terrGrid, IntVec3 c) => TSOMod.Grids[terrGrid.map].TerrainAtLayer(c, TerrainLayerDefOf.Bridge) ?? new TerrainDef();
    }
}