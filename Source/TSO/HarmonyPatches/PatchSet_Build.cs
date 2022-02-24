using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TSO
{
    public class PatchSet_Build : PatchSet
    {
        public PatchSet_Build(Harmony harm)
        {
            harm.Patch(AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewBlueprintDef_Terrain"), postfix: new HarmonyMethod(GetType(), nameof(FixBlueprintClass)));
            harm.Patch(AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewFrameDef_Terrain"), postfix: new HarmonyMethod(GetType(), nameof(FixFrameClass)));
            harm.Patch(AccessTools.Method(typeof(DesignationCategoryDef), nameof(DesignationCategoryDef.ResolveDesignators)),
                transpiler: new HarmonyMethod(GetType(), nameof(FixBuildDes)));
            harm.Patch(AccessTools.FirstMethod(typeof(DesignationCategoryDef), info => info.Name.Contains("get_AllIdeoDesignators") && info.Name.Contains("GetCachedDesignator")),
                transpiler: new HarmonyMethod(GetType(), nameof(FixBuildDes)));
            harm.Patch(AccessTools.FirstMethod(typeof(DesignationCategoryDef), info => info.Name.Contains("get_AllIdeoDesignators") && info.Name.Contains("GetCachedDropdown")),
                transpiler: new HarmonyMethod(GetType(), nameof(FixBuildDes)));
        }

        public static void FixBlueprintClass(ref ThingDef __result)
        {
            __result.thingClass = typeof(Blueprint_BuildTerrain);
        }

        public static void FixFrameClass(ref ThingDef __result)
        {
            __result.thingClass = typeof(Frame_Terrain);
        }

        public static IEnumerable<CodeInstruction> FixBuildDes(IEnumerable<CodeInstruction> instructions)
        {
            var desBuildCons = AccessTools.Constructor(typeof(Designator_Build), new[] {typeof(BuildableDef)});
            foreach (var instruction in instructions)
                if (instruction.opcode == OpCodes.Newobj && instruction.OperandIs(desBuildCons))
                    yield return CodeInstruction.Call(typeof(PatchSet_Build), nameof(MakeDesignator));
                else
                    yield return instruction;
        }

        public static Designator_Build MakeDesignator(BuildableDef entDef)
        {
            if (entDef is TerrainDef terrDef) return new Designator_BuildTerrain(terrDef);
            return new Designator_Build(entDef);
        }
    }
}