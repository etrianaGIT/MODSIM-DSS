using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.ModsimIO
{
    /// <summary>Reads a formatted .xy file from disk</summary>
    /// <remarks></remarks>
    public class XYFileReader
    {
        public static Node[] NodeArray;
        public static Link[] LinkArray;
        public static int NodeCount;
        public static int LinkCount;
        public static string XYVersion;
        private static InputVersion version = new InputVersion(InputVersionType.Undefined);
        private static XYFile xyFile;

        public static void Read(Model mi, string filename)
        {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                Model.RefModel = mi;
                mi.FireOnMessage("reading " + filename);
                TextFile file = new TextFile(filename);
                XYVersion = XYFileReader.ReadString("xyVersion", "xxxx", file, 0, file.Count - 1);
                mi.inputVersion = new InputVersion(XYVersion);
                mi.outputVersion = new OutputVersion(mi.inputVersion.Type);
                xyFile = new XYFile(mi, DirectionType.Input);
                if (mi.inputVersion.Type == InputVersionType.Undefined)
                {
                    string msg = "Error: this xy file version is not recognized. It could have been created with a newer version or a version older than 7.2.  If older, try to read this file first with a such as version 7.2. If newer, try reading it with a newer version.";
                    mi.FireOnError(msg);
                    throw new XYFileVersionException(msg);
                }
                ModelReader.ReadModelBasic(mi, file, 0, file.Count - 1);
                ModelReader.ReadModelDetails(mi, file, 0, file.Count - 1);
                mi.TimeStepManager.UpdateTimeStepsInfo(mi.timeStep);
                mi.fname = filename;
                HydroReader.ReadBeforeNodes(mi, file);
                NodeReader.CreateNodes(mi, file);
                // reads through xy file and creates all nodes
                LinkReader.CreateLinks(mi, file);
                // reads through xy file and creates all links
                NodeReader.ReadNodes(mi, file);
                // reads through xy file and reads details for all nodes
                LinkReader.ReadLinks(mi, file);
                // reads through xy file and reads details for all links
                HydroReader.ReadAfterNodes(mi, file);
                //Read time series if stored in the database
                NetworkUtils.ModelOutputSupport myMODSIMOutput = new NetworkUtils.ModelOutputSupport(mi, false, false);
                if (!mi.timeseriesInfo.xyFileTimeSeries)
                {
                    myMODSIMOutput.TimeseriesFromSQLite(ref mi);
                }
                else
                {
                    // checks to ensure that all the units for all timeseries were read in correctly. Changes any units if they are off.
                    mi.CheckUnits();
                }
                
                SetHasOwnerRentLinksFlags(mi);
                if (mi.HasOwnerLinks)
                {
                    SanityCheckAccrualLinksCapacities(mi);
                }
                ModelReader.ReadHydrologicStateTableReservoirNodes(mi, file, 0, file.Count - 1);
                if (mi.inputVersion.Type == InputVersionType.V056)
                {
                    //Ver7Upgrade.TruncateTimeManager(mi)
                    Ver7Upgrade.LinksLoopForConversion(mi);
                    Ver7Upgrade.NodesLoopForConversion(mi);
                    if (mi.runType == ModsimRunType.Explicit_Targets)
                    {
                        mi.HydStateTables = new HydrologicStateTable[-1 + 1];
                    }
                }
                if (mi.inputVersion.Type < InputVersionType.V8_4_0)
                {
                    Ver8_4Upgrade.AdjustXYPointsToVer8_4(mi);
                }
                CheckForExtentions(mi);
                CheckForNegativeValues(mi);
                CheckReservoirTargetsStartDate(mi);
                CheckForDuplicateNodeLinkNames(mi);
                sw.Stop();
                mi.FireOnMessage(string.Format("Successfully Read MODSIM Network (elapsed: {0:0.000})", sw.Elapsed.TotalMinutes));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private static void CheckForDuplicateNodeLinkNames(Model mi)
        {
            var currentNodes = new List<Node>(mi.Nodes_All);
            var currentLinks = new List<Link>(mi.Links_All);

            var dupNodes = from s in currentNodes
                           group s by s.name into groupValue
                           where groupValue.Count() > 1
                           select new
                           {
                               name = groupValue.Key,
                               count = groupValue.Count()
                           };

            var dupLinks = from s in currentLinks
                           group s by s.name into groupValue
                           where groupValue.Count() > 1
                           select new
                           {
                               name = groupValue.Key,
                               count = groupValue.Count()
                           };

            foreach (var item in dupNodes)
            {
                mi.FireOnError(string.Format("ERROR: duplicate node name: '{0}', found {1} duplicates", item.name, item.count));
            }

            foreach (var item in dupLinks)
            {
                mi.FireOnError(string.Format("ERROR: duplicate link name: '{0}', found {1} duplicates", item.name, item.count));
            }
        }

        public static void SanityCheckAccrualLinksCapacities(Model mi)
        {
            for (int j = 1; j < XYFileReader.NodeArray.Length; j++)
            {
                Node res = XYFileReader.NodeArray[j];
                if (ReservoirNodeHasOwners(res))
                {
                    long sumResAccLinkCap = 0;
                    for (int i = 0; i < res.InflowLinks.Count(); i++)
                    {
                        Link link = res.InflowLinks.Item(i);
                        if (mi.IsAccrualLink(link))
                        {
                            sumResAccLinkCap += link.m.lnkallow;
                            long sumOwnLinkCap = 0;
                            for (int k = 1; k < XYFileReader.LinkArray.Length; k++)
                            {
                                Link ownLink = XYFileReader.LinkArray[k];
                                if (ownLink.IsOwnerLink() & object.ReferenceEquals(ownLink.m.accrualLink, link))
                                {
                                    if (ownLink.m.groupNumber == 0)
                                    {
                                        sumOwnLinkCap += ownLink.m.capacityOwned;
                                    }
                                }
                            }
                            if (link.m.numberOfGroups > 0)
                            {
                                for (int n = 0; n < link.m.numberOfGroups; n++)
                                {
                                    sumOwnLinkCap += link.m.stgAmount[n];
                                }
                            }
                            if (link.m.lnkallow != sumOwnLinkCap)
                            {
                                mi.FireOnError(string.Format("ERROR: corrupt accrual link, AccrualLink: {0} lnkallow: {1} sumOwnCap: {2}", link.number, link.m.lnkallow, sumOwnLinkCap));
                            }
                        }
                    }
                    if ((res.m.max_volume-res.m.min_volume) != sumResAccLinkCap)
                    {
                        mi.FireOnError(string.Format("WARNING: reservoir node: {0} Active Volume: {1} Accrual Volume: {2}", res.name, res.m.max_volume, sumResAccLinkCap));
                    }
                }
            }
        }

        public static bool ReservoirNodeHasOwners(Node resnode)
        {
            if (resnode.InflowLinks == null || resnode.nodeType != NodeType.Reservoir)
            {
                return false;
            }
            for (int i = 0; i < resnode.InflowLinks.Count(); i++)
            {
                if (resnode.InflowLinks.Item(i).IsAccrualLink)
                {
                    return true;
                }
            }
            return false;
        }

        public static void SetHasOwnerRentLinksFlags(Model mi)
        {
            for (int i = 1; i < XYFileReader.LinkArray.Length; i++)
            {
                Link link = XYFileReader.LinkArray[i];
                if (mi.IsAccrualLink(link))
                {
                    mi.HasOwnerLinks = true;
                    break;
                }
            }
            for (int i = 1; i < XYFileReader.LinkArray.Length; i++)
            {
                Link link = XYFileReader.LinkArray[i];
                if (link.IsRentLink())
                {
                    mi.HasRentLinks = true;
                    break;
                }
            }
        }

        public static void CheckForExtentions(Model mi)
        {
            if (mi.ExtStorageRightActive == false)
            {
                for (int i = 1; i < XYFileReader.LinkArray.Length; i++)
                {
                    Link link = XYFileReader.LinkArray[i];
                    if (link.m.accrualLink != null)
                    {
                        mi.ExtStorageRightActive = true;
                        mi.ExtManualStorageRightActive = true;
                        break;
                    }
                }
            }
            if (mi.ExtWaterRightsActive == false)
            {
                DateTime defaultDate = TimeManager.missingDate;
                for (int i = 1; i < XYFileReader.LinkArray.Length; i++)
                {
                    Link link = XYFileReader.LinkArray[i];
                    if (link.m.waterRightsDate != defaultDate)
                    {
                        mi.ExtWaterRightsActive = true;
                        break;
                    }
                }
            }
            if (mi.ExtLastFillRentActive == false)
            {
                for (int i = 1; i < XYFileReader.NodeArray.Length; i++)
                {
                    Node node = XYFileReader.NodeArray[i];
                    if (node.m.lastFillLink != null)
                    {
                        mi.ExtLastFillRentActive = true;
                        break;
                    }
                }
            }
        }

        public static string ReadString(string cmdName, string defaultValue, TextFile file, int startIndex, int endindex)
        {
            int lineindex = file.FindBeginningWith(cmdName + " ", startIndex, endindex);
            if (lineindex == -1)
            {
                return defaultValue;
            }
            string s = file[lineindex].Substring(cmdName.Length + 1);
            lineindex++; //to display proper line number in xy file to users

            return s;
        }

        public static ModsimUnits ReadUnits(Model mi, string cmdName, ModsimUnits defaultValue, TextFile file, int startIndex, int endindex)
        {
            ModsimUnits rval = defaultValue;
            int lineindex = file.FindBeginningWith(cmdName + " ", startIndex, endindex);
            if (lineindex == -1)
            {
                return rval;
            }
            string s = file[lineindex].Substring(cmdName.Length + 1);
            int idxComment = s.IndexOf("#");
            if (idxComment > 0)
            {
                s = s.Substring(0, idxComment);
            }
            lineindex++; //to display proper line number in xy file to users

            try
            {
                rval = new ModsimUnits(s);
            }
            catch (Exception ex)
            {
                throw new XYFileReadingException(ex.Message, lineindex);
            }
            return rval;
        }

        public static int ReadInteger(string cmdName, int defaultValue, TextFile file, int startIndex, int endindex)
        {
            int rval = defaultValue;
            int lineindex = file.FindBeginningWith(cmdName + " ", startIndex, endindex);
            if (lineindex == -1)
            {
                return rval;
            }
            string s = file[lineindex].Substring(cmdName.Length + 1);
            int idxComment = s.IndexOf("#");
            if (idxComment > 0)
            {
                s = s.Substring(0, idxComment);
            }
            lineindex++; //to display proper line number in xy file to users

            if (!int.TryParse(s, out rval))
            {
                throw new XYFileReadingException("Unable to parse \"" + s + "\" to integer", lineindex);
            }
            return rval;
        }

        public static long ReadLong(string cmdName, long defaultValue, TextFile file, int startIndex, int endindex)
        {
            long rval = defaultValue;
            int lineindex = file.FindBeginningWith(cmdName + " ", startIndex, endindex);
            if (lineindex == -1)
            {
                return rval;
            }
            string s = file[lineindex].Substring(cmdName.Length + 1);
            int idxComment = s.IndexOf("#");
            if (idxComment > 0)
            {
                s = s.Substring(0, idxComment);
            }
            lineindex++; //to display proper line number in xy file to users

            if (!long.TryParse(s, out rval))
            {
                throw new XYFileReadingException("Unable to parse \"" + s + "\" to long", lineindex);
            }
            return rval;
        }

        public static bool ReadBoolean(string cmdName, bool defaultValue, TextFile file, int startIndex, int endindex)
        {
            bool rval = defaultValue;
            int lineindex = file.FindBeginningWith(cmdName + " ", startIndex, endindex);
            if (lineindex == -1)
            {
                return rval;
            }
            string s = file[lineindex].Substring(cmdName.Length + 1);
            int idxComment = s.IndexOf("#");
            if (idxComment > 0)
            {
                s = s.Substring(0, idxComment);
            }
            lineindex++; //to display proper line number in xy file to users

            if (!bool.TryParse(s, out rval))
            {
                throw new XYFileReadingException("Unable to parse \"" + s + "\" to boolean, True/False", lineindex);
            }
            return rval;
        }

        public static double ReadFloat(string cmdName, double defaultValue, TextFile file, int startIndex, int endindex)
        {
            double rval = defaultValue;
            int lineindex = file.FindBeginningWith(cmdName + " ", startIndex, endindex);
            if (lineindex == -1)
            {
                return rval;
            }
            string s = file[lineindex].Substring(cmdName.Length + 1);
            int idxComment = s.IndexOf("#");
            if (idxComment > 0)
            {
                s = s.Substring(0, idxComment);
            }
            lineindex++; //to display proper line number in xy file to users

            if (!double.TryParse(s, out rval))
            {
                throw new XYFileReadingException("Unable to parse \"" + s + "\" to float", lineindex);
            }
            return rval;
        }

        //ReadIntList is Used to read a list of integers
        //Example:
        //in 30
        //in 1
        public static int[] ReadIntList(string cmdName, int defaultValue, TextFile file, int startIndex, int endindex)
        {
            int[] rval = new int[0];

            List<int> valueList = new List<int>();
            int idx = file.FindBeginningWith(cmdName + " ", startIndex, endindex);
            if (idx == -1)
            {
                return rval;
            }
            // empty
            while (file[idx].IndexOf(cmdName + " ") == 0)
            {
                int i = Convert.ToInt32(file[idx].Substring(cmdName.Length + 1));
                valueList.Add(i);
                idx++;
            }
            rval = new int[valueList.Count];
            valueList.CopyTo(rval);
            return rval;
        }

        //ReadFloatList is Used to read a list of floating points
        //Example:
        //  efft
        //0 0.7
        //1 0.77    ' NOTE missing index 2 (this means use default value)
        //3 0.77
        //18 0.76
        //19 0.8
        //20-21 0.77
        //----------
        // i    val

        // 20-21 0.77  split({" ","-"})
        // tokens.Length = 2 or = 3
        public static double[] ReadIndexedFloatList(string cmdName, double defaultValue, TextFile file, int startIndex, int endindex)
        {
            List<double> values = new List<double>();
            double[] rval = new double[] { };
            int idx = file.Find(cmdName, startIndex, endindex);
            if (idx == -1)
            {
                return rval;
                // empty
            }
            if (idx >= endindex)
            {
                return rval;
            }
            idx++;
            bool NegativeMessageGiven = false;
            int prevIndex = -1;
            for (int lineNumber = idx; lineNumber <= endindex; lineNumber++)
            {
                string str = file[lineNumber];
                string[] tokens = str.Split(" ".ToCharArray());
                // single token may be new command
                if (tokens.Length != 2)
                {
                    break;
                }

                if (char.IsLetter(tokens[0][0]))
                {
                    break;
                }

                double val = Convert.ToDouble(tokens[1]);
                // check for range of constant value
                // for example  '1-3 0.77'
                string[] strList = tokens[0].Split("-".ToCharArray());

                int lower = Convert.ToInt32(strList[0]);
                int upper = lower;

                if (strList.Length > 2)
                {
                    throw new Exception("Error reading xy file line: " + lineNumber);
                }
                if (strList.Length == 2)
                {
                    upper = Convert.ToInt32(strList[1]);
                }

                if (upper < prevIndex || lower < prevIndex || upper < lower)
                {
                    throw new Exception("error reading xy file: bad index at line " + lineNumber);
                }
                // check for missing(default) values

                for (int j = prevIndex + 1; j < lower; j++)
                {
                    values.Add(defaultValue);
                }

                for (int k = lower; k <= upper; k++)
                {
                    values.Add(val);

                    if (val < 0 && !NegativeMessageGiven && cmdName != "lrentlim" && cmdName != "resbaliprio" && cmdName != "priority" && cmdName != "adaevam" && cmdName != "labelpos" && cmdName != "pos" && cmdName != "demr")
                    {
                        NegativeMessageGiven = true;
                        Model.FireOnErrorGlobal("WARNING: negative values in data :cmd = '" + cmdName + "' see line number: " + lineNumber);

                    }
                }
                prevIndex = upper;
            }
            rval = new double[values.Count];
            values.CopyTo(rval);
            return rval;
        }

        // Overloaded version ReadIndexedFloatList will return an array
        // of defaultLength if nothing is found in the xy file
        // and default values will be used
        public static double[] ReadIndexedFloatList(string cmdName, int defaultLength, double defaultValue, TextFile file, int startIndex, int endindex)
        {
            double[] floatList = null;
            floatList = ReadIndexedFloatList(cmdName, defaultValue, file, startIndex, endindex);

            int numFound = floatList.Length;
            // how many values were read from xy file
            if (floatList.Length < defaultLength)
            {
                Array.Resize(ref floatList, defaultLength);
                // minimum length, could be larger
            }

            // this float list will be returned
            double[] returnList = new double[floatList.Length];

            // fill in default values..
            for (int i = 0; i < floatList.Length; i++)
            {
                returnList[i] = defaultValue;
            }

            for (int j = 0; j < numFound; j++)
            {
                returnList[j] = floatList[j];
            }

            return returnList;
        }

        public static long[] ReadIndexedIntegerList(string cmdName, int defaultValue, TextFile file, int startIndex, int endindex)
        {
            double[] f = null;
            f = ReadIndexedFloatList(cmdName, defaultValue, file, startIndex, endindex);
            long[] i = new long[f.Length];

            for (int j = 0; j < f.Length; j++)
            {
                i[j] = Convert.ToInt64(f[j]);
            }
            return i;
        }

        // Overloaded version ReadIndexedIntegerList will return an array
        // of defaultLength if nothing is found in the xy file
        // and default values will be used
        public static long[] ReadIndexedIntegerList(string cmdName, int defaultLength, double defaultValue, TextFile file, int startIndex, int endindex)
        {
            double[] floatList = null;
            floatList = ReadIndexedFloatList(cmdName, defaultValue, file, startIndex, endindex);

            int numFound = floatList.Length;
            // how many values were read from xy file
            if (floatList.Length == 0 & defaultLength == 0)
            {
                return new long[0];
            }
            if (floatList.Length < defaultLength)
            {
                Array.Resize(ref floatList, defaultLength);
                // minimum length, could be larger
            }

            // this integer list will be returned
            long[] intList = new long[floatList.Length];

            // fill in default values..
            for (int i = 0; i < floatList.Length; i++)
            {
                intList[i] = Convert.ToInt64(defaultValue);
            }

            for (int j = 0; j < numFound; j++)
            {
                intList[j] = Convert.ToInt64(floatList[j]);
            }

            return intList;
        }

        public static TimeSeries ReadTimeSeries(Model mi, string cmdName, TextFile file, int startIndex, int endIndex)
        {
            TimeSeries rval = null;

            ModsimUnits units;
            TimeSeriesType ttype = GetTSTypeFromID(mi, cmdName, out units);

            rval = new TimeSeries(ttype);

            int idx = file.Find(cmdName, startIndex, endIndex);
            if (idx == -1)
            {
                rval.units = units;
                return rval;
            }
            int addIdx = 1;
            bool varies = false;
            if (file[idx + addIdx].IndexOf("variesbyyear") == 0)
            {
                varies = ReadBoolean("variesbyyear", false, file, idx + addIdx, idx + addIdx);
                addIdx++;
            }
            bool interp = false;
            if (file[idx + addIdx].IndexOf("interpolate") == 0)
            {
                interp = ReadBoolean("interpolate", false, file, idx + addIdx, idx + addIdx);
                addIdx++;
            }
            bool multicol = false;
            if (file[idx + addIdx].IndexOf("multicolumn") == 0)
            {
                multicol = ReadBoolean("multicolumn", false, file, idx + addIdx, idx + addIdx);
                addIdx++;
            }
            if (file[idx + addIdx].IndexOf("units") == 0)
            {
                units = XYFileReader.ReadUnits(mi, "units", mi.GetDefaultUnits(ttype), file, idx + addIdx, endIndex);
                addIdx++;
            }
            rval.VariesByYear = varies;
            rval.Interpolate = interp;
            rval.MultiColumn = multicol;
            rval.units = units;

            int row = 0;
            for (int line = idx + addIdx; line <= endIndex; line++)
            {
                // Get the line represented in a string
                string linestring = file[line];
                if (char.IsLetter(linestring[0]))
                {
                    break;
                }

                // Get the date and the data
                DateTime rowdate = TimeManager.missingDate;
                string[] data = null;
                GetTSData(linestring, out rowdate, out data);

                // Set the TimeSeries data within the TimeSeries object
                int numcols = data.Length;
                if (rval.MultiColumn == false)
                {
                    numcols = 1;
                }
                rval.setDate(row, rowdate);
                for (int i = 0; i < numcols; i++)
                {
                    if (rval.IsFloatType)
                    {
                        rval.setDataF(row, i, Convert.ToDouble(data[i]));
                    }
                    else
                    {
                        //ET: This operation fails when the number is not an integer - 
                        //      This might not happen if user uses MODSIM statement, but could happen when using custom operations.
                        //      The statement below can be used to read other values and convert to integer.
                        //      rval.setDataL(row, i, (long)Math.Round(Convert.ToDouble(data[i]),0));
                        rval.setDataL(row, i, Convert.ToInt64(data[i]));
                    }
                }
                row++;
            }
            return rval;
        }

        public static TimeSeriesType GetTSTypeFromID(Model mi, string cmdName, out ModsimUnits units)
        {
            TimeSeriesType ttype = default(TimeSeriesType);
            switch (cmdName)
            {
                case "tsdemand":
                case "adaDemandsM":
                    ttype = TimeSeriesType.Demand;
                    break;
                case "tstarget":
                case "adaTargetsM":
                    ttype = TimeSeriesType.Targets;
                    break;
                case "tsinflow":
                case "adaInflowsM":
                    ttype = TimeSeriesType.NonStorage;
                    break;
                case "tsforcast":
                case "adaForecastsM":
                    ttype = TimeSeriesType.Forecast;
                    break;
                case "maxCap":
                case "maxVariable":
                    ttype = TimeSeriesType.VariableCapacity;
                    break;
                case "tsevaprate":
                case "adaEvaporationsM":
                    ttype = TimeSeriesType.Evaporation;
                    break;
                case "tsgeneratehrs":
                case "adaGeneratingHrsM":
                    ttype = TimeSeriesType.Generating_Hours;
                    break;
                case "tsinfiltration":
                case "adaInfiltrationsM":
                    ttype = TimeSeriesType.Infiltration;
                    break;
                case "tspowerdemands":
                    case "":
                    ttype = TimeSeriesType.Power_Target;
                    break;
                case "measured":
                case "adaMeasured":
                    ttype = TimeSeriesType.Measured;
                    break;
                default:
                    throw new Exception(" unknown command " + cmdName);
            }
            units = null;
            switch (ttype)
            {
                case TimeSeriesType.Demand:
                    units = mi.GetDefaultUnits(TimeSeriesType.Demand);
                    break;
                case TimeSeriesType.Targets:
                    units = mi.GetDefaultUnits(TimeSeriesType.Targets);
                    break;
                case TimeSeriesType.NonStorage:
                    units = mi.GetDefaultUnits(TimeSeriesType.NonStorage);
                    break;
                case TimeSeriesType.Forecast:
                    units = mi.GetDefaultUnits(TimeSeriesType.Forecast);
                    break;
                case TimeSeriesType.VariableCapacity:
                    units = mi.GetDefaultUnits(TimeSeriesType.VariableCapacity);
                    break;
                case TimeSeriesType.Evaporation:
                    units = mi.GetDefaultUnits(TimeSeriesType.Evaporation);
                    break;
                case TimeSeriesType.Generating_Hours:
                    units = mi.GetDefaultUnits(TimeSeriesType.Generating_Hours);
                    break;
                case TimeSeriesType.Infiltration:
                    units = mi.GetDefaultUnits(TimeSeriesType.Infiltration);
                    break;
                case TimeSeriesType.Power_Target:
                    units = mi.GetDefaultUnits(TimeSeriesType.Power_Target);
                    break;
                case TimeSeriesType.Measured:
                    units = mi.GetDefaultUnits(TimeSeriesType.Measured);
                    break;
                default:
                    throw new Exception(" unknown TimeSeriesType " + ttype.ToString());
            }
            return ttype;
        }

        public static void GetTSData(string lineString, out DateTime theDate, out string[] theData)
        {
            theDate = TimeManager.missingDate;
            theData = null;
            int startIndex = 0;
            int i = 0;

            // Find a divider that the lineString contains
            do
            {
                startIndex = lineString.IndexOf(xyFile.DataDivider[i], 16);
                i++;
            } while (!(i >= xyFile.DataDivider.Length || startIndex >= 0));

            if (startIndex < 0)
            {
                return;
            }

            // Assign the appropriate values to the date and data
            theDate = Convert.ToDateTime(lineString.Substring(0, startIndex));
            theData = lineString.Substring(startIndex).Split(xyFile.DataDivider, StringSplitOptions.RemoveEmptyEntries);
        }

        public static List<long> ReadIntTimeSeries(string cmdName, int defaultValue, TextFile file, int startIndex, int endindex)
        {
            long[] tmpIntList = ReadIndexedIntegerList(cmdName, defaultValue, file, startIndex, endindex);
            List<long> rval = new List<long>();
            for (int i = 0; i < tmpIntList.Length; i++)
            {
                rval.Add(tmpIntList[i]);
            }
            return rval;
        }

        public static void FillDatesOldTimeSeries(Model mi, TimeSeries ts)
        {
            DateTime currDate = mi.TimeStepManager.dataStartDate;
            int tsSize = ts.getSize();
            DateTime lastDate = default(DateTime);
            int lper = mi.timeStep.NumOfTSsForV7Output;
            if (tsSize > lper)
            {
                ts.VariesByYear = true;
                while (mi.TimeStepManager.noDataTimeSteps - 1 < tsSize - 1)
                {
                    mi.TimeStepManager.dataEndDate = mi.timeStep.IncrementDate(mi.TimeStepManager.dataEndDate);
                    //XYFileReader.IncrementDate(mi, mi.TimeStepManager.dataEndDate)
                    mi.TimeStepManager.endingDate = mi.TimeStepManager.dataEndDate;
                    mi.TimeStepManager.UpdateTimeStepsInfo(mi.timeStep);
                }
                //Dim index As Integer = mi.TimeStepManager.noModelTimeSteps - 1
                int index = mi.TimeStepManager.noDataTimeSteps - 1;
                lastDate = mi.TimeStepManager.Index2Date(index, TypeIndexes.DataIndex);
            }
            else if (ts.VariesByYear == true)
            {
                int index = mi.TimeStepManager.noDataTimeSteps - 1;
                //lastDate = mi.TimeStepManager.Index2Date(index, TypeIndexes.ModelIndex)
                lastDate = mi.TimeStepManager.Index2Date(index, TypeIndexes.DataIndex);
            }
            else
            {
                switch (mi.timeStep.TSType)
                {
                    case ModsimTimeStepType.Daily:
                        lastDate = currDate.AddDays(6);
                        break;
                    case ModsimTimeStepType.Weekly:
                        lastDate = currDate.AddDays(11 * 7);
                        break;
                    case ModsimTimeStepType.Monthly:
                        lastDate = currDate.AddMonths(11);
                        break;
                    default:
                        throw new Exception("Version 7 output for timestep " + mi.timeStep.Label + " is undefined.");
                }
            }
            int i = 0;
            while (currDate <= lastDate)
            {
                ts.setDate(i, currDate);
                currDate = mi.timeStep.IncrementDate(currDate);
                i++;
            }
        }

        public static TimeSeries ReadTimeSeries(string cmdName, TimeSeriesType type, int defaultValue, TextFile file, int startIndex, int endIndex)
        {
            TimeSeries rval = new TimeSeries(type);
            if (rval.IsFloatType)
            {
                double[] tmpFloatList = ReadIndexedFloatList(cmdName, defaultValue, file, startIndex, endIndex);
                for (int i = 0; i < tmpFloatList.Length; i++)
                {
                    rval.setDataF(i, tmpFloatList[i]);
                }
            }
            else
            {
                long[] tmpIntList = ReadIndexedIntegerList(cmdName, defaultValue, file, startIndex, endIndex);
                for (int i = 0; i < tmpIntList.Length; i++)
                {
                    rval.setDataL(i, tmpIntList[i]);
                }
            }
            return rval;
        }

        //Reads a DateTime from an xy file.
        // Example: cmdName = 'startdate'
        // startdate 0 10 1928
        public static DateTime ReadLegacyDate(string cmdName, DateTime defaultValue, TextFile file, int startIndex, int endindex)
        {
            DateTime rval = defaultValue;
            string str = XYFileReader.ReadString(cmdName, "", file, startIndex, endindex);
            if (string.IsNullOrEmpty(str))
            {
                Model.FireOnErrorGlobal(" Date command " + cmdName + " not found, returning " + Convert.ToString(rval));
                return rval;
            }
            string separators = " ";
            string[] tokens = str.Split(separators.ToCharArray());
            if (tokens.Length != 3)
            {
                string msg = "Error reading Date.  " + cmdName;
                throw new Exception(msg);
            }
            int day = Convert.ToInt32(tokens[0]);
            int month = Convert.ToInt32(tokens[1]);
            int year = Convert.ToInt32(tokens[2]);
            if (day == 0)
            {
                day = 1;
            }
            rval = new DateTime(year, month, day);
            return rval;
        }

        //Reads a "TimeStep" from an xy file.
        // Example for Rent Pool  dates.
        // the TimeStep class is a list of dates (stored in an awkward way)
        // example 1
        //accdates
        //tstype 2   <---- this is optional... (only used internallin in TimeStep class...)
        //tsmajor 1
        //tsminor 1
        //----
        // example 2
        //rpdates
        //tsmajor 5
        //tsminor 1
        //rpdates
        //tsmajor 6
        //tsminor 1
        public static LegacyTimeStep[] ReadLegacyTimeStep(string cmdName, TextFile file, int startIndex, int endindex)
        {
            List<LegacyTimeStep> legacyList = new List<LegacyTimeStep>();
            int idx = file.Find(cmdName, startIndex, endindex);
            if (idx < 0)
            {
                LegacyTimeStep[] defaultTimeStep = null;
                defaultTimeStep = new LegacyTimeStep[1];
                defaultTimeStep[0] = new LegacyTimeStep();
                //FireOnErrorGlobal("using defaults for " & cmdName)
                return defaultTimeStep;
            }
            LegacyTimeStep tmStp = null;
            // look for multiple commands.
            do
            {
                tmStp = new LegacyTimeStep();
                tmStp.major = 1;
                tmStp.minor = 1;
                tmStp.tsType = 2;
                if (idx < 0)
                {
                    break;
                }
                // Assuming if you have a timeStep data type
                // you have both tsmajor and tsminor
                if (!(file[idx + 1].StartsWith("tsmajor") | file[idx + 1].StartsWith("tstype")))
                {
                    string msg = "Warning:  skipping command = " + cmdName + ". It is not valid.  xy file line number " + startIndex;
                    throw new Exception(msg);
                }
                tmStp.tsType = XYFileReader.ReadInteger("tstype", 2, file, idx + 1, idx + 1);
                // 2 = MONTHLY
                tmStp.major = XYFileReader.ReadInteger("tsmajor", -1, file, idx + 1, idx + 2);
                tmStp.minor = XYFileReader.ReadInteger("tsminor", -1, file, idx + 2, idx + 3);
                if (tmStp.major == -1 | tmStp.minor == -1)
                {
                    throw new Exception("Error reading " + cmdName + " in xy file at line " + idx);
                }
                legacyList.Add(tmStp);
                idx = file.Find(cmdName, idx + 1, endindex);
            } while (true);

            LegacyTimeStep[] rval = null;
            rval = new LegacyTimeStep[legacyList.Count];
            legacyList.CopyTo(rval);
            return rval;
        }

        public static DateTime ReadDateTime(string cmdName, TextFile file, DateTime defaultValue, int startIndex, int endindex)
        {
            string str = ReadString(cmdName, "xxxx", file, startIndex, endindex);
            if (str == "xxxx")
            {
                return defaultValue;
            }
            return Convert.ToDateTime(str);
        }

        public static void CheckForNegativeValues(Model mi)
        {
            Node aNode = mi.firstNode;
            Link aLink = mi.firstLink;
            string msg = "";

            // Nodes
            while (aNode != null)
            {
                if (aNode.nodeType == NodeType.Demand)
                {
                    if (aNode.m.adaDemandsM.dataTable.Rows.Count > 0)
                    {
                        if (aNode.m.adaDemandsM.HasNegativeValues())
                        {
                            msg += "  " + aNode.name + Environment.NewLine;
                        }
                    }
                    if (aNode.m.adaInfiltrationsM.dataTable.Rows.Count > 0)
                    {
                        if (aNode.m.adaInfiltrationsM.HasNegativeValues())
                        {
                            msg += "  " + aNode.name + Environment.NewLine;
                        }
                    }
                }
                else if (aNode.nodeType == NodeType.Reservoir)
                {
                    if (aNode.m.adaTargetsM.dataTable.Rows.Count > 0)
                    {
                        if (aNode.m.adaTargetsM.HasNegativeValues())
                        {
                            msg += "  " + aNode.name + Environment.NewLine;
                        }
                    }
                    if (aNode.m.adaGeneratingHrsM.dataTable.Rows.Count > 0)
                    {
                        if (aNode.m.adaGeneratingHrsM.HasNegativeValues())
                        {
                            msg += "  " + aNode.name + Environment.NewLine;
                        }
                    }
                    // Evaporation values can be negative. Don't include check for negative values.
                    //If aNode.m.adaEvaporationsM.dataTable.Rows.Count > 0 Then
                    //    If aNode.m.adaEvaporationsM.HasNegativeValues() Then msg &= "  " & aNode.name & vbCrLf
                    //End If
                    if (aNode.m.adaForecastsM.dataTable.Rows.Count > 0)
                    {
                        if (aNode.m.adaForecastsM.HasNegativeValues())
                        {
                            msg += "  " + aNode.name + Environment.NewLine;
                        }
                    }
                }
                else if (aNode.nodeType == NodeType.NonStorage)
                {
                    if (aNode.m.adaInflowsM.dataTable.Rows.Count > 0)
                    {
                        if (aNode.m.adaInflowsM.HasNegativeValues())
                        {
                            msg += "  " + aNode.name + Environment.NewLine;
                        }
                    }
                }
                aNode = aNode.next;
            }

            // Links
            while (aLink != null)
            {
                if (aLink.m.maxVariable.dataTable.Rows.Count > 0)
                {
                    if (aLink.m.maxVariable.HasNegativeValues())
                    {
                        msg += "  " + aLink.name + Environment.NewLine;
                    }
                }
                aLink = aLink.next;
            }

            // Warn the user about negative values in the timeseries.
            if (!string.IsNullOrEmpty(msg))
            {
                msg = "Negative values were found in timeseries of the following nodes: " + Environment.NewLine + msg;
                mi.FireOnError(msg);
            }
        }

        private static void CheckReservoirTargetsStartDate(Model mi)
        {
            mi.FireOnMessage("Checking reservoir targets.");
            for (int i = 0; i < mi.Nodes_Reservoirs.Length; i++)
            {
                Node resNode = mi.Nodes_Reservoirs[i];

                if (resNode.m.adaTargetsM.getSize() > 0)
                {
                    DateTime targetSimStartDate = mi.timeStep.IncrementDate(mi.TimeStepManager.startingDate);
                    DateTime targetDataStartDate = mi.timeStep.IncrementDate(mi.TimeStepManager.dataStartDate);
                    DateTime resTargetStartDate = resNode.m.adaTargetsM.getDate(0);

                    if (resTargetStartDate < targetDataStartDate)
                    {
                        mi.FireOnMessage("WARNING: reservoir node " + resNode.name + " targets may not have been properly imported from a previous version");
                    }
                    if (resTargetStartDate > targetSimStartDate)
                    {
                        mi.FireOnMessage("WARNING: reservoir node " + resNode.name + " no target defined at simulation start");
                    }
                }
            }
        }

    }
}
