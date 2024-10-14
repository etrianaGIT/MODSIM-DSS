using System.IO;
namespace Csu
{
    namespace Modsim
    {
        namespace ModsimModel
        {
            public static class GlobalMembersResdump
            {
                internal static string resDumpFName = "resdump.txt";
                internal static int resDumpExists;
                internal static bool alreadyEntered = false;
                internal static FileStream ResDumpFile;
                internal static StreamWriter swResDump;
                public static void ResDump(Model mi, int iy, int mon, int iter, string ident, int NatFlowIter, int AllDone)
                {
                    llrdump llrd;
                    int headerDone = 0;
                    Node n;
                    LinkList ll = null;

                    // Return if the resdump file does not exit
                    if (resDumpExists == 0)
                        return;

                    // Clear the file at the beginning of the simulation
                    if (!alreadyEntered)
                    {
                        if (File.Exists(resDumpFName))
                            File.Delete(resDumpFName);
                        alreadyEntered = true;
                    }

                    // Open the file
                    ResDumpFile = new FileStream(resDumpFName, FileMode.Append);
                    if (!ResDumpFile.CanWrite)
                        return;
                    swResDump = new StreamWriter(ResDumpFile);

                    // Loop through the links and place output into the output files...
                    for (llrd = llrdump.RDHead; llrd != null; llrd = llrd.next)
                    {
                        if (llrd.description.IndexOf(ident) > 0)
                        {
                            n = mi.mInfo.nList[llrd.number];
                            if (n == null || (n.type != DefineConstants.RESERVOIR))
                                continue;
                            if (headerDone == 0)
                            {
                                swResDump.WriteLine(string.Format("Month {0}, Year {1}, iter {2}, {3}, NF Flag {4}, AllDone {5}", mon + 1, iy, iter, ident, NatFlowIter, AllDone));
                                headerDone = 1;
                            }
                            GlobalMembersResdump.DumpPhysical(ResDumpFile, mi, n, NatFlowIter, AllDone);
                            for (ll = n.InflowLinks; ll != null; ll = ll.next)
                            {
                                if (!ll.link.mlInfo.isArtificial)
                                    GlobalMembersResdump.DumpInflowLinks(ResDumpFile, iter, ll.link, NatFlowIter, AllDone);
                            }
                            for (ll = n.OutflowLinks; ll != null; ll = ll.next)
                            {
                                if (!ll.link.mlInfo.isArtificial)
                                    GlobalMembersResdump.DumpOutflowLinks(ResDumpFile, ll.link);
                            }
                        }
                    }
                    swResDump.Close();
                }
                public static void ResDumpInit(Model mi)
                {
                    FileStream fs;
                    StreamReader sr;
                    string line;
                    llrdump llrd;
                    string dir = Path.GetDirectoryName(mi.fname) + @"\";

                    if (File.Exists(dir + resDumpFName))
                        resDumpFName = dir + resDumpFName;
                    if (File.Exists(dir + Path.GetFileNameWithoutExtension(resDumpFName)))
                        resDumpFName = dir + Path.GetFileNameWithoutExtension(resDumpFName);
                    else if (!File.Exists(resDumpFName))
                    {
                        resDumpExists = 0;
                        return;
                    }
                    resDumpExists = 1;

                    fs = new FileStream(resDumpFName, FileMode.Open);
                    sr = new StreamReader(fs);
                    for (llrd = null; !sr.EndOfStream; )
                    {
                        line = sr.ReadLine();
                        llrd = new llrdump();
                        llrd.next = llrdump.RDHead;
                        llrd.number = getInt(line);
                        llrd.description = line;
                        llrdump.RDHead = llrd;
                    }
                    sr.Close();
                }
                public static int getInt(string str)
                {
                    int retVal = (int)DefineConstants.NODATAVALUE;
                    str = str.Trim();
                    for (int i = 0; i < str.Length; i++)
                        if (int.TryParse(str.Substring(i, 1), out retVal))
                        {
                            int j = 1;
                            while (i + j < str.Length && int.TryParse(str.Substring(i + j, 1), out retVal))
                                j++;
                            if (!int.TryParse(str.Substring(i + j, 1), out retVal))
                                j--;
                            if (int.TryParse(str.Substring(i + j, 1), out retVal))
                                break;
                        }
                    return retVal;
                }
                public static void ResDumpFree()
                {
                    llrdump llrd;
                    llrdump llrd2;

                    for (llrd = llrdump.RDHead; llrd != null; )
                    {
                        llrd2 = llrd.next;
                        llrd = llrd2;
                    }
                }
                //void DumpPhysical(FILE *ResDumpFile, Model *mi, long mon,
                public static void DumpPhysical(FileStream ResDumpFile, Model mi, Node resNode, int NatFlowIter, int AllDone)
                {
                    LinkList ll;
                    LinkList ll2;
                    Node n = null;
                    long i;
                    long sumIn;
                    long sumOut;
                    long sumOwnAccrual = 0;
                    long lmSumOwnAccrual = 0;
                    long sumStglft = 0;
                    long lmSumStglft = 0;

                    swResDump.WriteLine(string.Format("Node {0}, Evpt {1}, Start {2}, Stend {3}, HydState({4}) {5}", resNode.number, resNode.mnInfo.evpt, resNode.mnInfo.start, resNode.mnInfo.stend, resNode.m.hydTable, resNode.mnInfo.hydStateIndex));
                    swResDump.WriteLine(string.Format("Spill Link bounds:  flow {0}, lo {1}, hi {2}, cost {3}", resNode.mnInfo.spillLink.mlInfo.flow, resNode.mnInfo.spillLink.mlInfo.lo, resNode.mnInfo.spillLink.mlInfo.hi, resNode.mnInfo.spillLink.mlInfo.cost));
                    swResDump.WriteLine(string.Format("Target Link bounds: flow {0}, lo {1}, hi {2}, cost {3}", resNode.mnInfo.targetLink.mlInfo.flow, resNode.mnInfo.targetLink.mlInfo.lo, resNode.mnInfo.targetLink.mlInfo.hi, resNode.mnInfo.targetLink.mlInfo.cost));
                    sumIn = 0;
                    for (ll = resNode.InflowLinks; ll != null; ll = ll.next)
                    {
                        if (!ll.link.mlInfo.isArtificial)
                            sumIn += ll.link.mlInfo.flow;
                    }
                    sumOut = 0;
                    for (ll = resNode.OutflowLinks; ll != null; ll = ll.next)
                    {
                        if (!ll.link.mlInfo.isArtificial)
                            sumOut += ll.link.mlInfo.flow;
                    }

                    sumOwnAccrual = 0;
                    lmSumOwnAccrual = 0;
                    sumStglft = 0;
                    lmSumStglft = 0;
                    // Either a parent or bad luck
                    if ((sumIn == 0 && sumOut == 0) || resNode.mnInfo.ownerType == DefineConstants.NONCH_ACCOUNT_RES)
                    {
                        //for(i = 0; i < mi->mInfo->resListLen; i++)
                        for (i = 0; i < mi.mInfo.resList.Length; i++)
                        {
                            n = mi.mInfo.resList[i];
                            if (n.myMother == resNode || (n.mnInfo.ownerType == DefineConstants.NONCH_ACCOUNT_RES && n == resNode))
                            {
                                if (n.mnInfo.ownerType != DefineConstants.NONCH_ACCOUNT_RES)
                                {
                                    for (ll = n.OutflowLinks; ll != null; ll = ll.next)
                                    {
                                        if (!ll.link.mlInfo.isArtificial)
                                            sumOut += ll.link.mlInfo.flow;
                                    }
                                    for (ll = n.InflowLinks; ll != null; ll = ll.next)
                                    {
                                        if (!ll.link.mlInfo.isArtificial)
                                            sumIn += ll.link.mlInfo.flow;
                                    }
                                }
                                for (ll = n.InflowLinks; ll != null; ll = ll.next)
                                {
                                    // Over all real links
                                    for (ll2 = ll.link.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                                    {
                                        lmSumOwnAccrual += ll2.link.mrlInfo.prevownacrul;
                                        if (AllDone != 0)
                                        {
                                            sumOwnAccrual += ll2.link.mrlInfo.own_accrual;
                                            sumStglft += ll2.link.mrlInfo.stglft;
                                        }
                                        else if (NatFlowIter != 0)
                                        {
                                            sumStglft += System.Math.Max(0, System.Math.Min(ll2.link.mrlInfo.cap_own, (ll2.link.mrlInfo.current_accrual + ll2.link.mrlInfo.prevstglft - ll2.link.mlInfo.flow0 - ll2.link.mrlInfo.current_evap)));
                                            sumOwnAccrual += ll2.link.mrlInfo.prevownacrul + ll2.link.mrlInfo.current_accrual - ll2.link.mrlInfo.current_evap;
                                        }
                                        else
                                        {
                                            sumStglft += System.Math.Max(0, System.Math.Min(ll2.link.mrlInfo.cap_own, (ll2.link.mrlInfo.current_accrual + ll2.link.mrlInfo.prevstglft - ll2.link.mlInfo.flow - ll2.link.mrlInfo.current_evap)));
                                            sumOwnAccrual += ll2.link.mrlInfo.prevownacrul + ll2.link.mrlInfo.current_accrual - ll2.link.mrlInfo.current_evap;
                                        }
                                        lmSumStglft += ll2.link.mrlInfo.prevstglft;
                                    }
                                    // Over all rental links
                                    for (ll2 = ll.link.mlInfo.rLinkL; ll2 != null; ll2 = ll2.next)
                                    {
                                        sumStglft += System.Math.Max(0, System.Math.Min(ll2.link.mrlInfo.cap_own, (ll2.link.mrlInfo.prevstglft - ll2.link.mlInfo.flow - ll2.link.mrlInfo.current_evap)));
                                        lmSumStglft += ll2.link.mrlInfo.prevstglft;
                                    }
                                }
                            }
                        }
                    }
                    swResDump.WriteLine(string.Format("Total Inflow: {0}, Total Outflow: {1}, Demout {2}", sumIn, sumOut, resNode.mnInfo.demout));
                    swResDump.WriteLine(string.Format("beginning sumstglft: {0}, beginning sumownacrul: {1}", lmSumStglft, lmSumOwnAccrual));
                    swResDump.WriteLine(string.Format("ending    sumstglft: {0}, ending    sumownacrul: {1}", sumStglft, sumOwnAccrual));
                }
                //void DumpInflowLinks(FILE *ResDumpFile, Model *mi, long mon, long iter,
                public static void DumpInflowLinks(FileStream ResDumpFile, int iter, Link resLink, int NatFlowIter, int AllDone)
                {
                    long sumstglft = 0;
                    long sumprevstglft = 0;
                    long sumOwnAccrual = 0;
                    long lmSumOwnAccrual = 0;
                    long sumStglft = 0;
                    long lmSumStglft = 0;
                    LinkList ll;
                    LinkList ll2;
                    if (resLink.mlInfo.isAccrualLink)
                    {
                        swResDump.WriteLine(string.Format("  Accrual Link {0}, SeasStorageCap {1}, evap {2}", resLink.number, resLink.mrlInfo.lnkSeasStorageCap, resLink.mrlInfo.current_evap));
                        swResDump.WriteLine(string.Format("  Accrual Link {0}, lo {1}, hi {2}, cost {3}", resLink.number, resLink.mlInfo.lo, resLink.mlInfo.hi, resLink.mlInfo.cost));
                        swResDump.WriteLine(string.Format("  Accrual Link {0}, flow {1}, flow0 {2}", resLink.number, resLink.mlInfo.flow, resLink.mlInfo.flow0));

                    }
                    else if (resLink.mlInfo.isLastFillLink)
                    {
                        swResDump.WriteLine(string.Format("  LastFill Link {0}, lo {1}, hi {2}, cost {3}", resLink.number, resLink.mlInfo.lo, resLink.mlInfo.hi, resLink.mlInfo.cost));
                        swResDump.WriteLine(string.Format("  LastFill Link {0}, flow {1}, flow0 {2}", resLink.number, resLink.mlInfo.flow, resLink.mlInfo.flow0));
                    }
                    else // Other type of link
                    {
                        swResDump.WriteLine(string.Format("  General Link {0}, lo {1}, hi {2}, cost {3}", resLink.number, resLink.mlInfo.lo, resLink.mlInfo.hi, resLink.mlInfo.cost));
                        swResDump.WriteLine(string.Format("  General Link {0}, flow {1}, flow0 {2}", resLink.number, resLink.mlInfo.flow, resLink.mlInfo.flow0));
                    }

                    sumOwnAccrual = 0;
                    lmSumOwnAccrual = 0;
                    sumStglft = 0;
                    lmSumStglft = 0;
                    for (ll2 = resLink.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                    {
                        lmSumOwnAccrual += ll2.link.mrlInfo.prevownacrul;

                        if (AllDone != 0)
                        {
                            sumOwnAccrual += ll2.link.mrlInfo.own_accrual;
                            sumStglft += ll2.link.mrlInfo.stglft;
                        }
                        else if (NatFlowIter != 0)
                        {
                            sumStglft += System.Math.Max(0, System.Math.Min(ll2.link.mrlInfo.cap_own, (ll2.link.mrlInfo.current_accrual + ll2.link.mrlInfo.prevstglft - ll2.link.mlInfo.flow0 - ll2.link.mrlInfo.current_evap)));
                            sumOwnAccrual += ll2.link.mrlInfo.prevownacrul + ll2.link.mrlInfo.current_accrual - ll2.link.mrlInfo.current_evap;
                        }
                        else
                        {
                            sumStglft += System.Math.Max(0, System.Math.Min(ll2.link.mrlInfo.cap_own, (ll2.link.mrlInfo.current_accrual + ll2.link.mrlInfo.prevstglft - ll2.link.mlInfo.flow - ll2.link.mrlInfo.current_evap)));
                            sumOwnAccrual += ll2.link.mrlInfo.prevownacrul + ll2.link.mrlInfo.current_accrual - ll2.link.mrlInfo.current_evap;
                        }
                        lmSumStglft += ll2.link.mrlInfo.prevstglft;
                    }
                    // Over all rental links
                    for (ll2 = resLink.mlInfo.rLinkL; ll2 != null; ll2 = ll2.next)
                    {
                        if (NatFlowIter != 0)
                        {
                            sumStglft += System.Math.Max(0, System.Math.Min(ll2.link.mrlInfo.cap_own, (ll2.link.mrlInfo.prevstglft - ll2.link.mlInfo.flow0 - ll2.link.mrlInfo.current_evap)));
                        }
                        else
                        {
                            sumStglft += System.Math.Max(0, System.Math.Min(ll2.link.mrlInfo.cap_own, (ll2.link.mrlInfo.prevstglft - ll2.link.mlInfo.flow - ll2.link.mrlInfo.current_evap)));
                        }

                        lmSumStglft += ll2.link.mrlInfo.prevstglft;
                    }

                    swResDump.WriteLine(string.Format("  beginning sumstglft: {0}, beginning sumownacrul: {1}", lmSumStglft, lmSumOwnAccrual));
                    swResDump.WriteLine(string.Format("  ending    sumstglft: {0}, ending    sumownacrul: {1}", sumStglft, sumOwnAccrual));

                    for (ll = resLink.mlInfo.cLinkL; ll != null; ll = ll.next)
                    {
                        sumstglft += ll.link.mrlInfo.stglft;
                        sumprevstglft += ll.link.mrlInfo.prevstglft;
                        swResDump.WriteLine(string.Format("    Ownership Link {0}, capown {1}, hydstate {2}", ll.link.number, ll.link.mrlInfo.cap_own, ll.link.mrlInfo.hydStateIndex));

                        swResDump.WriteLine(string.Format("      flow {0}, flow0 {1}, lo {2}, hi {3}, cost {4}", ll.link.mlInfo.flow, ll.link.mlInfo.flow0, ll.link.mlInfo.lo, ll.link.mlInfo.hi, ll.link.mlInfo.cost));
                        swResDump.WriteLine(string.Format("      beginning stglft: {0}, beginning ownacrul: {1}", ll.link.mrlInfo.prevstglft, ll.link.mrlInfo.prevownacrul));

                        if (ll.link.mrlInfo.irent != 0)
                        {
                            swResDump.WriteLine(string.Format("      contribRent: {0}, ", ll.link.mrlInfo.contribRent));

                            swResDump.WriteLine(string.Format("      rentLimits, {0},{1},{2},{3},{4},{5}", ll.link.m.rentLimit[0], ll.link.m.rentLimit[1], ll.link.m.rentLimit[2], ll.link.m.rentLimit[3], ll.link.m.rentLimit[4], ll.link.m.rentLimit[5]));
                        }

                        if (AllDone != 0)
                        {
                            swResDump.WriteLine(string.Format("      ending    stglft: {0}, ending    ownacrul: {1}", ll.link.mrlInfo.stglft, ll.link.mrlInfo.own_accrual));
                        }
                        else if (NatFlowIter != 0)
                        {
                            swResDump.WriteLine(string.Format("      ending    stglft: {0}, ending    ownacrul: {1}", System.Math.Max(0, System.Math.Min(ll.link.mrlInfo.cap_own, (ll.link.mrlInfo.current_accrual + ll.link.mrlInfo.prevstglft - ll.link.mlInfo.flow0 - ll.link.mrlInfo.current_evap))), ll.link.mrlInfo.prevownacrul + ll.link.mrlInfo.current_accrual - ll.link.mrlInfo.current_evap));
                        }
                        else
                        {
                            swResDump.WriteLine(string.Format("      ending    stglft: {0}, ending    ownacrul: {1}", System.Math.Max(0, System.Math.Min(ll.link.mrlInfo.cap_own, (ll.link.mrlInfo.current_accrual + ll.link.mrlInfo.prevstglft - ll.link.mlInfo.flow - ll.link.mrlInfo.current_evap))), ll.link.mrlInfo.prevownacrul + ll.link.mrlInfo.current_accrual - ll.link.mrlInfo.current_evap));
                        }

                        if (ll.link.mrlInfo.irent != 0)
                        {
                            swResDump.WriteLine(string.Format("      Rent Limit({0}): {1}, Last Fill {2}% ", ll.link.mrlInfo.hydStateIndex, ll.link.m.rentLimit[ll.link.mrlInfo.hydStateIndex], ll.link.m.lastFill));
                        }
                    }

                    for (ll = resLink.mlInfo.rLinkL; ll != null; ll = ll.next)
                    {
                        sumstglft += ll.link.mrlInfo.stglft;
                        sumprevstglft += ll.link.mrlInfo.prevstglft;
                        swResDump.WriteLine(string.Format("  Rent Link {0}, contribRent {1}, hydstate {2}", ll.link.number, ll.link.mrlInfo.contribRent, ll.link.mrlInfo.hydStateIndex));
                        if (iter % 2 == 0)
                        {
                            swResDump.WriteLine(string.Format("  lastMon's Stglft {0}, Calculated Stglft {1}, flow0 {2}", ll.link.mrlInfo.prevstglft, ll.link.mrlInfo.prevstglft + ll.link.mrlInfo.current_accrual - ll.link.mrlInfo.current_evap - ll.link.mlInfo.flow0, ll.link.mlInfo.flow0));
                        }
                        else
                        {
                            swResDump.WriteLine(string.Format("  lastMon's Stglft {0}, Calculated Stglft {1}", ll.link.mrlInfo.prevstglft, ll.link.mrlInfo.prevstglft + ll.link.mrlInfo.current_accrual - ll.link.mrlInfo.current_evap - ll.link.mlInfo.flow));
                            swResDump.WriteLine(string.Format("  flow {0}, lo {1}, hi {2}, cost {3}", ll.link.mlInfo.flow, ll.link.mlInfo.lo, ll.link.mlInfo.hi, ll.link.mlInfo.cost));
                        }

                        swResDump.WriteLine(string.Format("      current Evap {0}", ll.link.mrlInfo.current_evap));

                        if (ll.link.mrlInfo.irent != 0)
                        {
                            swResDump.WriteLine(string.Format("Rent Limit({0}): {1}, Last Fill {2}%, ", ll.link.mrlInfo.hydStateIndex, ll.link.m.rentLimit[ll.link.mrlInfo.hydStateIndex], ll.link.m.lastFill));
                        }
                    }
                }
                public static void DumpOutflowLinks(FileStream ResDumpFile, Link resLink)
                {
                    if (resLink.from.m.resOutLink == resLink)
                    {
                        swResDump.WriteLine(string.Format("  Main Outflow Link {0}, flow {1}, lo {2}, hi {3}, cost {4}", resLink.number, resLink.mlInfo.flow, resLink.mlInfo.lo, resLink.mlInfo.hi, resLink.mlInfo.cost));
                    }
                    else
                    {
                        swResDump.WriteLine(string.Format("  General Outflow Link {0}, flow {1}, lo {2}, hi {3}, cost {4}", resLink.number, resLink.mlInfo.flow, resLink.mlInfo.lo, resLink.mlInfo.hi, resLink.mlInfo.cost));
                    }
                }
            }

            public class llrdump //typedef struct _llrdump {
            {
                public llrdump()
                {
                }
                public static llrdump RDHead = null;
                public string description; //= __nogc [500];            // description string (use strtok)
                public int number; // Reservoir node number
                public llrdump next;
            } // llrdump;

        }
    }
}
