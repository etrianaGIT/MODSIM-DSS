using System;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.ModsimIO
{
    public class HydroReader
    {
        // indices
        private static string[] MainCmds = XYCommands.MainCommands;
        private static bool GetIndices(Model mi, string cmdName, TextFile file, ref int startIndex, ref int endIndex)
        {
            startIndex = file.Find(cmdName, endIndex, file.Count - 1);
            endIndex = file.FindAny(MainCmds, startIndex + 1);
            if (startIndex == -1)
                return false;
            if (endIndex == -1)
            {
                mi.FireOnError("Warning: " + cmdName + " appears to be the last item in xy file, which is not standard.");
                endIndex = file.Count - 1;
            }
            return true;
        }

        // read all
        public static void ReadBeforeNodes(Model mi, TextFile file)
        {
            ReadHydroDetails(mi, file);
            ReadEfficiencies(mi, file);
        }
        public static void ReadAfterNodes(Model mi, TextFile file)
        {
            ReadHydroUnits(mi, file);
            ReadHydroTargets(mi, file);
        }

        // hydropower controller details
        public static void ReadHydroDetails(Model mi, TextFile file)
        {
            int startIndex = 0;
            int endIndex = 0;
            if (!GetIndices(mi, HydropowerController.XYCmdName, file, ref startIndex, ref endIndex))
                return;
            mi.hydro.IsActive = (XYFileReader.ReadString("ExtHydropowerActive", "false", file, startIndex, endIndex) == "True");
            mi.hydro.WasUpgraded = XYFileReader.ReadBoolean("hydroUpgraded", false, file, startIndex, endIndex);
            mi.hydro.NumPrevIters = XYFileReader.ReadInteger("previters", 3, file, startIndex, endIndex);
            mi.hydro.NumPrevTSteps = XYFileReader.ReadInteger("prevtsteps", 5, file, startIndex, endIndex);
            mi.hydro.Tolerance = XYFileReader.ReadFloat("tolerance", 0.1, file, startIndex, endIndex);
        }

        // efficiencies
        public static void ReadEfficiencies(Model mi, TextFile file)
        {
            int startIndex = 0;
            int endIndex = 0;
            while (endIndex < file.Count)
            {
                // Update the indices
                if (!GetIndices(mi, PowerEfficiencyCurve.XYCmdName, file, ref startIndex, ref endIndex))
                    return;

                // Read efficiency
                ReadEfficiency(mi, file, startIndex, endIndex);
            }
        }
        public static PowerEfficiencyCurve ReadEfficiency(Model mi, TextFile file, int startIndex, int endIndex, Node node = null)
        {
            PowerEfficiencyCurve effCurve = null;
            string effCurveName = null;
            if (node != null)
            {
                // reading old version from reservoir node
                effCurveName = XYFileReader.ReadString("effCurveName", mi.PowerObjects.GetUniqueName(ModsimCollectionType.powerEff, node.name + "_effCurve"), file, startIndex, endIndex);
            }
            else
            {
                effCurveName = XYFileReader.ReadString("effCurveName", mi.PowerObjects.GetUniqueName(ModsimCollectionType.powerEff), file, startIndex, endIndex);
            }
            bool justTheName = XYFileReader.ReadBoolean("justTheName", false, file, startIndex, endIndex);
            if (justTheName)
                return mi.hydro.GetEfficiencyCurve(effCurveName);
            double[] heads = XYFileReader.ReadIndexedFloatList("fakeht", 0, file, startIndex, endIndex);
            ModsimUnits headUnits = XYFileReader.ReadUnits(mi, "ht_units", mi.LengthUnits, file, startIndex, endIndex);
            double[] flows = XYFileReader.ReadIndexedFloatList("qt", 0, file, startIndex, endIndex);
            ModsimUnits flowUnits = XYFileReader.ReadUnits(mi, "qt_units", mi.FlowUnits, file, startIndex, endIndex);
            double[] tmpFloat = XYFileReader.ReadIndexedFloatList("efft", 0, file, startIndex, endIndex);
            double[,] tmpEff = new double[heads.Length, flows.Length];
            if (heads.Length == 0 || flows.Length == 0 || tmpFloat.Length == 0)
                return null;
            bool isVer7 = (mi.inputVersion.Type == InputVersionType.V056);
            if (mi.inputVersion.Type > InputVersionType.V056)
            {
                Array.Resize(ref tmpFloat, heads.Length * flows.Length + 1);
            }
            try
            {
                for (int row = 0; row < heads.Length; row++)
                {
                    for (int col = 0; col < flows.Length; col++)
                    {
                        int idxFloat = (row * flows.Length) + col;
                        if (isVer7)
                        {
                            // version 7 hardcoded to 18 x 18 matrix
                            idxFloat = (row * 18) + col;
                        }

                        if (isVer7 && idxFloat > tmpFloat.Length - 1)
                        {
                            // if length of tmpFloat is less than 
                            // heads.length * 18, assume remaining values
                            // are zero due to xy file compaction

                            // tmpEff is already initialized to zeros
                        }
                        else
                        {
                            tmpEff[row, col] = tmpFloat[idxFloat];
                        }
                    }
                }

                // Build the power efficiency curve
                effCurve = new PowerEfficiencyCurve(mi, effCurveName, flows, flowUnits, heads, headUnits, tmpEff);
                effCurve.AddToController();
                return effCurve;
            }
            catch (Exception ex)
            {
                string msg = "Error reading Efficency Table " + ex.Message;
                mi.FireOnError(msg);
                throw new Exception(msg);
            }
        }

        // hydropower units
        public static void ReadHydroUnits(Model mi, TextFile file)
        {
            int startIndex = 0;
            int endIndex = 0;
            while (endIndex < file.Count)
            {
                // Update the indices
                if (!GetIndices(mi, HydropowerUnit.XYCmdName, file, ref startIndex, ref endIndex))
                    return;

                // Read hydropower unit
                ReadHydroUnit(mi, file, startIndex, endIndex);
            }
        }
        public static HydropowerUnit ReadHydroUnit(Model mi, TextFile file, int startIndex, int endIndex, Node node = null)
        {
            HydropowerUnit hydroUnit = null;
            try
            {
                string name = XYFileReader.ReadString("HydroUnitName", "", file, startIndex, endIndex);
                string desc = XYFileReader.ReadString("desc", "", file, startIndex, endIndex);
                HydroUnitType type = (HydroUnitType)Enum.Parse(typeof(HydroUnitType), XYFileReader.ReadString("type", HydroUnitType.Turbine.ToString(), file, startIndex, endIndex));
                double powerCap = XYFileReader.ReadFloat("powerCap", 0, file, startIndex, endIndex);
                Link[] links = Array.ConvertAll<int, Link>(Array.ConvertAll<long, int>(XYFileReader.ReadIndexedIntegerList("links", 0, file, startIndex, endIndex), (long l) => Convert.ToInt32(l)), (int l) => XYFileReader.LinkArray[l]);
                HydropowerElevDef elevDefFrom = ReadHydroElevDef("elevDefFrom", file, startIndex, endIndex);
                HydropowerElevDef elevDefTo = ReadHydroElevDef("elevDefTo", file, startIndex, endIndex);
                PowerEfficiencyCurve effcurve = ReadEfficiency(mi, file, startIndex, endIndex);
                TimeSeries genHours = XYFileReader.ReadTimeSeries(mi, "tsgeneratehrs", file, startIndex, endIndex);
                bool peakGenOnly = XYFileReader.ReadBoolean("peakGen", false, file, startIndex, endIndex);
                bool noFlowDowntime = XYFileReader.ReadBoolean("noFlowDowntime", true, file, startIndex, endIndex);

                // Build the HydropowerUnit object
                hydroUnit = new HydropowerUnit(mi, name, links, elevDefFrom, elevDefTo, effcurve, type, powerCap, genHours, peakGenOnly);
                hydroUnit.Description = desc;
                hydroUnit.NoFlowDuringDowntime = noFlowDowntime;
                hydroUnit.AddToController();
            }
            catch (Exception ex)
            {
                string msg = $"ERROR: [reading hydrounit info] at line {startIndex} " + ex.Message;
                mi.FireOnError(msg);
            }            
            return hydroUnit;
        }
        public static HydropowerElevDef ReadHydroElevDef(string cmd, TextFile file, int startIndex, int endIndex)
        {
            ElevType type = (ElevType)Enum.Parse(typeof(ElevType), XYFileReader.ReadString(cmd + "Type", ElevType.Forebay.ToString(), file, startIndex, endIndex));
            Node res = XYFileReader.NodeArray[XYFileReader.ReadInteger(cmd + "Res", -1, file, startIndex, endIndex)];
            return new HydropowerElevDef(res, type);
        }
        public static HydropowerUnit[] ReadHydroUnitIDList(Model mi, string cmd, TextFile file, int startIndex, int endIndex)
        {
            return Array.ConvertAll<int, HydropowerUnit>(Array.ConvertAll<long, int>(XYFileReader.ReadIndexedIntegerList(cmd, 0, file, startIndex, endIndex), (long l) => Convert.ToInt32(l)), (int l) => mi.hydro.GetHydroUnit(l));
        }

        // hydropower targets 
        public static void ReadHydroTargets(Model mi, TextFile file)
        {
            int startIndex = 0;
            int endIndex = 0;
            while (endIndex < file.Count)
            {
                // Update the indices
                if (!GetIndices(mi, HydropowerTarget.XYCmdName, file, ref startIndex, ref endIndex))
                    return;

                // Read hydropower target
                ReadHydroTarget(mi, file, startIndex, endIndex);
            }
        }
        public static HydropowerTarget ReadHydroTarget(Model mi, TextFile file, int startIndex, int endIndex, Node node = null)
        {
            string name = XYFileReader.ReadString("HydroTargetName", "", file, startIndex, endIndex);
            HydropowerUnit[] hydroUnits = ReadHydroUnitIDList(mi, "hydroUnits", file, startIndex, endIndex);
            TimeSeries powerDemand = XYFileReader.ReadTimeSeries(mi, "tspowerdemands", file, startIndex, endIndex);

            // Build the HydropowerTarget object
            HydropowerTarget hydroTarget = new HydropowerTarget(mi, name, powerDemand, hydroUnits);
            hydroTarget.AddToController();
            return hydroTarget;
        }
    }
}
