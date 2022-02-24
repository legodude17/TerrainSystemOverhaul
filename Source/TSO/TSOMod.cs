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
        public static ConditionalWeakTable<Job, ExtendedJobInfo> JobInfo = new();
        private static readonly List<PatchSet> PATCHES = new();

        public TSOMod(ModContentPack content) : base(content)
        {
            Harm = new Harmony("legoduded17.tso");
            foreach (var type in typeof(PatchSet).AllSubclassesNonAbstract()) PATCHES.Add((PatchSet) Activator.CreateInstance(type, Harm));
        }

        public static void SetToRemove(Job job, TerrainDef toRemove)
        {
            if (JobInfo.TryGetValue(job, out var info)) info.ToRemove = toRemove;
            else JobInfo.Add(job, new ExtendedJobInfo {ToRemove = toRemove});
        }
    }

    public class ExtendedJobInfo
    {
        public TerrainDef ToRemove;
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
        public TerrainLayerDef layer;

        public override IEnumerable<string> ConfigErrors()
        {
            if (layer is null) yield return "Must not have null layer";
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