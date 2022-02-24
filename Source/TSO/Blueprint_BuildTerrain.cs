using RimWorld;
using Verse;

namespace TSO
{
    public class Blueprint_BuildTerrain : Blueprint_Build
    {
        public TerrainPlaceMode Mode;
        public TerrainDef Terrain => def.entityDefToBuild as TerrainDef;

        public override Thing MakeSolidThing()
        {
            var frame = (Frame_Terrain) base.MakeSolidThing();
            frame.Mode = Mode;
            return frame;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Mode, "mode");
        }
    }
}