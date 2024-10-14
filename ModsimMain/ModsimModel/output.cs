using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Csu.Modsim.ModsimModel
{
    /// <summary>Controls output and ability to write in Version 7 format.</summary>
    public static class GlobalMembersOutput
    {
        private static string[] GetPeriod_strings = { "Year", "Annual", "Month", "Monthly", "Mon", "Quarter", "Quarterly", "Week", "Weekly", "Wk", "Day", "Daily" };
        private static int[,] GetPeriod_index = { { 0, 5, 7, 7, 5 }, { 1, 6, 8, 8, 6 }, { 2, 7, 10, 10, 7 }, { 3, 8, 11, 11, 8 }, { 4, 9, 10, 10, 9 } };

        private static string[] extensions = { "OUT", "ACC", "DEM", "GW", "FLO", "RES" };
        private static StreamWriter[] sw = null;

        public static void BuildShortageArray(Model mi)
        {
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                long demand = 0;
                if (n.mnInfo.nodedemand.Length > 0)
                {
                    demand = n.mnInfo.nodedemand[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
                }

                /*
                 * the gw depletion link unfortunately includes deep percolation
                 * due to application, as well as depletion from pumping at another
                 * node--we need to separate these out to accurately calculate
                 * shortage to demand!
                 */
                if (n.nodeType == NodeType.Demand || n.nodeType == NodeType.Sink)
                {
                    Link l = mi.mInfo.demList[i].mnInfo.demLink;
                    n.mnInfo.ishtm[mi.mInfo.CurrentModelTimeStepIndex] = demand - l.mlInfo.flow - n.mnInfo.ideep0;
                    if (n.mnInfo.ishtm[mi.mInfo.CurrentModelTimeStepIndex] < 0)
                    {
                        n.mnInfo.ishtm[mi.mInfo.CurrentModelTimeStepIndex] = 0;
                    }
                }


                /* adjust flow-through demand if flow*short > 0 */
                if (n.m.pdstrm != null && (n.mnInfo.ishtm[mi.mInfo.CurrentModelTimeStepIndex] > 0))
                {
                    n.mnInfo.ishtm[mi.mInfo.CurrentModelTimeStepIndex] -= n.m.pdstrm.mlInfo.flow;
                    if (n.mnInfo.ishtm[mi.mInfo.CurrentModelTimeStepIndex] < 0)
                    {
                        n.mnInfo.ishtm[mi.mInfo.CurrentModelTimeStepIndex] = 0;
                    }
                }
                if (n.m.idstrmx[0] != null && n.m.idstrmx[1] == null)
                {
                    long iqtot = 0;
                    for (LinkList ll = n.OutflowLinks; ll != null; ll = ll.next)
                    {
                        if (ll.link.from == n && ll.link.to == n.m.idstrmx[0])
                        {
                            iqtot += ll.link.mlInfo.flow;
                        }
                    }
                    n.mnInfo.ishtm[mi.mInfo.CurrentModelTimeStepIndex] -= iqtot;
                    if (n.mnInfo.ishtm[mi.mInfo.CurrentModelTimeStepIndex] < 0)
                    {
                        n.mnInfo.ishtm[mi.mInfo.CurrentModelTimeStepIndex] = 0;
                    }
                }
            }
        }

        /// <summary>Opens all model output files. If an error occurs, an error message is set and returns false. Otherwise, returns true.</summary>
        /// <param name="mi">The Model object</param>
        /// <param name="path">The full file path for the output files (the extension will automatically be changed for different types of outputs). </param>
        /// <returns></returns>
        public static bool outputInit(Model mi)
        {
            if (mi.timeStep.TSType != ModsimTimeStepType.Monthly &&
                    mi.timeStep.TSType != ModsimTimeStepType.Weekly &&
                    mi.timeStep.TSType != ModsimTimeStepType.Daily)
            {
                return false;
            }

            sw = new StreamWriter[DefineConstants.NUM_OUTPUT_FILES];

            // Open each output file.  If an error occurs, set an error message and return FALSE.
            for (int i = 0; i < DefineConstants.NUM_OUTPUT_FILES; i++)
            {
                string outfile = Path.ChangeExtension(mi.fname, extensions[i]);
                try
                {
                    sw[i] = new StreamWriter(outfile);
                }
                catch (Exception ex)
                {
                    mi.FireOnError("Could not open output file: " + outfile);
                    mi.FireOnError("Error Message: " + ex.ToString());
                    mi.FireOnError("\nClosing all output.");
                    outputFree();
                    return false;
                }
            }

            GlobalMembersOutput.WriteHeaders(mi);
            return true;
        }
        /// <summary>Closes all output files that have been opened.</summary>
        public static void outputFree()
        {
            for (int i = 0; i < DefineConstants.NUM_OUTPUT_FILES; i++)
            {
                if (sw != null && sw[i] != null)
                {
                    sw[i].Close();
                }
            }
            sw = null;
        }

        /// <summary>Returns a string indicated by the passed arguments.</summary>
        /// <param name="which">Equal to DefineConstants.PRD or .SUBPRD</param>
        /// <param name="per_num">Has a value from 0 to 4 specifying mi.timeStep</param>
        /// <returns></returns>
        public static string GetPeriod(int which, int per_num)
        {
            /* Make sure the arguments are within their bounds. */

            if (which > 4)
            {
                which = 4;
            }

            if (per_num < 0)
            {
                per_num = 0;
            }
            if (per_num > 4)
            {
                per_num = 4;
            }

            return (GetPeriod_strings[GetPeriod_index[which, per_num]]);
        }

        /**************************************************************************
        This function writes a year's worth of data for the year `year' into the
        output (OUT) file.  If an error occurs, an error message is set and FALSE
        is returned.  Otherwise, TRUE is returned.
        \**************************************************************************/

        public static void writeOUT2year(Model mi, int year, DateTime fDate, int[] hydstateindex)
        {
            if (sw == null)
            {
                return;
            }

            int cal_yr; // The calendar year of the data being output.
            int maxLper;
            int lper = mi.timeStep.NumOfTSsForV7Output;
            if (lper > (maxLper = mi.TimeStepManager.noModelTimeSteps - mi.TimeStepManager.Date2Index(fDate, TypeIndexes.ModelIndex)))
            {
                lper = maxLper;
            }

            if (mi.timeStep.TSType == ModsimTimeStepType.Monthly)
            {
                cal_yr = mi.TimeStepManager.dataStartDate.Year - 1 + year;
                if (mi.TimeStepManager.dataStartDate.Month != 1)
                {
                    cal_yr++;
                }
            }
            else
            {
                cal_yr = year;
            }

            StreamWriter ofp = sw[DefineConstants.OUT_FILE2];

            // write out hydrologic tables' indices

            for (int i = 0; i < mi.numHydTables; i++)
            {
                DateTime cDate = fDate;
                for (int j = 0; j < lper; j++)
                {
                    ofp.Write((i + 1).ToString() + ", ");
                    ofp.Write("\"hydstatetable" + (i + 1).ToString() + "\", ");
                    ofp.Write(cal_yr.ToString() + ", ");
                    ofp.Write((j + 1).ToString() + ", ");
                    ofp.Write(cDate.Year.ToString() + ", ");
                    ofp.Write(cDate.Month.ToString() + ", ");
                    if (mi.timeStep.TSType == ModsimTimeStepType.Monthly)
                    {
                        ofp.Write((DateTime.DaysInMonth(cDate.Year, cDate.Month) / 2).ToString() + ", ");
                    }
                    else if (mi.timeStep.HasPartialDays)
                    {
                        ofp.Write(cDate.ToString() + ", ");
                    }
                    else
                    {
                        ofp.Write(cDate.Day.ToString() + ", ");
                    }
                    ofp.Write(hydstateindex[i * 12 + j].ToString() + "\n");

                    cDate = mi.TimeStepManager.GetNextIniDate(cDate);
                }
            }
            ofp.Flush();
        }

        /**************************************************************************
        This function writes a year's worth of data for the year `year' into the
        account ACC) file.
        \**************************************************************************/

        public static void writeACC2year(Model mi, int year, DateTime fDate)
        {
            if (sw == null)
            {
                return;
            }

            DateTime cDate;
            int cal_yr = 0;
            StreamWriter ofp;
            Link l;
            Link l2;
            Link plink;
            LinkList ll;
            int m;
            int lper;
            int i;
            int maxLper;
            lper = mi.timeStep.NumOfTSsForV7Output;
            if (lper > (maxLper = mi.TimeStepManager.noModelTimeSteps - mi.TimeStepManager.Date2Index(fDate, TypeIndexes.ModelIndex)))
            {
                lper = maxLper;
            }
            ofp = sw[DefineConstants.ACCOUNT_PRN2];

            /* Calculate the calendar year the output is for (i.e., 1980). */

            if (mi.timeStep.TSType == ModsimTimeStepType.Monthly)
            {
                cal_yr = mi.TimeStepManager.dataStartDate.Year - 1 + year;
            }
            if (mi.TimeStepManager.dataStartDate.Month != 1)
            {
                cal_yr++;
            }
            else
            {
                cal_yr = year;
            }

            for (i = 0; i < mi.mInfo.outputLinkList.Length; i++)
            {
                l = mi.mInfo.outputLinkList[i];
                cDate = fDate;
                if (!l.mlInfo.isArtificial)
                {
                    for (m = 0; m < lper; m++)
                    {
                        ofp.Write(l.number.ToString() + ", ");
                        ofp.Write("\"" + l.name + "\", ");
                        ofp.Write(cal_yr.ToString() + ", ");
                        ofp.Write((m + 1).ToString() + ", ");
                        ofp.Write(cDate.Year.ToString() + ", ");
                        ofp.Write(cDate.Month.ToString() + ", ");
                        if (mi.timeStep.TSType == ModsimTimeStepType.Monthly)
                        {
                            ofp.Write((DateTime.DaysInMonth(cDate.Year, cDate.Month) / 2).ToString());
                        }
                        else if (mi.timeStep.HasPartialDays)
                        {
                            ofp.Write(cDate.ToString());
                        }
                        else
                        {
                            ofp.Write(cDate.Day.ToString());
                        }

                        if (OutputControlInfo.stgl)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, l.mrlInfo.link_store[m]);
                        }
                        if (OutputControlInfo.acrl)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, l.mrlInfo.link_accrual[m]);
                        }
                        if (OutputControlInfo.acc_flow)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, l.mrlInfo.link_flow[m]);
                        }
                        if (OutputControlInfo.stgl && l.m.groupNumber > 0 && l.m.accrualLink != null)
                        {
                            plink = l.m.accrualLink;
                            for (ll = plink.mlInfo.cLinkL; ll != null; ll = ll.next)
                            {
                                l2 = ll.link;
                                if (l2.mrlInfo.groupID == l.m.groupNumber)
                                {
                                    ofp.Write(", \"" + l2.name + "\"");
                                    ofp.Write(", \"" + l2.mrlInfo.link_store[m].ToString());
                                }
                            }
                        }
                        if (OutputControlInfo.acrl && l.m.groupNumber > 0 && l.m.accrualLink != null)
                        {
                            plink = l.m.accrualLink;
                            for (ll = plink.mlInfo.cLinkL; ll != null; ll = ll.next)
                            {
                                l2 = ll.link;
                                if (l2.mrlInfo.groupID == l.m.groupNumber)
                                {
                                    if (OutputControlInfo.stgl == false) //  'false????
                                    {
                                        ofp.Write(", \"" + l2.name + "\"");
                                    }
                                    ofp.Write(", " + l2.mrlInfo.link_accrual[m].ToString());
                                }
                            }
                        }

                        ofp.Write("\n");
                        cDate = mi.TimeStepManager.GetNextIniDate(cDate);
                    }
                }
            }
            ofp.Flush();
        }

        /**************************************************************************
        This function writes a year's worth of data for the year `year' into the
        demand (DEM) file.
        \**************************************************************************/

        public static void writeDEM2year(Model mi, int year, DateTime fDate)
        {
            if (sw == null)
            {
                return;
            }

            DateTime cDate;
            long cal_yr = 0;
            StreamWriter ofp;
            Node n;
            long i;
            long m;
            long lper;
            long iswat;
            long igwat;
            long t;
            int maxLper;
            lper = mi.timeStep.NumOfTSsForV7Output;
            if (lper > (maxLper = mi.TimeStepManager.noModelTimeSteps - mi.TimeStepManager.Date2Index(fDate, TypeIndexes.ModelIndex)))
            {
                lper = maxLper;
            }

            ofp = sw[DefineConstants.DEMAND_PRN2];

            /* Calculate the calendar year the output is for (i.e., 1980). */

            if (mi.timeStep.TSType == ModsimTimeStepType.Monthly)
            {
                cal_yr = mi.TimeStepManager.dataStartDate.Year - 1 + year;
            }

            if (mi.TimeStepManager.dataStartDate.Month != 1)
            {
                cal_yr++;
            }
            else
            {
                cal_yr = year;
            }


            for (i = 0; i < mi.mInfo.outputNodeList.Length; i++)
            {
                n = mi.mInfo.outputNodeList[i];
                cDate = fDate;
                if (n.nodeType == NodeType.Demand || n.nodeType == NodeType.Sink)
                {
                    long m_TimeStep = mi.mInfo.CurrentModelTimeStepIndex - lper;
                    for (m = 0; m < lper; m++)
                    {
                        m_TimeStep += 1;
                        ofp.Write(n.number.ToString() + ", ");
                        if (n.name == null || n.name.Equals(""))
                        {
                            ofp.Write("\"" + n.number.ToString() + "\", ");
                        }
                        else
                        {
                            ofp.Write("\"" + n.name + "\", ");
                        }

                        ofp.Write(cal_yr.ToString() + ", ");
                        ofp.Write((m + 1).ToString() + ", ");
                        ofp.Write(cDate.Year.ToString() + ", ");
                        ofp.Write(cDate.Month.ToString() + ", ");
                        if (mi.timeStep.TSType == ModsimTimeStepType.Monthly)
                        {
                            ofp.Write((DateTime.DaysInMonth(cDate.Year, cDate.Month) / 2).ToString());
                        }
                        else if (mi.timeStep.HasPartialDays)
                        {
                            ofp.Write(cDate.ToString());
                        }
                        else
                        {
                            ofp.Write(cDate.Day.ToString());
                        }

                        /* Calculate total avaliable surface water.  */
                        // upstrm_release, canal_in, unreg_inflow for demands?????
                        //  this should NEVER be needed, should it??
                        iswat = n.mnInfo.upstrm_release[m] + n.mnInfo.canal_in[m] + n.mnInfo.irtnflowthruNF_OUT[m] + n.mnInfo.unreg_inflow[m]; // why NF step??

                        // The rest went to demand or groundwater or was pumped out.
                        // Pumping should not happen at demand nodes with GW.  It just
                        // doesn't work yet output wise.
                        t = n.mnInfo.demand[m_TimeStep] - n.mnInfo.demand_shortage[m];
                        t = System.Math.Max(0, t);
                        iswat = System.Math.Max(0, System.Math.Min(iswat, t));

                        /* Estimate groundwater. */

                        igwat = t - iswat;
                        igwat = System.Math.Max(0, igwat);

                        if (OutputControlInfo.demand)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, n.mnInfo.demand[m_TimeStep]);
                        }
                        if (OutputControlInfo.surf_in)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, iswat);
                        }
                        if (OutputControlInfo.gw_in)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, System.Math.Min(igwat, n.mnInfo.gw_to_node[m]));
                        }
                        if (OutputControlInfo.dem_sht)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, n.mnInfo.demand_shortage[m]);
                        }
                        ofp.Write("\n");

                        cDate = mi.TimeStepManager.GetNextIniDate(cDate);
                    }
                }
            }
            ofp.Flush();
        }

        /**************************************************************************
        This function writes a year's worth of data for the year `year' into the
        groundwater (GW) file.
        \**************************************************************************/

        public static void writeGW2year(Model mi, int year, DateTime fDate)
        {
            if (sw == null)
            {
                return;
            }

            DateTime cDate;
            int cal_yr = 0;
            StreamWriter ofp;
            Node n;
            int m;
            int lper;
            int i;
            int maxLper;
            lper = mi.timeStep.NumOfTSsForV7Output;
            if (lper > (maxLper = mi.TimeStepManager.noModelTimeSteps - mi.TimeStepManager.Date2Index(fDate, TypeIndexes.ModelIndex)))
            {
                lper = maxLper;
            }

            ofp = sw[DefineConstants.GROUNDWATER_PRN2];

            /* Calculate the calendar year the output is for (i.e., 1980). */

            if (mi.timeStep.TSType == ModsimTimeStepType.Monthly)
            {
                cal_yr = mi.TimeStepManager.dataStartDate.Year - 1 + year;
            }

            if (mi.TimeStepManager.dataStartDate.Month != 1)
            {
                cal_yr++;
            }
            else
            {
                cal_yr = year;
            }

            for (i = 0; i < mi.mInfo.outputNodeList.Length; i++)
            {
                n = mi.mInfo.outputNodeList[i];
                cDate = fDate;
                //if(1 /*n->mnInfo->printme*/ /*|| 1*/)
                {
                    for (m = 0; m < lper; m++)
                    {
                        ofp.Write(n.number.ToString() + ", ");
                        ofp.Write("\"" + n.name + "\", ");
                        ofp.Write(cal_yr.ToString() + ", ");
                        ofp.Write((m + 1).ToString() + ", ");
                        ofp.Write(cDate.Year.ToString() + ", ");
                        ofp.Write(cDate.Month.ToString() + ", ");
                        if (mi.timeStep.TSType == ModsimTimeStepType.Monthly)
                        {
                            ofp.Write((DateTime.DaysInMonth(cDate.Year, cDate.Month) / 2).ToString());
                        }
                        else
                        {
                            ofp.Write(cDate.Day.ToString());
                        }
                        if (OutputControlInfo.gwinfiltration)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, n.mnInfo.node_to_gw[m]);
                        }
                        if (OutputControlInfo.fromgwtonode)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, n.mnInfo.gw_to_node[m]);
                        }
                        ofp.Write("\n");

                        cDate = mi.TimeStepManager.GetNextIniDate(cDate);
                    }
                }
            }
            ofp.Flush();
        }

        /**************************************************************************
        This function writes a year's worth of data for the year `year' into the
        link (LNK*) file. *Note - the extension for this file was apparently
        changed to ARC and then more recently to FLO.
        \**************************************************************************/

        public static void writeLNK2year(Model mi, int year, DateTime fDate)
        {
            if (sw == null)
            {
                return;
            }

            DateTime cDate;
            int cal_yr = 0;
            StreamWriter ofp;
            Link l;
            int m;
            int lper;
            int i;
            int maxLper;
            lper = mi.timeStep.NumOfTSsForV7Output;
            if (lper > (maxLper = mi.TimeStepManager.noModelTimeSteps - mi.TimeStepManager.Date2Index(fDate, TypeIndexes.ModelIndex)))
            {
                lper = maxLper;
            }
            ofp = sw[DefineConstants.LINK_PRN2];

            /* Calculate the calendar year the output is for (i.e., 1980). */

            if (mi.timeStep.TSType == ModsimTimeStepType.Monthly)
            {
                cal_yr = mi.TimeStepManager.dataStartDate.Year - 1 + year;
            }

            if (mi.TimeStepManager.dataStartDate.Month != 1)
            {
                cal_yr++;
            }
            else
            {
                cal_yr = year;
            }

            for (i = 0; i < mi.mInfo.outputLinkList.Length; i++)
            {
                l = mi.mInfo.outputLinkList[i];
                cDate = fDate;
                if (!l.mlInfo.isArtificial)
                {
                    for (m = 0; m < lper; m++)
                    {
                        ofp.Write(l.number + ", ");
                        ofp.Write("\"" + l.name + "\", ");
                        ofp.Write(cal_yr.ToString() + ", ");
                        ofp.Write((m + 1).ToString() + ", ");
                        ofp.Write(cDate.Year.ToString() + ", ");
                        ofp.Write(cDate.Month.ToString() + ", ");
                        if (mi.timeStep.TSType == ModsimTimeStepType.Monthly)
                        {
                            ofp.Write((DateTime.DaysInMonth(cDate.Year, cDate.Month) / 2).ToString());
                        }
                        else if (mi.timeStep.HasPartialDays)
                        {
                            ofp.Write(cDate.ToString());
                        }
                        else
                        {
                            ofp.Write(cDate.Day.ToString());
                        }
                        if (OutputControlInfo.flo_flow)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, l.mrlInfo.link_flow[m]);
                        }
                        if (OutputControlInfo.loss)
                        {
                            //long2doutput(ofp, mi, l->mrlInfo->closs[m]);
                            GlobalMembersOutput.long2doutput(ofp, mi, l.mrlInfo.link_closs[m]);
                        }
                        if (OutputControlInfo.natflow)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, l.mrlInfo.natFlow[m]);
                        }
                        ofp.Write("\n");

                        cDate = mi.TimeStepManager.GetNextIniDate(cDate);
                    }
                }
            }
            ofp.Flush();
        }

        /**************************************************************************
        This function writes a year's worth of data for the year `year' into the
        reservoir (RES) file.
        \**************************************************************************/

        public static void writeRES2year(Model mi, int year, DateTime fDate)
        {
            if (sw == null)
            {
                return;
            }

            DateTime cDate;
            long cal_yr = 0;
            long lper;
            StreamWriter ofp;
            Node n;
            double rhead;
            double ghrs;
            long ieng;
            long i2nd;
            long i;
            long m;
            long q;
            double fsur;
            double endingelev;
            long ovalue = 0;
            //long datatimestepindex, modeltimestepindex;
            long modeltimestepindex;
            int maxLper;
            lper = mi.timeStep.NumOfTSsForV7Output;
            if (lper > (maxLper = mi.TimeStepManager.noModelTimeSteps - mi.TimeStepManager.Date2Index(fDate, TypeIndexes.ModelIndex)))
            {
                lper = maxLper;
            }
            ofp = sw[DefineConstants.RESERVOIR_PRN2];

            /* Calculate the calendar year the output is for (i.e., 1980). */

            if (mi.timeStep.TSType == ModsimTimeStepType.Monthly)
            {
                cal_yr = mi.TimeStepManager.dataStartDate.Year - 1 + year;
            }

            if (mi.TimeStepManager.dataStartDate.Month != 1)
            {
                cal_yr++;
            }
            else
            {
                cal_yr = year;
            }
            for (i = 0; i < mi.mInfo.outputNodeList.Length; i++)
            {
                n = mi.mInfo.outputNodeList[i];
                cDate = fDate;
                if (n.nodeType == NodeType.Reservoir)
                {
                    modeltimestepindex = mi.TimeStepManager.Date2Index(fDate, TypeIndexes.ModelIndex);
                    for (m = 0; m < lper; m++)
                    {
                        ofp.Write(n.number + ", ");
                        ofp.Write("\"" + n.name + "\", ");
                        ofp.Write(cal_yr.ToString() + ", ");
                        ofp.Write((m + 1).ToString() + ", ");
                        ofp.Write(cDate.Year.ToString() + ", ");
                        ofp.Write(cDate.Month.ToString() + ", ");
                        if (mi.timeStep.TSType == ModsimTimeStepType.Monthly)
                        {
                            ofp.Write((DateTime.DaysInMonth(cDate.Year, cDate.Month) / 2).ToString());
                        }
                        else if (mi.timeStep.HasPartialDays)
                        {
                            ofp.Write(cDate.ToString());
                        }
                        else
                        {
                            ofp.Write(cDate.Day.ToString());
                        }

                        /* Hydropower calculations... */
                        ghrs = 0.0;
                        if (n.mnInfo.generatinghours.Length > 0)
                        {
                            ghrs = n.mnInfo.generatinghours[modeltimestepindex, 0];
                        }
                        ieng = (long)((double)(n.mnInfo.avg_hydropower[m]) * ghrs / 1000.0 + DefineConstants.ROFF); // 0.4999999);

                        if (n.m.peakGeneration)
                        {
                            i2nd = 0;
                        }
                        else
                        {
                            ghrs = mi.timeStep.ToTimeSpan(cDate).TotalHours - ghrs;
                            if (ghrs < 0.0)
                            {
                                ghrs = 0.0;
                            }
                            i2nd = (long)((double)(n.mnInfo.avg_hydropower[m]) * ghrs / 1000.0 + DefineConstants.ROFF); // 0.4999999);
                        }

                        rhead = n.mnInfo.avg_head[m];

                        if (rhead < 0)
                        {
                            rhead = 0.0;
                        }

                        HydropowerElevDef.GetData(n, (long)(n.mnInfo.end_storage[modeltimestepindex]), out fsur, out endingelev, out q);

                        if (OutputControlInfo.stor_beg)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, n.mnInfo.start_storage[modeltimestepindex]);
                        }
                        if (OutputControlInfo.stor_end)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, n.mnInfo.end_storage[modeltimestepindex]);
                        }
                        if (OutputControlInfo.stor_trg)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, n.mnInfo.trg_storage[m]);
                        }
                        if (OutputControlInfo.spills)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, n.mnInfo.res_spill[m]);
                        }
                        if (OutputControlInfo.evp_rate)
                        {
                            double tmpflt = 0.0;
                            if (n.mnInfo.evaporationrate.Length > 0)
                            {
                                tmpflt = n.mnInfo.evaporationrate[modeltimestepindex, 0];
                            }
                            ofp.Write(", " + tmpflt.ToString("F3"));
                        }
                        if (OutputControlInfo.evp_loss)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, n.mnInfo.reservoir_evaporation[m]);
                        }
                        if (OutputControlInfo.seepage)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, n.mnInfo.iseepr[m]);
                        }
                        if (OutputControlInfo.unreg_in)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, n.mnInfo.unreg_inflow[m]);
                        }
                        if (OutputControlInfo.ups_rel)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, n.mnInfo.upstrm_release[m]);
                        }
                        if (OutputControlInfo.pump_in)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, n.mnInfo.canal_in[m]);
                        }
                        if (OutputControlInfo.gwater)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, n.mnInfo.gw_to_node[m]);
                        }
                        if (OutputControlInfo.dws_rel)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, n.mnInfo.downstrm_release[m]);
                        }
                        if (OutputControlInfo.pump_out)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, n.mnInfo.canal_out[m]);
                        }
                        if (OutputControlInfo.head_avg)
                        {
                            ofp.Write(", " + rhead.ToString("F2"));
                        }
                        if (OutputControlInfo.powr_avg)
                        {
                            ovalue = (long)(n.mnInfo.avg_hydropower[m] + DefineConstants.ROFF);
                            GlobalMembersOutput.long2doutput(ofp, mi, ovalue);
                        }
                        if (OutputControlInfo.powr_pk)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, ieng);
                        }
                        if (OutputControlInfo.pwr_2nd)
                        {
                            GlobalMembersOutput.long2doutput(ofp, mi, i2nd);
                        }
                        if (OutputControlInfo.elev_end)
                        {
                            ofp.Write(", " + endingelev.ToString("F2"));
                        }
                        ofp.Write("\n");

                        cDate = mi.TimeStepManager.GetNextIniDate(cDate);
                        modeltimestepindex++;
                    }
                }
            }
            ofp.Flush();
        }

        /**************************************************************************
        This function writes header information to each of the output data files.
        These headers indicate the name of the model, and a description of each
        column in the data file.
        \**************************************************************************/

        internal static void WriteHeaders(Model mi)
        {
            if (sw == null)
            {
                return;
            }

            string[,] labels = { { "YEAR", "MONTH" }, { "QRTR", "WEEK" }, { "WEEK", "DAY" } };
            int i;
            StreamWriter ofp;
            if (!mi.name.Equals("BASIC TITLE")) // No need to put out crud info
            {
                for (i = 0; i < DefineConstants.NUM_OUTPUT_FILES; i++)
                {
                    ofp = sw[i];
                    ofp.Write("\"" + mi.name + "\"\n");
                    ofp.Flush();
                }
            }


            /* Write the account (ACC2) file header. */
            ofp = sw[DefineConstants.ACCOUNT_PRN2];
            if (OutputControlInfo.acc_output)
            {
                ofp.Write("\"LINK\", \"NAME\", ");
                ofp.Write("\"" + labels[(int)ModsimTimeStep.GetOldType(mi.timeStep.TSType), 0] + "\", ");
                ofp.Write("\"" + labels[(int)ModsimTimeStep.GetOldType(mi.timeStep.TSType), 1] + "\", ");
                ofp.Write("\"CALENDAR_YEAR\", \"CALENDAR_MONTH\", \"CALENDAR_DATE\"");
                if (OutputControlInfo.stgl)
                {
                    ofp.Write(", \"STGL\"");
                }
                if (OutputControlInfo.acrl)
                {
                    ofp.Write(", \"ACRL\"");
                }
                if (OutputControlInfo.acc_flow)
                {
                    ofp.Write(", \"FLOW\"");
                }
                if (OutputControlInfo.stgl)
                {
                    ofp.Write(", \"GLINK\"");
                }
                if (OutputControlInfo.stgl)
                {
                    ofp.Write(", \"GSTGL\"");
                }
                if (OutputControlInfo.acrl)
                {
                    ofp.Write(", \"GACRL\"");
                }
                ofp.Write("\n");
            }
            else
            {
                ofp.Write("This output file has been disabled using the output control feature.");
            }
            ofp.Flush();

            /* Write the demand (DEM2) file header. */
            ofp = sw[DefineConstants.DEMAND_PRN2];
            if (OutputControlInfo.dem_output)
            {
                ofp.Write("\"NODE\", \"NAME\", ");
                ofp.Write("\"" + labels[(int)ModsimTimeStep.GetOldType(mi.timeStep.TSType), 0] + "\", ");
                ofp.Write("\"" + labels[(int)ModsimTimeStep.GetOldType(mi.timeStep.TSType), 1] + "\", ");
                ofp.Write("\"CALENDAR_YEAR\", \"CALENDAR_MONTH\", \"CALENDAR_DATE\"");
                if (OutputControlInfo.demand)
                {
                    ofp.Write(", \"DEMAND\"");
                }
                if (OutputControlInfo.surf_in)
                {
                    ofp.Write(", \"SURF_IN\"");
                }
                if (OutputControlInfo.gw_in)
                {
                    ofp.Write(", \"GW_IN\"");
                }
                if (OutputControlInfo.dem_sht)
                {
                    ofp.Write(", \"DEM_SHT\"");
                }
                ofp.Write("\n");
            }
            else
            {
                ofp.Write("This output file has been disabled using the output control feature.");
            }
            ofp.Flush();

            /* Write the groundwater (GW2) file header. */
            ofp = sw[DefineConstants.GROUNDWATER_PRN2];
            if (OutputControlInfo.gw_output)
            {
                ofp.Write("\"NODE\", \"NAME\", ");
                ofp.Write("\"" + labels[(int)ModsimTimeStep.GetOldType(mi.timeStep.TSType), 0] + "\", ");
                ofp.Write("\"" + labels[(int)ModsimTimeStep.GetOldType(mi.timeStep.TSType), 1] + "\", ");
                ofp.Write("\"CALENDAR_YEAR\", \"CALENDAR_MONTH\", \"CALENDAR_DATE\"");
                if (OutputControlInfo.gwinfiltration)
                {
                    ofp.Write(", \"GWInfiltration\"");
                }
                if (OutputControlInfo.fromgwtonode)
                {
                    ofp.Write(", \"FromGWtoNode\"");
                }
                ofp.Write("\n");
            }
            else
            {
                ofp.Write("This output file has been disabled using the output control feature.");
            }
            ofp.Flush();

            /* Write the link (LINK2) file header. */
            ofp = sw[DefineConstants.LINK_PRN2];
            if (OutputControlInfo.flo_output)
            {
                ofp.Write("\"LINK\", \"NAME\", ");
                ofp.Write("\"" + labels[(int)ModsimTimeStep.GetOldType(mi.timeStep.TSType), 0] + "\", ");
                ofp.Write("\"" + labels[(int)ModsimTimeStep.GetOldType(mi.timeStep.TSType), 1] + "\", ");
                ofp.Write("\"CALENDAR_YEAR\", \"CALENDAR_MONTH\", \"CALENDAR_DATE\"");
                if (OutputControlInfo.flo_flow)
                {
                    ofp.Write(", \"FLOW\"");
                }
                if (OutputControlInfo.loss)
                {
                    ofp.Write(", \"LOSS\"");
                }
                if (OutputControlInfo.natflow)
                {
                    ofp.Write(", \"NATFLOW\"");
                }
                ofp.Write("\n");
            }
            else
            {
                ofp.Write("This output file has been disabled using the output control feature.");
            }
            ofp.Flush();

            /* Write the reservoir (RES2) file header. */
            ofp = sw[DefineConstants.RESERVOIR_PRN2];
            if (OutputControlInfo.res_output)
            {
                ofp.Write("\"NODE\", \"NAME\", ");
                ofp.Write("\"" + labels[(int)ModsimTimeStep.GetOldType(mi.timeStep.TSType), 0] + "\", ");
                ofp.Write("\"" + labels[(int)ModsimTimeStep.GetOldType(mi.timeStep.TSType), 1] + "\", ");
                ofp.Write("\"CALENDAR_YEAR\", \"CALENDAR_MONTH\", \"CALENDAR_DATE\"");
                if (OutputControlInfo.stor_beg)
                {
                    ofp.Write(", \"STOR_BEG\"");
                }
                if (OutputControlInfo.stor_end)
                {
                    ofp.Write(", \"STOR_END\"");
                }
                if (OutputControlInfo.stor_trg)
                {
                    ofp.Write(", \"STOR_TRG\"");
                }
                if (OutputControlInfo.spills)
                {
                    ofp.Write(", \"SPILLS\"");
                }
                if (OutputControlInfo.evp_rate)
                {
                    ofp.Write(", \"EVP_RATE\"");
                }
                if (OutputControlInfo.evp_loss)
                {
                    ofp.Write(", \"EVP_LOSS\"");
                }
                if (OutputControlInfo.seepage)
                {
                    ofp.Write(", \"SEEPAGE\"");
                }
                if (OutputControlInfo.unreg_in)
                {
                    ofp.Write(", \"UNREG_IN\"");
                }
                if (OutputControlInfo.ups_rel)
                {
                    ofp.Write(", \"UPS_REL\"");
                }
                if (OutputControlInfo.pump_in)
                {
                    ofp.Write(", \"PUMP_IN\"");
                }
                if (OutputControlInfo.gwater)
                {
                    ofp.Write(", \"GWATER\"");
                }
                if (OutputControlInfo.dws_rel)
                {
                    ofp.Write(", \"cDWS_REL\"");
                }
                if (OutputControlInfo.pump_out)
                {
                    ofp.Write(", \"PUMP_OUT\"");
                }
                if (OutputControlInfo.head_avg)
                {
                    ofp.Write(", \"HEAD_AVG\"");
                }
                if (OutputControlInfo.powr_avg)
                {
                    ofp.Write(", \"POWR_AVG\"");
                }
                if (OutputControlInfo.powr_pk)
                {
                    ofp.Write(", \"POWR_PK\"");
                }
                if (OutputControlInfo.pwr_2nd)
                {
                    ofp.Write(", \"PWR_2ND\"");
                }
                if (OutputControlInfo.elev_end)
                {
                    ofp.Write(", \"ELEV_END\"");
                }
                ofp.Write("\n");
            }
            else
            {
                ofp.Write("This output file has been disabled using the output control feature.");
            }
            ofp.Flush();

            /* Write the out (OUT2) file header. */
            ofp = sw[DefineConstants.OUT_FILE2];
            ofp.Write("\"TABLE\", \"NAME\", ");
            ofp.Write("\"" + labels[(int)ModsimTimeStep.GetOldType(mi.timeStep.TSType), 0] + "\", ");
            ofp.Write("\"" + labels[(int)ModsimTimeStep.GetOldType(mi.timeStep.TSType), 1] + "\", ");
            ofp.Write("\"CALENDAR_YEAR\", \"CALENDAR_MONTH\", \"CALENDAR_DATE\", \"INDEX\"\n");
            ofp.Flush();
        }

        public static void long2doutput(StreamWriter ofp, Model mi, long value)
        {
            //ofp.Write(", " + ((value != mi.defaultMaxCap) ? (double)value / mi.ScaleFactor : (double)value).ToString("F" + mi.accuracy.ToString()));
            ofp.Write(", " + ((double)value / mi.ScaleFactor).ToString("F" + mi.accuracy.ToString()));
        }

    }
}
