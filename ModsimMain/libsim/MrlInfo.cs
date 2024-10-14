namespace Csu.Modsim.ModsimModel
{

    /// <summary>Model data structues for "real" links; mostly output and storage allocation</summary>
    public class MrlInfo
    {
        public MrlInfo()
        {
            link_flow = new long[12]; // output array link flow
            link_closs = new long[12]; // output array link channel loss
            link_store = new long[12]; // output array link storage left
            link_accrual = new long[12];
            lnktot = 0; // seasonal accumulated flow
            linkPrevflow = new long[DefineConstants.MAXLAG];
            natFlow = new long[12]; // 12 months of Natural flow values for output
            hydStateIndex = 0;
            current_last_evap = 0;
            currentLastAccrual = 0;
            contribRent = 0;
            contribLast = 0;
            contribLastThisSeason = 0;
            lastaval = 0;
            rentlast = 0;
            prevstglft = 0;
        }
        /// <summary>Output array of link flow</summary>
        public long[] link_flow;
        /// <summary>Output array of flows in the last natural flow iteration</summary>
        public long[] natFlow;
        /// <summary>Output array of channel loss computed based on link flow</summary>
        public long[] link_closs;
        /// <summary>Output array of the stglft for a storage contract link</summary>
        public long[] link_store;
        /// <summary>Output array of the ownaccrual for a storage contract link</summary>
        public long[] link_accrual;
        /// <summary>Channel loss computed based on flow from the last solver solution</summary>
        public long closs;
        /// <summary>Channel loss from the previous (storage step) iteration</summary>
        public long closs0;
        /// <summary>For accrual links only, the total volume of this priority</summary>
        /// <remarks> Acts as the annual limit of accrual for the storage priority</remarks>
        public long lnkSeasStorageCap;
        /// <summary>For ownership links only, difference between hi and flow for a iteration solution</summary>
        /// <remarks> This is a shortage due to network constraints</remarks>
        public long accumsht;
        /// <summary>Ownership links - amount for LastFill accrual distributed to this link. Accrual links - sum of ownership links' LastFill accrual this iteration</summary>
        /// <remarks> Flow through the last fill link in the natural flow iteration is distributed to last fill rent pool contributors in proportion to their contribLast</remarks>
        public long currentLastAccrual;
        /// <summary>Accrual and ownerlinks that contribute to rent pool- Computed evap charged to rent pool subscriber</summary>
        public long current_rent_evap;
        /// <summary>Accrual and ownerlinks than contribute to rent pool- Computed evap charged to rent pool subscriber subject to last fill rule</summary>
        public long current_last_evap;
        /// <summary>Annual accumulated flow</summary>
        public long lnktot;
        /// <summary>Ownership link - computed evap charged to this ownership link Accural link - sum of current_evap for ownerhips links associated with this accrual link</summary>
        public long current_evap;
        /// <summary>Ownership link - accrual distributed to this ownership link. Accural link - natural flow iteration flow in the accrual link</summary>
        public long current_accrual;
        /// <summary>Ownership link - accumulated accrual for this contract based on last solver solution</summary>
        public long own_accrual;
        /// <summary>Amount of storage left in this contract account at the beginning of the time step</summary>
        public long prevstglft;
        /// <summary>Ownership link - accumulated accrual at the beginning of the time step<summary>
        public long prevownacrul;
        /// <summary>Ownership link - computed storage left in the contract account</summary>
        public long stglft;
        //WARNING This is set in setnet from m.capacityOwned and is used in model code in place of m.capacityOwned
        /// <summary>Model variable representing the amount of a ownership link contract</summary>
        public long cap_own;
        /// <summary>Accrual link - sum of ownership links accural at the beginning of the time step</summary>
        public long sumPrevOwnAcrul;
        //WARNING  NOT USED
        //long  stglft0;
        /// <summary>Flag representing link's rent pool status - set in setnet
        /// = 1 for an ownership link that contributes to rent pool
        /// = 0 for an ownership link that does not participate in rent pool
        /// = -1 for rental subsciber link</summary>
        public long irent;
        /// <summary>Array of future lagged flows asscociated with routing or channel loss</summary>
        public long[] linkPrevflow;
        /// <summary>Represents this links proportion of an undistributed amount because of integer math</summary>
        public double biasFrac;
        /// <summary>Ownership link - amount of contribution to last fill rent pool
        ///  Rental link amount (negative) subscribed from last fill rent pool</summary>
        public long contribLast;
        /// <summary>Ownership link - amount contributed to last fill rent pool this year no meaning for rent links</summary>
        public long contribLastThisSeason;
        /// <summary>Ownership link - amount of contribution to rent pool not subject to last fill Rental link amount (negative) of subscription</summary>
        public long contribRent;
        /*WARNING rentaval, lastaval, rentdem, rentlast are all should be removed; they are used locally
        // in RentPool and do not carry (and should not) data outside the routine */
        public long rentaval;
        public long lastaval;
        public long rentdem;
        public long rentlast;
        /// <summary>ID of the artificial link represeting a group ownership under an accrual link</summary>
        public long groupID;
        /// <summary>Saves the link cost between NF & STG iterations.</summary>
        public long csave;
        /// <summary>Saves the link lower bound between NF & STG iterations.</summary>
        public long losave;
        /// <summary>Saves the hi bounds between iterations</summary>
        public long hibnd;
        /// <summary>Used to set the hi bounds based on the latest iteration for watch logic</summary>
        public long watchNew;
        /// <summary>This is on channel loss links only which is an artificial link (confused yet ;) set to the amount of ownership links (that have this channel loss link) channel loss</summary>
        public long attributeLossToStg;
        /// <summary>Right now this is set to 99999999 and never updated; the intent is to be the amount that was actually delivered and use it to constrain hi after a number of iterations</summary>
        public long accumshtMaxDeliv;
        /// <summary>Hydrologic state index; used to set rent the amount of rent pool contribution/subscription</summary>
        public int hydStateIndex;
        /// <summary>Array of ownership links flow needed to satisfy future time demand?</summary>
        public double[] storageWaterCalcFutureTSteps; //keep track of the water releases from the reservoirs in previous time steps to meet the demand shortage.  Only use during the Storage allocation with back-routing.
    }
}
