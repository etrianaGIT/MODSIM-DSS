namespace Csu.Modsim.ModsimModel
{
    public class diststr
    {
        public long constraintLo;
        public long constraintHi;
        public long extra;
        public double biasFrac; // Original bias for fractional amounts
        public long returnValWhole;
        public double returnValFrac;
        public long remove_from_consideration; // Used internally by the routines
        public Node referencePtrN;
        public Link referencePtr;
        public long referencePtrType;
        public double lossFactorCharge; // Not implicit loss
        public double lossFactorCredit; // Not implicit loss
        public diststr next;
    }

}
