using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TSO
{
    public class TSOMod : Mod
    {
        public static Harmony Harm;

        public static Dictionary<Map, AdvancedTerrainGrid> Grids = new();
        public static ConditionalWeakTable<Job, TerrainDef> ToRemove = new();
        private static readonly List<PatchSet> PATCHES = new();

        public TSOMod(ModContentPack content) : base(content)
        {
            Harm = new Harmony("legoduded17.tso");
            foreach (var type in typeof(PatchSet).AllSubclassesNonAbstract()) PATCHES.Add((PatchSet) Activator.CreateInstance(type, Harm));
        }
    }


    // ReSharper disable InconsistentNaming
    public class TerrainLayerDef : Def
    {
        public int order;

        public override IEnumerable<string> ConfigErrors()
        {
            label ??= defName.ToLower();
            description ??= label;
            return base.ConfigErrors();
        }
    }

    public class TerrainExtension : DefModExtension
    {
        public List<TerrainLayerDef> layers;

        public override IEnumerable<string> ConfigErrors()
        {
            if (layers.NullOrEmpty()) yield return "Must specify at least one layer";
            else if (layers.Any(layer => layer is null)) yield return "Must not have any null layers";
        }
    }

    [DefOf]
    public static class TerrainLayerDefOf
    {
        public static TerrainLayerDef Base;
        public static TerrainLayerDef Bridge;
        public static TerrainLayerDef Floor;
        public static TerrainLayerDef Carpet;

        static TerrainLayerDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TerrainLayerDefOf));
        }
    }

    public abstract class PatchSet
    {
    }
}