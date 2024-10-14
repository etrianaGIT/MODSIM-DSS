using System;
using System.Drawing;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.ModsimIO
{
    public class Ver8_4Upgrade
    {
        // In version 8.4.0 we're using GoDiagram's Location property, try to use the old
        // Position and LabelOffset values to reposition the nodes and labels into Location
        public static void AdjustXYPointsToVer8_4(Model mi)
        {
            Node myNode = mi.firstNode;
            while (myNode != null)
            {
                float xfactor = 0;
                float yfactor = 0;
                double nodeSizeFactor = 1;

                if ((myNode.nodeType == NodeType.Demand || myNode.nodeType == NodeType.Sink) & mi.graphics.demandSize > 0)
                {
                    nodeSizeFactor = mi.graphics.demandSize / 100.0;
                }
                else if (myNode.nodeType == NodeType.NonStorage & mi.graphics.nonstorageSize > 0)
                {
                    nodeSizeFactor = mi.graphics.nonstorageSize / 100.0;
                }
                else if (myNode.nodeType == NodeType.Reservoir & mi.graphics.reservoirSize > 0)
                {
                    nodeSizeFactor = mi.graphics.reservoirSize / 100.0;
                }

                //BLounsbury: Adjustment for node position, I have no idea why this works
                if (myNode.graphics.nodeLabelLoc.X < 0)
                    xfactor = myNode.graphics.nodeLabelLoc.X;
                if (myNode.graphics.nodeLabelLoc.Y < 0)
                    yfactor = myNode.graphics.nodeLabelLoc.Y;

                if (myNode.graphics.nodeLabelLoc.IsEmpty)
                {
                    myNode.graphics.nodeLabelLoc = new PointF(0, 0);
                }

                myNode.graphics.nodeLoc.X = Convert.ToSingle(myNode.graphics.nodeLoc.X + ((17.0 / 2) * nodeSizeFactor) - xfactor);
                myNode.graphics.nodeLoc.Y = Convert.ToSingle(myNode.graphics.nodeLoc.Y + ((17.0 / 2) * nodeSizeFactor) - yfactor);
                myNode.graphics.nodeLabelLoc.X = myNode.graphics.nodeLoc.X;
                myNode.graphics.nodeLabelLoc.Y = Convert.ToSingle(myNode.graphics.nodeLoc.Y + ((17.0 / 2) * nodeSizeFactor));

                myNode = myNode.next;
            }
        }
    }
}
