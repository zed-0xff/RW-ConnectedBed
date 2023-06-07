using System.Linq;
using RimWorld;
using Verse;

namespace zed_0xff.VNPE;

public class PlaceWorker_BedConnector: PlaceWorker {
    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
            Thing thingToIgnore = null, Thing thing = null) {

        Building edifice = loc.GetEdifice( map );
        if( !(edifice is Building_Bed || edifice is Building_Enterable) ){
            return "Must be placed over a bed or an enterable building.";
        }

        foreach (Thing thingHere in map.thingGrid.ThingsListAtFast(loc)){
            if( thingHere == thingToIgnore ) continue;

            if( thingHere.def.defName.Contains("BedConnector") || thingHere.def.defName == "VNPE_ConnectedBed" ){
                return "IdenticalThingExists".Translate();
            }

            if (thingHere.def.entityDefToBuild?.defName?.Contains("BedConnector") ?? false) {
                if (thingHere is Blueprint) {
                    return new AcceptanceReport("IdenticalBlueprintExists".Translate());
                }
                return new AcceptanceReport("IdenticalThingExists".Translate());
            }
        }

        return true;
    }
}
