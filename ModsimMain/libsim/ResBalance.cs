namespace Csu.Modsim.ModsimModel
{
    /// <summary>Represents vertical layers in the reservoir to "balance" physical operation between
    ///reservoirs by staggering priorities between various layers of various reservoirs</summary>
    public class ResBalance
    {
        private void Clear()
        {
            /* RKL
            // is this still ok if we change incPriorities to double subscript?
            RKL */
            incrPriorities = new long[0];
            targetPercentages = new double[0];
            PercentBasedOnMaxCapacity = false;
        }

        /* RKL
        //  need to make the incrPriorities a double subscript to get rid of priorities on the 
        // reservoir atificial target storage links
        RKL */
        // long incrPriorities __gc[,];
        /// <summary>Array of layer priorities</summary>
        /// <remarks>Translated (multiplied by 10) to set cost on artificial link representing the layer</remarks>
        public long[] incrPriorities;
        /// <summary>Array of volume percentages for reservoir layers</summary>
        /// <remarks> Percent is of max_volume by default; option to use the target which is dynamic</remarks>
        public double[] targetPercentages;
        /// <summary>True if percentages are of max_volume; false means percents are of target content</summary>
        public bool PercentBasedOnMaxCapacity;
        public ResBalance()
        {
            Clear();
        }
        /// <summary>Returns address for a new resbalance equivalent to this one</summary>
        // copies timeseries to newTS and returns newTS
        public ResBalance Clone()
        {
            ResBalance newResBal = new ResBalance();
            if (this != null)
            {
                newResBal.incrPriorities = new long[this.incrPriorities.Length];
                this.incrPriorities.CopyTo(newResBal.incrPriorities, 0);
                newResBal.targetPercentages = new double[this.targetPercentages.Length];
                this.targetPercentages.CopyTo(newResBal.targetPercentages, 0);
                newResBal.PercentBasedOnMaxCapacity = this.PercentBasedOnMaxCapacity;
            }
            return newResBal;
        }
    }
}
