﻿// <copyright file="JobGiver_AwesomeInventory_FindApparels.cs" company="Zizhen Li">
// Copyright (c) Zizhen Li. All rights reserved.
// Licensed under the GPL-3.0-only license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Linq;
using AwesomeInventory.Jobs;
using RimWorld;
using Verse;
using Verse.AI;

namespace AwesomeInventory.Loadout
{
    /// <summary>
    /// Gives out a job if a proper apparel is found on the map.
    /// </summary>
    public class JobGiver_AwesomeInventory_FindApparels : ThinkNode_JobGiver
    {
        private JobGiver_FindItemByRadius _parent;

        /// <summary>
        /// Gives out a job if a proper apparel is found on the map.
        /// </summary>
        /// <param name="pawn"> The pawn in question. </param>
        /// <returns> A 9 to 5 job. </returns>
        protected override Job TryGiveJob(Pawn pawn)
        {
            ValidateArg.NotNull(pawn, nameof(pawn));
#if DEBUG
            Log.Message(pawn.Name + "Looking for apparels");
#endif

            CompAwesomeInventoryLoadout ailoadout = ((ThingWithComps)pawn).TryGetComp<CompAwesomeInventoryLoadout>();

            if (ailoadout == null || !ailoadout.NeedRestock)
                return null;

            if (_parent == null)
            {
                if (parent is JobGiver_FindItemByRadius p)
                {
                    _parent = p;
                }
                else
                {
                    throw new InvalidOperationException(ErrorText.WrongTypeParentThinkNode);
                }
            }

            foreach (ThingGroupSelector groupSelector in ailoadout.ItemsToRestock.Select(p => p.Key))
            {
                if (groupSelector.AllowedThing.IsApparel)
                {
                    Thing targetA =
                        _parent.FindItem(
                            pawn
                            , pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel)
                            , (thing) => groupSelector.Allows(thing, out _)
                                         &&
                                         !ailoadout.Loadout.IncludedInBlacklist(thing));

                    if (targetA != null)
                    {
                        Job job = new DressJob(AwesomeInventory_JobDefOf.AwesomeInventory_Dress, targetA, false);
                        if (pawn.Reserve(targetA, job, errorOnFailed: false))
                        {
                            return job;
                        }
                        else
                        {
                            JobMaker.ReturnToPool(job);
                            return null;
                        }
                    }
                }
            }

            return null;
        }
    }
}
