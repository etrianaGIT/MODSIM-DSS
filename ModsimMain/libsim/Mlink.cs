
namespace Csu.Modsim.ModsimModel
{
    /// Maybe we should split this class and the MrlInfo class so we have a class for ownership links
    /// seems like we could have a pointer to this class here that is null by default and everything
    /// dealing with ownership links would be in one place.

    /// <summary>The Mlink class contains all the information for a Link that will be used in the model, and can be edited in the interface.</summary>
    public class Mlink
    {
        public Mlink()
        {
            maxVariable = new TimeSeries(TimeSeriesType.VariableCapacity);
            lagfactors = new double[DefineConstants.MAXLAG];
            waterRightsDate = TimeManager.missingDate;
            rentLimit = new long[7]; // wired to 7 levels??
            initStglft = new long[0];
            stgAmount = new long[0];
            long i;
            min = 0;
            cost = 0;
            loss_coef = 0.0;
            spyldc = 0.0;
            transc = 0.0;
            distc = 0.0;
            returnNode = null;
            //m->laglink          = 0;
            lnkallow = 0;
            accrualLink = null;
            waterRightsRank = 0;
            adminNumber = 0.0;
            lastFill = 0;
            exchangeLimitLinks = null;
            // hydTable default should be zero
            hydTable = 0;
            // lagfactors
            // overkill MAXLAG is 1200 always for all links
            for (i = 0; i < DefineConstants.MAXLAG; i++)
                lagfactors[i] = 0.0;
            // rentLimit is wired to size 7; should be dynamic and based on hydTable
            for (i = 0; i < rentLimit.Length; i++)
                rentLimit[i] = 0;
            //Initialize Observations timesereis
            adaMeasured = new TimeSeries(TimeSeriesType.Measured);
            //Display layer 
            lLayer = "Default";
            }
        /// <summary>TimeSeries of link maximum capacities read from XYFile</summary>
        /// <remarks>
        /// maxVariable is maximum capacity in the link that varies with time steps. 
        /// When the time series is empty, a constant maximum capacity is assumed = ModsimModel.defaultMaxCap '9999999
        /// When the time series contains only one value a constant maximum capacity is set in setnet to maxConstant
        /// </remarks>
        public TimeSeries maxVariable;
        /// <summary>Link maximum capacity used if capacity does not vary with time</summary>
        /// <remarks>
        /// MaxConstant is kept for model internal use only to avoid creating the maxVariable array for all links.
        /// maxConstant can be used in custom code to set the current iteration link capacity when the original link
        ///   has not variable capacity defined.
        /// maxConstant=0 == variable capacity link
        /// since this is not used in the xy file anywhere; it should be moved to mlInfo ??
        /// <remarks>
        public long maxConstant;
        /// <summary>Ownership link volume of contract</summary>
        public long capacityOwned; // ownership links volume of contract
        /// <summary>Ownership link accrual at the study  beginning</summary>
        public long initialStglft; // ownership link accrued at beginning of the study
        /*we should get rid of max, min, and oldmaxVarCapacity*/
        /*TimeSeries* mincapacity;								*/
        /// <summary>User specified link minimum capacity</summary>
        /// <remarks>Needless to say this can be VERY dangerous to set</remarks>
        public long min;
        /// <summary>User specified link relative priority</summary>
        /// <remarks>this is usually used for river reaches, links with flags to restrict them to certain iterations and other ways of tailoring the operation</remarks>
        public long cost;
        /// <summary>Ownership link - user specified order of use</summary>
        /// <remarks>Normally a contract with the best refill potential will be used first</remarks>
        public long relativeUseOrder;
        /// <summary>Channel loss coefficient</summary>
        /// <remarks>Should be set for river reach links only</remarks>
        public double loss_coef; // XLCF
        /// <summary>The saved channel loss coefficient, which is used to temporarily turn off routing.</summary>
        public double loss_coef_saved; 
        /// <summary>Specific yield for channel loss lag factors computation</summary>
        /// <remarks>If lag factores are model generated, specific yield, transmistivity, and distance from influence location are used with Glover equation to generate lag factors</remarks>
        public double spyldc;
        /// <summary>Transmistivity for channel loss lag factors computation</summary>
        public double transc;
        /// <summary>Distance to influence location for channel loss lag factors computation</summary>
        public double distc;
        /// <summary>Return location node for channel loss or routing</summary>
        /// <remarks>Optional for channel loss; required for routing links</remarks>
        public Node returnNode;
        //		long   laglink;/// boolean used internally NOT USED
        //RKL WARNING
        /*annual accumulated capacity this needs some work for time steps other than monthly*/
        /*long  lnkSeasStorageCap;*/
        /* accural links sum of ownership contracts*/
        /*we should move lnkSeasStorageCap from MrlInfo to Mlink and get rid of the dual meaning
        //for lnkallow; sanity checking so both cannot be set
        // we should maybe consider adding linkType to the Link class that would
        // disallow many inputs depending on the link type
        //DateTime SeasCapDate; // we should have a date for resetting seasonal capacity for each link
        //this would mean getting rid of the model global accrual dates*/
        /// <summary>"Seasonal" (annual) accumulated volume capacity</summary>
        /// <remarks> annual volume of flow limit</remarks>
        public long lnkallow;
        /// <summary>Channel loss return or routing lag factors</summary>
        /// <remarks> User specified lag factors (coefficients) if not model generated</remarks>
        public double[] lagfactors;
        /// <summary>This link's accrual link; defines this link as a storage ownership contract</summary>
        public Link accrualLink;
        /// <summary>This link's priority date</summary>
        /// <remarks> For accrual links and natural flow links; interface uses waterRightsDate to rank links in order and assign relative priority (cost) to this link</remarks>
        public System.DateTime waterRightsDate;
        public long waterRightsRank; // WARNING NOT USED should be removed
        public double adminNumber; // WARNING NOT USED should be removed
        public long touchedSorted; // WARNING USED IN SETNET ONLY should be removed
        /// <summary>For ownership link only, rent limit ARRAY IS WIRED TO SEVEN VALES</summary>
        /// <remarks>Rental Pool allows an annual temporary transfer of storage ownership of accrued storage water</remarks>
        public long[] rentLimit;
        /// <summary>For ownership link only. Flag to indicate if rental transfer is subject to "Last Fill" rule</summary>
        /// <remarks>If true; space contributed and rented must accrue under "lastFillLink" instead of the original accrualLink until it fills </remarks>
        public long lastFill;
        /// <summary>Ownership link - Flag is true if this link is upstream of reservoir where storage is contracted</summary>
        /// <remarks>Used in an effort to speed up convergence if this link is being shorted</remarks>
        public bool upsOwner;
        /// <summary>Option to set upper bound of this link equal to the flow in the exchangeLimitLink</summary>
        /// <remarks> should not be used on ownership links</remarks>
        public Link exchangeLimitLinks;
        //WARNING sanity checking needed; we should probably not allow input of things like hiVariable, max, min, etc
        /// <summary>True if user has selected this link for output</summary>
        /// <remarks> if no links / nodes are explicitly selected, ALL are output</remarks>
        public bool selected;
        /// <summary>For ownership link only. Numeric ID of the hydrologic table this link uses to select rent limit for the current time step</summary>
        public int hydTable;
        /// <summary>If true this link opens only in the second storage step iterations</summary>
        /// <remarks>Second storage step is invoked if there are flo thru demand with ownership links</remarks>
        public long flagSecondStgStepOnly;
        /// <summary>If true, this links opens only in "storage" step (odd) iterations</summary>
        public long flagStorageStepOnly;
        /// <summary>if true, this link's upper bounds is constrained in the storage step iteration to no more than it flowed in the last natural flow iteration</summary>
        public long flagSTGeqNF;
        /// <summary>For ownership links only, constrain the upper bounds of this link to the "residual" capacity of the linkConstraintUPS link</summary>
        /// <remarks> Residual capacity is the linkConstraintUPS link's maximum capacity minus the flow the linkConstraintUPS recieved in the last natural flow step </remarks>
        public Link linkConstraintUPS; // The upstream constraint btwn us and our reservoir
        /// <summary>Essentially has the same effect as linkConstraintUPS</summary>
        public Link linkConstraintDWS;
        /// <summary>For ownership links only, the linkChannelLoss link's channel loss coefficient is used to inflate the debit against this links use of storage water</summary>
        /// <remarks>If linkChannelLoss has a channel loss coefficient of .05, then any flow through this link results in a debit of 1.05 times the flow from the storage accrued</remarks>
        public Link linkChannelLoss;
        /// <summary>For accrual link only. The number of group ownerships in this priority</summary>
        /// <remarks>A group ownership is where two or more ownership links share the same physical SPACE not just the same priority.</remarks>
        public int numberOfGroups;
        /// <summary>For accrual link only. Array of initial accrual for each group ownership under this accrual link</summary>
        public long[] initStglft;
        /// <summary>For accrual link only. Array of volume capacity for each group ownership under this accrual link</summary>
        public long[] stgAmount;
        /// <summary>For an ownership link, if this link is a group ownership, then this is the group number under the accrual Link</summary>
        /// <remarks>MUST be zero if this link's accrualLink does not have any groups defined</remarks>
        public int groupNumber;
        /// <summary>Observations timeSeries of link flow</summary>
        /// <remarks>
        /// This is a timeseries the user can use in calibration to compare resutls with a measured value
        /// it is not used in the simulation but it's transfered to the output for display.
        /// </remarks>
        public TimeSeries adaMeasured;
        /// <summary>
        /// Optional text to group links for display purposes.
        /// All links are created with the "Defaul" layer. 
        /// </summary>
        public string lLayer;
    }
}
