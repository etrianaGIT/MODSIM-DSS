using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.ModsimIO
{
    /// <summary>Writes a formatted .xy file to disk.</summary>
    /// <remarks>
    /// The Xyfile library is developed as a seperate library that can be replaced if desired.
    /// The modsim model and modsim data structures are totally independent of the Xyfile library
    ///		The xyfile library loads all the data in a modsim model.
    ///		The xyfile library can also save all the data in a modsim model.
    ///		the xyfile library has "compression" optiions that control how the xy file
    ///		is saved.  There are three options 1) None.  2) Partial, and 3) full compression.
    ///		The different options were created to simplify development/debugging cycle.  Perhaps
    ///		only the Full compression option will be used by the interface.
    ///
    ///		1) None: saves the xy file will all default values which shows explicitly what is being
    ///	       used by the model. This options takes the most disk space.  It would show 1200 lags
    ///		   even if they were all zero.
    ///
    ///		2) Partial:  simple default values are shown in the xy file. However
    ///		   default values are not written to lists of data.
    ///
    ///     3) Full: This option is uses the version 7 (original) xy file style and saves
    ///		   as much disk space as possible.
    /// </remarks>
    public class XYFileWriter
    {
        private static CompressionLevelTypes CompressionLevel = CompressionLevelTypes.Full;

        public static XYFile xyFile;
        public enum CompressionLevelTypes
        {
            None = 1,
            // put everything in xy file
            Part = 2,
            // put default values in xy file, but compress repeating data by using range indexing
            Full = 3
            // dont save default values to xy file, and compress reappearing data
        }

        // Example link output Without compression
        //-----------------------------
        //lnum 340
        //fromnum 150
        //tonum 27
        //lmax 12079
        //lmin 0
        //lcost -9667
        //lrentlim
        //0 0
        //1 0
        //2 0
        //3 0
        //4 0
        //5 0
        //6 0
        //llastfill 0
        //xlcf 0
        //spyldc 0
        //transc 0
        //distc 0
        //cmaxv
        //0 12481
        //1 12079
        //2 12481
        //3 12481
        //4 11273
        //5 12481
        //6 12079
        //7 12481
        //8 12079
        //9 12481
        //10 12481
        //11 12079
        //
        // Example 2 With compression
        //-----------------------------
        //lnum 340
        //fromnum 150
        //tonum 27
        //lmax 12079
        //lcost -9667
        //cmaxv
        //0 12481
        //1 12079
        //2-3 12481
        //4 11273
        //5 12481
        //6 12079
        //7 12481
        //8 12079
        //9-10 12481
        //11 12079
        public static void Write(Model mi, string filename, CompressionLevelTypes compressionOption = CompressionLevelTypes.Full, OutputVersionType outputVersion = OutputVersionType.Undefined,bool checkTSDBCopy = false)
        {
            // Utilize parameters for the write method...
            CompressionLevel = compressionOption;
            if (outputVersion != OutputVersionType.Undefined)
            {
                mi.outputVersion = new OutputVersion(outputVersion);
            }
            if (outputVersion == OutputVersionType.Undefined || outputVersion == OutputVersionType.LatestVersion)
            {
                mi.outputVersion = new OutputVersion(OutputVersionType.LatestVersion);
            }
            xyFile = new XYFile(mi, DirectionType.Output);

            // Begin...
            DateTime m_ini = DateTime.Now;
            StreamWriter xyOutStream = null;
            try
            {
                mi.FireOnMessage("saving " + filename);
                xyOutStream = new StreamWriter(filename);

                ModelWriter.WriteModelBasic(mi, xyOutStream);
                mi.fname = filename;
                HydroWriter.WriteBeforeNodes(mi, xyOutStream);
                // efficiencies
                NodeWriter.WriteNodes(mi, xyOutStream);
                LinkWriter.WriteLinks(mi, xyOutStream);
                HydroWriter.WriteAfterNodes(mi, xyOutStream);
                // hydro units and demands
                ModelWriter.WriteModelDetails(mi, xyOutStream);
                //Wtrite timeseries (Measured)
                NetworkUtils.ModelOutputSupport myMODSIMOutput = new NetworkUtils.ModelOutputSupport(mi, false,false);
                if (!mi.timeseriesInfo.xyFileTimeSeries) myMODSIMOutput.TimeseriesToSQLite(mi,checkDBCopy:checkTSDBCopy);
                //Write the timeseries info after the relative path has been processed. This is for save as networks.
                ModelWriter.WriteModelTimeSeriesInfo(mi, xyOutStream);
            }
            catch (Exception ex)
            {
                string msg = "Error saving xy file.  The file was not completly saved " + ex.Message;
                mi.FireOnError(msg + " " + ex.ToString());
                xyOutStream.Close();
                throw new XYFileReadingException(msg);
            }
            xyOutStream.Close();
            DateTime m_end = DateTime.Now;
            TimeSpan elapsed = m_end.Subtract(m_ini);
            string msgEnd = "Successfully Saved MODSIM Network (elapsed: " + elapsed.TotalMinutes.ToString("#####0.000 min.)");
            mi.FireOnMessage(msgEnd);
        }

        public static void WriteInteger(string cmd, StreamWriter sw, long value, long defaultvalue)
        {
            if (value != defaultvalue || CompressionLevel <= CompressionLevelTypes.Part)
            {
                sw.WriteLine(cmd + " " + value.ToString());
            }
        }
        public static void WriteBoolean(string cmd, StreamWriter sw, bool value, bool defaultvalue = false)
        {
            if (value != defaultvalue || CompressionLevel <= CompressionLevelTypes.Part)
            {
                sw.WriteLine(cmd + " " + value.ToString());
            }
        }
        public static void WriteFloat(string cmd, StreamWriter sw, double value, double defaultvalue)
        {
            if (value != defaultvalue || CompressionLevel <= CompressionLevelTypes.Part)
            {
                sw.WriteLine(cmd + " " + value);
            }
        }

        // Writes a list of integers to xy file
        // if CompressionEnabled is true then any
        // values that are equal to the default are not written
        // to the xy file. this makes the xy file smaller, and compatable with version 7
        public static void WriteIndexedIntList(string cmdName, long[] myList, long defaultValue, StreamWriter xyOutStream)
        {
            if (CompressionLevel > CompressionLevelTypes.None && AllEqualTo(myList, defaultValue))
            {
                return;
                // everything is the same as defaults do nothing
            }
            if (myList.Length > 0)
            {
                xyOutStream.WriteLine(cmdName);
                int sz = myList.Length;

                int i = 0;
                while (i < sz)
                {
                    int numRepeats = HowManyRepeats(myList, i);
                    if (CompressionLevel == CompressionLevelTypes.Full && numRepeats > 0 && myList[i] != defaultValue)
                    {
                        int idxEnd = i + numRepeats;
                        xyOutStream.WriteLine(i + "-" + idxEnd + " " + myList[i]);
                        i = i + numRepeats;
                    }
                    else if (myList[i] != defaultValue || CompressionLevel == CompressionLevelTypes.None)
                    {
                        xyOutStream.WriteLine(i + " " + myList[i]);
                    }
                    i++;
                }

            }
        }
        // Writes a list of floats to xy file
        // if CompressionEnabled is true then any
        // values that are equal to the default are not written
        // to the xy file. this makes the xy file smaller, and compatable with version 7
        public static void WriteIndexedFloatList(string cmdName, double[] myList, double defaultValue, StreamWriter xyOutStream)
        {
            if (CompressionLevel > CompressionLevelTypes.None && AllEqualTo(myList, defaultValue))
            {
                return;
                // everything is the same as defaults do nothing
            }
            if (myList.Length > 0)
            {
                xyOutStream.WriteLine(cmdName);
                int sz = myList.Length;

                int i = 0;
                while (i < sz)
                {
                    int numRepeats = HowManyRepeats(myList, i);
                    if (CompressionLevel == CompressionLevelTypes.Full && numRepeats > 0 && myList[i] != defaultValue)
                    {
                        int idxEnd = i + numRepeats;
                        xyOutStream.WriteLine(i + "-" + idxEnd + " " + myList[i]);
                        i = i + numRepeats;
                    }
                    else if (myList[i] != defaultValue || CompressionLevel == CompressionLevelTypes.None)
                    {
                        xyOutStream.WriteLine(i + " " + myList[i]);
                    }
                    i++;
                }

            }
        }

        // How many repeating values are the same beginning at index idx
        // used for compression
        public static int HowManyRepeats(long[] myList, int idx)
        {
            int rval = 0;
            for (int i = idx + 1; i < myList.Length; i++)
            {
                if (myList[i] == myList[idx])
                {
                    rval++;
                }
                else
                {
                    return rval;
                }
            }
            return rval;
        }
        // How many repeating values are the same beginning at index idx
        // used for compression
        public static int HowManyRepeats(double[] myList, int idx)
        {
            int rval = 0;
            for (int i = idx + 1; i < myList.Length; i++)
            {
                if (myList[i] == myList[idx])
                {
                    rval++;
                }
                else
                {
                    return rval;
                }
            }
            return rval;
        }

        public static bool AllEqualTo(long[] myList, long myInt)
        {
            for (int i = 0; i < myList.Length; i++)
            {
                if (myInt != myList[i])
                {
                    return false;
                }
            }
            return true;

        }
        public static bool AllEqualTo(double[] myList, double myval)
        {
            for (int i = 0; i < myList.Length; i++)
            {
                if (myval != myList[i])
                {
                    return false;
                }
            }
            return true;
        }
        public static void WriteTimeSeries(string cmd, TimeSeries ts, StreamWriter sw)
        {
            if (ts == null)
            {
                Model.FireOnErrorGlobal(" Command " + cmd + " TimeSeries has zero size. You should not be writing a zero size structure. Kill this process and fix the problem.");
                return;
            }
            sw.WriteLine(cmd);
            WriteBoolean("variesbyyear", sw, ts.VariesByYear, false);
            WriteBoolean("interpolate", sw, ts.Interpolate, false);
            WriteBoolean("multicolumn", sw, ts.MultiColumn, false);
            if (ts.units != null)
            {
                sw.WriteLine("units " + ts.units.Label);
            }
            DataTable table = ts.dataTable;
            int numcols = table.Columns.Count;
            double[] checkarrayF = new double[numcols + 1];
            long[] checkarrayL = new long[numcols + 1];
            bool Need2Write = true;

            for (int i = 0; i < ts.getSize(); i++)
            {
                // Check to see if the data needs to be written...
                for (int j = 1; j < numcols; j++)
                {
                    if (Need2Write)
                    {
                        break;    // TODO: might not be correct. Was : Exit For
                    }
                    if (ts.IsFloatType)
                    {
                        if (Convert.ToDouble(table.Rows[i][j]) != checkarrayF[j])
                        {
                            Need2Write = true;
                        }
                    }
                    else
                    {
                        if (Convert.ToInt64(table.Rows[i][j]) != checkarrayL[j])
                        {
                            Need2Write = true;
                        }
                    }
                }
                if (Need2Write)
                {
                    // Write date
                    sw.Write(ts.getDate(i).ToString(TimeManager.DateFormat));

                    // Write data
                    for (int j = 1; j < numcols; j++)
                    {
                        sw.Write(xyFile.DataDivider[0]);
                        if (ts.IsFloatType)
                        {
                            double fValue = Convert.ToDouble(table.Rows[i][j]);
                            checkarrayF[j] = fValue;
                            sw.Write(fValue.ToString("F4").PadLeft(xyFile.DataNumOfSpaces));
                        }
                        else
                        {
                            long iValue = Convert.ToInt64(table.Rows[i][j]);
                            checkarrayL[j] = iValue;
                            sw.Write(iValue.ToString().PadLeft(xyFile.DataNumOfSpaces));
                        }
                    }
                    sw.WriteLine();
                    Need2Write = false;
                }
            }
        }

        public static void WriteIntegerTimeSeries(string cmd, TimeSeries timeSeries, StreamWriter sw)
        {
            if (timeSeries == null)
            {
                return;
            }

            long[] valueList = null;
            int sz = timeSeries.getSize();
            valueList = new long[sz];
            // copy time series data to integer list
            for (int i = 0; i < sz; i++)
            {
                valueList[i] = timeSeries.getDataL(i);
            }
            WriteIndexedIntList(cmd, valueList, 0, sw);
        }
        public static void WriteFloatTimeSeries(string cmd, TimeSeries timeSeries, StreamWriter sw)
        {
            if (timeSeries == null)
            {
                return;
            }

            double[] valueList = null;
            int sz = timeSeries.getSize();
            valueList = new double[sz];
            // copy time series data to integer list
            for (int i = 0; i < sz; i++)
            {
                valueList[i] = timeSeries.getDataF(i);
            }
            WriteIndexedFloatList(cmd, valueList, 0, sw);
        }

        public static void WriteNodeNumber(string cmd, Node node, StreamWriter xyOutStream)
        {
            if (node == null)
            {
                return;
            }
            xyOutStream.WriteLine(cmd + " " + node.number);
        }
        public static void WriteNodeNumberList(string cmd, Node[] nodelist, StreamWriter xyOutStream)
        {
            if (nodelist == null | nodelist.Length == 0)
            {
                return;
            }
            // find the number of non null node numbers.
            List<int> list = new List<int>();
            for (int i = 0; i < nodelist.Length; i++)
            {
                if (nodelist[i] != null)
                {
                    list.Add(nodelist[i].number);
                }
            }
            if (list.Count == 0)
            {
                return;
            }
            xyOutStream.WriteLine(cmd);
            for (int i = 0; i < list.Count; i++)
            {
                xyOutStream.WriteLine(i + " " + Convert.ToString(list[i]));
            }
        }
        public static void WriteLinkNumber(string cmd, Link link, StreamWriter xyOutStream)
        {
            if (link == null)
            {
                return;
            }
            xyOutStream.WriteLine(cmd + " " + link.number);
        }
        public static void WriteLinkNumberList(string cmd, Link[] linkList, StreamWriter xyOutStream)
        {
            if (linkList == null | linkList.Length == 0)
            {
                return;
            }
            // find the number of non null node numbers.
            List<int> list = new List<int>();
            for (int i = 0; i < linkList.Length; i++)
            {
                if (linkList[i] != null)
                {
                    list.Add(linkList[i].number);
                }
            }
            if (list.Count == 0)
            {
                return;
            }
            xyOutStream.WriteLine(cmd);
            for (int i = 0; i < list.Count; i++)
            {
                xyOutStream.WriteLine(i + " " + Convert.ToString(list[i]));
            }
        }

    }
}
