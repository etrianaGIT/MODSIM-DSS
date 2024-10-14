namespace Csu.Modsim.ModsimModel
{
    public static class GlobalMembersRestargbalance
    {
        public static void PrepReservoirTargetBalancingConstruct(Model mi)
        {
            LinkList ll;
            Link l;
            Node n;
            int i;
            /* RKL
            // Do we really need to do this?  If we set the artificial target storage link hi to zero,
            //  then these links could just have their hi unchanged
            //  All we need is the following UpdateReservoirTargetBalancingConstruct without the dependence
            // on iter and without the cost setting.
            RKL */
            //for(i = 0; i < mi->mInfo->resListLen; i++)
            for (i = 0; i < mi.mInfo.resList.Length; i++)
            {
                n = mi.mInfo.resList[i];
                if (n.m.resBalance != null && n.mnInfo.balanceLinks != null) //&& n->mnInfo->bPassL
                {
                    for (ll = n.mnInfo.balanceLinks; ll != null; ll = ll.next)
                    {
                        l = ll.link;
                        l.mlInfo.hi = 0;
                    }
                }
            }

            GlobalMembersRestargbalance.UpdateReservoirTargetBalancingConstruct(mi, 0);
        }
        //void UpdateReservoirTargetBalancingConstruct(Model *mi);
        public static void UpdateReservoirTargetBalancingConstruct(Model mi, int iter)
        {
            LinkList ll;
            Link l;
            Node n;
            int i;
            int j;
            long TargetOrCapacity;
            int numOf;
            for (i = 0; i < mi.mInfo.resList.Length; i++)
            {
                n = mi.mInfo.resList[i];
                if (n.m.resBalance != null && n.mnInfo.balanceLinks != null)
                {
                    if (iter % 2 == 1 || (iter % 2 == 0 && (n.mnInfo.ownerType != DefineConstants.CHILD_ACCOUNT_RES && n.mnInfo.ownerType != DefineConstants.NONCH_ACCOUNT_RES && n.mnInfo.ownerType != DefineConstants.ZEROSYS_ACCOUNT_RES)))
                    {
                        if (n.m.resBalance.PercentBasedOnMaxCapacity)
                        {
                            TargetOrCapacity = n.m.max_volume;
                        }
                        else
                        {
                            if (n.mnInfo.targetcontent.Length == 0)
                                throw new System.Exception(string.Concat("The reservoir node ", n.name, " does not have targets defined"));
                            long target = n.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
                            TargetOrCapacity = target;
                        }
                        numOf = n.m.resBalance.incrPriorities.Length;
                        double prevPercentage = 0.0;
                        for (j = 0, ll = n.mnInfo.balanceLinks; j < numOf && ll != null; j++, ll = ll.next)
                        {
                            l = ll.link;
                            if (j == 0)
                            {
                                if (n.m.min_volume > 0)
                                {
                                    //First layer is the minimum volume in the reservoir.
                                    l.mlInfo.hi = n.m.min_volume;
                                    prevPercentage = (double)n.m.min_volume / (double)TargetOrCapacity * 100;
                                }
                                else
                                {
                                    l.mlInfo.hi = (long)(TargetOrCapacity * n.m.resBalance.targetPercentages[j] / 100.0 + DefineConstants.ROFF);
                                    prevPercentage = n.m.resBalance.targetPercentages[j];
                                }

                            }
                            else
                            {
                                //Checks if the layer in the reservoir is below the minimum volume and set the layer to 0.
                                l.mlInfo.hi = (long)(TargetOrCapacity * (System.Math.Max(n.m.resBalance.targetPercentages[j] - prevPercentage, (double)0)) / 100.0 + DefineConstants.ROFF);
                                if (n.m.resBalance.targetPercentages[j] > prevPercentage)
                                    prevPercentage = n.m.resBalance.targetPercentages[j];
                            }
                            /* RKL
                            //  No translation desired
                            RKL */
                            l.mlInfo.cost = n.m.resBalance.incrPriorities[j];
                        }
                    }
                    else
                    {
                        // Only needed for account reservoirs in the NF step
                        if (n.mnInfo.ownerType != DefineConstants.CHILD_ACCOUNT_RES || n.mnInfo.ownerType != DefineConstants.NONCH_ACCOUNT_RES || n.mnInfo.ownerType != DefineConstants.ZEROSYS_ACCOUNT_RES)
                        {
                            numOf = n.m.resBalance.incrPriorities.Length;
                            for (j = 0, ll = n.mnInfo.balanceLinks; j < numOf && ll != null; j++, ll = ll.next)
                            {
                                l = ll.link;
                                l.mlInfo.cost = 0;
                                l.mlInfo.hi = mi.defaultMaxCap; // 99999999;
                            }
                        }
                    }
                }
            }
        }
    }
}
