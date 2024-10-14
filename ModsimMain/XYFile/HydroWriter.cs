using System.IO;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.ModsimIO
{
    public class HydroWriter
    {
        // Write all
        public static void WriteBeforeNodes(Model mi, StreamWriter xyOutStream)
        {
            WriteHydroDetails(mi, xyOutStream);
            WriteAllEfficiencies(mi, xyOutStream);
        }
        public static void WriteAfterNodes(Model mi, StreamWriter xyOutStream)
        {
            if (mi.outputVersion.Type < OutputVersionType.V8_3_0)
                return;
            WriteAllHydroUnits(mi, xyOutStream);
            WriteAllHydroTargets(mi, xyOutStream);
        }

        // Hydropower controller details
        public static void WriteHydroDetails(Model mi, StreamWriter xyOutStream)
        {
            if (mi.outputVersion.Type < OutputVersionType.V8_3_0)
                return;
            xyOutStream.WriteLine(HydropowerController.XYCmdName);
            xyOutStream.WriteLine("ExtHydropowerActive " + mi.hydro.IsActive.ToString());
            XYFileWriter.WriteBoolean("hydroUpgraded", xyOutStream, mi.hydro.WasUpgraded, false);
            XYFileWriter.WriteInteger("previters", xyOutStream, mi.hydro.NumPrevIters, 3);
            XYFileWriter.WriteInteger("prevtsteps", xyOutStream, mi.hydro.NumPrevTSteps, 5);
            XYFileWriter.WriteFloat("tolerance", xyOutStream, mi.hydro.Tolerance, 0.1);
        }

        // Efficiency tables
        public static void WriteAllEfficiencies(Model mi, StreamWriter xyOutStream)
        {
            foreach (PowerEfficiencyCurve effCurve in mi.hydro.EfficiencyCurves)
            {
                xyOutStream.WriteLine(PowerEfficiencyCurve.XYCmdName);
                WriteEfficiency(mi, effCurve, xyOutStream, false);
            }
        }
        public static void WriteEfficiency(Model mi, PowerEfficiencyCurve effCurve, StreamWriter xyOutStream, bool WriteOnlyIfNotInController = false)
        {
            if (effCurve == null)
                return;
            if (mi.outputVersion.Type >= OutputVersionType.V8_3_0)
            {
                xyOutStream.WriteLine("effCurveName " + effCurve.Name);
                if (WriteOnlyIfNotInController && effCurve.IsInController)
                {
                    XYFileWriter.WriteBoolean("justTheName", xyOutStream, true, false);
                    return;
                }
            }
            XYFileWriter.WriteIndexedFloatList("fakeht", effCurve.Heads, 0, xyOutStream);
            XYFileWriter.WriteIndexedFloatList("qt", effCurve.Flows, 0, xyOutStream);
            // qt.Length = number of columns
            xyOutStream.WriteLine("qt_units " + effCurve.FlowUnits.Label);
            if (mi.outputVersion.Type >= OutputVersionType.V8_2)
                xyOutStream.WriteLine("ht_units " + effCurve.HeadUnits.Label);
            int row = 0;
            int col = 0;
            bool writeit = false;
            for (row = 0; row < effCurve.Heads.Length; row++)
            {
                for (col = 0; col < effCurve.Flows.Length; col++)
                {
                    if (effCurve.Efficiencies[row, col] != 0.0)
                    {
                        writeit = true;
                    }
                }
            }
            if (writeit == true)
            {
                xyOutStream.WriteLine("efft");
                //  Efficency Table
                for (row = 0; row < effCurve.Heads.Length; row++)
                {
                    for (col = 0; col < effCurve.Flows.Length; col++)
                    {
                        int idxFloat = (row * effCurve.Flows.Length) + col;
                        xyOutStream.WriteLine(idxFloat + " " + effCurve.Efficiencies[row, col]);
                    }
                }
            }
        }

        // Hydropower units
        public static void WriteAllHydroUnits(Model mi, StreamWriter xyOutStream)
        {
            foreach (HydropowerUnit hydroUnit in mi.hydro.HydroUnits)
            {
                xyOutStream.WriteLine(HydropowerUnit.XYCmdName);
                WriteHydroUnit(mi, hydroUnit, xyOutStream);
            }
        }
        public static void WriteHydroUnit(Model mi, HydropowerUnit hydroUnit, StreamWriter xyOutStream, bool justTheName = false)
        {
            xyOutStream.WriteLine("HydroUnitName " + hydroUnit.Name);
            xyOutStream.WriteLine("desc " + hydroUnit.Description);
            xyOutStream.WriteLine("type " + hydroUnit.Type.ToString());
            XYFileWriter.WriteFloat("powerCap", xyOutStream, hydroUnit.PowerCapacity, 0);
            XYFileWriter.WriteLinkNumberList("links", hydroUnit.FlowLinks, xyOutStream);
            WriteHydroElevDef("elevDefFrom", hydroUnit.ElevDefnFrom, xyOutStream);
            WriteHydroElevDef("elevDefTo", hydroUnit.ElevDefnTo, xyOutStream);
            WriteEfficiency(mi, hydroUnit.EfficiencyCurve, xyOutStream, true);
            XYFileWriter.WriteTimeSeries("tsgeneratehrs", hydroUnit.GeneratingHoursTS, xyOutStream);
            XYFileWriter.WriteBoolean("peakGen", xyOutStream, hydroUnit.PeakGenerationOnly, false);
            XYFileWriter.WriteBoolean("noFlowDowntime", xyOutStream, hydroUnit.NoFlowDuringDowntime, true);
        }
        public static void WriteHydroElevDef(string cmd, HydropowerElevDef elevDef, StreamWriter xyOutStream)
        {
            xyOutStream.WriteLine(cmd + "Type " + elevDef.Type.ToString());
            XYFileWriter.WriteNodeNumber(cmd + "Res", elevDef.Reservoir, xyOutStream);
        }
        public static void WriteHydroUnitIDList(string cmd, HydropowerUnit[] hydroUnits, StreamWriter xyOutStream)
        {
            if (hydroUnits == null | hydroUnits.Length == 0)
                return;
            xyOutStream.WriteLine(cmd);
            for (int i = 0; i < hydroUnits.Length; i++)
            {
                xyOutStream.WriteLine(i.ToString() + " " + hydroUnits[i].ID.ToString());
            }
        }

        // Hydropower targets
        public static void WriteAllHydroTargets(Model mi, StreamWriter xyOutStream)
        {
            foreach (HydropowerTarget hydroTarget in mi.hydro.HydroTargets)
            {
                xyOutStream.WriteLine(HydropowerTarget.XYCmdName);
                WriteHydroTarget(hydroTarget, xyOutStream);
            }
        }
        public static void WriteHydroTarget(HydropowerTarget hydroTarget, StreamWriter xyOutStream)
        {
            xyOutStream.WriteLine("HydroTargetName " + hydroTarget.Name);
            WriteHydroUnitIDList("hydroUnits", hydroTarget.HydroUnits, xyOutStream);
            XYFileWriter.WriteTimeSeries("tspowerdemands", hydroTarget.PowerTargetsTS, xyOutStream);
        }
    }
}
