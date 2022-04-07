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
            harm.Patch(AccessTools.Method(typeof(JobDriver_RemoveFloor), nameof(JobDriver_RemoveFloor.DoEffect)), new HarmonyMethod(GetType(), nameof(DoEffect)));
            harm.Patch(AccessTools.Method(typeof(Job), nameof(Job.ExposeData)), postfix: new HarmonyMethod(GetType(), nameof(SaveToRemove)));
            harm.Patch(AccessTools.Method(typeof(WorkGiver_ConstructDeliverResources), nameof(WorkGiver_ConstructDeliverResources.ShouldRemoveExistingFloorFirst)),
                new HarmonyMethod(GetType(), nameof(ShouldRemoveExistingFloorFirst)));
            harm.Patch(AccessTools.Method(typeof(WorkGiver_ConstructDeliverResources), nameof(WorkGiver_ConstructDeliverResources.RemoveExistingFloorJob)),
                new HarmonyMethod(GetType(), nameof(RemoveExistingFloorJob)));
        }

        public static bool DoEffect(JobDriver_RemoveFloor __instance, IntVec3 c)
        {
            if (c.GetFirstThing<Blueprint_BuildTerrain>(__instance.Map) is {Mode: TerrainPlaceMode.Replace, HasRemovedBelow: false} bt) bt.HasRemovedBelow = true;
            if (!TSOMod.JobInfo.TryGetValue(__instance.job, out var extendedJobInfo) || extendedJobInfo.ToRemove is null) return true;
            TSOMod.Grids[__instance.Map].RemoveTerrain(c, extendedJobInfo.ToRemove);
            return false;
        }

        public static void SaveToRemove(Job __instance)
        {
            if (!TSOMod.JobInfo.TryGetValue(__instance, out var extendedJobInfo)) extendedJobInfo = null;
            Scribe_Deep.Look(ref extendedJobInfo, "tso_extendedInfo");
            if (Scribe.mode == LoadSaveMode.LoadingVars && extendedJobInfo is not null) TSOMod.JobInfo.Add(__instance, extendedJobInfo);
        }

        public static bool ShouldRemoveExistingFloorFirst(Pawn pawn, Blueprint blue, ref bool __result)
        {
            if (blue is not Blueprint_BuildTerrain bt) return true;
            __result = bt.Mode == TerrainPlaceMode.Replace && !bt.HasRemovedBelow && TSOMod.Grids[pawn.Map].CanRemoveTopLayerAt(blue.Position);
            return false;
        }

        public static bool RemoveExistingFloorJob(Pawn pawn, Blueprint blue, ref Job __result)
        {
            if (WorkGiver_ConstructDeliverResources.ShouldRemoveExistingFloorFirst(pawn, blue) && pawn.CanReserve(blue.Position, 1, -1, ReservationLayerDefOf.Floor))
            {
                var existing = TSOMod.Grids[pawn.Map].TerrainAt(blue.Position);
                if (existing.Removable)
                {
                    var job = JobMaker.MakeJob(JobDefOf.RemoveFloor, blue.Position);
                    job.ignoreDesignations = true;
                    __result = job;
                }
                else if (SRFCompat.Active)
                {
                    if (existing.IsDiggable())
                    {
                        var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("SR_Dig"), blue.Position);
                        job.ignoreDesignations = true;
                        __result = job;
                    }
                    else if (existing.IsWater && blue.def.entityDefToBuild is TerrainDef def && def.IsDiggable())
                        __result = null;
                    else
                    {
                        Log.Error($"[TSO] Tried to remove existing floor {existing} but it was not removable");
                        __result = null;
                    }
                }
                else
                {
                    Log.Error($"[TSO] Tried to remove existing floor {existing} but it was not removable");
                    __result = null;
                }
            }
            else __result = null;

            return false;
        }
    }
}