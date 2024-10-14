namespace Csu.Modsim.ModsimModel
{
    /// <summary>Linked list of laginfo data structures used to lag flow over time steps.</summary>
    /// <remarks>
    /// LagInfo is used to lag flow over a number of time steps to one or more locations
    /// LagInfo contains the required data to simulate lagged flow
    /// LagInfo does not contain the lagged flow values (see MrlInfo->linkPrevflow)
    /// </remarks>
    public class LagInfo
    {
        /// <summary>Sets defaults.</summary>
        private void Defaults()
        {
            next = null;
            numLags = 0;
            location = null;
            percent = (double)1.0;
        }
        /// <summary>Node location of flow influence (return or depletion</summary>
        public Node location; // Return/Depletion location.
        /// <summary>Percent of total influence for this node location</summary>
        public double percent; // Percent of total this LagInfo node takes.
        /// <summary>number of time step lags for the location influence</summary>
        public int numLags; // Number of lags in this LagInfo.
        /// <summary>If not null; the next laginfo in the linked list</summary>
        public LagInfo next; // Next LagInfo in a linked list.
        /// <summary>time step lag factors for influence at this location</summary>
        public double[] lagInfoData; // lag factors
        /// <summary>Default constructor.</summary>
        public LagInfo()
        {
            Defaults();
            lagInfoData = new double[0];
        }
        /// <summary>Gets the reference of the specified element in the linked list.</summary>
        public LagInfo GetIndexed(int num)
        {
            LagInfo li;
            li = this;
            while ((li.next != null) && (num-- != 0))
                li = li.next;

            return li;
        }
        /// <summary>Gets the length of the linked list.</summary>
        public int GetListLength()
        {
            LagInfo li;
            int num = 0;
            for (li = this; li != null; li = li.next)
                num++;
            return num;
        }
        /// <summary>Returns true if lag factors were successully set to zero for the specified return location.</summary>
        public bool Initialize(Node n, double[] fa)
        {
            int i;
            // Initialize the lagInfo data.
            location = n;
            // Fill out the lags array, if there are any lags.
            if (fa != null)
            {
                // Find the size of the lag factors array.
                i = DefineConstants.MAXLAG - 1;
                while ((i > 0) && (fa[i] == (double)0.0))
                    i--;
                // If there are any lag factors, set the lag array size and copy the lags.
                if (i > 0)
                    if (!SetNumLags(i + 1))
                        return false;
            }
            return true;
        }
        /// <summary> remove the specifed laginfo from this linked list</summary>
        /// <returns> next laginfo after the one removed </returns>
        public LagInfo RemoveFromList(LagInfo lagInfo)
        {
            LagInfo prev;
            if (lagInfo == this)
            {
                prev = next;
                return prev;
            }
            prev = this;
            while (prev.next != null && (prev.next != lagInfo))
                prev = prev.next;
            if (prev.next != null)
                prev.next = lagInfo.next;
            return this;
        }
        /// <summary>set number of lag factors for this laginfo</summary>
        public bool SetNumLags(int num)
        {
            numLags = num;
            return true;
        }
        /// <summary>add the passed lagInfo to the end of this linked list</summary>
        public void Add(LagInfo lagInfo)
        {
            LagInfo prev;
            prev = this;
            while (prev.next != null)
                prev = prev.next;
            prev.next = lagInfo;
        }
    }
}
