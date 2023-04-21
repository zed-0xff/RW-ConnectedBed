using System.Linq;
using System.Collections.Generic;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;
using VNPE;

// code partially copied from Vanilla Nutrient Paste Expanded (c) Oskar Potocki
// https://steamcommunity.com/sharedfiles/filedetails/?id=2920385763

namespace zed_0xff.VNPE
{
    public class Building_ConnectedBed : Building_Bed
    {
        public CompPowerTrader powerComp;
        public CompResource pasteComp = null;
        public CompResource hemogenComp = null;

        public override Color DrawColor => new Color32(127, 138, 108, 255);

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();

            foreach (CompResource comp in GetComps<CompResource>())
            {
//                if (ModLister.HasActiveModWithName("Vanilla Races Expanded - Sanguophage")){
                    if (comp?.Props?.pipeNet?.defName == "VRE_HemogenNet")
                    {
                        hemogenComp = comp;
                    }
//                }
//                if (ModLister.HasActiveModWithName("Vanilla Nutrient Paste Expanded")) {
                    if (comp?.Props?.pipeNet?.defName == "VNPE_NutrientPasteNet")
                    {
                        pasteComp = comp;
                    }
//                }
            }
        }

        private void FeedOccupant(){
            var net = pasteComp.PipeNet;
            if( net.Stored < 1 )
                return;

            var occupants = CurOccupants.ToList();
            for (int o = 0; o < occupants.Count; o++)
            {
               var pawn = occupants[o];
               if (pawn.needs.food.CurLevelPercentage <= 0.4)
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

                  var ingestedNum = meal.Ingested(pawn, pawn.needs.food.NutritionWanted);
                  pawn.needs.food.CurLevel += ingestedNum;
                  pawn.records.AddTo(RecordDefOf.NutritionEaten, ingestedNum);
               }
            }
        }

        private ref ConnectedBedSettings.TypeSettings GetSettingForPawn(Pawn pawn){
            if( pawn.IsPrisonerOfColony ){
                return ref ModConfig.Settings.prisoners;
            } else if ( pawn.IsSlaveOfColony ){
                return ref ModConfig.Settings.slaves;
            } else if( pawn.IsColonist ){
                return ref ModConfig.Settings.colonists;
            }
            return ref ModConfig.Settings.others;
        }

        private void TransfuseBlood(){
            var net = hemogenComp.PipeNet;
            if( net.Stored < 1 )
                return;

            float stored = net.Stored;

            var occupants = CurOccupants.ToList();
            for (int o = 0; o < occupants.Count; o++)
            {
               var pawn = occupants[o];
               var bloodLossHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
               if( bloodLossHediff == null) continue;

               var ts = GetSettingForPawn(pawn);

               if( bloodLossHediff.Severity < (1.0f - ts.transfuseIfLess) ) continue;
               
               while( bloodLossHediff.Severity > (1.0f - ts.fillUpTo) && stored >= 1 ){
                   if( ModConfig.Settings.general.debugLog ) Log.Message("[d] ConnectedBed: transfuse " + pawn + " " + ts.fillUpTo);
                   net.DrawAmongStorage(1, net.storages);
                   bloodLossHediff.Severity -= 0.35f; // see RimWorld/Recipe_BloodTransfusion.cs
                   if (pawn.genes?.GetFirstGeneOfType<Gene_Hemogen>() != null)
                   {
                       GeneUtility.OffsetHemogen(pawn, JobGiver_GetHemogen.HemogenPackHemogenGain);
                   }
                   stored -= 1;
               }

               if( stored < 1 ) break;
            }
        }

        private void DrawBlood(){
            if( !ModConfig.Settings.prisoners.draw ) return;

            var net = hemogenComp.PipeNet;

            var occupants = CurOccupants.ToList();
            for (int o = 0; o < occupants.Count; o++)
            {
               var pawn = occupants[o];
               if( !pawn.IsPrisonerOfColony ) continue;
               if( pawn.guest.interactionMode != PrisonerInteractionModeDefOf.HemogenFarm ) continue;
               if( !RecipeDefOf.ExtractHemogenPack.Worker.AvailableOnNow(pawn) ) continue;
               if( pawn.health.hediffSet.HasHediff(HediffDefOf.BloodLoss) ) continue;

               // should be created by Pawn_GuestTracker::GuestTrackerTick
               if( !pawn.BillStack.Bills.Any((Bill x) => x.recipe == RecipeDefOf.ExtractHemogenPack) ) continue;

               float cap = net.AvailableCapacity;
               if( cap > 1 ){
                   float fillRate = net.Stored / (net.Stored + cap);
                   if( fillRate >= ModConfig.Settings.general.maxFillRate )
                       return;

                   if( ModConfig.Settings.general.debugLog ) Log.Message("[d] ConnectedBed: draw " + pawn);

                   Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.BloodLoss, pawn);
                   hediff.Severity = 0.59f; // 0.6 pops up unwanted health alert
                   pawn.health.AddHediff(hediff);
                   net.DistributeAmongStorage(1);
                   if (IsViolationOnPawn(pawn, Faction.OfPlayer))
                   {
                       ReportViolation(pawn, pawn.HomeFaction, -1, HistoryEventDefOf.ExtractedHemogenPack);
                   }
               }

               if( net.storages.Any() ){
                   // remove pawn's all ExtractHemogenPack bills, if any
                   List<Bill> bills = pawn.BillStack.Bills.Where((Bill b) => b.recipe == RecipeDefOf.ExtractHemogenPack).ToList();
                   foreach (Bill b in bills) {
                       pawn.BillStack.Delete(b);
                   }
               } else {
                   // bed is not connected to the network, leave bill as is for manual extraction
               }
            }
        }

        bool IsViolationOnPawn(Pawn pawn, Faction billDoerFaction) {
            if (pawn.Faction == billDoerFaction && !pawn.IsQuestLodger())
            {
                return false;
            }
            return true;
        }

        void ReportViolation(Pawn pawn, Faction factionToInform, int goodwillImpact, HistoryEventDef overrideEventDef = null)
        {
            if (factionToInform != null )
            {
                Faction.OfPlayer.TryAffectGoodwillWith(factionToInform, goodwillImpact, canSendMessage: true, !factionToInform.temporary, overrideEventDef ?? HistoryEventDefOf.PerformedHarmfulSurgery);
                QuestUtility.SendQuestTargetSignals(pawn.questTags, "SurgeryViolation", pawn.Named("SUBJECT"));
            }
        }

        public override void TickRare()
        {
            if (!powerComp.PowerOn)
                return;

            if( pasteComp != null)
                FeedOccupant();

            if( hemogenComp != null){
                TransfuseBlood();
                DrawBlood();
            }

            base.TickRare();
        }
    }
}
