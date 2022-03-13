using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TSO
{
    public class Designator_BuildTerrain : Designator_Build
    {
        public Designator_BuildTerrain(TerrainDef entDef) : base(entDef)
        {
        }

        public TerrainDef Terrain => entDef as TerrainDef;

        private static void GetTerrainMode(Action<TerrainPlaceMode> action, Predicate<TerrainPlaceMode> validator)
        {
            var list = new List<FloatMenuOption>();
            if (validator(TerrainPlaceMode.OnTop))
                list.Add(new FloatMenuOption("Place over", () => { action(TerrainPlaceMode.OnTop); }));

            if (validator(TerrainPlaceMode.Replace))
                list.Add(new FloatMenuOption("Replace", () => { action(TerrainPlaceMode.Replace); }));

            switch (list.Count)
            {
                case 0:
                    action(TerrainPlaceMode.None);
                    return;
                case 1:
                    list[0].action();
                    return;
                default:
                    Find.WindowStack.Add(new FloatMenu(list));
                    break;
            }
        }

        private void PlaceOn(IntVec3 c, TerrainPlaceMode mode)
        {
            if (mode == TerrainPlaceMode.None) return;
            if (DebugSettings.godMode || entDef.GetStatValueAbstract(StatDefOf.WorkToBuild, StuffDef) == 0f)
                switch (mode)
                {
                    case TerrainPlaceMode.OnTop:
                        TSOMod.Grids[Find.CurrentMap].SetTerrain(c, Terrain);
                        break;
                    case TerrainPlaceMode.Replace:
                        TSOMod.Grids[Find.CurrentMap].RemoveTopLayer(c);
                        TSOMod.Grids[Find.CurrentMap].SetTerrain(c, Terrain);
                        break;
                }
            else
            {
                GenSpawn.WipeExistingThings(c, placingRot, entDef.blueprintDef, Map, DestroyMode.Deconstruct);
                var blueprint = (Blueprint_BuildTerrain) GenConstruct.PlaceBlueprintForBuild_NewTemp(entDef, c, Map, placingRot, Faction.OfPlayer, StuffDef);
                blueprint.StyleSourcePrecept = sourcePrecept;
                blueprint.Mode = mode;
            }

            FleckMaker.ThrowMetaPuffs(GenAdj.OccupiedRect(c, placingRot, entDef.Size), Map);
            if (entDef.PlaceWorkers is null) return;
            foreach (var pw in entDef.PlaceWorkers)
                pw.PostPlace(Map, entDef, c, placingRot);
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            var a = CanPlaceAt(Terrain, c, Map, TerrainPlaceMode.Replace, DebugSettings.godMode);
            var b = CanPlaceAt(Terrain, c, Map, TerrainPlaceMode.OnTop, DebugSettings.godMode);
            if (a.Accepted || b.Accepted) return true;
            return new AcceptanceReport(a.Reason ?? b.Reason);
        }

        private static AcceptanceReport CanPlaceAt(TerrainDef terrain, IntVec3 center, Map map, TerrainPlaceMode mode, bool godMode = false)
        {
            switch (mode)
            {
                case TerrainPlaceMode.OnTop:
                    return GenConstruct.CanPlaceBlueprintAt(terrain, center, Rot4.North, map, godMode);
                case TerrainPlaceMode.Replace:
                    var list = TSOMod.Grids[map].TerrainsAt(center).ToList();
                    if (list.Count == 1) return new AcceptanceReport("Cannot replace bottom type.");
                    var a = GenConstruct.CanPlaceBlueprintAt(terrain, center, Rot4.North, map, godMode);
                    var affordance = terrain.terrainAffordanceNeeded;
                    if (a.Accepted || affordance is null && a.Reason == "TerrainCannotSupport".Translate(terrain).CapitalizeFirst() || affordance is not null && a.Reason ==
                        "TerrainCannotSupport_TerrainAffordance".Translate(terrain, affordance).CapitalizeFirst())
                    {
                        var existing = list[list.Count - 2];
                        if (!existing.changeable) return new AcceptanceReport("TerrainCannotSupport".Translate(terrain).CapitalizeFirst());
                        if (affordance is null || existing.affordances.Contains(affordance))
                            return true;
                        return new AcceptanceReport("TerrainCannotSupport_TerrainAffordance".Translate(terrain, affordance)
                            .CapitalizeFirst());
                    }

                    return a;
            }

            return false;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            if (TutorSystem.TutorialMode && !TutorSystem.AllowAction(new EventPack(TutorTagDesignate, c))) return;
            var grid = TSOMod.Grids[Find.CurrentMap];
            var existing = grid.TerrainAt(c);
            GetTerrainMode(mode => PlaceOn(c, mode), mode => mode switch
            {
                TerrainPlaceMode.OnTop => CanPlaceAt(Terrain, c, Map, TerrainPlaceMode.OnTop, DebugSettings.godMode),
                TerrainPlaceMode.Replace => CanPlaceAt(Terrain, c, Map, TerrainPlaceMode.Replace, DebugSettings.godMode) && existing != grid.UnderTerrainAt(c),
                _ => false
            });
            if (TutorSystem.TutorialMode) TutorSystem.Notify_Event(new EventPack(TutorTagDesignate, c));
        }

        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            var list = cells.ToList();
            if (TutorSystem.TutorialMode && !TutorSystem.AllowAction(new EventPack(TutorTagDesignate, list))) return;
            GetTerrainMode(mode =>
            {
                var somethingSucceeded = false;
                var flag = false;
                foreach (var intVec in list.Where(intVec => CanPlaceAt(Terrain, intVec, Map, mode)))
                {
                    PlaceOn(intVec, mode);
                    somethingSucceeded = true;
                    if (!flag) flag = ShowWarningForCell(intVec);
                }

                Finalize(somethingSucceeded);
            }, mode => list.Any(c => CanPlaceAt(Terrain, c, Map, mode, DebugSettings.godMode)));
            if (TutorSystem.TutorialMode) TutorSystem.Notify_Event(new EventPack(TutorTagDesignate, list));
        }
    }

    public enum TerrainPlaceMode
    {
        None,
        OnTop,
        Replace
    }
}