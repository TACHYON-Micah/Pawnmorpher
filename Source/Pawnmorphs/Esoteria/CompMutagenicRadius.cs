﻿using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pawnmorph.Hediffs;
using Pawnmorph.Utilities;
using Verse;
using RimWorld;
using UnityEngine;

namespace Pawnmorph
{
    /// <summary>
    /// comp for mutating things within a radius 
    /// </summary>
    public class CompMutagenicRadius : ThingComp
    {
        private const float LEAFLESS_PLANT_KILL_CHANCE = 0.09f;
        private const float MUTATE_IN_RADIUS_CHANCE = 0.50f;

        private int plantHarmAge;
        private int ticksToPlantHarm;
        
        CompProperties_MutagenicRadius PropsPlantHarmRadius
        {
            get
            {
                return (CompProperties_MutagenicRadius)props;
            }
        }
        /// <summary>
        /// call to save/load data 
        /// </summary>
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref plantHarmAge, "plantHarmAge", 0, false);
            Scribe_Values.Look(ref ticksToPlantHarm, "ticksToPlantHarm", 0, false);
        }

        [NotNull]
        private readonly List<Pawn> _pawnsCache = new List<Pawn>(); 


        /// <summary>
        /// called every tick after it's parent updates 
        /// </summary>
        public override void CompTick()
        {
            if (parent.IsHashIntervalTick(60))
            {
                if (!parent.Spawned)
                {
                    return;
                }
                plantHarmAge += 60;
                ticksToPlantHarm--;
                if (ticksToPlantHarm <= 0)
                {
                    float x = plantHarmAge / 60000f;
                    float num = PropsPlantHarmRadius.radiusPerDayCurve.Evaluate(x);
                    float num2 = Mathf.PI * num * num;
                    float num3 = num2 * PropsPlantHarmRadius.harmFrequencyPerArea;
                    float num4 = 60f / num3;
                    int num5;

                    if (num4 >= 1f)
                    {
                        ticksToPlantHarm = GenMath.RoundRandom(num4);
                        num5 = 1;
                    }
                    else
                    {
                        ticksToPlantHarm = 1;
                        num5 = GenMath.RoundRandom(1f / num4);
                    }

                    for (int i = 0; i < num5; i++)
                    {
                        MutateInRadius(num, PropsPlantHarmRadius.hediff);
                    }
                }
            }

            if (parent.IsHashIntervalTick(540))
            {
                _pawnsCache.Clear();
                float x = plantHarmAge / 60000f;
                float num = PropsPlantHarmRadius.radiusPerDayCurve.Evaluate(x) * Rand.Range(0.7f, 1f);
                var pawns = GenRadial.RadialDistinctThingsAround(parent.Position, parent.Map, num, true)
                                     .MakeSafe()
                                     .OfType<Pawn>()
                                     .Where(p => MutagenDefOf.defaultMutagen.CanInfect(p)); 
                _pawnsCache.AddRange(pawns);
            }


        }

        private void MutateInRadius(float radius, HediffDef hediff)
        {
            IntVec3 c = parent.Position + (Rand.InsideUnitCircleVec3 * radius).ToIntVec3();
            if (!c.InBounds(parent.Map))
            {
                return;
            }

            foreach (Pawn pawn in _pawnsCache)
            {
                if (pawn.Position.DistanceTo(parent.Position) < radius)
                    MutatePawn(pawn); 
            }
          
            

            Plant plant = c.GetPlant(parent.Map);

            if (plant != null && !plant.def.IsMutantPlant()) //don't harm mutant plants 
            {
                if (!plant.LeaflessNow)
                {
                    plant.MakeLeafless(Plant.LeaflessCause.Poison);
                }
                else if (Rand.Value < 0.3f) //30% chance
                {
                    PMPlantUtilities.TryMutatePlant(plant);
                }
            }


        }

        private static void MutatePawn(Pawn pawn)
        {
            if (pawn != null && MutagenDefOf.defaultMutagen.CanInfect(pawn))
            {
                if (Rand.Value < MUTATE_IN_RADIUS_CHANCE)
                {
                    var num = 0.028758334f/12;
                    num *= pawn.GetMutagenicBuildupMultiplier();
                    if (num != 0f)
                    {
                        float num2 = Mathf.Lerp(0.85f, 1.15f, Rand.ValueSeeded(pawn.thingIDNumber ^ 0x46EDC5D)); //should be ok
                        num *= num2; //what's the magic number? 
                        HealthUtility.AdjustSeverity(pawn, MorphTransformationDefOf.MutagenicBuildup, num);
                    }
                }

            }
        }
    }
}
