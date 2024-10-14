using System;

namespace Csu.Modsim.ModsimModel
{

    public class PowerEfficiencyCurve : IModsimObject, IComparable
    {
        /// <summary>The command name used in the xy file.</summary>
        public static string XYCmdName { get { return "effCurve"; } }

        private int id = -1;
        private string name = "";
        private Model model;
        private double[] flows = { 0.0, 1000.0 };
        private ModsimUnits flowUnits = HydropowerUnit.DefaultFlowUnits;
        private double[] heads = { 0.0, 1000.0 };
        private ModsimUnits headUnits = HydropowerUnit.DefaultHeadUnits;
        private double[,] efficiencies = { { 1.0, 1.0 }, { 1.0, 1.0 } };

        /// <summary>Gets the ID number of this efficiency table.</summary>
        public int ID
        {
            get { return this.id; }
        }
        /// <summary>Gets whether this efficiency table is in the hydropower controller.</summary>
        public bool IsInController
        {
            get { return this.id != -1; }
        }
        /// <summary>Gets and sets the name of this efficiency table.</summary>
        public string Name
        {
            get { return this.name; }
            set 
            {
                if (value == null) value = "";
                if (this.id != -1)
                {
                    if (this.model.PowerObjects.Exists(this.ModsimObjectType, this.id))
                    {
                        this.model.PowerObjects.SetName(this.ModsimObjectType, this.id, ref value);
                    }
                }
                this.name = value; 
            }
        }
        /// <summary>Gets the ModsimCollectionType of this instance.</summary>
        public ModsimCollectionType ModsimObjectType { get { return ModsimCollectionType.powerEff; } }
        /// <summary>Gets and sets the units of flow in the columns of the efficiency table.</summary>
        public ModsimUnits FlowUnits
        {
            get { return this.flowUnits; }
            set { this.flowUnits = value; }
        }
        /// <summary>Gets the units of elevation head in the rows of the efficiency table.</summary>
        public ModsimUnits HeadUnits
        {
            get { return this.headUnits; }
            set { this.headUnits = value; }
        }
        /// <summary>Gets and sets the array of flows defining the columns of the efficiency table.</summary>
        public double[] Flows
        {
            get { return this.flows; }
            set { this.flows = value; }
        }
        /// <summary>Gets and sets the array of heads defining the rows of the efficiency table.</summary>
        public double[] Heads
        {
            get { return this.heads; }
            set { this.heads = value; }
        }
        /// <summary>Gets and sets the array of efficiencies.</summary>
        public double[,] Efficiencies
        {
            get { return this.efficiencies; }
            set { this.efficiencies = value; }
        }

        // Constructors
        /// <summary>Constructs a default power efficiency curve.</summary>
        public PowerEfficiencyCurve(Model model)
        {
            this.model = model;
        }
        /// <summary>Constructs a new instance.</summary>
        /// <param name="name">The name to set this efficiency table to.</param>
        /// <param name="flows">An array of flows defining the columns of the efficiency table.</param>
        /// <param name="flowUnits">The units of flow.</param>
        /// <param name="heads">An array of elevation head values defining the rows of the efficiency table.</param>
        /// <param name="headUnits">The units of elevation head.</param>
        /// <param name="efficiencies">A two-dimensional array of power efficiencies.</param>
        public PowerEfficiencyCurve(Model model, string name, double[] flows, ModsimUnits flowUnits, double[] heads, ModsimUnits headUnits, double[,] efficiencies)
        {
            this.model = model;
            this.name = name;
            this.flows = flows;
            this.flowUnits = flowUnits;
            this.heads = heads;
            this.headUnits = headUnits;
            this.efficiencies = efficiencies;
        }
       
        // Copy methods 
        /// <summary>Makes a new copy of this instance.</summary>
        public PowerEfficiencyCurve Copy()
        {
            return this.Copy(this.model); 
        }
        /// <summary>Makes a new copy of this instance.</summary>
        /// <returns>Returns the copied instance.</returns>
        public PowerEfficiencyCurve Copy(Model newModelReference)
        {
            PowerEfficiencyCurve retVal = (PowerEfficiencyCurve)this.MemberwiseClone();
            retVal.model = newModelReference;
            retVal.flows = (double[])this.flows.Clone();
            retVal.heads = (double[])this.heads.Clone();
            retVal.efficiencies = (double[,])this.efficiencies.Clone();
            this.flows.CopyTo(retVal.flows, 0);
            this.heads.CopyTo(retVal.heads, 0);
            for (int i = 0; i < this.efficiencies.GetLength(0); i++)
            {
                for (int j = 0; j < this.efficiencies.GetLength(1); j++)
                {
                    retVal.efficiencies[i, j] = this.efficiencies[i, j];
                }
            }
            retVal.flowUnits = this.flowUnits.Copy();
            retVal.headUnits = this.headUnits.Copy();
            retVal.id = -1;
            return retVal;
        }

        // Methods
        /// <summary>Gets efficiencies at each head value in the table at a specified flow.</summary>
        /// <param name="flow">The specified flow in Model.FlowUnits (not scaled).</param>
        public double[] GetEfficienciesAtFlow(double flow, bool flowIsScaled)
        {
            double[] retVal = new double[this.heads.Length];
            for (int i = 0; i < this.heads.Length; i++)
            {
                retVal[i] = this.GetEfficiency(flow, this.flowUnits, this.heads[i], this.headUnits, flowIsScaled);
            }
            return retVal;
        }
        /// <summary>Gets efficiencies at each flow value in the table at a specified head.</summary>
        /// <param name="head">The specified head.</param>
        /// <returns>Returns the array of efficiencies corresponding to the head value</returns>
        public double[] GetEfficienciesAtHead(double head)
        {
            double[] retVal = new double[this.flows.Length];
            for (int i = 0; i < this.flows.Length; i++)
            {
                retVal[i] = this.GetEfficiency(this.flows[i], this.flowUnits, head, this.headUnits, true);
            }
            return retVal;
        }
        /// <summary>Converts the head and flow units associated with this instance.</summary>
        public void ConvertUnits(ModsimUnits newFlowUnits, ModsimUnits newHeadUnits)
        {
            foreach (double flow in this.flows)
            {
                this.flowUnits.ConvertTo(flow, newFlowUnits);
            }
            foreach (double head in this.heads)
            {
                this.headUnits.ConvertTo(head, newHeadUnits);
            }
            this.flowUnits = newFlowUnits;
            this.headUnits = newHeadUnits;
        }
        /// <summary>Retrieves an efficiency value from the efficiency table.</summary>
        /// <param name="flow">The flow for which to get an efficiency value.</param>
        /// <param name="flowUnits">The units of flow.</param>
        /// <param name="head">The head for which to get an efficiency value.</param>
        /// <param name="headUnits">The units of head.</param>
        /// <returns>Returns an efficiency value from the efficiency table.</returns>
        public double GetEfficiency(double flow, ModsimUnits flowUnits, double head, ModsimUnits headUnits, bool flowIsScaled)
        {
            if (!flowIsScaled)
            {
                flow *= this.model.ScaleFactor;
            }
            flow = flowUnits.ConvertTo(flow, this.flowUnits, this.model.mInfo.CurrentBegOfPeriodDate);
            head = headUnits.ConvertTo(head, this.headUnits);

            double flow_orig = flow; //long qp;
            int i;
            int ih;
            int iq;
            int ih1 = 0;
            int iq1 = 0;
            int ih2 = 0;
            int iq2 = 0;
            double ef1;
            double ef2;
            bool found;

            // Efficiency with no data or before all data is assumed to be zero.
            if (this.heads.Length == 0 || this.flows.Length == 0 || head < this.heads[0] || flow < this.flows[0])
                return 0.0; 

            // If there's only one point in the surface, the interploated value is the point's value
            if (this.heads.Length == 1 && this.flows.Length == 1)
            {
                if (head >= this.heads[0] && flow > this.flows[0])
                    return this.efficiencies[0, 0];
                else
                    return 0.0;
            }

            // Check the large numbers
            ih = -1;
            iq = -1;
            if (head >= this.heads[this.heads.Length - 1])
            {
                ih = this.heads.Length - 1;
            }
            if (flow >= this.flows[this.flows.Length - 1])
            {
                iq = this.flows.Length - 1;
                flow = this.flows[iq];
            }

            found = false;
            for (i = 0; !found && i < this.heads.Length; i++)
            {
                if (head > this.heads[i])
                {
                    ih1 = i;
                }
                if (head == this.heads[i])
                {
                    ih = i;
                    found = true;
                }
            }
            if (!found)
            {
                ih2 = ih1 + 1;
            }

            found = false;
            for (i = 0; !found && i < this.flows.Length; i++)
            {
                if (flow > this.flows[i])
                {
                    iq1 = i;
                }
                if (flow == this.flows[i])
                {
                    iq = i;
                    found = true;
                }
            }
            if (!found)
            {
                iq2 = iq1 + 1;
            }

            // For point classified to corner or grid point, just return value. 
            if ((ih != -1) && (iq != -1))
            {
                return (this.efficiencies[ih, iq]);
            }
            if (ih != -1)
            {
                return interpolate(flow, this.flows[iq1], this.efficiencies[ih, iq1], this.flows[iq2], this.efficiencies[ih, iq2]);
            }
            if (iq != -1)
            {
                return interpolate(head, this.heads[ih1], this.efficiencies[ih1, iq], this.heads[ih2], this.efficiencies[ih2, iq]);
            }

            // Interpolate head first then flow
            ef1 = interpolate(head, this.heads[ih1], this.efficiencies[ih1, iq1], this.heads[ih2], this.efficiencies[ih2, iq1]);
            ef2 = interpolate(head, this.heads[ih1], this.efficiencies[ih1, iq2], this.heads[ih2], this.efficiencies[ih2, iq2]);
            return interpolate(flow, this.flows[iq1], ef1, this.flows[iq2], ef2);
        }
        /// <summary>Interpolates (finds a value 'y') between points (x1,y1) and (x2,y2) given a value x.</summary>
        /// <param name="x">The value on the x-axis from which to interpolate.</param>
        /// <returns>Returns 'y' found at 'x' on the line defined by the two points (x1,y1) and (x2,y2).</returns>
        private static double interpolate(double x, double x1, double y1, double x2, double y2)
        {
            if (x1 == x2)
            {
                if (y1 != y2)
                    throw new Exception("Can't interpolate between y's for the same x.");
                return y1;
            }
            else
                return y2 + (y2 - y1) * (x - x2) / (x2 - x1);
        }

        /// <summary>Adds this power efficiency table to a list shared with the hydropower controller.</summary>
        public void AddToController()
        {
            if (this.id == -1)
                this.model.PowerObjects.Add(this.ModsimObjectType, this, ref this.name, out this.id);
        }
        /// <summary>Removes this power efficiency table from the list shared with the hydropower controller.</summary>
        public void RemoveFromController()
        {
            this.model.PowerObjects.Remove(this.ModsimObjectType, this.id);
            this.id = -1;
        }

        /// <summary>Compares two PowerEfficiencyCurves according to their unique IDs.</summary>
        /// <param name="obj">The object to compare this PowerEfficiencyCurve with.</param>
        public int CompareTo(object obj)
        {
            // < 0, this is before obj 
            // = 0, this is in same position as obj
            // > 0, this is after obj
            if (obj == null) return 1;
            PowerEfficiencyCurve curve = obj as PowerEfficiencyCurve;
            if (curve != null)
                return this.ID.CompareTo(curve.ID);
            else
                throw new ArgumentException("When comparing two PowerEfficiencyCurves, need to specify a PowerEfficiencyCurve.");
        }

    }
}
