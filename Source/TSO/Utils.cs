using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MonoMod.Utils;
using UnityEngine;
using Verse;

namespace TSO
{
    [StaticConstructorOnStartup]
    public static class Utils
    {
        private static readonly Cloner<TerrainDef> terrainDefCloner;

        static Utils()
        {
            var dm = new DynamicMethodDefinition("__Cloner__TerrainDef", typeof(void), new[] {typeof(TerrainDef), typeof(TerrainDef)});
            var gen = dm.GetILGenerator();
            foreach (var field in typeof(TerrainDef).GetFields(AccessTools.all))
            {
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, field);
                gen.Emit(OpCodes.Stfld, field);
            }

            gen.Emit(OpCodes.Ret);
            terrainDefCloner = dm.Generate().CreateDelegate<Cloner<TerrainDef>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Removable(this TerrainDef def) => def.Removable || SRFCompat.Active && def.IsDiggable();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Translated(this TerrainPlaceMode mode) => $"TSO.Mode.{mode}".Translate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TerrainDef Clone(this TerrainDef def)
        {
            var newDef = new TerrainDef();
            terrainDefCloner(def, newDef);
            return newDef;
        }

        public static IEnumerable<T> Combine<T>(params IEnumerable<T>[] sources)
        {
            var result = Enumerable.Empty<T>();
            for (var i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                if (source is not null) result = result.Concat(source);
            }

            return result;
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TerrainDef GetBridgeAdvanced(this AdvancedTerrainGrid grid, IntVec3 c) => grid.TerrainsAt(c)
            .FirstOrFallback(terr => terr.GetModExtension<TerrainExtension>().type == TerrainTypeDefOf.Bridge);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Place<T>(this IList<T> source, int idx, T item)
        {
            if (idx >= source.Count)
            {
                while (idx > source.Count) source.Add(default);

                source.Add(item);
            }
            else source.Insert(Mathf.Clamp(0, idx, source.Count - 1), item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Get<T>(this IList<T> source, int idx)
        {
            if (idx >= 0 && idx < source.Count) return source[idx];
            return default;
        }

        public static void ReplaceTerrain(this TerrainGrid grid, IntVec3 c, TerrainDef newTerr)
        {
            TSOMod.Grids[grid.map].ReplaceTerrain(c, newTerr);
        }

        private delegate void Cloner<in T>(T from, T to);
    }
}