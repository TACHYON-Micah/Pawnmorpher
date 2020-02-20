﻿// FloatMenuMakerMapPatches.cs modified by Iron Wolf for Pawnmorph on //2019 
// last updated 08/25/2019  7:11 PM

using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Pawnmorph
{
    internal static class FloatMenuMakerMapPatches
    {
        [HarmonyPatch(typeof(FloatMenuMakerMap))]
        [HarmonyPatch("AddHumanlikeOrders")]
        internal static class AddHumanlikeOrdersPatch
        {
            [HarmonyPrefix]
            private static bool Prefix_AddHumanlikeOrders(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
            {
                if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                    foreach (LocalTargetInfo localTargetInfo3 in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn),
                                                                                 true))
                    {
                        LocalTargetInfo localTargetInfo4 = localTargetInfo3;
                        var victim = (Pawn) localTargetInfo4.Thing;
                        MutagenDef mutagen = MutagenDefOf.MergeMutagen;
                        if (mutagen.CanTransform(victim)
                         && pawn.CanReserveAndReach(victim, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true)
                         && Building_MutagenChamber.FindCryptosleepCasketFor(victim, pawn, true) != null)
                        {
                            string text4 = "CarryToChamber".Translate(localTargetInfo4.Thing.LabelCap, localTargetInfo4.Thing);
                            JobDef jDef = Mutagen_JobDefOf.CarryToMutagenChamber;
                            Action action3 = delegate
                            {
                                Building_MutagenChamber building_chamber =
                                    Building_MutagenChamber.FindCryptosleepCasketFor(victim, pawn);
                                if (building_chamber == null)
                                    building_chamber = Building_MutagenChamber.FindCryptosleepCasketFor(victim, pawn, true);
                                if (building_chamber == null)
                                {
                                    Messages.Message("CannotCarryToChamber".Translate() + ": " + "NoChamber".Translate(), victim,
                                                     MessageTypeDefOf.RejectInput, false);
                                    return;
                                }

                                var job = new Job(jDef, victim, building_chamber);
                                job.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job);
                            };
                            string label = text4;
                            Action action2 = action3;
                            Pawn revalidateClickTarget = victim;
                            opts.Add(FloatMenuUtility
                                        .DecoratePrioritizedTask(new FloatMenuOption(label, action2, MenuOptionPriority.Default, null, revalidateClickTarget),
                                                                 pawn, victim));
                        }
                    }

                return true;
            }
        }
#if true
        [HarmonyPatch(typeof(FloatMenuMakerMap))]
        [HarmonyPatch("CanTakeOrder")]
        internal static class CanTakeOrderPatch
        {
            [HarmonyPostfix]
            private static void MakePawnControllable(Pawn pawn, ref bool __result)
            {
                if (pawn?.Faction?.IsPlayer != true) return;

                if (!pawn.RaceProps.Animal) return;
                Hediff formerHuman = pawn.health.hediffSet.GetFirstHediffOfDef(TfHediffDefOf.TransformedHuman);
                if (formerHuman?.CurStageIndex != 2 || pawn.drafter == null) return;

                __result = true;
            }
        }
   
#endif


    }
}