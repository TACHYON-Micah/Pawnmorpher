﻿// MorphUtilities.cs modified by Iron Wolf for Pawnmorph on 08/02/2019 3:48 PM
// last updated 08/02/2019  3:48 PM

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pawnmorph.Hediffs;
using Pawnmorph.Hybrids;
using Pawnmorph.Utilities;
using RimWorld;
using Verse;
using Verse.Noise;

namespace Pawnmorph
{
    /// <summary>
    ///     Static collection of useful morph related functions. <br />
    ///     TransformerUtilities was getting a bit crowded.
    /// </summary>
    public static class MorphUtilities
    {
        /// <summary>
        ///     scalar used to make it easier for pawns to become hybrids
        /// </summary>
        [Obsolete] public const float HUMAN_CHANGE_FACTOR = 0.65f;

        /// <summary>the percent influence needed for a single morph to be selected to turn the pawn into, rather then a chimera</summary>
        public const float CHIMERA_THRESHOLD = 0.4f;

        /// <summary>
        ///     the percent human influence below which a pawn is 'no longer considered human'
        /// </summary>
        public const float MORPH_TF_THRESHOLD = 2f / 3f;

        
        private static Dictionary<HediffDef, List<MorphDef>> _morphAssociationCache =
            new Dictionary<HediffDef, List<MorphDef>>(); // So we don't calculate the associations more then we have to.

        private static float? _maxHumanInfluence;

        /// <summary> The maximum possible human influence. </summary>
        public static float MaxHumanInfluence
        {
            get
            {
                if (_maxHumanInfluence == null)
                {
                    HashSet<VTuple<BodyPartRecord, MutationLayer>> set = new HashSet<VTuple<BodyPartRecord, MutationLayer>>();
                    var enumerable = MutationDef.AllMutations.SelectMany(m => m.GetAllMutationSites(BodyDefOf.Human));
                    set.AddRange(enumerable);
                    _maxHumanInfluence = set.Count; 
                }

                return _maxHumanInfluence.Value;
            }
        }

        /// <summary>
        ///     Checks the race of this pawn. If the pawn is mutated enough it's race is changed to one of the hybrids
        /// </summary>
        /// <param name="pawn">The pawn.</param>
        /// <param name="addMissingMutations">if true, any missing mutations from the highest morph influence will be added</param>
        /// <exception cref="System.ArgumentNullException">pawn</exception>
        public static void CheckRace([NotNull] this Pawn pawn, bool addMissingMutations = true)
        {
            if (pawn == null) throw new ArgumentNullException(nameof(pawn));

            if (pawn.ShouldBeConsideredHuman()) return;

            MutationTracker mutTracker = pawn.GetMutationTracker();

            var hInfluence = mutTracker.HighestInfluence;

            if (hInfluence == null) return;
            float morphInfluence = mutTracker.GetDirectNormalizedInfluence(hInfluence);
            int morphInfluenceCount = mutTracker.Count();
            var isBelowChimeraThreshold = morphInfluence < CHIMERA_THRESHOLD && morphInfluenceCount > 1;

            MorphDef setMorph = GetMorphForPawn(pawn, isBelowChimeraThreshold, hInfluence, out MorphDef curMorph);

            if (curMorph != setMorph) RaceShiftUtilities.ChangePawnToMorph(pawn, setMorph, addMissingMutations);
        }

        private static MorphDef GetMorphForPawn(Pawn pawn, bool isBelowChimeraThreshold, AnimalClassBase hInfluence, out MorphDef curMorph)
        {
            MorphDef setMorph;
            curMorph = pawn.def.GetMorphOfRace();
            try
            {
                Rand.PushState(pawn.thingIDNumber); // make sure this is deterministic for each pawn 

                if (isBelowChimeraThreshold) //if they'er below turn them into a chimera 
                {
                    setMorph = GetChimeraRace(hInfluence);
                }
                else
                {
                    setMorph = hInfluence as MorphDef;
                    //if the highest influence isn't a morph just set it to a random morph in that class
                    setMorph = setMorph ?? ((AnimalClassDef) hInfluence).GetAllMorphsInClass().RandomElementWithFallback();
                }
            }
            finally
            {
                Rand.PopState();
            }

            return setMorph;
        }

        /// <summary>Gets all morphs.</summary>
        /// <value>All morphs.</value>
        public static IEnumerable<MorphDef> AllMorphs => DefDatabase<MorphDef>.AllDefs; 

        
        /// <summary> Gets all morphDefs associated with the given transformation. </summary>
        /// <param name="transformationDef"> The transformation definition. </param>
        [NotNull]
        public static IEnumerable<MorphDef> GetAssociatedMorph(HediffDef transformationDef)
        {
            if (_morphAssociationCache.TryGetValue(transformationDef, out List<MorphDef> lst)) return lst;

            lst = new List<MorphDef>();

            var enumerable = transformationDef.GetAllHediffGivers().Select(g => g.hediff).OfType<MutationDef>();

            foreach (MutationDef mutationDef in enumerable)
            {
                if (mutationDef.classInfluence is MorphDef morph)
                {
                    if (!lst.Contains(morph))
                    {
                        lst.Add(morph);
                    }
                }
            }
            _morphAssociationCache[transformationDef] = lst;
            return lst;
        }

        /// <summary> Gets the amount of influence a pawn has that's still human.</summary>
        /// <param name="pawn">the pawn</param>
        /// <param name="normalize"> Whether or not the resulting influence should be normalized between [0,1] </param>
        /// <returns></returns>
        public static float GetHumanInfluence([NotNull] this Pawn pawn, bool normalize = false)
        {
            var mutatedRecords = new HashSet<BodyPartRecord>();

            foreach (Hediff_AddedMutation hediffAddedMutation in pawn.health.hediffSet.hediffs.OfType<Hediff_AddedMutation>())
                mutatedRecords.Add(hediffAddedMutation.Part);
            var hInfluence = MaxHumanInfluence - mutatedRecords.Count;
            if (normalize) hInfluence /= MaxHumanInfluence;
            return hInfluence; 
        }


        /// <summary> Gets the type of the transformation. </summary>
        /// <param name="inst"> The instance. </param>
        /// <returns> The type of the transformation. </returns>
        /// <exception cref="ArgumentNullException"> inst is null </exception>
        public static MorphTransformationTypes GetTransformationType([NotNull] this HediffDef inst)
        {
            if (inst == null) throw new ArgumentNullException(nameof(inst));
            if (!typeof(Hediff_Morph).IsAssignableFrom(inst.hediffClass)) return 0;

            var comp = inst.CompProps<HediffCompProperties_Single>();
            return comp == null ? MorphTransformationTypes.Full : MorphTransformationTypes.Partial;
        }


        /// <summary>checks if the hybrid system is enabled for the given race def.</summary>
        /// <param name="raceDef">The race definition.</param>
        /// <returns></returns>
        public static bool HybridsAreEnabledFor(ThingDef raceDef)
        {
            if (raceDef == ThingDefOf.Human) return true;
            return raceDef.IsHybridRace();
        }

        /// <summary>
        ///     Determines whether this instance is a chimera morph.
        /// </summary>
        /// <param name="morphDef">The morph definition.</param>
        /// <returns>
        ///     <c>true</c> if this instance is a chimera morph; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsChimeraMorph([NotNull] this MorphDef morphDef)
        {
            return MorphCategoryDefOf.Chimera.AllMorphsInCategories.Contains(morphDef);
        }


        /// <summary>
        ///     Determines whether this pawn is a hybrid race.
        /// </summary>
        /// <param name="pawn">The pawn.</param>
        /// <returns>
        ///     <c>true</c> if this pawn is a hybrid race ; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">pawn</exception>
        public static bool IsHybridRace([NotNull] this Pawn pawn)
        {
            if (pawn == null) throw new ArgumentNullException(nameof(pawn));
            foreach (MorphDef morphDef in DefDatabase<MorphDef>.AllDefs)
                if (pawn.def == morphDef.hybridRaceDef)
                    return true;

            return false;
        }

        /// <summary>
        ///     Determines whether this instance is hybrid race.
        /// </summary>
        /// <param name="raceDef">The race definition.</param>
        /// <returns>
        ///     <c>true</c> if this instance is a hybrid race; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsHybridRace([NotNull] this ThingDef raceDef)
        {
            foreach (MorphDef morphDef in DefDatabase<MorphDef>.AllDefs)
                if (raceDef == morphDef.hybridRaceDef)
                    return true;

            return false;
        }

        /// <summary> Get whether or not the given pawn should still be considered 'human'. </summary>
        public static bool ShouldBeConsideredHuman([NotNull] this Pawn pawn)
        {
            if (pawn == null) throw new ArgumentNullException(nameof(pawn));
            if (pawn.health?.hediffSet?.hediffs == null) return true;

            MutationTracker tracker = pawn.GetMutationTracker();
            if (tracker == null) return true;


            float humanInfluence = GetHumanInfluence(pawn, true);

            return humanInfluence > MORPH_TF_THRESHOLD;
        }

        /// <summary> Get all morphs defs associated with this transformation hediff def. </summary>
        /// <param name="transformationDef"> The transformation definition. </param>
        [Obsolete]
        private static IEnumerable<MorphDef> GetAssociatedMorphInternal(
                HediffDef transformationDef) //might want to add it the hediff defs themselves rather then check at runtime 
        {
            IEnumerable<HediffGiver_Mutation> mutationsGiven =
                transformationDef.stages?.SelectMany(s => s.hediffGivers?.OfType<HediffGiver_Mutation>()
                                                       ?? Enumerable.Empty<HediffGiver_Mutation>())
             ?? Enumerable.Empty<HediffGiver_Mutation>(); //all mutations in the def 

            foreach (HediffGiver_Mutation hediffGiverMutation in mutationsGiven)
            {
                IEnumerable<CompProperties_MorphInfluence> comps =
                    hediffGiverMutation.hediff.comps?.OfType<CompProperties_MorphInfluence>();

                if (comps == null) continue;

                foreach (CompProperties_MorphInfluence morphInfluence in comps) yield return morphInfluence.morph;
            }
        }

        private static MorphDef GetChimeraRace(AnimalClassBase hInfluence)
        {
            var morph = hInfluence as MorphDef;
            //if the highest influence isn't a morph pick a random morph from the animal class
            morph = morph ?? ((AnimalClassDef) hInfluence).GetAllMorphsInClass().RandomElementWithFallback();
            if (morph.categories.Contains(MorphCategoryDefOf.Canid)) //TODO use the classes of these not the categories 
                return MorphDefOfs.ChaofoxMorph;
            if (morph.categories.Contains(MorphCategoryDefOf.Reptile))
                return MorphDefOfs.ChaodinoMorph;
            if (morph == MorphDefOfs.BoomalopeMorph) return MorphDefOfs.ChaoboomMorph;
            if (morph == MorphDefOfs.CowMorph) return MorphDefOfs.ChaocowMorph;

            return MorphCategoryDefOf.Chimera.AllMorphsInCategories.RandomElement();
            
    }

        /// <summary>
        ///     get the largest influence on this pawn
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        [CanBeNull]
        public static MorphDef GetHighestInfluence([NotNull] this Pawn pawn)
        {
            MutationTracker comp = pawn.GetMutationTracker();
            if (comp == null) return null;

            MorphDef highest = null;
            float max = float.NegativeInfinity;
            foreach (KeyValuePair<AnimalClassBase, float> keyValuePair in comp)
            {
                if (!(keyValuePair.Key is MorphDef morph)) continue;
                if (max < keyValuePair.Value)
                {
                    max = keyValuePair.Value;
                    highest = morph;
                }
            }

            return highest;
        }
    }
}