using RimWorld;
using Verse;

namespace TSO
{
    public class Frame_Terrain : Frame
    {
        public TerrainPlaceMode Mode;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Mode, "mode");
        }
    }
}