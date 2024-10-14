using System;
using System.Collections.Generic;
using System.IO;

namespace Csu.Modsim.ModsimModel
{
    public static class GlobalMembersArcdump
    {

        /*          LINK BASED PARAMETERS:
         * nf - array accessed by link# containing the upstream node# 
         * nt - array accessed by link# containing the downstream node# 
         * lo - array accessed by link# containing the low bound in units of flow
         * flow - array accessed by link# containing the amount of flow from the
         * nl - number of links in this dataset
         *        last solution of relax
         * hi - array accessed by link# containing the high bound in units of flow
         * cost - array accessed by link# containing the unit cost
         *          RESERVOIR BASED PARAMETERS:
         * nreslist - array accessed by reservoir# that returns the node number
         * nodelist - array accessed by node# that returns reservoir number, 
         *            or zero if this node is not a reservoir
         * nres - number of reservoirs in this dataset
         *          DEMAND BASED PARAMETERS:
         * ndmd - array accessed by demand# that returns the node number
         * nd - number of demands in this dataset
         *          GENERAL PARAMETERS
         * nj - number of nodes in this dataset
         * timeStep (iprd) - 0 for monthly, 1 for weekly, 2 for daily, 4 for 10 days
         * mon - month number
         * l1,l2 - start/end of artificial inflow + initial storage links
         * l3,l4 - start/end of artificial desired storage arcs
         * l5,l6 - start/end of artificial final storage arcs
         * l7,l8 - start/end of artificial demand arcs
         * l9,l10 - start/end of artificial excess demand arcs
         * l11,l12 - start/end of artificial spill arcs
         * l13,l14 - start/end of gw accretion 
         * l15,l16 - start/end of gw depletion arcs
         *
         *        Wierd positions:
         *                l12+1,...,l12+4 are artificial mass balance arcs
         *                l16+1 and l16+2 define artificial gw mass balance arcs
         */

        /* file format:   for the "NDUMP" file.  Note that the NDUMP file
         *  is optional.
         * node #'s to dump 
         * EOF
         */

        internal static StreamWriter swDump = null;    // For the dump file
        internal static StreamWriter swArcDump = null; // For the arcdump file
        internal static string indumppath = "ndump.txt";
        internal static string outdumppath = "dumpfile.txt";
        internal static string arcdumppath = "arcdump.txt";
        internal static string namesPath = "LinksAndNodes.csv";
        internal static bool ignoreDump = false;
        internal static bool ignoreArcDump = false;

        // Link names file...
        public static void WriteNames(Model mi)
        {
            try
            {
                // Open the file...
                string dir = Path.GetDirectoryName(mi.fname) + @"\";
                if (dir.Equals(@"\")) dir = ""; 
                StreamWriter swNames = new StreamWriter(dir + namesPath);
                swNames.WriteLine("This file contains numbers and names for all links and nodes in the network " + Path.GetFileName(mi.fname) + " simulated on " + DateTime.Now.ToString());
                swNames.WriteLine();

                // Write link names 
                swNames.WriteLine("LINKS");
                swNames.WriteLine("Number,Name,From Node,To Node,User-defined Cost");
                for (Link l = mi.firstLink; l != null; l = l.next)
                {
                    string extra = "," + l.from.name + "," + l.to.name + "," + l.m.cost;
                    if (l.name == null)
                        swNames.WriteLine(l.number + ",NoName" + extra);
                    else
                        swNames.WriteLine(l.number + "," + l.name + extra);
                }
                // Write node names 
                swNames.WriteLine("NODES");
                swNames.WriteLine("Number,Name");
                for (Node n = mi.firstNode; n != null; n = n.next)
                    if (n.name == null)
                        swNames.WriteLine(n.number + ",NoName");
                    else
                        swNames.WriteLine(n.number + "," + n.name);

                // Close the file and exit.
                swNames.Close();
            }
            catch (Exception ex)
            {
                mi.FireOnError("Could not write to " + namesPath + "\n\n" + ex.Message + "\n" + ex.StackTrace + "\n");
            }
        }

        // Node Dump file...
        internal static int[] m_dumpnodes ;
        private static bool OpenDump(Model mi, out int[] dumpnodes)
        {
            dumpnodes = m_dumpnodes;// new int[0];
            if (!ignoreDump && swDump == null)
            {
                
                string dir = Path.GetDirectoryName(mi.fname) + @"\";
                if (dir.Equals(@"\")) dir = "";
                string fname = Path.GetFileNameWithoutExtension(mi.fname);
                if (File.Exists(dir + "NDUMP")) indumppath = dir + "NDUMP";
                else if (File.Exists(dir + "ndump.txt")) indumppath = dir + "ndump.txt";
                else if (File.Exists(dir + fname + ".nd")) indumppath = dir + fname + ".nd";
                else if (File.Exists(dir + fname + ".txt")) indumppath = dir + fname + ".txt";
                else
                {
                    ignoreDump = true;
                    return false;
                }

                // Read the nodes to place in the output.
                try
                {
                    int i, nodenum;
                    List<int> dnodes = new List<int>();
                    StreamReader sr = new StreamReader(indumppath);
                    for (i = 0; !sr.EndOfStream && i < DefineConstants.DNODES; i++)
                    {
                        if (int.TryParse(sr.ReadLine(), out nodenum))
                        {
                            if (nodenum <= 0)
                                break;
                            dnodes.Add(nodenum);
                        }
                    }
                    sr.Close();
                    dumpnodes = new int[dnodes.Count];
                    //dnodes.CopyTo(dumpnodes);
                    m_dumpnodes = new int[dnodes.Count];
                    //dnodes.CopyTo(m_dumpnodes);
                    for (int d = 0; d < dnodes.Count; d++)
                    {
                        for (int k = 1; k < mi.mInfo.nList.Length; k++)
                        {
                            if (mi.mInfo.nList[k].number == dnodes[d])
                            {
                                dumpnodes[d] = k;
                                m_dumpnodes[d] = k;
                                break;
                            }
                        }
                    }
                    
                    if (i >= DefineConstants.DNODES)
                        mi.FireOnError("maximum number of dump nodes exceeded - TRUNCATING LIST");
                    mi.FireOnMessage("ndumps = " + dumpnodes.Length.ToString());
                    for (i = 0; i < dumpnodes.Length; i++)
                        mi.FireOnMessage(string.Format("dnodes[{0}] = {1}", i, dumpnodes[i]));

                }
                catch (Exception ex)
                {
                    ignoreDump = true;
                    mi.FireOnError("Could not open " + indumppath + "\n\n" + ex.Message + "\n" + ex.StackTrace + "\n");
                    return false;
                }

                try
                {
                    swDump = new StreamWriter(dir + outdumppath);
                    WriteNames(mi);
                }
                catch (Exception ex)
                {
                    swDump = null;
                    ignoreDump = true;
                    mi.FireOnError("Could not open " + outdumppath + "\n\n" + ex.Message + "\n" + ex.StackTrace + "\n");
                    return false;
                }
            }
            return !ignoreDump;
        }
        public static void performDump(string label, Model mi)
        {
            performDump(label, mi, false);
        }
        public static void performDump(string label, Model mi, bool beforeSolution)
        {
            int[] dumpnodes;
            if (!OpenDump(mi, out dumpnodes))
                return;
            Node n = null;
            Link l = null;
            LinkList ll = null;
            int timeindex = mi.mInfo.CurrentModelTimeStepIndex;
            DateTime now = mi.TimeStepManager.Index2Date(timeindex, TypeIndexes.ModelIndex);

            int iteration = mi.mInfo.Iteration;
            if (label.Equals("second stg step"))
                iteration = iteration--;

            /* translated very literally...  should optimize a little later */
            /* Most people have come to depend on the sorted numbering on
             * the links.  This might be a problem because of ordering in the
             * linked lists.
             */
            swDump.WriteLine(string.Format("ModelTimeStepIndex {0}; iter {1}; {2}", timeindex, iteration, now.ToString(TimeManager.DateFormat)));
            for (int i = 0; i < dumpnodes.Length; i++)
            {
                n = ((dumpnodes[i] < mi.mInfo.nList.Length) ? mi.mInfo.nList[dumpnodes[i]] : null);
            DoItAgain:
                if (n != null)
                {
                    if (label != null)
                        swDump.WriteLine(label);
                    swDump.WriteLine("All arcs for Node " + n.number.ToString());
                    swDump.WriteLine("Link,From,To,Lower Bound,Current Flow,Upper Bound,Unit Cost,ModelTimeStepIndex,iter");

                    /* get all incoming links */
                    swDump.WriteLine("incoming links");
                    for (ll = n.InflowLinks; ll != null; ll = ll.next)
                    {
                        /* note that getLinkDumpType is pretty specific to the
                         * arc dump stuff.  There are possibilities that the link
                         * types are overlapping
                         */
                        l = ll.link;
                        if (l.to.number != n.number) // xy file numbering problem
                            swDump.WriteLine("Link " + l.number.ToString() + " on the wrong node - xyfile problem");
                        if (l.from == l.to && l.mrlInfo == null)
                            continue;

                        // Write the output
                        if (beforeSolution)
                            swDump.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", l.number, l.from.number, l.to.number, l.mlInfo.lo, "BeforeSolution", l.mlInfo.hi, l.mlInfo.cost,timeindex,iteration));
                        else
                            switch (GlobalMembersArcdump.getLinkDumpType(l))
                            {
                                case DefineConstants.REAL_LINK:
                                    swDump.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", l.number, l.from.number, l.to.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.INFLOW_LINK:
                                    swDump.WriteLine(string.Format("{0},INFL,{1},{2},{3},{4},{5},{6},{7}", l.number, l.to.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.TARGSTO_LINK:
                                    swDump.WriteLine(string.Format("{0},{1},TARGSTO,{2},{3},{4},{5},{6},{7}", l.number, l.from.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.EVAP_LINK:
                                    swDump.WriteLine(string.Format("{0},{1},EVAP,{2},{3},{4},{5},{6},{7}", l.number, l.from.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.FINALSTO_LINK:
                                    swDump.WriteLine(string.Format("{0},{1},FINSTO,{2},{3},{4},{5},{6},{7}", l.number, l.from.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.DEMAND_LINK:
                                    swDump.WriteLine(string.Format("{0},{1},DEMAND,{2},{3},{4},{5},{6},{7}", l.number, l.from.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.SPILL_LINK:
                                    swDump.WriteLine(string.Format("{0},{1},SPILL,{2},{3},{4},{5},{6},{7}", l.number, l.from.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.GWOUT_LINK:
                                    swDump.WriteLine(string.Format("{0},{1},GW,{2},{3},{4},{5},{6},{7}", l.number, l.from.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.GWIN_LINK:
                                    swDump.WriteLine(string.Format("{0},GW,{1},{2},{3},{4},{5},{6},{7}", l.number, l.to.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.FTRTN_LINK:
                                    swDump.WriteLine(string.Format("{0},{1},FTRTN,{2},{3},{4},{5},{6},{7}", l.number, l.from.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                default:
                                    mi.FireOnError("unknown link type in arcdump.c");
                                    mi.FireOnError(string.Concat("data type is  ", GlobalMembersArcdump.getLinkDumpType(l).ToString()));
                                    break;
                            }
                    }

                    /* get all outgoing links */
                    swDump.WriteLine("Outgoing Links");
                    for (ll = n.OutflowLinks; ll != null; ll = ll.next)
                    {
                        /* note that getLinkDumpType is pretty specific to the
                         * arc dump stuff.  There are possibilities that the link
                         * types are overlapping
                         */
                        l = ll.link;
                        if (l.from.number != n.number) // xy file numbering problem
                            swDump.WriteLine("Link " + l.number.ToString() + " on the wrong node - xyfile problem");
                        if (l.from == l.to && l.mrlInfo == null)
                            continue;

                        // Write the output
                        if (beforeSolution)
                            swDump.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", l.number, l.from.number, l.to.number, l.mlInfo.lo, "BeforeSolution", l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                        else
                            switch (GlobalMembersArcdump.getLinkDumpType(l))
                            {
                                case DefineConstants.REAL_LINK:
                                    swDump.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", l.number, l.from.number, l.to.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.INFLOW_LINK:
                                    swDump.WriteLine(string.Format("{0},INFL,{1},{2},{3},{4},{5},{6},{7}", l.number, l.to.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.TARGSTO_LINK:
                                    swDump.WriteLine(string.Format("{0},{1},TARGSTO,{2},{3},{4},{5},{6},{7}", l.number, l.from.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.EVAP_LINK:
                                    swDump.WriteLine(string.Format("{0},{1},EVAP,{2},{3},{4},{5},{6},{7}", l.number, l.from.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.FINALSTO_LINK:
                                    swDump.WriteLine(string.Format("{0},{1},FINSTO,{2},{3},{4},{5},{6},{7}", l.number, l.from.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.DEMAND_LINK:
                                    swDump.WriteLine(string.Format("{0},{1},DEMAND,{2},{3},{4},{5},{6},{7}", l.number, l.from.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.SPILL_LINK:
                                    swDump.WriteLine(string.Format("{0},{1},SPILL,{2},{3},{4},{5},{6},{7}", l.number, l.from.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.GWOUT_LINK:
                                    swDump.WriteLine(string.Format("{0},{1},GW,{2},{3},{4},{5},{6},{7}", l.number, l.from.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.GWIN_LINK:
                                    swDump.WriteLine(string.Format("{0},GW,{1},{2},{3},{4},{5},{6},{7}", l.number, l.to.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                case DefineConstants.FTRTN_LINK:
                                    swDump.WriteLine(string.Format("{0},{1},FTRTN,{2},{3},{4},{5},{6},{7}", l.number, l.from.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
                                    break;
                                default:
                                    mi.FireOnError("unknown link type in arcdump.c");
                                    mi.FireOnError("data type is  " + GlobalMembersArcdump.getLinkDumpType(l).ToString());
                                    break;
                            }
                    }
                    swDump.WriteLine();
                    if (n.nodeType == NodeType.Reservoir && n.m.resBalance != null && n.mnInfo.balanceLinks != null)
                    {
                        n = n.mnInfo.resBalanceNode;
                        goto DoItAgain;
                    }
                }
            }
            swDump.WriteLine("\n");
        }
        public static void closeDumpFile()
        {
            if (swDump != null)
            {
                swDump.Close();
                swDump = null;
            }
        }

        // Arc Dump file...
        private static bool OpenArcDump(Model mi)
        {
            if (!ignoreArcDump && swArcDump == null)
            {
                string dir = Path.GetDirectoryName(mi.fname) + @"\";
                if (dir.Equals(@"\")) dir = "";
                string fname = Path.GetFileNameWithoutExtension(mi.fname);
                if (File.Exists(dir + "arcdump.txt")) arcdumppath = dir + "arcdump.txt";
                else if (File.Exists(dir + "ARCDUMP")) arcdumppath = dir + "ARCDUMP";
                else if (File.Exists(dir + fname + ".ad")) arcdumppath = dir + fname + ".ad";
                else if (File.Exists(dir + fname + ".txt")) arcdumppath = dir + fname + ".txt";
                else
                {
                    ignoreArcDump = true;
                    return false;
                }

                try
                {
                    swArcDump = new StreamWriter(arcdumppath);
                    WriteNames(mi);
                }
                catch (Exception ex)
                {
                    ignoreArcDump = true;
                    swArcDump = null;
                    mi.FireOnError("Could not open " + arcdumppath + "\n\n" + ex.Message + "\n" + ex.StackTrace + "\n");
                    return false;
                }
            }
            return !ignoreArcDump;
        }
        public static void performArcDump(string label, Model mi)
        {
            performArcDump(label, mi, false);
        }
        public static void performArcDump(string label, Model mi, bool beforeSolution)
        {
            if (!OpenArcDump(mi))
                return;

            Link l = null;
            int timeindex = mi.mInfo.CurrentModelTimeStepIndex;
            DateTime now = mi.TimeStepManager.Index2Date(timeindex, TypeIndexes.ModelIndex);

            if (label != null)
                swArcDump.WriteLine(label);
            int iteration = mi.mInfo.Iteration;
            if (string.Compare(label, "second stg step") == 0)
                iteration = iteration--;
            swArcDump.WriteLine(string.Format("ModelTimeStepIndex {0}; iter {1}; {2}", timeindex, iteration, now.ToString(TimeManager.DateFormat)));
            swArcDump.WriteLine("Link,From,To,Lower Bound,Current Flow,Upper Bound,Unit Cost,ModelTimeStepIndex,iter");
            if (beforeSolution)
                for (l = mi.firstLink; l != null; l = l.next)
                    swArcDump.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", l.number, l.from.number, l.to.number, l.mlInfo.lo, "BeforeSolution", l.mlInfo.hi, l.mlInfo.cost,timeindex,iteration));
            else
                for (l = mi.firstLink; l != null; l = l.next)
                    swArcDump.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", l.number, l.from.number, l.to.number, l.mlInfo.lo, l.mlInfo.flow, l.mlInfo.hi, l.mlInfo.cost, timeindex, iteration));
        }
        public static void closeArcDump()
        {
            if (swArcDump != null)
            {
                swArcDump.Close();
                swArcDump = null;
            }
        }
        public static long DumpInfeasNodes(Model mi,bool inMemory = false)
        {
            bool arcDumpOpen = true;
            string msg="";
            if (!OpenArcDump(mi))
            {
                arcDumpOpen = false;
                if(!inMemory)return 0;
            }

            int lout = 0;
            Node n = null;
            LinkList ll = null;
            Link l = null;
            long nodeSum;
            long totNodeSum = 0;
            for (n = mi.firstNode; n != null; n = n.next)
            {
                nodeSum = 0;
                for (ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    l = ll.link;
                    nodeSum += l.mlInfo.flow;
                }
                for (ll = n.OutflowLinks; ll != null; ll = ll.next)
                {
                    l = ll.link;
                    nodeSum -= l.mlInfo.flow;
                }
                if (nodeSum != 0)
                {
                    if (lout == 0)
                        lout = 1;
                    msg = string.Format("Sum failed for node {0}, by {1}", n.name, nodeSum.ToString());
                    if (arcDumpOpen)swArcDump.WriteLine(msg);
                    mi.FireOnError(msg);
                    totNodeSum += nodeSum;
                }
            }
            if (totNodeSum != 0)
                msg = string.Format("Total infeasible sum = {0}", totNodeSum.ToString());
                if (arcDumpOpen) swArcDump.WriteLine(msg);
                mi.FireOnError(msg);
            return totNodeSum;
        }
        public static int getLinkDumpType(Link l)
        {
            if (l.to.mnInfo.infLink == l)
                return DefineConstants.INFLOW_LINK;
            if (l.from.mnInfo.targetLink == l)
                return DefineConstants.TARGSTO_LINK;
            if (l.from.mnInfo.evapLink == l)
                return DefineConstants.EVAP_LINK;
            if (l.from.mnInfo.excessStoLink == l)
                return DefineConstants.FINALSTO_LINK;
            if (l.from.mnInfo.demLink == l)
                return DefineConstants.DEMAND_LINK;
            if (l.from.mnInfo.spillLink == l)
                return DefineConstants.SPILL_LINK;
            if (l.from.mnInfo.gwoutLink == l)
                return DefineConstants.GWOUT_LINK;
            if (l.to.mnInfo.gwrtnLink == l)
                return DefineConstants.GWIN_LINK;
            if (l.from.mnInfo.flowThruReturnLink == l)
                return DefineConstants.FTRTN_LINK;
            return DefineConstants.REAL_LINK;
        }
    }
}
