﻿// MutagenicBuildup.cs modified by Iron Wolf for Pawnmorph on 08/29/2019 8:27 AM
// last updated 08/29/2019  8:27 AM


//using Multiplayer.API;
using Pawnmorph.Utilities;
using UnityEngine;
using Verse;

namespace Pawnmorph.Hediffs
{
    /// <summary>
    /// hediff type for mutagenic buildup 
    /// </summary>
    /// should add more and more mutations as severity increases, with a full tf at a severity of 1 
    /// <seealso cref="Pawnmorph.Hediff_Morph" />
    public class MutagenicBuildup : MorphTf
    {
        /// <summary>
        /// Tries the merge with the other hediff
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns></returns>
        public override bool TryMergeWith(Hediff other)
        {
            if (other is MutagenicBuildup buildup)
            {
                var sCompOther = other.TryGetComp<HediffComp_Single>();
                var sComp = SingleComp;
                if (sComp != null && sCompOther != null)
                {
                    sComp.stacks = Mathf.Clamp(sComp.stacks + sCompOther.stacks, 0, sComp.Props.maxStacks);
                }

                Severity += other.Severity; 
                foreach (HediffComp hediffComp in comps)
                {
                    hediffComp.CompPostMerged(other); 
                }

                ResetMutationOrder();
                return true;
            }

            return false; 
        }
    }

  
}