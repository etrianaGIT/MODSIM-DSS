using System;

namespace Csu.Modsim.ModsimModel
{
    internal class ReservoirSystem
    {
        internal Node[] ReservoirList;
        internal int sysnum;

        internal ReservoirSystem(int sysnumber)
        {
            this.sysnum = sysnumber;
        }

        internal void AddReservoirs(int sysnum, Node[] reslist)
        {
            if (sysnum <= 0)
            {
                Model.FireOnMessageGlobal("AddReservoirs is intended for sysnum > zero");
                return;
            }
            int numres = 0;
            for (int i = 0; i < reslist.Length; i++)
            {
                Node n = reslist[i];
                if (!ResSystemList.ReservoirNodeHasOwners(n))
                {
                    continue;
                }
                if (n.m.sysnum == sysnum)
                {
                    numres++;
                }
            }
            if (numres > 0)
            {
                this.ReservoirList = new Node[numres];
                int k = 0;
                for (int i = 0; i < reslist.Length; i++)
                {
                    Node n = reslist[i];
                    if (!ResSystemList.ReservoirNodeHasOwners(n))
                    {
                        continue;
                    }
                    if (n.m.sysnum == sysnum)
                    {
                        ReservoirList[k] = n;
                        k++;
                    }
                }
            }
        }

        internal long SumStorageEnd()
        {
            long sumStend = 0;
            for (int i = 0; i < ReservoirList.Length; i++)
            {
                sumStend += ReservoirList[i].mnInfo.stend-ReservoirList[i].m.min_volume;
            }
            return sumStend;
        }

        internal long SumStorageLeft()
        {
            long sumStglft = 0;
            for (int i = 0; i < ReservoirList.Length; i++)
            {
                Node n = ReservoirList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link;
                    if (l.IsAccrualLink)
                    {
                        for (LinkList ll2 = l.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                        {
                            Link l2 = ll2.link;
                            if (l2.mrlInfo.cap_own <= 0)
                            {
                                continue;
                            }
                            if (l2.mrlInfo.stglft < 0)
                            {
                                Console.WriteLine(string.Format(" Link {0} stglft {1}", l2.number, l2.mrlInfo.stglft));
                                continue;
                            }
                            if (l2.m.groupNumber == 0 || l2.mrlInfo.groupID > 0)
                            {
                                sumStglft += l2.mrlInfo.stglft;
                            }
                        }
                        for (LinkList ll2 = l.mlInfo.rLinkL; ll2 != null; ll2 = ll2.next)
                        {
                            Link l2 = ll2.link;
                            if (l2.mrlInfo.stglft < 0)
                            {
                                Console.WriteLine(string.Format(" Link {0} stglft {1}", l2.number, l2.mrlInfo.stglft));
                                continue;
                            }
                            sumStglft += l2.mrlInfo.stglft;
                        }
                    }
                }
            }
            return sumStglft;
        }

        private long SumSpaceNotRented()
        {
            long sumSpaceNotRented = 0;
            for (int i = 0; i < ReservoirList.Length; i++)
            {
                Node n = ReservoirList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link;
                    if (l.IsAccrualLink)
                    {
                        for (LinkList ll2 = l.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                        {
                            Link l2 = ll2.link;
                            if (l2.mrlInfo.cap_own <= 0)
                            {
                                continue;
                            }
                            if (l2.m.groupNumber == 0 || l2.mrlInfo.groupID > 0)
                            {
                                long space = l2.mrlInfo.cap_own - l2.mrlInfo.stglft - l2.mrlInfo.contribLast - l2.mrlInfo.contribLastThisSeason - l2.mrlInfo.contribRent;
                                if (space > 0)
                                {
                                    sumSpaceNotRented += space;
                                }
                            }
                        }
                    }
                }
            }
            return sumSpaceNotRented;
        }

        internal long SumContribLast()
        {
            long sumContribLast = 0;
            for (int i = 0; i < ReservoirList.Length; i++)
            {
                Node n = ReservoirList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link;
                    if (l.IsAccrualLink)
                    {
                        for (LinkList ll2 = l.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                        {
                            Link l2 = ll2.link;
                            if (l2.mrlInfo.cap_own <= 0 || l2.mrlInfo.contribLast <= 0)
                            {
                                continue;
                            }
                            if (l2.m.groupNumber == 0 || l2.mrlInfo.groupID > 0)
                            {
                                sumContribLast += l2.mrlInfo.contribLast;
                            }
                        }
                    }
                }
            }
            return sumContribLast;
        }

        private long SumNonContribLastSpace()
        {
            long sumNonContribLastSpace = 0;
            for (int i = 0; i < ReservoirList.Length; i++)
            {
                Node n = ReservoirList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link;
                    if (l.IsAccrualLink)
                    {
                        for (LinkList ll2 = l.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                        {
                            Link l2 = ll2.link;
                            if (l2.mrlInfo.cap_own <= 0 || l2.mrlInfo.contribLast <= 0)
                            {
                                continue;
                            }
                            if (l2.m.groupNumber == 0 || l2.mrlInfo.groupID > 0)
                            {
                                long space = l2.mrlInfo.cap_own - l2.mrlInfo.stglft - l2.mrlInfo.contribLast;
                                if (space > 0)
                                {
                                    sumNonContribLastSpace += space;
                                }
                            }
                        }
                    }
                }
            }
            return sumNonContribLastSpace;
        }

        private void CheckSums2Capown(diststr distListOwn)
        {
            for (diststr dptr = distListOwn; dptr != null; dptr = dptr.next)
            {
                Link l = dptr.referencePtr;
                if (l.mrlInfo.stglft > l.mrlInfo.cap_own)
                {
                    Console.WriteLine(string.Format("{0} stglft {1} > capown {2} truncating", l.name, l.mrlInfo.stglft, l.mrlInfo.cap_own));
                    l.mrlInfo.stglft = l.mrlInfo.cap_own;
                    l.mrlInfo.prevstglft = l.mrlInfo.cap_own;
                    l.mrlInfo.contribLast = 0;
                    l.mrlInfo.contribLastThisSeason = 0;
                    continue;
                }
                long space = l.mrlInfo.cap_own - l.mrlInfo.stglft - l.mrlInfo.contribLast - l.mrlInfo.contribLastThisSeason;
                if (space < 0) // we are overallocated
                {
                    if (l.mrlInfo.contribLast > 0)
                    {
                        if (Math.Abs(space) > l.mrlInfo.contribLast)
                        {
                            space += l.mrlInfo.contribLast;
                            l.mrlInfo.contribLast = 0;
                        }
                        else
                        {
                            l.mrlInfo.contribLast += space;
                            space = 0;
                            continue;
                        }
                    }
                }
                if (space < 0) // still overallocated
                {
                    if (l.mrlInfo.contribLastThisSeason > 0)
                    {
                        if (Math.Abs(space) > l.mrlInfo.contribLastThisSeason)
                        {
                            space += l.mrlInfo.contribLastThisSeason;
                            l.mrlInfo.contribLastThisSeason = 0;
                        }
                        else
                        {
                            l.mrlInfo.contribLastThisSeason += space;
                            space = 0;
                            continue;
                        }
                    }
                }
                if (space < 0) // I give up, we SHOULD never get to here
                {
                    Console.WriteLine(string.Format("{0} overallocated Capown {1} stglft {2} contribLast {3} contribLastTS {4}", l.name, l.mrlInfo.cap_own, l.mrlInfo.stglft, l.mrlInfo.contribLast, l.mrlInfo.contribLastThisSeason));
                }
            }
        }

        // build a distribution list of all owner and rent links that have stglft > 0 along with the
        // basis of proportion = capown / contribRent or contribLast
        private diststr BuildDistributionListAllStglft()
        {
            diststr distList = null; // distribution list of links
            for (int i = 0; i < ReservoirList.Length; i++)
            {
                Node n = ReservoirList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    if (!ll.link.IsAccrualLink)
                    {
                        continue;
                    }
                    Link l = ll.link;
                    for (LinkList ll2 = l.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                    {
                        Link l2 = ll2.link;
                        if (l2.m.groupNumber == 0 || l2.mrlInfo.groupID > 0)
                        {
                            GlobalMembersConstraint.fake_alloc_diststr(ref distList, l2.mrlInfo.cap_own, 0, l2.mrlInfo.stglft);
                            distList.referencePtr = l2;
                            distList.biasFrac = 0.0;
                        }
                    }
                    for (LinkList ll2 = l.mlInfo.rLinkL; ll2 != null; ll2 = ll2.next)
                    {
                        Link l2 = ll2.link;
                        // we should never have BOTH contribRent and contribLast nonzero
                        // for rent links contribLastThisSeason is always zero
                        long hiConstraint = Math.Abs(l2.mrlInfo.contribRent) + Math.Abs(l2.mrlInfo.contribLast);
                        // On accrual date stglft should be zero because we put everthing back
                        if (l.mrlInfo.stglft > 0 && hiConstraint > 0)
                        {
                            GlobalMembersConstraint.fake_alloc_diststr(ref distList, hiConstraint, 0, l2.mrlInfo.stglft);
                            distList.referencePtr = l2;
                            distList.biasFrac = 0.0;
                        }
                    }
                }
            }
            return distList;
        }

        private diststr BuildDistributionListContribLast()
        {
            diststr distList = null;
            for (int i = 0; i < ReservoirList.Length; i++)
            {
                Node n = ReservoirList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    if (!ll.link.mlInfo.isAccrualLink)
                    {
                        continue;
                    }
                    // note we put only ownership links on the list - no rental links
                    for (LinkList ll2 = ll.link.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                    {
                        Link l = ll2.link;
                        if (l.mrlInfo.cap_own <= 0)
                        {
                            continue;
                        }
                        if (l.m.groupNumber == 0 || l.mrlInfo.groupID > 0)
                        {
                            if (l.mrlInfo.contribLast > 0)
                            {
                                GlobalMembersConstraint.fake_alloc_diststr(ref distList, l.mrlInfo.cap_own, 0, l.mrlInfo.contribLast);
                                distList.referencePtr = l;
                                distList.biasFrac = 0.0;
                            }
                        }
                    }
                }
            }
            return distList;
        }

        private diststr BuildDistributionListOwners()
        {
            diststr distList = null;
            for (int i = 0; i < ReservoirList.Length; i++)
            {
                Node n = ReservoirList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    if (!ll.link.mlInfo.isAccrualLink)
                    {
                        continue;
                    }
                    // note we put only ownership links on the list - no rental links
                    for (LinkList ll2 = ll.link.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                    {
                        Link l = ll2.link;
                        if (l.mrlInfo.cap_own <= 0)
                        {
                            continue;
                        }
                        if (l.m.groupNumber == 0 || l.mrlInfo.groupID > 0)
                        {
                            long space = l.mrlInfo.cap_own - l.mrlInfo.stglft;
                            if (space > 0)
                            {
                                GlobalMembersConstraint.fake_alloc_diststr(ref distList, l.mrlInfo.cap_own, 0, space);
                                distList.referencePtr = l;
                                distList.biasFrac = 0.0;
                            }
                        }
                    }
                }
            }
            return distList;
        }

        private void UpdateContribLast(diststr distList)
        {
            for (diststr dptr = distList; dptr != null; dptr = dptr.next)
            {
                Link l = dptr.referencePtr;
                l.mrlInfo.contribLast -= dptr.returnValWhole;
                if (l.mrlInfo.contribLast < 0)
                {
                    l.mrlInfo.contribLast = 0;
                }
            }
        }

        private void UpdateStorageLeftMinus(diststr distList)
        {
            for (diststr dptr = distList; dptr != null; dptr = dptr.next)
            {
                Link l = dptr.referencePtr;
                l.mrlInfo.stglft -= dptr.returnValWhole;
                if (l.mrlInfo.stglft < 0)
                {
                    l.mrlInfo.stglft = 0;
                }
                l.mrlInfo.prevstglft = l.mrlInfo.stglft;
            }
        }

        private void UpdateStorageLeftPlus(diststr distList)
        {
            for (diststr dptr = distList; dptr != null; dptr = dptr.next)
            {
                Link l = dptr.referencePtr;
                l.mrlInfo.stglft += dptr.returnValWhole;
                l.mrlInfo.own_accrual += dptr.returnValWhole;
                if (l.mrlInfo.stglft > l.mrlInfo.cap_own)
                {
                    l.mrlInfo.stglft = l.mrlInfo.cap_own;
                }
                l.mrlInfo.prevstglft = l.mrlInfo.stglft;
                if (l.mrlInfo.stglft > l.mrlInfo.own_accrual)
                {
                    l.mrlInfo.own_accrual = l.mrlInfo.stglft;
                }
                if (l.mrlInfo.own_accrual > l.mrlInfo.cap_own)
                {
                    l.mrlInfo.own_accrual = l.mrlInfo.cap_own;
                }
                l.mrlInfo.prevownacrul = l.mrlInfo.own_accrual;
            }
        }

        internal void ReduceStglft2Stend(long sumStglft, long sumStend)
        {
            //  This is pretty easy; we just build a list of all ownership and rent links
            //  and reduce them proportionately to sum the physical water
            //  On accrual date all rent water should be zero because we put it back
            diststr distList = BuildDistributionListAllStglft();
            if (distList != null)
            {
                long distribAmount = sumStglft - sumStend;
                GlobalMembersConstraint.DistributeProportional(distribAmount, 0, distList, true);
                UpdateStorageLeftMinus(distList);
            }
            distList = null;
        }

        private void ReportSystemUnfilledContracts(string msg)
        {
            long sumSystemStend = 0;
            long sumSystemOwnStglft = 0;
            long sumSystemRentStglft = 0;
            long sumSystemVol = 0;

            bool printOwner = false;
            bool printRent = false;

            Console.WriteLine(msg);
            Console.WriteLine("************************************");

            for (int i = 0; i < ReservoirList.Length; i++)
            {
                Node n = ReservoirList[i];
                Console.WriteLine(Node.GetLabel(NodeType.Reservoir));
                sumSystemStend += n.mnInfo.stend;
                sumSystemVol += n.m.max_volume;
                Console.WriteLine(string.Format(" {0} Vol {1} Stend {2} ", n.name, n.m.max_volume, n.mnInfo.stend));

                long sumResOwnStglft = 0;
                long sumResRentStglft = 0;

                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link;
                    if (l.IsAccrualLink)
                    {
                        Console.WriteLine("Accrual Link");
                        long sumAccrualLinkOwnStglft = 0;
                        long sumAccrualLinkRentStglft = 0;
                        Console.WriteLine(string.Format("  {0} SeasCap {1}", l.name, l.mrlInfo.lnkSeasStorageCap));
                        for (LinkList ll2 = l.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                        {
                            Link l2 = ll2.link;
                            if (l2.m.groupNumber == 0 || l2.mrlInfo.groupID > 0)
                            {
                                if (printOwner)
                                {
                                    Console.WriteLine(string.Format("   {0} Capown {1} Stglft {2}", l2.name, l2.mrlInfo.cap_own, l2.mrlInfo.stglft));
                                    Console.WriteLine(string.Format("      ContribRent {0} ContribLast {1} ContribLastTS {2}", l2.mrlInfo.contribRent, l2.mrlInfo.contribLast, l2.mrlInfo.contribLastThisSeason));
                                }
                                sumResOwnStglft += l2.mrlInfo.stglft;
                                sumSystemOwnStglft += l2.mrlInfo.stglft;
                                sumAccrualLinkOwnStglft += l2.mrlInfo.stglft;
                            }
                        }
                        Console.WriteLine(string.Format("    sumAccrualLinkOwnStglft {0}", sumAccrualLinkOwnStglft));
                        if (l.mlInfo.rLinkL != null)
                        {
                            if (printRent)
                            {
                                Console.WriteLine("Rent Links");
                            }
                            for (LinkList ll2 = l.mlInfo.rLinkL; ll2 != null; ll2 = ll2.next)
                            {
                                Link l2 = ll2.link;
                                if (printRent)
                                {
                                    Console.WriteLine(string.Format("   {0} Stglft {1}", l2.name, l2.mrlInfo.stglft));
                                }
                                sumResRentStglft += l2.mrlInfo.stglft;
                                sumSystemRentStglft += l2.mrlInfo.stglft;
                                sumAccrualLinkRentStglft += l2.mrlInfo.stglft;
                            }
                            Console.WriteLine(string.Format("    sumAccrualLinkRentStglft {0}", sumAccrualLinkRentStglft));
                        }
                        Console.WriteLine(string.Format(" Accrual Link {0} SeasCap {1}", l.name, l.mrlInfo.lnkSeasStorageCap));
                        Console.WriteLine(string.Format(" sumAccrualLinkTotalStglft {0}", sumAccrualLinkOwnStglft + sumAccrualLinkRentStglft));
                    }
                }
                Console.WriteLine(string.Format(" sumResOwnStglft {0} sumResRentStglft {1} sumResTotalStglft {2} Vol {3}", sumResOwnStglft, sumResRentStglft, sumResOwnStglft + sumResRentStglft, n.m.max_volume));
            }
            Console.WriteLine(string.Format(" sumSystemOwnStglft {0} sumSystemRentStglft {1}", sumSystemOwnStglft, sumSystemRentStglft));
            Console.WriteLine(string.Format(" sumSystemTotalStglft {0} sumSystemStend {1} sumSystemVol {2}", sumSystemOwnStglft + sumSystemRentStglft, sumSystemStend, sumSystemVol));
        }

        internal bool HasRentLinks()
        {
            for (int i = 0; i < ReservoirList.Length; i++)
            {
                Node n = ReservoirList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link;
                    if (!l.IsAccrualLink)
                    {
                        continue;
                    }
                    if (l.mlInfo.rLinkL != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // for nonaccrual balance dates where physical water > paper water; we fill contribLast first
        internal void DistributeExcess2ContribLast(long space2fill)
        {
            long systemContribLast = SumContribLast();
            if (systemContribLast <= 0 || space2fill <= 0)
            {
                return;
            }
            long distAmount = Math.Min(systemContribLast, space2fill);
            // build list of accrual links; each will have a sumContribLast as constraintHi
            diststr distListAcc = BuildDistributionListOfContribLastAccrualLinks();
            GlobalMembersConstraint.DistributeByPriority(distAmount, 0, distListAcc, true);
            //		DistributeProportional(distAmount, 0, distListAcc, 1);
            for (diststr dptr = distListAcc; dptr != null; dptr = dptr.next)
            {
                if (dptr.returnValWhole > 0)
                {
                    // build list of ownerships with contribLast > 0
                    diststr distListOwn = BuildDistributionListOfContribLastOwnerLinks(dptr.referencePtr);
                    if (distListOwn != null)
                    {
                        GlobalMembersConstraint.DistributeProportional(dptr.returnValWhole, 0, distListOwn, true);
                        // increase the stglft for each ownership link
                        UpdateStorageLeftPlus(distListOwn);
                        // decrease contribLast for each ownership link
                        UpdateContribLast(distListOwn);
                        // Sanity check for contribLastThisSeason ...
                        CheckSums2Capown(distListOwn);
                        distListOwn = null;
                    }
                }
            }
            distListAcc = null;
        }

        // build list of accrual links with contribLast space
        private diststr BuildDistributionListOfContribLastAccrualLinks()
        {
            diststr distList = null;
            for (int i = 0; i < ReservoirList.Length; i++)
            {
                Node n = ReservoirList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link;
                    if (!l.IsAccrualLink)
                    {
                        continue;
                    }
                    long sumspace = 0;
                    for (LinkList ll2 = l.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                    {
                        Link l2 = ll2.link;
                        if (l2.mrlInfo.cap_own <= 0)
                        {
                            continue;
                        }
                        if (l2.m.groupNumber == 0 || l2.mrlInfo.groupID > 0)
                        {
                            long space = l2.mrlInfo.contribLast;
                            if (space > 0)
                            {
                                sumspace += space;
                            }
                        }
                    }
                    if (sumspace > 0)
                    {
                        GlobalMembersConstraint.fake_alloc_diststr(ref distList, l.m.cost, 0, sumspace);
                        distList.referencePtr = l;
                        distList.biasFrac = 0.0;
                    }
                }
            }
            return distList;
        }

        //Build list of ownershup links under specified accrual link with contribLast > 0
        private diststr BuildDistributionListOfContribLastOwnerLinks(Link accrualLink)
        {
            diststr distList = null;
            for (LinkList ll = accrualLink.mlInfo.cLinkL; ll != null; ll = ll.next)
            {
                Link l = ll.link;
                if (l.mrlInfo.cap_own <= 0)
                {
                    continue;
                }
                if (l.m.groupNumber == 0 || l.mrlInfo.groupID > 0)
                {
                    long space = l.mrlInfo.contribLast;
                    if (space > 0)
                    {
                        GlobalMembersConstraint.fake_alloc_diststr(ref distList, space, 0, space);
                        distList.referencePtr = l;
                        distList.biasFrac = 0.0;
                    }
                }
            }
            return distList;
        }

        // where physical water > paper water,  distribute excess water to space not rented
        internal void DistributeExcess2SpaceNotRented(long space2fill)
        {
            long spaceNotRented = SumSpaceNotRented();
            long distAmount = Math.Min(spaceNotRented, space2fill);
            if (distAmount <= 0)
            {
                return;
            }
            // build the accrural link list; each will have a limit of space
            diststr distListAcc = BuildDistributionListOfSpaceNotRentedAccrualLinks();
            // find the distrtibution of space to each accrual link
            GlobalMembersConstraint.DistributeByPriority(space2fill, 0, distListAcc, true);
            //			DistributeProportional(distAmount, 0, distListAcc, 1);
            // Now for each accrual link with space, distribute to the owners
            for (diststr dptr = distListAcc; dptr != null; dptr = dptr.next)
            {
                if (dptr.returnValWhole > 0)
                {
                    // build list of owners with space under this accrual link
                    diststr distListOwn = BuildDistributionListOfSpaceNotRentedOwnerLinks(dptr.referencePtr);
                    if (distListOwn != null)
                    {
                        // find the accrual link space distribution to owners
                        GlobalMembersConstraint.DistributeProportional(dptr.returnValWhole, 0, distListOwn, true);
                        // give each owner it's share
                        UpdateStorageLeftPlus(distListOwn);
                        CheckSums2Capown(distListOwn);
                        distListOwn = null;
                    }
                }
            }
            distListAcc = null;
        }

        // build ist of accrual links that have unrented space available
        private diststr BuildDistributionListOfSpaceNotRentedAccrualLinks()
        {
            diststr distList = null;
            for (int i = 0; i < ReservoirList.Length; i++)
            {
                Node n = ReservoirList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link; // accrual link
                    if (!l.IsAccrualLink)
                    {
                        continue;
                    }
                    long sumspace = 0;
                    for (LinkList ll2 = l.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                    {
                        Link l2 = ll2.link; // ownership link
                        if (l2.mrlInfo.cap_own <= 0)
                        {
                            continue;
                        }
                        if (l2.m.groupNumber == 0 || l2.mrlInfo.groupID > 0)
                        {
                            long space = l2.mrlInfo.cap_own - l2.mrlInfo.stglft - l2.mrlInfo.contribLast - l2.mrlInfo.contribLastThisSeason - l2.mrlInfo.contribRent;
                            if (space > 0)
                            {
                                sumspace += space;
                            }
                        }
                    }
                    if (sumspace > 0)
                    {
                        GlobalMembersConstraint.fake_alloc_diststr(ref distList, l.m.cost, 0, sumspace);
                        distList.referencePtr = l;
                        distList.biasFrac = 0.0;
                    }
                }
            }
            return distList;
        }

        // build list of onwership links under specified accrual link that have space not rented
        private diststr BuildDistributionListOfSpaceNotRentedOwnerLinks(Link accrualLink)
        {
            diststr distList = null;
            for (LinkList ll = accrualLink.mlInfo.cLinkL; ll != null; ll = ll.next)
            {
                Link l = ll.link;
                if (l.mrlInfo.cap_own <= 0)
                {
                    continue;
                }
                if (l.m.groupNumber == 0 || l.mrlInfo.groupID > 0)
                {
                    long space = l.mrlInfo.cap_own - l.mrlInfo.stglft - l.mrlInfo.contribRent - l.mrlInfo.contribLast - l.mrlInfo.contribLastThisSeason;
                    if (space > 0)
                    {
                        //distribution basis is proprotional to cap_own
                        // should this be space?
                        GlobalMembersConstraint.fake_alloc_diststr(ref distList, l.mrlInfo.cap_own, 0, space);
                        distList.referencePtr = l;
                        distList.biasFrac = 0.0;
                    }
                }
            }
            return distList;
        }

        // Fill in any space (cap_own - stglft)
        // We need to make a list of accrual links; each has it's owners' space distributed
        // This is so we don't end up over distributing to some accrual links' owners
        internal void DistributeExcess2Space2Fill(long space2fill)
        {
            if (space2fill <= 0)
            {
                return;
            }
            // build the accrural link list; each will have a limit of space
            diststr distListAcc = BuildDistributionListOfSpace2FillAccrualLinks();
            // find the distrtibution of space to each accrual link
            GlobalMembersConstraint.DistributeByPriority(space2fill, 0, distListAcc, true);
            // Now for each accrual link with space, distribute to the owners
            for (diststr dptr = distListAcc; dptr != null; dptr = dptr.next)
            {
                if (dptr.returnValWhole > 0)
                {
                    // build list of owners with space under this accrual link
                    diststr distListOwn = BuildDistributionListOfSpace2FillOwnerLinks(dptr.referencePtr);
                    if (distListOwn != null)
                    {
                        // find the accrual link space distribution to owners
                        GlobalMembersConstraint.DistributeProportional(dptr.returnValWhole, 0, distListOwn, true);
                        // give each owner it's share
                        UpdateStorageLeftPlus(distListOwn);
                        CheckSums2Capown(distListOwn);
                        distListOwn = null;
                    }
                }
            }
            distListAcc = null;
        }

        // build the accrual link list that have owners that are not full
        // each accrual link will have the sum of space as a constraintHi
        private diststr BuildDistributionListOfSpace2FillAccrualLinks()
        {
            diststr distList = null;
            for (int i = 0; i < ReservoirList.Length; i++)
            {
                Node n = ReservoirList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link; // accrual link
                    if (!l.IsAccrualLink)
                    {
                        continue;
                    }
                    long sumspace = 0;
                    for (LinkList ll2 = l.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                    {
                        Link l2 = ll2.link; // owner link
                        if (l2.mrlInfo.cap_own <= 0)
                        {
                            continue;
                        }
                        if (l2.m.groupNumber == 0 || l2.mrlInfo.groupID > 0)
                        {
                            long space = l2.mrlInfo.cap_own - l2.mrlInfo.stglft;
                            if (space > 0)
                            {
                                sumspace += space;
                            }
                        }
                    }
                    if (l.mlInfo.rLinkL != null)
                    {
                        long sumRentStglft = 0;
                        for (LinkList ll2 = l.mlInfo.rLinkL; ll2 != null; ll2 = ll2.next)
                        {
                            Link l2 = ll2.link;
                            sumRentStglft += l2.mrlInfo.stglft;
                        }
                        sumspace -= sumRentStglft;
                    }
                    // sum of unfilled sapce is the basis for distribution and constraintHi
                    if (sumspace > 0)
                    {
                        // distribute by priority (cost)
                        GlobalMembersConstraint.fake_alloc_diststr(ref distList, l.m.cost, 0, sumspace);
                        distList.referencePtr = l;
                        distList.biasFrac = 0.0;
                    }
                }
            }
            return distList;
        }

        // build a list of ownership links under the specfied accrualLink that have unfilled space
        private diststr BuildDistributionListOfSpace2FillOwnerLinks(Link accrualLink)
        {
            diststr distList = null;
            for (LinkList ll = accrualLink.mlInfo.cLinkL; ll != null; ll = ll.next)
            {
                Link l = ll.link;
                if (l.mrlInfo.cap_own <= 0)
                {
                    continue;
                }
                if (l.m.groupNumber == 0 || l.mrlInfo.groupID > 0)
                {
                    long space = l.mrlInfo.cap_own - l.mrlInfo.stglft;
                    if (space > 0)
                    {
                        // distribution to each owner proportional to cap_own
                        GlobalMembersConstraint.fake_alloc_diststr(ref distList, space, 0, space);
                        distList.referencePtr = l;
                        distList.biasFrac = 0.0;
                    }
                }
            }
            return distList;
        }

        internal void DistributeExcess2NonContribLast(long space2fill)
        {
            if (space2fill <= 0)
            {
                return;
            }
            // build the accrural link list; each will have a limit of space
            diststr distListAcc = BuildDistributionListOfAccrualLinksMinusContribLast();
            if (distListAcc == null) // no space to distribute
            {
                return;
            }
            long sumNonContribLast = SumNonContribLastSpace();
            long distAmount = Math.Min(space2fill, sumNonContribLast);
            // find the distrtibution of space to each accrual link
            GlobalMembersConstraint.DistributeByPriority(distAmount, 0, distListAcc, true);
            // Now for each accrual link with space, distribute to the owners
            for (diststr dptr = distListAcc; dptr != null; dptr = dptr.next)
            {
                if (dptr.returnValWhole > 0)
                {
                    // build list of owners with space under this accrual link
                    diststr distListOwn = BuildDistributionListOfOwnerLinksMinusContribLast(dptr.referencePtr);
                    if (distListOwn != null)
                    {
                        // find the accrual link space distribution to owners
                        GlobalMembersConstraint.DistributeProportional(dptr.returnValWhole, 0, distListOwn, true);
                        // give each owner it's share
                        UpdateStorageLeftPlus(distListOwn);
                        CheckSums2Capown(distListOwn);
                        distListOwn = null;
                    }
                }
            }
            distListAcc = null;
        }

        private diststr BuildDistributionListOfAccrualLinksMinusContribLast()
        {
            diststr distList = null;
            for (int i = 0; i < ReservoirList.Length; i++)
            {
                Node n = ReservoirList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link; // accrual link
                    if (!l.IsAccrualLink)
                    {
                        continue;
                    }
                    long sumspace = 0;
                    for (LinkList ll2 = l.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                    {
                        Link l2 = ll2.link; // owner link
                        if (l2.mrlInfo.cap_own <= 0)
                        {
                            continue;
                        }
                        if (l2.m.groupNumber == 0 || l2.mrlInfo.groupID > 0)
                        {
                            long space = l2.mrlInfo.cap_own - l2.mrlInfo.stglft - l2.mrlInfo.contribLast;
                            if (space > 0)
                            {
                                sumspace += space;
                            }
                        }
                    }
                    // we should not have to deal with rent stglft, but let's make sure
                    //  in case we use this routine in some other way
                    if (l.mlInfo.rLinkL != null)
                    {
                        long sumRentStglft = 0;
                        for (LinkList ll2 = l.mlInfo.rLinkL; ll2 != null; ll2 = ll2.next)
                        {
                            Link l2 = ll2.link;
                            sumRentStglft += l2.mrlInfo.stglft;
                        }
                        sumspace -= sumRentStglft;
                    }
                    // sun of unfilled space = constraintHi, basis is cost
                    if (sumspace > 0)
                    {
                        GlobalMembersConstraint.fake_alloc_diststr(ref distList, l.m.cost, 0, sumspace);
                        distList.referencePtr = l;
                        distList.biasFrac = 0.0;
                    }
                }
            }
            return distList;
        }

        private diststr BuildDistributionListOfOwnerLinksMinusContribLast(Link accrualLink)
        {
            diststr distList = null;
            for (LinkList ll = accrualLink.mlInfo.cLinkL; ll != null; ll = ll.next)
            {
                Link l = ll.link;
                if (l.mrlInfo.cap_own <= 0)
                {
                    continue;
                }
                if (l.m.groupNumber == 0 || l.mrlInfo.groupID > 0)
                {
                    long space = l.mrlInfo.cap_own - l.mrlInfo.stglft - l.mrlInfo.contribLast;
                    if (space > 0)
                    {
                        // distribution to each owner proportional to cap_own
                        // should this be space?
                        GlobalMembersConstraint.fake_alloc_diststr(ref distList, l.mrlInfo.cap_own, 0, space);
                        distList.referencePtr = l;
                        distList.biasFrac = 0.0;
                    }
                }
            }
            return distList;
        }

    }
}
