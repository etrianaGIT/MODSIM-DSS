using System;
using System.IO;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.ModsimIO
{
    public class LagInfo
    {
        //inflagi
        //lagloc 1
        //lagnumlag 1200
        //laglags
        //0-10 0.1
        //inflagi
        //lagloc 2
        //lagfrac 0.5
        //lagnumlag 1200
        //laglags
        //0 0.5
        //11 0.5
        //inflagi
        //lagloc 3
        //lagfrac 1
        //lagnumlag 1200
        //laglags
        //0-1 0.5
        //ially 1
        //n_nodes 4
        //n_lags 5
        //rdim
        //2 20480
        //3 3320
        //title BASIC TITLE
        //gw_cp 0.05
        //flowth_cp 5e-005
        //startdate 0 1 1980
        //xyver 56

        //ReadNode function reads a LagInfo from the xy file
        // cmdName  is either 'inflagi' or 'pumplagi'
        public static ModsimModel.LagInfo ReadLagInfo(string cmdName, Model mi, Node node, TextFile file, int startIndex, int endIndex)
        {

            ModsimModel.LagInfo rval = null;

            while (startIndex < endIndex)
            {
                int idxLag = file.Find(cmdName, startIndex, endIndex);

                if (idxLag > endIndex | idxLag == -1)
                {
                    return rval;
                }
                if (file[idxLag + 1].IndexOf("lagloc") != 0 | file[idxLag + 2].IndexOf("lagfrac") != 0)
                {
                    //Or file(idxLag + 3).IndexOf("lagnumlag") <> 0 Then
                    //Or file(idxLag + 4).IndexOf("laglags") <> 0 Then
                    mi.FireOnError("Warning (non-fatal): LagInfo is not complete xy file line number: " + idxLag);
                    return rval;
                }

                //{ MY_T("lagloc"),      IOXYn_num,  NULL,  0,       0.0,   },(lagi->location);
                int tmpNodeNumber = XYFileReader.ReadInteger("lagloc", -1, file, idxLag + 1, idxLag + 1);
                Node tmpNode = XYFileReader.NodeArray[tmpNodeNumber];
                //mi.FindNode(tmpNodeNumber)
                if (tmpNode == null)
                {
                    throw new Exception("Error:  lagloc not found for lagInfo. xy file line number " + idxLag);
                }

                ModsimModel.LagInfo lagInfo = new ModsimModel.LagInfo();
                lagInfo.location = tmpNode;

                if (rval == null)
                {
                    rval = lagInfo;
                }
                else
                {
                    rval.Add(lagInfo);
                }

                //{ MY_T("lagfrac"),     IOXYfloat,  NULL,  0,       0.0,   },(lagi->percent);
                lagInfo.percent = XYFileReader.ReadFloat("lagfrac", 0, file, idxLag + 2, idxLag + 2);
                //{ MY_T("lagnumlag"),   IOXYnumlag, NULL,  0,     120000.0,   },(lagi->numLags);
                //WOW default of 1200 lags might be a performance issue
                lagInfo.numLags = XYFileReader.ReadInteger("lagnumlag", 1200, file, idxLag + 3, idxLag + 3);
                //{ MY_T("laglags"),     IOXYFloatTimeSeries, NULL,  MMM,       0.0,   },(lagi->lagInfoData);
                lagInfo.lagInfoData = XYFileReader.ReadIndexedFloatList("laglags", 0, file, idxLag + 4, endIndex);
                if (mi.timeStep.TSType == ModsimTimeStepType.Daily && mi.inputVersion.Type == InputVersionType.V056)
                {
                    FixDailyLagIndexes(lagInfo);
                }
                //lagInfo.lagInfoData = XYFileReader.ReadFloatTimeSeries("laglags", 0, file, idxLag + 4, endIndex)
                if (lagInfo.lagInfoData.Length == 0)
                {
                    mi.FireOnError(" No lag factors were read for node " + node.name + " location " + lagInfo.location.name + ". Setting the first lag to 1.0");
                    lagInfo.lagInfoData = new double[1];
                    lagInfo.lagInfoData[0] = 1.0;
                    startIndex = idxLag + 4;
                }
                else
                {
                    startIndex = idxLag + 5;
                }
            }
            return rval;
        }
        public static void FixDailyLagIndexes(ModsimModel.LagInfo laginfo)
        {
            // old xy  files for daily time steps assumed that some of the lag factor indices were not used
            // the old interface form had 7 rows for each week of lag factors; this of course was nonsense
            // since the factors is a simple flat array of floats.
            int i = 0;
            int j = 0;
            int newlength = 0;
            int chop = 0;
            for (i = laginfo.lagInfoData.Length - 1; i >= 11; i--)
            {
                chop = 0;
                if (i % 12 == 0 & i > 0)
                {
                    for (j = i - 5; j <= laginfo.lagInfoData.Length - 5; j++)
                    {
                        if (j + 5 < laginfo.lagInfoData.Length)
                        {
                            laginfo.lagInfoData[j] = laginfo.lagInfoData[j + 5];
                        }
                        else
                        {
                            chop = chop + 1;
                        }
                    }
                    newlength = laginfo.lagInfoData.Length - (5 + chop);
                    Array.Resize(ref laginfo.lagInfoData, newlength + 1);
                }
            }
        }

        public static void WriteLagInfo(string cmdName, ModsimModel.LagInfo myLag, Model mi, Node node, StreamWriter xyOutFile)
        {
            ModsimModel.LagInfo CurrentLag = null;
            CurrentLag = myLag;
            while (CurrentLag != null)
            {
                xyOutFile.WriteLine(cmdName);
                XYFileWriter.WriteNodeNumber("lagloc", CurrentLag.location, xyOutFile);
                XYFileWriter.WriteFloat("lagfrac", xyOutFile, CurrentLag.percent, 0);
                XYFileWriter.WriteInteger("lagnumlag", xyOutFile, CurrentLag.numLags, 0);
                XYFileWriter.WriteIndexedFloatList("laglags", CurrentLag.lagInfoData, 0, xyOutFile);
                CurrentLag = CurrentLag.next;
            }
        }

    }
}
