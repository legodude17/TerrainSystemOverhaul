using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TSO
{
    public class PatchSet_Jobs : PatchSet
    {
        public PatchSet_Jobs(Harmony harm)
        {
            harm.Patch(AccessTools.Method(typeof(WorkGiver_ConstructDeliverResources), nameof(WorkGiver_ConstructDeliverResources.ShouldRemoveExistingFloorFirst)),
                new HarmonyMethod(GetType(), nameof(ShouldRemoveExistingFloorFirst)));
            harm.Patch(AccessTools.Method(typeof(WorkGiver_ConstructDeliverResources), nameof(WorkGiver_ConstructDeliverResources.RemoveExistingFloorJob)),
                new HarmonyMethod(GetType(), nameof(RemoveExistingFloorJob)));
            harm.Patch(AccessTools.Method(typeof(JobDriver_RemoveFloor), nameof(JobDriver_RemoveFloor.DoEffect)), new HarmonyMethod(GetType(), nameof(DoEffect)));
            harm.Patch(AccessTools.Method(typeof(Job), nameof(Job.ExposeData)), postfix: new HarmonyMethod(GetType(), nameof(SaveToRemove)));
        }

        public static bool ShouldRemoveExistingFloorFirst(Pawn pawn, Blueprint blue, ref bool __result)
        {
            if (blue.def.entityDefToBuild is TerrainDef terrain)
                __result = TSOMod.Grids[pawn.Map].FirstBlockingTerrain(blue.Position, terrain) is {Removable: true};
            else __result = false;

            return false;
        }

        public static bool RemoveExistingFloorJob(Pawn pawn, Blueprint blue, ref Job __result)
        {
            if (WorkGiver_ConstructDeliverResources.ShouldRemoveExistingFloorFirst(pawn, blue) && pawn.CanReserve(blue.Position, 1, -1, ReservationLayerDefOf.Floor))
            {
                var job = JobMaker.MakeJob(JobDefOf.RemoveFloor, blue.Position);
                job.ignoreDesignations = true;
                TSOMod.ToRemove.Add(job, TSOMod.Grids[pawn.Map].FirstBlockingTerrain(blue.Position, blue.def.entityDefToBuild as TerrainDef));
                __result = job;
            }
            else __result = null;

            return false;
        }

        public static bool DoEffect(JobDriver_RemoveFloor __instance, IntVec3 c)
        {
            if (!TSOMod.ToRemove.TryGetValue(__instance.job, out var toRemove)) return true;
            TSOMod.Grids[__instance.Map].RemoveTerrain(c, toRemove);
            return false;
        }

        public static void SaveToRemove(Job __instance)
        {
            if (!TSOMod.ToRemove.TryGetValue(__instance, out var toRemove)) toRemove = null;
            Scribe_Defs.Look(ref toRemove, "toRemove");
            if (Scribe.mode == LoadSaveMode.LoadingVars && toRemove is not null) TSOMod.ToRemove.Add(__instance, toRemove);
        }
    }
}