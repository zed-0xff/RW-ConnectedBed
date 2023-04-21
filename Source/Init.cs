using RimWorld;
using Verse;

namespace zed_0xff.VNPE
{
    [StaticConstructorOnStartup]
    public class Init
    {
        static Init()
        {
            var connected_bed = VThingDefOf.VNPE_ConnectedBed;
            if( connected_bed == null )
                return;

            if( connected_bed.GetCompProperties<CompProperties_AffectedByFacilities>() is CompProperties_AffectedByFacilities connected_props ){
                var hospital_bed = DefDatabase<ThingDef>.GetNamed("HospitalBed");
                if( hospital_bed != null
                        && hospital_bed.GetCompProperties<CompProperties_AffectedByFacilities>() is CompProperties_AffectedByFacilities hospital_props
                  ) {
                    // copy all linkables from hospital bed
                    foreach( ThingDef x in hospital_props.linkableFacilities ){
                        if ( !connected_props.linkableFacilities.Contains(x) ){
                            connected_props.linkableFacilities.Add(x);
                        }
                    }
                }

                // Unlink dripper from ConnectedBed
                var dripper = DefDatabase<ThingDef>.GetNamed("VNPE_NutrientPasteDripper");
                if( connected_props.linkableFacilities.Contains(dripper) ){
                    connected_props.linkableFacilities.Remove(dripper);
                }

            }
        }
    }
}
