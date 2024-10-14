using System.Data;

namespace Csu.Modsim.ModsimModel
{
    /// <summary>Static booleans set to true if desired output is desired</summary>
    public struct OutputControlInfo
    {
        public static bool res_output = true;
        public static bool dws_rel = true;
        public static bool elev_end = true;
        public static bool evp_loss = true;
        public static bool evp_rate = true;
        public static bool gwater = true;
        public static bool head_avg = true;
        public static bool hydra_Cap = true;
        public static bool powr_avg = true;
        public static bool powr_pk = true;
        public static bool pump_in = true;
        public static bool pump_out = true;
        public static bool pwr_2nd = true;
        public static bool seepage = true;
        public static bool spills = true;
        public static bool stor_beg = true;
        public static bool stor_end = true;
        public static bool stor_trg = true;
        public static bool unreg_in = true;
        public static bool ups_rel = true;
        public static bool dem_output = true;
        public static bool dem_sht = true;
        public static bool demand = true;
        public static bool gw_in = true;
        public static bool surf_in = true;
        public static bool acc_flow = true;
        public static bool acc_output = true;
        public static bool acrl = true;
        public static bool stgl = true;
        public static bool flo_flow = true;
        public static bool flo_output = true;
        public static bool loss = true;
        public static bool natflow = true;
        public static bool fromgwtonode = true;
        public static bool gw_output = true;
        public static bool gwinfiltration = true;
        public static bool special_rpts = false;
        public static bool partial_flows = false;
        //Ver 8.1 output control vars
        public static bool hydroState = true;
        public static bool nonStorage_output = true;
        public static bool l_Max = true;
        public static bool l_Min = true;
        public static bool Flow_Thru = true;
        public static bool Rout_Ret = true;
        public static bool Inflow = true;
        //Output format selection flags
        public static bool ver7OutputFiles = false;
        public static bool ver8MSDBOutputFiles = false;
        public static bool SQLiteOutputFiles = true;
        public static bool DeleteTempVer8OutputFiles = true;
        public static int noTimeStepsInMemory = 12;
        //table with output variables name and type for ModsimGraphs
        public DataTable variableOutputNames;
        //table with output variables types identified in the output DB for ModsimGraphs
        public DataTable variableOutputTypes;
        /// <summary>Creates a new copy of this OutputControlInfo instance.</summary>
        /// <returns>Returns a copy of this OutputControlInfo instance.</returns>
        public OutputControlInfo Copy()
        {
            OutputControlInfo oc = new OutputControlInfo();
            oc = (OutputControlInfo)this.MemberwiseClone();
            if (this.variableOutputNames != null) oc.variableOutputNames = this.variableOutputNames.Copy();
            if (this.variableOutputTypes != null) oc.variableOutputTypes = this.variableOutputTypes.Copy();
            return oc;
        }
    }
}
