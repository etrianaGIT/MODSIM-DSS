using System;
using System.IO;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.ModsimIO
{
    //ModelReader Class Writes the  Model level commands in the xy file.
    //!!!BLounsbury: Do not add comments "#" to WriteLine's that are read back into Modsim as strings!!!
    public class ModelWriter
    {
        //WriteModelBasic writes basic infor at top of xy file
        public static void WriteModelBasic(Model mi, StreamWriter xyOutStream)
        {
            xyOutStream.WriteLine("xyVersion " + mi.outputVersion.Label);
            xyOutStream.WriteLine("numhydtables " + mi.HydStateTables.Length);
            xyOutStream.WriteLine("n_links " + mi.real_links);
            xyOutStream.WriteLine("n_nodes " + mi.real_nodes);
            xyOutStream.WriteLine("n_lags " + mi.nlags);
            // Extensions
            xyOutStream.WriteLine("ExtManualStorageRightActive " + mi.ExtManualStorageRightActive.ToString());
            xyOutStream.WriteLine("ExtStorageRightActive " + mi.ExtStorageRightActive.ToString());
            xyOutStream.WriteLine("ExtWaterRightsActive " + mi.ExtWaterRightsActive.ToString());
            xyOutStream.WriteLine("ExtLastFillRentActive " + mi.ExtLastFillRentActive.ToString());
            // Sizes
            xyOutStream.WriteLine("NonstorageNodeSize " + mi.graphics.nonstorageSize);
            xyOutStream.WriteLine("DemandNodeSize " + mi.graphics.demandSize);
            xyOutStream.WriteLine("ReservoirNodeSize " + mi.graphics.reservoirSize);
            xyOutStream.WriteLine("LinkSize " + mi.graphics.linkSize);
            string relativeImageLocation = null;
            if (!object.ReferenceEquals(mi.graphics.imageLocation, string.Empty))
            {
                relativeImageLocation = Path.Combine(FileUtility.RelativePathTo(Path.GetDirectoryName(mi.fname), Path.GetDirectoryName(mi.graphics.imageLocation)), Path.GetFileName(mi.graphics.imageLocation));
            }
            xyOutStream.WriteLine("BackgroundImageLocation " + relativeImageLocation);
            xyOutStream.WriteLine("BackgroundImageSize " + mi.graphics.imageSize);
            if (mi.ExtWaterRightsActive == true)
            {
                xyOutStream.WriteLine("WRInitialPriority " + mi.WRinitialpriority);
            }
            xyOutStream.WriteLine("DefaultFlowUnits " + mi.startingFlowUnits.Label);
            xyOutStream.WriteLine("DefaultStorageUnits " + mi.startingStorageUnits.Label);

            //GeoFactors
            if(mi.geoScaleFactorX!=1.0)
                xyOutStream.WriteLine("geoFactorX " + mi.geoScaleFactorX);
            if (mi.geoScaleFactorY != 1.0)
                xyOutStream.WriteLine("geoFactorY " + mi.geoScaleFactorY);
        }

        public static void WriteModelTimeSeriesInfo(Model mi, StreamWriter xyOutStream)
        {
            XYFileWriter.WriteBoolean("xyFileTimeSeries", xyOutStream, mi.timeseriesInfo.xyFileTimeSeries, true);
            if (!mi.timeseriesInfo.xyFileTimeSeries)
            {
                if (mi.timeseriesInfo.dbPath != "") xyOutStream.WriteLine("TimeSeriesDB " + mi.timeseriesInfo.dbPath);
                xyOutStream.WriteLine("TimeSeriesActive " + mi.timeseriesInfo.activeScn);
            }
        }


        public static void WriteModelDetails(Model mi, StreamWriter xyOutStream)
        {
            WriteAnnotations(mi, xyOutStream);
            xyOutStream.WriteLine("ially " + (Int32)mi.runType + " # " + mi.runType.ToString());
            //all years
            xyOutStream.WriteLine("iplot " + mi.iplot);
            xyOutStream.WriteLine("iprd " + mi.timeStep.TSType.ToString());

            // If the timestep is user-defined, write the timespan and the label.
            if (mi.timeStep.TSType == ModsimTimeStepType.UserDefined)
            {
                xyOutStream.WriteLine("UserDefTimeSpan " + mi.timeStep.UserDefSpan.ToString("R") + " # in days");
                xyOutStream.WriteLine("UserDefTSType " + mi.timeStep.UserDefTSType.ToString());
            }

            // Write other model details to the file.
            string str = null;
            if (mi.UseMetricUnits)
            {
                str = "1";
            }
            else
            {
                str = "0";
            }
            // 0: English Units, 1: Metric Units
            xyOutStream.WriteLine("iut " + str);
            XYFileWriter.WriteBoolean("ConvertUnits", xyOutStream, mi.ConvertUnitsAndFillTimeSeries, true);
            xyOutStream.WriteLine("n_years " + mi.Nyears);
            xyOutStream.WriteLine("rdim");
            xyOutStream.WriteLine("0 " + mi.dimensions_x1);
            xyOutStream.WriteLine("1 " + mi.dimensions_y1);
            xyOutStream.WriteLine("2 " + mi.dimensions_x2);
            xyOutStream.WriteLine("3 " + mi.dimensions_y2);
            xyOutStream.WriteLine("title " + mi.name);
            // uselags = 0  Model Generated lags, useLags = 1  user generated lags (default)
            xyOutStream.WriteLine("uselags " + mi.useLags);
            // max iterations
            xyOutStream.WriteLine("maxit " + mi.maxit);
            // criteira for groundwater convergence as percent
            xyOutStream.WriteLine(string.Format("gw_cp {0}", (decimal)mi.gw_cp));
            // criteira for flow through convergence as percent
            xyOutStream.WriteLine(string.Format("flowth_cp {0}", (decimal)mi.flowthru_cp));
            // criteira for storage convergence as percent
            xyOutStream.WriteLine(string.Format("evap_cp {0}", (decimal)mi.evap_cp));
            // restart on infeasible solution with added spill links
            xyOutStream.WriteLine("infeasrestart " + mi.infeasibleRestart);

            WriteHydrologicState(mi, xyOutStream);
            xyOutStream.WriteLine("relxacrul " + mi.relaxAccrual);
            xyOutStream.WriteLine("dataStartDate " + mi.TimeStepManager.dataStartDate.ToString(TimeManager.DateFormat));
            xyOutStream.WriteLine("dataEndDate " + mi.TimeStepManager.dataEndDate.ToString(TimeManager.DateFormat));
            xyOutStream.WriteLine("startingDate " + mi.TimeStepManager.startingDate.ToString(TimeManager.DateFormat));
            xyOutStream.WriteLine("endingDate " + mi.TimeStepManager.endingDate.ToString(TimeManager.DateFormat));
            xyOutStream.WriteLine("accrualDate " + mi.accrualDate.ToString(TimeManager.DateFormat));
            xyOutStream.WriteLine("seasCapDate " + mi.seasCapDate.ToString(TimeManager.DateFormat));
            if (mi.rentPoolDates != null)
            {
                for (int i = 0; i < mi.rentPoolDates.Count; i++)
                {
                    xyOutStream.WriteLine("rentPoolDates " + mi.rentPoolDates.Item(i).ToString(TimeManager.DateFormat));
                }
            }
            if (mi.accBalanceDates != null)
            {
                for (int i = 0; i < mi.accBalanceDates.Count; i++)
                {
                    xyOutStream.WriteLine("accBalanceDates " + mi.accBalanceDates.Item(i).ToString(TimeManager.DateFormat));
                }
            }

            xyOutStream.WriteLine("accuracy " + mi.accuracy);
            xyOutStream.WriteLine("defaultMaxCap " + mi.defaultMaxCap);

            WriteOutputControl(mi, xyOutStream);
            xyOutStream.WriteLine("nomaxmesg " + mi.nomaxitmessage);
            xyOutStream.WriteLine("constsysst " + mi.constMonthly);
            xyOutStream.WriteLine("IsBackRoutingActive " + Convert.ToInt32(mi.backRouting));
            xyOutStream.WriteLine("IsStorageAccountWithRouting " + Convert.ToInt32(mi.storageAccountsWithBackRouting));
            
        }

        public static void WriteOutputControl(Model mi, StreamWriter xyOutStream)
        {
            xyOutStream.WriteLine("res_output " + Convert.ToInt64(OutputControlInfo.res_output));
            xyOutStream.WriteLine("dws_rel " + Convert.ToInt64(OutputControlInfo.dws_rel));
            xyOutStream.WriteLine("elev_end " + Convert.ToInt64(OutputControlInfo.elev_end));
            xyOutStream.WriteLine("evp_loss " + Convert.ToInt64(OutputControlInfo.evp_loss));
            xyOutStream.WriteLine("evp_rate " + Convert.ToInt64(OutputControlInfo.evp_rate));
            xyOutStream.WriteLine("gwater " + Convert.ToInt64(OutputControlInfo.gwater));
            xyOutStream.WriteLine("head_avg " + Convert.ToInt64(OutputControlInfo.head_avg));
            xyOutStream.WriteLine("hydra_Cap " + Convert.ToInt64(OutputControlInfo.hydra_Cap));
            xyOutStream.WriteLine("powr_avg " + Convert.ToInt64(OutputControlInfo.powr_avg));
            xyOutStream.WriteLine("powr_pk " + Convert.ToInt64(OutputControlInfo.powr_pk));
            xyOutStream.WriteLine("pump_in " + Convert.ToInt64(OutputControlInfo.pump_in));
            xyOutStream.WriteLine("pump_out " + Convert.ToInt64(OutputControlInfo.pump_out));
            xyOutStream.WriteLine("pwr_2nd " + Convert.ToInt64(OutputControlInfo.pwr_2nd));
            xyOutStream.WriteLine("seepage " + Convert.ToInt64(OutputControlInfo.seepage));
            xyOutStream.WriteLine("spills " + Convert.ToInt64(OutputControlInfo.spills));
            xyOutStream.WriteLine("stor_beg " + Convert.ToInt64(OutputControlInfo.stor_beg));
            xyOutStream.WriteLine("stor_end " + Convert.ToInt64(OutputControlInfo.stor_end));
            xyOutStream.WriteLine("stor_trg " + Convert.ToInt64(OutputControlInfo.stor_trg));
            xyOutStream.WriteLine("unreg_in " + Convert.ToInt64(OutputControlInfo.unreg_in));
            xyOutStream.WriteLine("ups_rel " + Convert.ToInt64(OutputControlInfo.ups_rel));
            xyOutStream.WriteLine("dem_output " + Convert.ToInt64(OutputControlInfo.dem_output));
            xyOutStream.WriteLine("dem_sht " + Convert.ToInt64(OutputControlInfo.dem_sht));
            xyOutStream.WriteLine("demand " + Convert.ToInt64(OutputControlInfo.demand));
            xyOutStream.WriteLine("gw_in " + Convert.ToInt64(OutputControlInfo.gw_in));
            xyOutStream.WriteLine("surf_in " + Convert.ToInt64(OutputControlInfo.surf_in));
            xyOutStream.WriteLine("acc_flow " + Convert.ToInt64(OutputControlInfo.acc_flow));
            xyOutStream.WriteLine("acc_output " + Convert.ToInt64(OutputControlInfo.acc_output));
            xyOutStream.WriteLine("acrl " + Convert.ToInt64(OutputControlInfo.acrl));
            xyOutStream.WriteLine("stgl " + Convert.ToInt64(OutputControlInfo.stgl));
            xyOutStream.WriteLine("flo_flow " + Convert.ToInt64(OutputControlInfo.flo_flow));
            xyOutStream.WriteLine("flo_output " + Convert.ToInt64(OutputControlInfo.flo_output));
            xyOutStream.WriteLine("loss " + Convert.ToInt64(OutputControlInfo.loss));
            xyOutStream.WriteLine("natflow " + Convert.ToInt64(OutputControlInfo.natflow));
            xyOutStream.WriteLine("fromgwtono " + Convert.ToInt64(OutputControlInfo.fromgwtonode));
            xyOutStream.WriteLine("gw_output " + Convert.ToInt64(OutputControlInfo.gw_output));
            xyOutStream.WriteLine("noTimeStepsInMemorygwinfiltra " + Convert.ToInt64(OutputControlInfo.gwinfiltration));
            xyOutStream.WriteLine("spcl_rpts " + Convert.ToInt64(OutputControlInfo.special_rpts));
            xyOutStream.WriteLine("part_flows " + Convert.ToInt64(OutputControlInfo.partial_flows));
            xyOutStream.WriteLine("ver7OutputFiles " + Convert.ToInt64(OutputControlInfo.ver7OutputFiles));
            xyOutStream.WriteLine("ver8MSDBOutputFiles " + Convert.ToInt64(OutputControlInfo.ver8MSDBOutputFiles));
            xyOutStream.WriteLine("SQLiteOutputFiles " + Convert.ToInt64(OutputControlInfo.SQLiteOutputFiles));
            xyOutStream.WriteLine("DeleteTempVer8OutputFiles " + Convert.ToInt64(OutputControlInfo.DeleteTempVer8OutputFiles));
            xyOutStream.WriteLine("noTimeStepsInMemory " + Convert.ToInt64(OutputControlInfo.noTimeStepsInMemory));
            //ver 8.1 Output control vars
            xyOutStream.WriteLine("hydroStateOUT " + Convert.ToInt64(OutputControlInfo.hydroState));
            xyOutStream.WriteLine("nonStorage_output " + Convert.ToInt64(OutputControlInfo.nonStorage_output));
            xyOutStream.WriteLine("l_MaxOUT " + Convert.ToInt64(OutputControlInfo.l_Max));
            xyOutStream.WriteLine("l_MinOUT " + Convert.ToInt64(OutputControlInfo.l_Min));
            xyOutStream.WriteLine("Flow_ThruOUT " + Convert.ToInt64(OutputControlInfo.Flow_Thru));
            xyOutStream.WriteLine("Rout_RetOUT " + Convert.ToInt64(OutputControlInfo.Rout_Ret));
            xyOutStream.WriteLine("InflowOUT " + Convert.ToInt64(OutputControlInfo.Inflow));
        }


        public static void WriteHydrologicState(Model mi, StreamWriter xyOutStream)
        {
            try
            {
                if (mi.HydStateTables.Length == 0)
                {
                    return;
                }
                xyOutStream.WriteLine("--Hydrologic State Tables--");
                xyOutStream.WriteLine("n_hydr");
                // number of reserovirs for each hydrologic state table
                for (int i = 0; i < mi.HydStateTables.Length; i++)
                {
                    xyOutStream.WriteLine(i + " " + mi.HydStateTables[i].NumReservoirs);
                }
                // Nreservoirs.Length is number of Hydrologic state tables
                for (int tableNum = 0; tableNum < mi.HydStateTables.Length; tableNum++)
                {
                    xyOutStream.WriteLine("hydroTableName" + tableNum + " " + mi.HydStateTables[tableNum].TableName);
                    // write name for each Hyd State Table
                    xyOutStream.WriteLine("hydrosub " + tableNum);
                    // output node numbers of resevoirs
                    for (int j = 0; j < mi.HydStateTables[tableNum].NumReservoirs; j++)
                    {
                        xyOutStream.WriteLine(j + " " + mi.HydStateTables[tableNum].Reservoirs[j].number);
                    }

                    xyOutStream.WriteLine("NumHydBounds table " + tableNum + " " + mi.HydStateTables[tableNum].NumHydBounds);
                    xyOutStream.WriteLine("NumHydDates table " + tableNum + " " + mi.HydStateTables[tableNum].hydDates.Count);

                    int numrows = Convert.ToInt32(mi.HydStateTables[tableNum].hydBounds.Length / mi.HydStateTables[tableNum].NumHydBounds);
                    int numdates = mi.HydStateTables[tableNum].hydDates.Count;
                    if (numrows != numdates)
                    {
                        throw new Exception("Number of dates and rows don't match for hydrologic state table " + tableNum);
                    }
                    for (int monthIndex = 0; monthIndex < numrows; monthIndex++)
                    {
                        xyOutStream.Write(mi.HydStateTables[tableNum].hydDates.Item(monthIndex).ToString(TimeManager.DateFormat));
                        for (int stateIndex = 0; stateIndex < mi.HydStateTables[tableNum].NumHydBounds; stateIndex++)
                        {
                            xyOutStream.Write(XYFileWriter.xyFile.DataDivider[0] + mi.HydStateTables[tableNum].hydBounds[stateIndex, monthIndex].ToString().PadLeft(XYFileWriter.xyFile.DataNumOfSpaces));
                        }
                        xyOutStream.WriteLine();
                    }
                }

            }
            catch
            {
                mi.FireOnError("Error writing Hydrologic State Tables");
            }

        }

        /// <summary>Writes comments (annotations) to xy file</summary>
        /// <param name="mi">Instance of the Model class.</param>
        /// <param name="xyOutStream">The XY file stream.</param>
        /// <remarks>
        /// annot
        /// astr Link upstream of OWNdiv diversion point has 10
        /// aposx 563
        /// aposy 988
        /// annot
        /// astr Link OWNER to OWNdiv refers to link Channel Loss
        /// </remarks>
        public static void WriteAnnotations(Model mi, StreamWriter xyOutStream)
        {
            for (int i = 0; i < mi.Annotations.Count; i++)
            {
                xyOutStream.WriteLine("annot");
                xyOutStream.WriteLine("astr " + mi.Annotations.Item1(i).Text);
                xyOutStream.WriteLine("aposx " + mi.Annotations.Item1(i).x);
                xyOutStream.WriteLine("aposy " + mi.Annotations.Item1(i).y);
            }
        }

    }
}
