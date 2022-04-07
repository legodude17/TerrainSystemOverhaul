using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TSO
{
    public class PatchSet_ModCompat : PatchSet
    {
        public PatchSet_ModCompat(Harmony harm)
        {
            if (ModLister.HasActiveModWithName("Simply More Bridges (Continued)"))
            {
                Log.Message("[TSO] Activating compatibility patch for: Simply More Bridges (Continued)");
                harm.Patch(AccessTools.Method(AccessTools.TypeByName("SimplyMoreBridges.SectionLayer_BridgeProps"), "ShouldDrawPropsBelow"),
                    transpiler: new HarmonyMethod(GetType(), nameof(ShouldDrawPropsBelow)));
            }

            if (ModLister.HasActiveModWithName("Vanilla Furniture Expanded - Architect"))
            {
                Log.Message("[TSO] Activating compatibility patch for: Vanilla Furniture Expanded - Architect");
                harm.Patch(AccessTools.Method(AccessTools.TypeByName("VFEArchitect.SectionLayer_CustomBridgeProps"), "ShouldDrawPropsBelow"), transpiler:
                    new HarmonyMethod(GetType(), nameof(ShouldDrawPropsBelow)));
                harm.Patch(AccessTools.Method(AccessTools.TypeByName("VFEArchitect.SectionLayer_CustomBridgeProps"), "Regenerate"), transpiler:
                    new HarmonyMethod(GetType(), nameof(ShouldDrawPropsBelow)));
            }

            if (ModLister.HasActiveModWithName("Soil Relocation Framework"))
            {
                Log.Message("[TSO] Activating compatibility patch for: Soil Relocation Framework");
                harm.Patch(AccessTools.Method(AccessTools.TypeByName("SR.JobDriver_Dig"), "DoEffect"), postfix: new HarmonyMethod(GetType(), nameof(SetHasRemoved)));
            }
        }

        public static IEnumerable<CodeInstruction> ShouldDrawPropsBelow(IEnumerable<CodeInstruction> instructions) =>
            instructions.MethodReplacer(AccessTools.Method(typeof(TerrainGrid), nameof(TerrainGrid.TerrainAt), new[] {typeof(IntVec3)}),
                AccessTools.Method(typeof(Utils), nameof(Utils.GetBridgeNoNull)));

        public static void SetHasRemoved(JobDriver __instance, IntVec3 c)
        {
            if (c.GetFirstThing<Blueprint_BuildTerrain>(__instance.Map) is {Mode: TerrainPlaceMode.Replace, HasRemovedBelow: false} bt) bt.HasRemovedBelow = true;
        }
    }

    [StaticConstructorOnStartup]
    public static class Unpatcher
    {
        static Unpatcher()
        {
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (ModLister.HasActiveModWithName("Simply More Bridges (Continued)"))
                {
                    Log.Message("[TSO] Activating compatibility unpatch for: Simply More Bridges (Continued)");
                    TSOMod.Harm.Unpatch(AccessTools.Method(typeof(GenConstruct), nameof(GenConstruct.CanPlaceBlueprintAt)), HarmonyPatchType.Postfix,
                        "rimworld.lanilor.simplymorebridges");
                }
            });
        }
    }
}