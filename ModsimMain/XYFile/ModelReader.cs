using System;
using System.IO;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.ModsimIO
{
    //ModelReader Class reads the  Model level commands in the xy file.
    public class ModelReader
    {
        // numHydBounds+1
        public static int numHydStates;
        public static int realNumHydTables;
        public static void ReadModelBasic(Model mi, TextFile file, int startIndex, int endIndex)
        {
            mi.runType = (ModsimRunType)XYFileReader.ReadInteger("ially", (Int32)ModsimRunType.Conditional_Rules, file, startIndex, endIndex);
            if (mi.inputVersion.Type == InputVersionType.V056 && mi.runType == ModsimRunType.Explicit_Targets && mi.HydStateTables.Length > 0)
            {
                mi.HydStateTables = new HydrologicStateTable[-1 + 1];
            }
            if (mi.inputVersion.Type == InputVersionType.V056)
            {
                numHydStates = XYFileReader.ReadInteger("nhydst", 3, file, startIndex, endIndex - 1);
                mi.numHydTables = XYFileReader.ReadInteger("hydtables", 1, file, startIndex, endIndex - 1);
                if (mi.runType == ModsimRunType.Explicit_Targets)
                {
                    realNumHydTables = 0;
                }
                else
                {
                    realNumHydTables = XYFileReader.ReadInteger("hydtables", 1, file, startIndex, endIndex - 1);
                }
            }
            else
            {
                //eventually we may get rid of mi.numHydTables
                mi.numHydTables = XYFileReader.ReadInteger("hydtables", 1, file, startIndex, endIndex - 1);
                realNumHydTables = XYFileReader.ReadInteger("numhydtables", 0, file, startIndex, endIndex - 1);
            }
            // real_links is updated by mi->AddLink() so we don't read it into the model
            XYFileReader.LinkCount = XYFileReader.ReadInteger("n_links", -1, file, 0, file.Count - 1);
            // real_nodes is updated by mi->AddNode() so we don't read it into the model
            XYFileReader.NodeCount = XYFileReader.ReadInteger("n_nodes", -1, file, 0, file.Count - 1);
            mi.nlags = XYFileReader.ReadInteger("n_lags", 0, file, startIndex, endIndex);
            // Extensions
            mi.ExtManualStorageRightActive = (XYFileReader.ReadString("ExtManualStorageRightActive", "false", file, startIndex, endIndex) == "True");
            mi.ExtStorageRightActive = (XYFileReader.ReadString("ExtStorageRightActive", "false", file, startIndex, endIndex) == "True");
            mi.ExtWaterRightsActive = (XYFileReader.ReadString("ExtWaterRightsActive", "false", file, startIndex, endIndex) == "True");
            mi.ExtLastFillRentActive = (XYFileReader.ReadString("ExtLastFillRentActive", "false", file, startIndex, endIndex) == "True");
            // Sizes
            mi.graphics.nonstorageSize = XYFileReader.ReadInteger("NonstorageNodeSize", 0, file, startIndex, endIndex);
            mi.graphics.demandSize = XYFileReader.ReadInteger("DemandNodeSize", 0, file, startIndex, endIndex);
            mi.graphics.reservoirSize = XYFileReader.ReadInteger("ReservoirNodeSize", 0, file, startIndex, endIndex);
            mi.graphics.linkSize = XYFileReader.ReadInteger("LinkSize", 0, file, startIndex, endIndex);
            string imageFile = XYFileReader.ReadString("BackgroundImageLocation", "", file, startIndex, endIndex);
            string absoluteImageFile = FileUtility.AbsolutePathTo(file.fname, imageFile);
            if (File.Exists(absoluteImageFile))
            {
                mi.graphics.imageLocation = absoluteImageFile;
            }
            mi.graphics.imageSize = XYFileReader.ReadFloat("BackgroundImageSize", 0, file, startIndex, endIndex);

            //geo Factors
            mi.geoScaleFactorX = XYFileReader.ReadFloat("geoFactorX", 1.0, file, startIndex, endIndex);
            mi.geoScaleFactorY = XYFileReader.ReadFloat("geoFactorY", 1.0, file, startIndex, endIndex);

            if (mi.ExtWaterRightsActive == true)
            {
                mi.WRinitialpriority = XYFileReader.ReadLong("WRInitialPriority", 0, file, startIndex, endIndex);
            }
            mi.startingFlowUnits = XYFileReader.ReadUnits(mi, "DefaultFlowUnits", mi.FlowUnits, file, startIndex, endIndex);
            mi.startingStorageUnits = XYFileReader.ReadUnits(mi, "DefaultStorageUnits", mi.StorageUnits, file, startIndex, endIndex);
            mi.timeseriesInfo.xyFileTimeSeries = XYFileReader.ReadBoolean("xyFileTimeSeries", true, file, startIndex, endIndex);
            mi.timeseriesInfo.dbPath = XYFileReader.ReadString("TimeSeriesDB", "", file, startIndex, endIndex) ;
            if (mi.timeseriesInfo.dbPath != "")
                mi.timeseriesInfo.dbFullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file.fname), mi.timeseriesInfo.dbPath));
            else
                mi.timeseriesInfo.dbFullPath = Path.GetFullPath(file.fname.Replace(".xy",".sqlite"));
            mi.timeseriesInfo.activeScn = XYFileReader.ReadInteger("TimeSeriesActive",0, file, startIndex, endIndex);
        }


        public static void ReadModelDetails(Model mi, TextFile file, int startIndex, int endIndex)
        {
            ReadAnnotations(mi, file);
            mi.iplot = XYFileReader.ReadInteger("iplot", 0, file, startIndex, endIndex);

            // Get the time step type... If older than MODSIM 8.2.0, convert the type.
            ModsimTimeStepType type = default(ModsimTimeStepType);
            if (mi.inputVersion.Type < InputVersionType.V8_2)
            {
                type = ModsimTimeStep.GetTSType((ModsimTimeStepType_V8_1)XYFileReader.ReadInteger("iprd", Convert.ToInt32(ModsimTimeStepType_V8_1.Monthly), file, startIndex, endIndex));
            }
            else
            {
                type = ModsimTimeStep.GetTSType(XYFileReader.ReadString("iprd", ModsimUnits.DefaultTimeStep.TSType.ToString(), file, startIndex, endIndex).Trim());
            }

            // Define the time step object... It's different if user-defined and it's different with different versions of MODSIM xy file input...
            if (type == ModsimTimeStepType.UserDefined || type == ModsimTimeStepType.Undefined || type == ModsimTimeStepType.Seconds || type == ModsimTimeStepType.FifteenMin || type == ModsimTimeStepType.Hourly)
            {
                // Throw Exception if input is not version 8.2.0 or higher
                if (mi.inputVersion.Type < InputVersionType.V8_2)
                {
                    throw new Exception("Version " + XYFileReader.XYVersion + " does not support user-defined timesteps.");
                }

                // Get extra info for user-defined timestep.
                if (type == ModsimTimeStepType.UserDefined)
                {
                    double span = XYFileReader.ReadFloat("UserDefTimeSpan", ModsimUnits.DefaultTimeStep.UserDefSpan, file, startIndex, endIndex);
                    ModsimUserDefinedTimeStepType TSType = ModsimUserDefinedTimeStep.GetUserDefTSType(XYFileReader.ReadString("UserDefTSType", ModsimUnits.DefaultTimeStep.UserDefTSType.ToString(), file, startIndex, endIndex));
                    mi.timeStep = new ModsimTimeStep(span, TSType);
                }
                else
                {
                    mi.timeStep = new ModsimTimeStep(type);
                }
            }
            else
            {
                mi.timeStep = new ModsimTimeStep(type);
            }

            // Fill other model parameters
            mi.UseMetricUnits = Convert.ToBoolean(XYFileReader.ReadInteger("iut", 0, file, startIndex, endIndex));
            if (mi.inputVersion.Type >= InputVersionType.V8_2)
            {
                mi.ConvertUnitsAndFillTimeSeries = XYFileReader.ReadBoolean("ConvertUnits", true, file, startIndex, endIndex);
            }
            mi.Nyears = XYFileReader.ReadInteger("n_years", 1, file, 0, file.Count - 1);
            long[] tmpIntList = XYFileReader.ReadIndexedIntegerList("rdim", 4, 0, file, startIndex, endIndex);
            mi.dimensions_x1 = Convert.ToInt32(tmpIntList[0]);
            mi.dimensions_y1 = Convert.ToInt32(tmpIntList[1]);
            mi.dimensions_x2 = Convert.ToInt32(tmpIntList[2]);
            mi.dimensions_y2 = Convert.ToInt32(tmpIntList[3]);
            mi.name = XYFileReader.ReadString("title", "", file, 0, file.Count - 1);
            mi.useLags = XYFileReader.ReadInteger("uselags", 1, file, startIndex, endIndex);
            mi.maxit = XYFileReader.ReadInteger("maxit", 100, file, startIndex, endIndex);
            // criteira for groundwater convergence as percent
            mi.gw_cp = XYFileReader.ReadFloat("gw_cp", mi.gw_cp, file, startIndex, endIndex);
            // criteira for flow through convergence as percent
            mi.flowthru_cp = XYFileReader.ReadFloat("flowth_cp", mi.flowthru_cp, file, startIndex, endIndex);
            // criteira for storage convergence as percent
            mi.evap_cp = XYFileReader.ReadFloat("evap_cp", mi.evap_cp, file, startIndex, endIndex);
            mi.infeasibleRestart = Convert.ToInt16(XYFileReader.ReadInteger("infeasrestart", 0, file, startIndex, endIndex));
            mi.relaxAccrual = Convert.ToInt16(XYFileReader.ReadInteger("relxacrul", 0, file, startIndex, endIndex));
            if (mi.inputVersion.Type == InputVersionType.V056)
            {
                ModelReader.ReadVersion7Dates(mi, file, startIndex, endIndex);
                mi.constMonthly = XYFileReader.ReadInteger("constsysst", 0, file, startIndex, endIndex);
            }
            else
            {
                // version 8 uses standard date formats  see:  XYFileWriter.OutDateFormat
                DateTime defaultDate = new DateTime(1900, 1, 1);
                mi.TimeStepManager.dataStartDate = XYFileReader.ReadDateTime("dataStartDate", file, defaultDate, startIndex, endIndex);
                mi.TimeStepManager.dataEndDate = XYFileReader.ReadDateTime("dataEndDate", file, defaultDate, startIndex, endIndex);
                mi.TimeStepManager.startingDate = XYFileReader.ReadDateTime("startingDate", file, defaultDate, startIndex, endIndex);
                mi.TimeStepManager.endingDate = XYFileReader.ReadDateTime("endingDate", file, defaultDate, startIndex, endIndex);
                mi.accrualDate = XYFileReader.ReadDateTime("accrualDate", file, defaultDate, startIndex, endIndex);
                mi.seasCapDate = XYFileReader.ReadDateTime("seasCapDate", file, defaultDate, startIndex, endIndex);
                mi.TimeStepManager.UpdateTimeStepsInfo(mi.timeStep);
                int idxRent = file.FindBeginningWith("rentPoolDates", startIndex, endIndex);
                while (idxRent > 0)
                {
                    DateTime rpdate = XYFileReader.ReadDateTime("rentPoolDates", file, defaultDate, idxRent, endIndex);
                    idxRent = file.FindBeginningWith("rentPoolDates", idxRent + 1, endIndex);
                    mi.rentPoolDates.Add(rpdate);
                }
                int idxBal = file.FindBeginningWith("accBalanceDates", startIndex, endIndex);
                while (idxBal > 0)
                {
                    DateTime abdate = XYFileReader.ReadDateTime("accBalanceDates", file, defaultDate, idxBal, endIndex);
                    idxBal = file.FindBeginningWith("accBalanceDates", idxBal + 1, endIndex);
                    mi.accBalanceDates.Add(abdate);
                }
            }
            ReadHydrologicState(mi, file, startIndex, endIndex);
            //need to know dates before reading hydrostate
            mi.accuracy = XYFileReader.ReadInteger("accuracy", 0, file, startIndex, endIndex);
            if (mi.inputVersion.Type >= InputVersionType.V8_2)
            {
                mi.defaultMaxCap = XYFileReader.ReadLong("defaultMaxCap", DefineConstants.origDefMaxLinkCap, file, startIndex, endIndex);
            }
            else
            {
                if (mi.accuracy != 0)
                {
                    mi.accuracy = 2;
                }
                mi.defaultMaxCap = 99999999;
            }
            ReadOutputControl(mi, file, startIndex, endIndex);
            mi.nomaxitmessage = XYFileReader.ReadInteger("nomaxmesg", 0, file, startIndex, endIndex);
            mi.backRouting = Convert.ToBoolean(XYFileReader.ReadInteger("IsBackRoutingActive", 0, file, startIndex, endIndex));
            mi.storageAccountsWithBackRouting = Convert.ToBoolean(XYFileReader.ReadInteger("IsStorageAccountWithRouting", 0, file, startIndex, endIndex));
            
            
        }

        public static void ReadOutputControl(Model mi, TextFile file, int startIndex, int endIndex)
        {
            mi.controlinfo = new OutputControlInfo();
            OutputControlInfo.res_output = Convert.ToBoolean(XYFileReader.ReadInteger("res_output", 1, file, startIndex, endIndex));
            OutputControlInfo.dws_rel = Convert.ToBoolean(XYFileReader.ReadInteger("dws_rel", 1, file, startIndex, endIndex));
            OutputControlInfo.elev_end = Convert.ToBoolean(XYFileReader.ReadInteger("elev_end", 1, file, startIndex, endIndex));
            OutputControlInfo.evp_loss = Convert.ToBoolean(XYFileReader.ReadInteger("evp_loss", 1, file, startIndex, endIndex));
            OutputControlInfo.evp_rate = Convert.ToBoolean(XYFileReader.ReadInteger("evp_rate", 1, file, startIndex, endIndex));
            OutputControlInfo.gwater = Convert.ToBoolean(XYFileReader.ReadInteger("gwater", 1, file, startIndex, endIndex));
            OutputControlInfo.head_avg = Convert.ToBoolean(XYFileReader.ReadInteger("head_avg", 1, file, startIndex, endIndex));
            OutputControlInfo.hydra_Cap = Convert.ToBoolean(XYFileReader.ReadInteger("hydra_Cap", 1, file, startIndex, endIndex));
            OutputControlInfo.powr_avg = Convert.ToBoolean(XYFileReader.ReadInteger("powr_avg", 1, file, startIndex, endIndex));
            OutputControlInfo.powr_pk = Convert.ToBoolean(XYFileReader.ReadInteger("powr_pk", 1, file, startIndex, endIndex));
            OutputControlInfo.pump_in = Convert.ToBoolean(XYFileReader.ReadInteger("pump_in", 1, file, startIndex, endIndex));
            OutputControlInfo.pump_out = Convert.ToBoolean(XYFileReader.ReadInteger("pump_out", 1, file, startIndex, endIndex));
            OutputControlInfo.pwr_2nd = Convert.ToBoolean(XYFileReader.ReadInteger("pwr_2nd", 1, file, startIndex, endIndex));
            OutputControlInfo.seepage = Convert.ToBoolean(XYFileReader.ReadInteger("seepage", 1, file, startIndex, endIndex));
            OutputControlInfo.spills = Convert.ToBoolean(XYFileReader.ReadInteger("spills", 1, file, startIndex, endIndex));
            OutputControlInfo.stor_beg = Convert.ToBoolean(XYFileReader.ReadInteger("stor_beg", 1, file, startIndex, endIndex));
            OutputControlInfo.stor_end = Convert.ToBoolean(XYFileReader.ReadInteger("stor_end", 1, file, startIndex, endIndex));
            OutputControlInfo.stor_trg = Convert.ToBoolean(XYFileReader.ReadInteger("stor_trg", 1, file, startIndex, endIndex));
            OutputControlInfo.unreg_in = Convert.ToBoolean(XYFileReader.ReadInteger("unreg_in", 1, file, startIndex, endIndex));
            OutputControlInfo.ups_rel = Convert.ToBoolean(XYFileReader.ReadInteger("ups_rel", 1, file, startIndex, endIndex));
            OutputControlInfo.dem_output = Convert.ToBoolean(XYFileReader.ReadInteger("dem_output", 1, file, startIndex, endIndex));
            OutputControlInfo.dem_sht = Convert.ToBoolean(XYFileReader.ReadInteger("dem_sht", 1, file, startIndex, endIndex));
            OutputControlInfo.demand = Convert.ToBoolean(XYFileReader.ReadInteger("demand", 1, file, startIndex, endIndex));
            OutputControlInfo.gw_in = Convert.ToBoolean(XYFileReader.ReadInteger("gw_in", 1, file, startIndex, endIndex));
            OutputControlInfo.surf_in = Convert.ToBoolean(XYFileReader.ReadInteger("surf_in", 1, file, startIndex, endIndex));
            OutputControlInfo.acc_flow = Convert.ToBoolean(XYFileReader.ReadInteger("acc_flow", 1, file, startIndex, endIndex));
            OutputControlInfo.acc_output = Convert.ToBoolean(XYFileReader.ReadInteger("acc_output", 1, file, startIndex, endIndex));
            OutputControlInfo.acrl = Convert.ToBoolean(XYFileReader.ReadInteger("acrl", 1, file, startIndex, endIndex));
            OutputControlInfo.stgl = Convert.ToBoolean(XYFileReader.ReadInteger("stgl", 1, file, startIndex, endIndex));
            OutputControlInfo.flo_flow = Convert.ToBoolean(XYFileReader.ReadInteger("flo_flow", 1, file, startIndex, endIndex));
            OutputControlInfo.flo_output = Convert.ToBoolean(XYFileReader.ReadInteger("flo_output", 1, file, startIndex, endIndex));
            OutputControlInfo.loss = Convert.ToBoolean(XYFileReader.ReadInteger("loss", 1, file, startIndex, endIndex));
            OutputControlInfo.natflow = Convert.ToBoolean(XYFileReader.ReadInteger("natflow", 1, file, startIndex, endIndex));
            OutputControlInfo.fromgwtonode = Convert.ToBoolean(XYFileReader.ReadInteger("fromgwtono", 1, file, startIndex, endIndex));
            OutputControlInfo.gw_output = Convert.ToBoolean(XYFileReader.ReadInteger("gw_output", 1, file, startIndex, endIndex));
            OutputControlInfo.gwinfiltration = Convert.ToBoolean(XYFileReader.ReadInteger("gwinfiltra", 1, file, startIndex, endIndex));
            OutputControlInfo.special_rpts = Convert.ToBoolean(XYFileReader.ReadInteger("spcl_rpts", 0, file, startIndex, endIndex));
            OutputControlInfo.partial_flows = Convert.ToBoolean(XYFileReader.ReadInteger("part_flows", 0, file, startIndex, endIndex));
            OutputControlInfo.ver7OutputFiles = Convert.ToBoolean(XYFileReader.ReadInteger("ver7OutputFiles", 0, file, startIndex, endIndex));
            OutputControlInfo.ver8MSDBOutputFiles = Convert.ToBoolean(XYFileReader.ReadInteger("ver8MSDBOutputFiles", 0, file, startIndex, endIndex));
            OutputControlInfo.SQLiteOutputFiles = Convert.ToBoolean(XYFileReader.ReadInteger("SQLiteOutputFiles", 0, file, startIndex, endIndex));
            OutputControlInfo.DeleteTempVer8OutputFiles = Convert.ToBoolean(XYFileReader.ReadInteger("DeleteTempVer8OutputFiles", 1, file, startIndex, endIndex));
            OutputControlInfo.noTimeStepsInMemory = XYFileReader.ReadInteger("noTimeStepsInMemory", 12, file, startIndex, endIndex);
            //ver8.1 output flags
            OutputControlInfo.hydroState = Convert.ToBoolean(XYFileReader.ReadInteger("hydroStateOUT", 1, file, startIndex, endIndex));
            OutputControlInfo.nonStorage_output = Convert.ToBoolean(XYFileReader.ReadInteger("nonStorage_output", 1, file, startIndex, endIndex));
            OutputControlInfo.l_Max = Convert.ToBoolean(XYFileReader.ReadInteger("l_MaxOUT", 1, file, startIndex, endIndex));
            OutputControlInfo.l_Min = Convert.ToBoolean(XYFileReader.ReadInteger("l_MinOUT", 1, file, startIndex, endIndex));
            OutputControlInfo.Flow_Thru = Convert.ToBoolean(XYFileReader.ReadInteger("Flow_ThruOUT", 1, file, startIndex, endIndex));
            OutputControlInfo.Rout_Ret = Convert.ToBoolean(XYFileReader.ReadInteger("Rout_RetOUT", 1, file, startIndex, endIndex));
            OutputControlInfo.Inflow = Convert.ToBoolean(XYFileReader.ReadInteger("InflowOUT", 1, file, startIndex, endIndex));
        }
        public static void ReadVersion7Dates(Model mi, TextFile file, int startIndex, int endIndex)
        {
            mi.TimeStepManager.dataStartDate = XYFileReader.ReadLegacyDate("startdate", new DateTime(1, 1, 1), file, startIndex, endIndex);
            if (mi.TimeStepManager.dataStartDate.Year == 1 && mi.TimeStepManager.dataStartDate.Month == 1 && mi.TimeStepManager.dataStartDate.Day == 1)
            {
                // really old xy file does not have startdate command - try to look for year1 and month1
                int year1 = XYFileReader.ReadInteger("year1", 1, file, startIndex, endIndex);
                if (year1 == 1)
                {
                    throw new Exception(" I can't find anything that resembles a starting date - I give up");
                }
                int month1 = XYFileReader.ReadInteger("month1", 1, file, startIndex, endIndex);
                mi.TimeStepManager.dataStartDate = new DateTime(year1, month1, 1);
            }
            // older version had adjustsment for water year - when we read Oct 1961, this is really Oct 1960
            // water years in version 7 interface
            if (mi.timeStep.TSType == ModsimTimeStepType.Monthly && mi.TimeStepManager.dataStartDate.Month != 1)
            {
                mi.TimeStepManager.dataStartDate = mi.TimeStepManager.dataStartDate.AddYears(-1);
            }
            LegacyTimeStep[] stdates = null;
            stdates = XYFileReader.ReadLegacyTimeStep("stdates", file, startIndex, endIndex);
            if (stdates[0].InXYFile == true && stdates[0].major != 1)
            {
                switch (mi.timeStep.TSType)
                {
                    case ModsimTimeStepType.Monthly:
                        mi.TimeStepManager.startingDate = mi.TimeStepManager.dataStartDate.AddYears(stdates[0].major - 1);
                        break;
                    case ModsimTimeStepType.Weekly:
                        mi.TimeStepManager.startingDate = mi.TimeStepManager.dataStartDate.AddDays((stdates[0].major - 1) * 12 * 7);
                        break;
                    case ModsimTimeStepType.Daily:
                        mi.TimeStepManager.startingDate = mi.TimeStepManager.dataStartDate.AddDays((stdates[0].major - 1) * 7);
                        break;
                    case ModsimTimeStepType.TenDays:
                        mi.TimeStepManager.startingDate = mi.TimeStepManager.dataStartDate.AddDays((stdates[0].major - 1) * 9 * 10);
                        break;
                    default:
                        throw new Exception("Version 7 import for the timestep of type '" + mi.timeStep.Label + "' is not supported.");
                }
            }
            else
            {
                mi.TimeStepManager.startingDate = mi.TimeStepManager.dataStartDate;
            }
            mi.TimeStepManager.UpdateTimeStepsInfo(mi.timeStep);

            if (mi.timeStep.TSType == ModsimTimeStepType.Monthly)
            {
                LegacyTimeStep[] rpdates = null;
                rpdates = XYFileReader.ReadLegacyTimeStep("rpdates", file, startIndex, endIndex);
                if (rpdates != null)
                {
                    mi.rentPoolDates = new DateList();
                    for (int i = 0; i < rpdates.Length; i++)
                    {
                        mi.rentPoolDates.Add(rpdates[i].ToMonthlyDate(mi.TimeStepManager.startingDate));
                        mi.FireOnMessage("rent pool Month (" + i.ToString() + " ) = " + mi.rentPoolDates.Item(i).ToString("MMM"));
                    }
                }

                LegacyTimeStep[] abdates = null;
                abdates = XYFileReader.ReadLegacyTimeStep("abdates", file, startIndex, endIndex);
                if (abdates != null)
                {
                    mi.accBalanceDates = new DateList();
                    for (int i = 0; i < abdates.Length; i++)
                    {
                        mi.accBalanceDates.Add(abdates[i].ToMonthlyDate(mi.TimeStepManager.startingDate));
                        mi.FireOnMessage("account balance Month (" + i.ToString() + " ) = " + mi.accBalanceDates.Item(i).ToString("MMM"));
                    }
                }
            }
            LegacyTimeStep[] scdates = null;
            scdates = XYFileReader.ReadLegacyTimeStep("scdates", file, startIndex, endIndex);
            if (scdates[0].InXYFile == true)
            {
                if (mi.timeStep.TSType == ModsimTimeStepType.Monthly)
                {
                    scdates[0].major = mi.TimeStepManager.dataStartDate.Month;
                }
                DateTime scDate = new DateTime(1900, scdates[0].major, scdates[0].minor);
                mi.seasCapDate = scDate;
                mi.FireOnMessage("seasonal capacity Date  = " + scDate.ToString("ddd-MMM"));
            }
            if (mi.timeStep.TSType == ModsimTimeStepType.Monthly)
            {
                LegacyTimeStep[] accdates = null;
                accdates = XYFileReader.ReadLegacyTimeStep("accdates", file, startIndex, endIndex);
                if (accdates != null)
                {
                    mi.accrualDate = accdates[0].ToMonthlyDate(mi.TimeStepManager.startingDate);
                    mi.FireOnMessage("Accrual Month = " + mi.accrualDate.ToString("MMM"));
                }
            }
            LegacyTimeStep[] enddates = null;
            enddates = XYFileReader.ReadLegacyTimeStep("enddates", file, startIndex, endIndex);
            if (enddates[0].InXYFile == false)
            {
                enddates[0].major = mi.Nyears;
            }
            else
            {
                if (enddates[0].major < stdates[0].major)
                {
                    mi.FireOnError(" Ending date is before starting date - I'm going to set the ending date for you");
                    mi.FireOnError("  Ending date will be set to start date + the specified end date but not greater than the end of data");
                    enddates[0].major += (stdates[0].major - 1);
                    if (enddates[0].major > mi.Nyears)
                    {
                        enddates[0].major = mi.Nyears;
                    }
                }
            }
            switch (mi.timeStep.TSType)
            {
                case ModsimTimeStepType.Monthly:
                    //example: dataStartDate = 0ct 1, 1927 (after adjustment above)
                    // enddates.major = 73
                    // we want ending date as sep 30, 2000
                    mi.TimeStepManager.endingDate = mi.TimeStepManager.dataStartDate.AddYears(enddates[0].major);
                    //.AddMinutes(-1.0)
                    mi.TimeStepManager.dataEndDate = mi.TimeStepManager.dataStartDate.AddYears(mi.Nyears);
                    //.AddMinutes(-1.0)
                    break;
                case ModsimTimeStepType.Weekly:
                    mi.TimeStepManager.endingDate = mi.TimeStepManager.dataStartDate.AddDays((enddates[0].major) * 12 * 7);
                    mi.TimeStepManager.dataEndDate = mi.TimeStepManager.dataStartDate.AddDays((mi.Nyears) * 12 * 7);
                    break;
                case ModsimTimeStepType.Daily:
                    mi.TimeStepManager.endingDate = mi.TimeStepManager.dataStartDate.AddDays((enddates[0].major * 7));
                    mi.TimeStepManager.dataEndDate = mi.TimeStepManager.dataStartDate.AddDays((mi.Nyears) * 7);
                    break;
                case ModsimTimeStepType.TenDays:
                    mi.TimeStepManager.endingDate = mi.TimeStepManager.dataStartDate.AddDays((enddates[0].major) * 9 * 10);
                    mi.TimeStepManager.dataEndDate = mi.TimeStepManager.dataStartDate.AddDays((mi.Nyears) * 9 * 10);
                    break;
            }
            mi.TimeStepManager.UpdateTimeStepsInfo(mi.timeStep);
        }

        public static void ReadHydrologicStateTableReservoirNodes(Model mi, TextFile file, int startIndex, int endIndex)
        {
            if (mi.HydStateTables == null)
            {
                return;
            }
            if (mi.HydStateTables.Length == 0)
            {
                return;
            }
            long[] Nreservoirs = XYFileReader.ReadIndexedIntegerList("n_hydr", 0, file, 0, file.Count - 1);
            if (Nreservoirs.Length == 0)
            {
                if (mi.HydStateTables.Length <= 1)
                {
                    mi.FireOnError(" No complete hydrologic state tables were found.");
                    throw new Exception(" Incomplete hydrologic state table");
                }
            }
            for (int tableNum = 0; tableNum < mi.HydStateTables.Length; tableNum++)
            {
                HydrologicStateTable hydTable = mi.HydStateTables[tableNum];
                string tmpStr = "hydrosub " + tableNum;
                long[] tmpNodeNumbers = XYFileReader.ReadIndexedIntegerList(tmpStr, 0, file, startIndex, endIndex);
                if (tmpNodeNumbers.Length != Nreservoirs[tableNum])
                {
                    mi.FireOnError("Number of model tables" + mi.HydStateTables.Length.ToString());
                    mi.FireOnError("Table " + tableNum.ToString() + " has " + Nreservoirs.Length.ToString() + " reservoirs defined");
                    throw new Exception("Error:  Hydrologic Tables not defined properly.  xy file line " + startIndex);
                }
                Node[] ReservoirList = new Node[tmpNodeNumbers.Length];
                for (int resIndex = 0; resIndex < tmpNodeNumbers.Length; resIndex++)
                {
                    ReservoirList[resIndex] = mi.FindNode(Convert.ToInt32(tmpNodeNumbers[resIndex]));
                }
                hydTable.Reservoirs = ReservoirList;
            }
        }

        public static void ReadHydrologicState(Model mi, TextFile file, int mainStartIndex, int endIndex)
        {
            int startIndex = 0;
            if (mi.inputVersion.Type == InputVersionType.V056)
            {
                if (mi.runType == ModsimRunType.Explicit_Targets)
                {
                    mi.HydStateTables = null;
                    return;
                }
                else
                {
                    if (mi.timeStep.TSType != ModsimTimeStepType.Monthly)
                    {
                        throw new Exception("Hydrologic State really did not have a meaning for old xy files that were not Monthly time step");
                    }
                }
                startIndex = mainStartIndex;
            }
            else
            {
                startIndex = file.FindBeginningWith("--Hydrologic State Tables--", mainStartIndex, endIndex);
            }
            long[] Nreservoirs = XYFileReader.ReadIndexedIntegerList("n_hydr", 0, file, 0, file.Count - 1);
            if (Nreservoirs.Length > 0 && realNumHydTables == 0)
            {
                realNumHydTables = 1;
            }
            int NumHydBoundsLineIndex = 0;
            // for v8 only
            int numDates = 0;
            // v8
            HydrologicStateTable[] HydTableList = null;
            HydTableList = new HydrologicStateTable[realNumHydTables];
            for (int tableNum = 0; tableNum < realNumHydTables; tableNum++)
            {
                HydrologicStateTable hydTable = new HydrologicStateTable();
                hydTable.TableName = XYFileReader.ReadString("hydroTableName" + tableNum, "HydroTable" + tableNum, file, startIndex, endIndex);
                int numStates = 0;
                if (mi.inputVersion.Type == InputVersionType.V056)
                {
                    numStates = ModelReader.numHydStates;
                    // constant for all hyd tables
                    numDates = 12;
                }
                else
                {
                    numStates = XYFileReader.ReadInteger("NumHydBounds table " + tableNum, -1, file, startIndex, endIndex);
                    numDates = XYFileReader.ReadInteger("NumHydDates table " + tableNum, -1, file, startIndex, endIndex);
                    NumHydBoundsLineIndex = file.FindBeginningWith("NumHydBounds table " + tableNum, startIndex, endIndex);
                    NumHydBoundsLineIndex = NumHydBoundsLineIndex + 2;
                    if (numStates == -1)
                    {
                        throw new Exception("Error:  could not find " + "NumHydBounds table " + tableNum);
                    }
                    numStates = numStates + 1;
                }

                double[,] tmpHydBounds = new double[numStates - 1, numDates];

                if (mi.inputVersion.Type == InputVersionType.V056)
                {
                    DateTime rowdate = mi.TimeStepManager.dataStartDate;
                    // The number of columns in hyd bounds is 1 less thatn number of states
                    for (int stateIndex = 0; stateIndex < numStates - 1; stateIndex++)
                    {
                        string tmpStr2 = "hydbounds " + stateIndex + " " + tableNum;
                        double[] tmpFloatList = XYFileReader.ReadIndexedFloatList(tmpStr2, 0, file, startIndex, endIndex);
                        if (tmpFloatList.Length == 0)
                        {
                            tmpFloatList = new double[12];
                            for (int monthIndex = 0; monthIndex <= 11; monthIndex++)
                            {
                                tmpFloatList[monthIndex] = 0.0;
                            }
                        }
                        if (tmpFloatList.Length < 12)
                        {
                            int numRead = tmpFloatList.Length;
                            Array.Resize(ref tmpFloatList, 12);
                            for (int monthIndex = numRead; monthIndex <= 11; monthIndex++)
                            {
                                tmpFloatList[monthIndex] = 0.0;
                            }
                        }
                        for (int monthIndex = 0; monthIndex < numDates; monthIndex++)
                        {
                            tmpHydBounds[stateIndex, monthIndex] = tmpFloatList[monthIndex];
                            if (stateIndex == 0)
                            {
                                hydTable.hydDates.Add(rowdate);
                                rowdate = mi.timeStep.IncrementDate(rowdate);
                            }
                        }
                    }
                    hydTable.hydBounds = tmpHydBounds;
                    HydTableList[tableNum] = hydTable;
                }
                else
                {
                    //                            ^
                    //                0123456789001234567890123456789
                    //08/01/1980 00:00      0.016       0.02
                    //09/01/1980 00:00      0.011       0.01
                    //mi.HydStateTables
                    hydTable.hydDates = new DateList();
                    for (int dateIndex = 0; dateIndex < numDates; dateIndex++)
                    {
                        string linestring = file[NumHydBoundsLineIndex + dateIndex];
                        DateTime rowdate = TimeManager.missingDate;
                        string[] data = null;
                        XYFileReader.GetTSData(linestring, out rowdate, out data);
                        hydTable.hydDates.Add(rowdate);

                        for (int k = 0; k < numStates - 1; k++)
                        {
                            tmpHydBounds[k, dateIndex] = Convert.ToDouble(data[k]);
                        }
                    }
                    hydTable.VariesByTimeStep = (numDates > 1);
                    hydTable.hydBounds = tmpHydBounds;
                    HydTableList[tableNum] = hydTable;
                }
            }
            mi.HydStateTables = HydTableList;
        }
        public static void ReadAnnotations(Model mi, TextFile file)
        {
            int lineCounter = 0;
            while (lineCounter < file.Count)
            {
                int startIndex = file.Find("annot", lineCounter, file.Count - 1);
                if (startIndex == -1)
                {
                    return;
                }
                // read annotation
                string lineString = file[startIndex];
                if ((file[startIndex + 1].IndexOf("astr ") != 0 || file[startIndex + 2].IndexOf("aposx ") != 0 || file[startIndex + 3].IndexOf("aposy ") != 0))
                {
                    mi.FireOnError("Error: (non-fatal) invalid annotation.  xy file line " + startIndex);
                    lineCounter = startIndex + 1;
                }
                else
                {
                    Annotate ann = new Annotate();
                    ann.Text = XYFileReader.ReadString("astr", "", file, startIndex + 1, startIndex + 1);
                    ann.x = XYFileReader.ReadInteger("aposx", 0, file, startIndex + 2, startIndex + 2);
                    ann.y = XYFileReader.ReadInteger("aposy", 0, file, startIndex + 3, startIndex + 3);
                    mi.Annotations.Add(ann);
                    lineCounter = startIndex + 1;
                }
            }
        }

    }
}
