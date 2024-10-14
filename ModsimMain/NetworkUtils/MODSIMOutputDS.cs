using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.NetworkUtils
{
    partial class MODSIMOutputDS
    {
        partial class LinksOutputDataTable
        {
        }

        partial class TimeStepsDataTable
        {
        }

        public MODSIMOutputDS(Model mi)
        : this()
        {
            //if (mi.hydro.IsActive)
            //{
            //    // Add the hydropower unit table
            //    this.Tables.Add(mi.hydro.HydroUnitsTable);

            //    // Add the hydropower target table
            //    this.Tables.Add(mi.hydro.HydroTargetsTable);
            //}
        }

        /// <summary>Deletes unused columns within the datatables.</summary>
        public void CleanColumns()
        {
            // Demand
            MODSIMOutputDS.DEMOutputDataTable dem = this.DEMOutput;
            if (!OutputControlInfo.demand)
            {
                dem.Columns.Remove(dem.DemandColumn);
            }
            if (!OutputControlInfo.surf_in)
            {
                dem.Columns.Remove(dem.Surf_InColumn);
            }
            if (!OutputControlInfo.gw_in)
            {
                dem.Columns.Remove(dem.Gw_InColumn);
            }
            if (!OutputControlInfo.dem_sht)
            {
                dem.Columns.Remove(dem.ShortageColumn);
            }
            if (!OutputControlInfo.hydroState)
            {
                dem.Columns.Remove(dem.Hydro_StateColumn);
            }
            dem = null;
            // Reservoir storage output
            MODSIMOutputDS.RES_STOROutputDataTable resStor = this.RES_STOROutput;
            if (!OutputControlInfo.stor_beg)
            {
                resStor.Columns.Remove(resStor.Stor_BegColumn);
            }
            if (!OutputControlInfo.stor_end)
            {
                resStor.Columns.Remove(resStor.Stor_EndColumn);
            }
            if (!OutputControlInfo.stor_trg)
            {
                resStor.Columns.Remove(resStor.Stor_TrgColumn);
            }
            if (!OutputControlInfo.elev_end)
            {
                resStor.Columns.Remove(resStor.Elev_EndColumn);
            }
            resStor = null;
            // Reservoir output
            MODSIMOutputDS.RESOutputDataTable res = this.RESOutput;
            if (!OutputControlInfo.pump_in)
            {
                res.Columns.Remove(res.Pump_InColumn);
            }
            if (!OutputControlInfo.gwater)
            {
                res.Columns.Remove(res.Gw_InColumn);
            }
            if (!OutputControlInfo.dws_rel)
            {
                res.Columns.Remove(res.Dws_RelColumn);
            }
            if (!OutputControlInfo.pump_out)
            {
                res.Columns.Remove(res.Pump_OutColumn);
            }
            if (!OutputControlInfo.spills)
            {
                res.Columns.Remove(res.SpillsColumn);
            }
            if (!OutputControlInfo.ups_rel)
            {
                res.Columns.Remove(res.Ups_RelColumn);
            }
            if (!OutputControlInfo.hydra_Cap)
            {
                res.Columns.Remove(res.Hydra_CapColumn);
            }
            if (!OutputControlInfo.seepage)
            {
                res.Columns.Remove(res.SeepageColumn);
            }
            if (!OutputControlInfo.head_avg)
            {
                res.Columns.Remove(res.Head_AvgColumn);
            }
            if (!OutputControlInfo.evp_loss)
            {
                res.Columns.Remove(res.Evap_LossColumn);
            }
            if (!OutputControlInfo.powr_avg)
            {
                res.Columns.Remove(res.Powr_AvgColumn);
            }
            if (!OutputControlInfo.powr_pk)
            {
                res.Columns.Remove(res.EnergyColumn);
            }
            if (!OutputControlInfo.pwr_2nd)
            {
                res.Columns.Remove(res.Pwr_2ndColumn);
            }
            if (!OutputControlInfo.hydroState)
            {
                res.Columns.Remove(res.Hydro_StateColumn);
            }
            res = null;
            // Flow output
            MODSIMOutputDS.LinksOutputDataTable flow = this.LinksOutput;
            if (!OutputControlInfo.flo_flow)
            {
                flow.Columns.Remove(flow.FlowColumn);
            }
            if (!OutputControlInfo.loss)
            {
                flow.Columns.Remove(flow.LossColumn);
            }
            if (!OutputControlInfo.natflow)
            {
                flow.Columns.Remove(flow.NaturalFlowColumn);
            }
            if (!OutputControlInfo.l_Max)
            {
                flow.Columns.Remove(flow.LMaxColumn);
            }
            if (!OutputControlInfo.l_Min)
            {
                flow.Columns.Remove(flow.LMinColumn);
            }
            if (!OutputControlInfo.hydroState)
            {
                flow.Columns.Remove(flow.Hydro_StateColumn);
            }
            // Account output file
            if (!OutputControlInfo.acc_output)
            {
                flow.Columns.Remove(flow.StorLeftColumn);
                flow.Columns.Remove(flow.AccrualColumn);
                flow.Columns.Remove(flow.GroupLinkColumn);
                flow.Columns.Remove(flow.GroupStorLeftColumn);
                flow.Columns.Remove(flow.GroupAccrualColumn);
            }
            else
            {
                if (!OutputControlInfo.stgl)
                {
                    flow.Columns.Remove(flow.StorLeftColumn);
                }
                if (!OutputControlInfo.acrl)
                {
                    flow.Columns.Remove(flow.AccrualColumn);
                }
            }
            // Nonstorage
            MODSIMOutputDS.NON_STOROutputDataTable nonS = this.NON_STOROutput;
            if (!OutputControlInfo.nonStorage_output)
            {
                if (!OutputControlInfo.Inflow)
                {
                    nonS.Columns.Remove(nonS.InflowColumn);
                }
                if (!OutputControlInfo.Flow_Thru)
                {
                    nonS.Columns.Remove(nonS.Flow_ThruColumn);
                }
                if (!OutputControlInfo.Rout_Ret)
                {
                    nonS.Columns.Remove(nonS.Rout_RetColumn);
                }
            }
        }
        /// <summary>Clears all the output tables for back-routing routines.</summary>
        public void ClearOutputTables()
        {
            this.LinksOutput.Clear();
            this.RESOutput.Clear();
            this.RES_STOROutput.Clear();
            this.DEMOutput.Clear();
            this.NON_STOROutput.Clear();
            this.HydroUnitOutput.Clear();
            this.HydroTargetOutput.Clear();
        }
    }
}
