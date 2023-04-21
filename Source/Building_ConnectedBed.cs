using System.Linq;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;
using VNPE;

// code copied from Vanilla Nutrient Paste Expanded (c) Oskar Potocki
// https://steamcommunity.com/sharedfiles/filedetails/?id=2920385763

namespace zed_0xff.VNPE
{
    public class Building_ConnectedBed : Building_Bed
    {
        public CompPowerTrader powerComp;
        public CompResource resourceComp;

        public override Color DrawColor => new Color32(127, 138, 108, 255);

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            resourceComp = GetComp<CompResource>();
            powerComp = GetComp<CompPowerTrader>();
        }

        public override void TickRare()
        {
            if (!powerComp.PowerOn)
                return;

            var net = resourceComp.PipeNet;
            var occupants = CurOccupants.ToList();
            for (int o = 0; o < occupants.Count; o++)
            {
               var occupant = occupants[o];
               if (occupant.needs.food.CurLevelPercentage <= 0.4)
               {
                  net.DrawAmongStorage(1, net.storages);
                  var meal = ThingMaker.MakeThing(ThingDefOf.MealNutrientPaste);
                  if (meal.TryGetComp<CompIngredients>() is CompIngredients ingredients)
                  {
                     for (int s = 0; s < net.storages.Count; s++)
                     {
                        var parent = net.storages[s].parent;
                        if (parent.TryGetComp<CompRegisterIngredients>() is CompRegisterIngredients storageIngredients)
                        {
                           for (int ig = 0; ig < storageIngredients.ingredients.Count; ig++)
                              ingredients.RegisterIngredient(storageIngredients.ingredients[ig]);
                        }
                     }
                  }

                  var ingestedNum = meal.Ingested(occupant, occupant.needs.food.NutritionWanted);
                  occupant.needs.food.CurLevel += ingestedNum;
                  occupant.records.AddTo(RecordDefOf.NutritionEaten, ingestedNum);
               }
            }
            base.TickRare();
        }
    }
}
