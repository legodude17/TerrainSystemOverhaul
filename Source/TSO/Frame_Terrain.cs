using RimWorld;
using Verse;

namespace TSO
{
    public class Frame_Terrain : Frame
    {
        public TerrainPlaceMode Mode;
        public TerrainDef Terrain => def.entityDefToBuild as TerrainDef;
        public override string GetInspectString() => $"{base.GetInspectString()}\n{"TSO.Mode".Translate()} {Mode.Translated()}";

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Mode, "mode");
        }
    }
}