using System;
using System.Drawing;

namespace Csu.Modsim.ModsimModel
{

    /// <summary>The Gnode class contains all information for graphically representing a node.</summary>
    public class Gnode
    {
        public Gnode()
        {
            visible = true;
            storageRightReservoir = false;
            labelVisible = true;
        }
        /// <summary>Creates a copy of this instance.</summary>
        public Gnode Copy()
        {
            Gnode retVal = (Gnode)this.MemberwiseClone();
            retVal.nodeLoc = new PointF(this.nodeLoc.X, this.nodeLoc.Y);
            retVal.nodeLabelLoc = new PointF(this.nodeLabelLoc.X, this.nodeLabelLoc.Y);
            return retVal;
        }
        
        public PointF nodeLoc = new PointF();
        public PointF nodeLabelLoc = new PointF();
        public bool visible;
        public bool storageRightReservoir;
        public bool labelVisible;
    }
}
