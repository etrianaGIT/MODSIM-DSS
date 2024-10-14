using System;
using System.IO;
using System.Text;

namespace Csu.Modsim.ModsimModel
{
    public class CostSummary 
    {
        // Shared variables
        public readonly static string[] Headers = { "TimeStep", "Iteration", "Link", "Lower_Bound", "Cost", "Upper_Bound", "Flow", "Total_Cost", "Total_Dynamic_Cost" };

        // Instance variables
        private Model model;
        private StreamWriter sw = null;
        private object thislock = new object();
        private bool fileExists = false;
        private Link[] links;
        /// <summary>The total summed network cost of the solution over all timesteps before the current timestep.</summary>
        public double TotalDynamicCost = 0.0;
        /// <summary>The total cost of the network at the current timestep.</summary>
        public double TotalCost = 0.0;
        /// <summary>The total flow through all links in the network at the current timestep.</summary>
        public double TotalFlow = 0.0;

        // Constructors
        public CostSummary(Model model, string FileName)
        {
            // Declare variables
            this.model = model;
            this.TotalDynamicCost = 0.0;
            this.links = this.model.mInfo.lList;

            // Open file for writing if it exists
            string dir = Path.GetDirectoryName(model.fname) + @"\";
            if (dir.Equals(@"\")) dir = ""; 
            string netBaseName = Path.GetFileNameWithoutExtension(model.fname);
            string costBaseName = FileName;
            if (File.Exists(FileName = dir + netBaseName + "_" + costBaseName) 
                || File.Exists(FileName = dir + netBaseName + costBaseName) 
                || File.Exists(FileName = dir + costBaseName))
            {
                try
                {
                    sw = new StreamWriter(FileName);
                    sw.Write(string.Join(",", Headers) + "\n");
                    GlobalMembersArcdump.WriteNames(model);
                    fileExists = true;
                }
                catch
                {
                    sw = null;
                }
            }
        }

        // Destructors
        /// <summary>Closes and disposes the file to which the cost summary is being written.</summary>
        public void Close()
        {
            lock (thislock)
            {
                try
                {
                    if(sw!=null) sw.Close();
                }
                catch { }
                finally
                {
                    sw = null;
                }
            }
        }

        // Methods
        public void Write()
        {
            // make sure file exists
            if (!fileExists)
                return;

            // Initialize variables
            StringBuilder s = new StringBuilder();
            double CurrCost;

            lock (thislock)
            {
                if (this.model.mInfo.Iteration == 0)
                    this.TotalDynamicCost += this.TotalCost;

                this.TotalCost = 0.0;
                this.TotalFlow = 0.0;

                // Loop through all links and place output and sum total cost.
                foreach (Link l in this.links)
                {
                    if (l != null)
                    {
                        CurrCost = l.mlInfo.flow * l.mlInfo.cost;
                        if (this.sw != null) s.Append(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},\n", this.model.mInfo.CurrentModelTimeStepIndex, this.model.mInfo.Iteration, l.number, l.mlInfo.lo, l.mlInfo.cost, l.mlInfo.hi, l.mlInfo.flow, CurrCost));
                        this.TotalFlow += l.mlInfo.flow;
                        this.TotalCost += CurrCost;
                    }
                }
                if (this.sw != null)
                {
                    s.Append(string.Format("{0},{1},Total,,,,{2},{3},{4}\n", this.model.mInfo.CurrentModelTimeStepIndex, this.model.mInfo.Iteration, this.TotalFlow, this.TotalCost, this.TotalDynamicCost));
                    this.sw.Write(s.ToString());
                }
            }
        }
        
    }
}
