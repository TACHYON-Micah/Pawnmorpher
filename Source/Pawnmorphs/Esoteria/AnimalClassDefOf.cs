﻿// AnimalClassDefOf.cs modified by Iron Wolf for Pawnmorph on 01/10/2020 5:25 PM
// last updated 01/10/2020  5:25 PM

using JetBrains.Annotations;
using RimWorld;
#pragma warning disable 1591

// ReSharper disable NotNullMemberIsNotInitialized

namespace Pawnmorph
{
    /// <summary>
    ///     def of for animal classifications
    /// </summary>
    [DefOf]
    public static class AnimalClassDefOf
    {
        static AnimalClassDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AnimalClassDef));
        }

        [NotNull] [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public static AnimalClassDef Animal;
        [NotNull]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public static AnimalClassDef Canidae;
        [NotNull]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public static AnimalClassDef Canis;

        [NotNull]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public static AnimalClassDef Vulpini; 
    }
}