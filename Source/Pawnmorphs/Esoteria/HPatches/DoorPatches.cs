﻿// DoorPatches.cs created by Iron Wolf for Pawnmorph on 04/19/2020 6:59 PM
// last updated 04/19/2020  6:59 PM

using HarmonyLib;
using RimWorld;
using Verse;

namespace Pawnmorph.HPatches
{
    [HarmonyPatch(typeof(Building_Door))]
    static class DoorPatches
    {

        [HarmonyPatch(nameof(Building_Door.PawnCanOpen)), HarmonyPostfix]
        static void FixFormerHumanDoorPatch(ref bool __result, Pawn p, Building_Door __instance)
        {
            if (__result)
            {
                if (p.Faction != __instance.Faction)
                {
                    if (p.IsFormerHuman())
                    {
                        __result = false;
                    }
                } 
            }

            return; 
        }
    }
}