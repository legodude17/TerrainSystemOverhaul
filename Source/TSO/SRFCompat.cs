using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace TSO
{
    [StaticConstructorOnStartup]
    public static class SRFCompat
    {
        public static bool Active;

        static SRFCompat()
        {
            if (ModLister.HasActiveModWithName("Soil Relocation Framework")) Activate();
        }

        public static void Activate()
        {
            Log.Message("[TSO] Activating compatibility for: Soil Relocation Framework");
            Active = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDiggable(this TerrainDef def) => def.affordances.Contains(TerrainAffordanceDefOf.Diggable) && def.driesTo == null;
    }
}