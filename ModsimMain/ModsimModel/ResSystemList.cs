using System;
using System.Collections;
using System.Collections.Generic;

namespace Csu.Modsim.ModsimModel
{
    internal class ResSystemList
    {
        private Model mi;
        private ReservoirSystem[] ResSystemArray;

        internal ResSystemList(Model mi)
        {
            this.mi = mi;
            BuildResSystemArray();
        }

        internal static bool ReservoirNodeHasOwners(Node n)
        {
            if (n.nodeType == NodeType.Reservoir && n.mnInfo.ownerType != DefineConstants.OLD_MODSIM_RES && n.mnInfo.ownerType != DefineConstants.PARENT_ACCOUNT_RES)
            {
                return true;
            }
            return false;
        }

        // Balance on the accrual date is different because of rent pool; we wish to distribute
        // "excess" to contribLast LAST; this is because we just put back any unused rent water,
        // we just shifted contribLastThisSeason to contribLast, and most importantly, we are
        // starting the next accrual season; so contribLast should be the last to fill
        internal void BalanceAccrualDate()
        {
            for (int i = 0; i < ResSystemArray.Length; i++)
            {
                ReservoirSystem rs = ResSystemArray[i];
                long sumStend = rs.SumStorageEnd();
                long sumStglft = rs.SumStorageLeft();
                if (sumStend == sumStglft)
                {
                    continue;
                }
                if (sumStend < sumStglft)
                {
                    //physical water is < paper water
                    // just reduce each stglft so sum is = physical water
                    rs.ReduceStglft2Stend(sumStglft, sumStend);
                }
                else
                {
                    // physical water is > paper water
                    if (!rs.HasRentLinks() || rs.SumContribLast() <= 0)
                    {
                        // no need to bother with contribLast stuff; do this just like nonaccrual date
                        rs.DistributeExcess2Space2Fill((sumStend - sumStglft));
                        continue;
                    }
                    // Alas, we have ownerships with outstanding contribLast; this complicates things
                    // On accrual date we are beginning the next accrual season; we have just put back any
                    //	any unused rent water and shifted contribLastThisSeason to contribLast.
                    // Unlike balance on nonaccrual dates we want to fill in any "excess" to contribLast LAST
                    // 1. Build a list of accrual links that have contracts with
                    // space = capown - stglft - contribLast > 0
                    //		We should not have to deal with rental stglft; we just put it back
                    // 2. Distribute (sumStend - sumStglft) to accrual links
                    // 3. for each accrual link, build the list of ownerships with
                    //  space = capown - stglft - contribLast > 0
                    // 4. Distribute accrual link space to each contract and update stglft
                    // 5. compute new sumStglft; if >= sumStend we are done
                    // 6. Still some excess sumStend; give it to contribLast space
                    // 7. build list of accrual links with contribLast
                    // 8. distribute Min(sumContribLast, (sumStend - sumStglft)) to accrual links
                    // 9. For each accrual link; build list of ownerlinks with contribLast
                    // 10. Distribute to each ownership link and update stglft and contribLast

                    rs.DistributeExcess2NonContribLast((sumStend - sumStglft));
                    sumStglft = rs.SumStorageLeft(); // new sum stglft
                    if (sumStend <= sumStglft) // done with this system
                    {
                        continue;
                    }
                    rs.DistributeExcess2ContribLast((sumStend - sumStglft));
                    sumStglft = rs.SumStorageLeft(); // new sum stglft

                    // check against convergence criteria
                    if (!Utils.NearlyEqual((double)sumStend, (double)sumStglft, mi.evap_cp))
                    {
                        mi.FireOnMessage(string.Format("System: {0}, Physical: {1}, Paper: {2}", rs.sysnum, sumStend, sumStglft));
                    }
                }
            }
        }

        internal void BalanceNonAccrualDate(DateTime dt)
        {
            for (long i = 0; i < ResSystemArray.Length; i++)
            {
                ReservoirSystem rs = ResSystemArray[i];

                if (GlobalMembersOperate.IsBalanceDate(mi, dt) || rs.sysnum == 0)
                {
                    long sumStend = rs.SumStorageEnd();
                    long sumStglft = rs.SumStorageLeft();
                    if (sumStend == sumStglft)
                    {
                        continue;
                    }
                    if (sumStend < sumStglft)
                    {
                        // physical water < paper water
                        // Easy; just reduce all stglft to = stend
                        rs.ReduceStglft2Stend(sumStglft, sumStend);
                    }
                    else
                    {
                        if (!rs.HasRentLinks())
                        {
                            // Fairly easy with no rent pool
                            // distribute excess to all ownerw
                            // we still need to distribute to each accrual link so we don't exceed lnkSeasonalCap
                            rs.DistributeExcess2Space2Fill(sumStend - sumStglft);
                            continue;
                        }
                        // With Rent Pool
                        // Three steps filling in space
                        //		1. For some reason we MAY have over restricted the last fill link; this balance
                        //			is taking place AFTER convergence, so anyone who was entitled to natural flow
                        //			should have gotten it. Therefore we will fill in any outstanding contribLast first
                        //		2. Next we will fill in any space that is unfilled and was NOT rented out
                        //		3. Finally, any space left SHOULD be space that was rented and the subscriber was
                        //			debited through use or charges like evap.  We are going to allow this space to
                        //			be backfilled to the contributor - we never "accrue" to a rent link
                        //	We cannot complete the distribution for all ownerships within the system at once.  If we
                        //    try to do this we will end up with over distribution to some accounts and the sum for
                        //    an accrual link will end up greater than the sum of contract amounts
                        // So for each of the three steps:
                        //      A. For each accrual link, compute the step space (sum of contribLast for example)
                        //      B. Get the sum of the step space for the system
                        //      C. Distribute the system step space to each accrual link
                        //      D. For each acrual link, Build the list of ownership links to recieve the extra water
                        // for that accrual link
                        //      E. DistributeProportional and UpdateStglftPlus

                        // Step 1 contribLast
                        rs.DistributeExcess2ContribLast(sumStend - sumStglft);
                        sumStglft = rs.SumStorageLeft();
                        if (sumStend <= sumStglft) // done with this system
                        {
                            continue;
                        }
                        // Step 2 fill in space not rented
                        rs.DistributeExcess2SpaceNotRented(sumStend - sumStglft);
                        sumStglft = rs.SumStorageLeft();
                        if (sumStend <= sumStglft) // done with this system
                        {
                            continue;
                        }
                        // step 3 fill in any space
                        rs.DistributeExcess2Space2Fill(sumStend - sumStglft);
                        sumStglft = rs.SumStorageLeft();

                        // check against convergence criteria
                        if (!Utils.NearlyEqual((double)sumStend, (double)sumStglft, mi.evap_cp))
                        {
                            mi.FireOnMessage(string.Format("System: {0}, Physical: {1}, Paper: {2}", rs.sysnum, sumStend, sumStglft));
                        }
                    }
                }
            }
        }

        private bool AllreadyHaveSystem(int sysNum, List<ReservoirSystem> sysList)
        {
            for (int i = 0; i < sysList.Count; i++)
            {
                if (sysList[i].sysnum == sysNum)
                {
                    return true;
                }
            }
            return false;
        }

        // walk through resList
        // check sysnum
        // if sysnum = 0 and owner links add new ReservoirSystem to list with this reservoir = ReservoirList
        // if sysnum > 0 check to see if we already have a ReservoirSystem with this number
        //      no add new ReservoirSystem with all reservoirs with this sysnum to ReservoirList
        //      yes continue to the next reservoir
        private void BuildResSystemArray()
        {
            ResSystemArray = new ReservoirSystem[0];
            List<ReservoirSystem> sysList = new List<ReservoirSystem>();

            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                if (!ReservoirNodeHasOwners(n))
                {
                    continue;
                }
                if (n.m.sysnum == 0)
                {
                    ReservoirSystem rSystem = new ReservoirSystem(n.m.sysnum);
                    sysList.Add(rSystem);
                    rSystem.ReservoirList = new Node[1];
                    rSystem.ReservoirList[0] = n;
                }
                else // nonzero sysnum
                {
                    if (AllreadyHaveSystem(n.m.sysnum, sysList) == false) // add new system
                    {
                        ReservoirSystem rSystem = new ReservoirSystem(n.m.sysnum);
                        sysList.Add(rSystem);
                        // add the ReservoirList to the ReservoirSystem
                        rSystem.AddReservoirs(n.m.sysnum, mi.mInfo.resList);
                    }
                }
                if (sysList.Count > 0)
                {
                    ResSystemArray = new ReservoirSystem[sysList.Count];
                    sysList.CopyTo(this.ResSystemArray);
                }
            }
        }

    }
}
