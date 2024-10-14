using System;
using System.IO;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.ModsimIO
{
    public class LinkWriter
    {
        // Writes Lists to xyfile
        public static void WriteLinks(Model mi, StreamWriter xyOutStream)
        {
            int sz = mi.LinkCount;
            int i = 0;
            int linkNumber = 0;
            Link link = null;
            for (linkNumber = 1; linkNumber <= sz; linkNumber++)
            {
                link = mi.FindLink(linkNumber);
                xyOutStream.WriteLine("link");
                xyOutStream.WriteLine("lname " + link.name);
                xyOutStream.WriteLine("luid " + link.uid.ToString());
                xyOutStream.WriteLine("ldescription " + link.description);
                xyOutStream.WriteLine("lnum " + link.number);
                xyOutStream.WriteLine("fromnum " + link.@from.number);
                xyOutStream.WriteLine("tonum " + link.to.number);
                XYFileWriter.WriteInteger("lmin", xyOutStream, link.m.min, 0);
                // set the link cost equal to the relative use order for owner links
                //AndAlso link.m.accrualLink IsNot link AndAlso link.to.nodeType = NodeType.Demand Then
                if (link.m.accrualLink != null)
                {
                    link.m.cost = link.m.relativeUseOrder;
                }
                XYFileWriter.WriteInteger("lcost", xyOutStream, link.m.cost, 0);

                // this should be changed for ownership links
                // we should write ownership amount to xy file under new command
                if (mi.timeseriesInfo.xyFileTimeSeries && link.m.maxVariable.getSize() > 0)
                {
                    if (mi.timeseriesInfo.xyFileTimeSeries) XYFileWriter.WriteTimeSeries("maxCap", link.m.maxVariable, xyOutStream);
                }
                // Write measured time series
                if (mi.timeseriesInfo.xyFileTimeSeries && link.m.adaMeasured.getSize() > 0)
                {
                    if (mi.timeseriesInfo.xyFileTimeSeries) XYFileWriter.WriteTimeSeries("measured", link.m.adaMeasured, xyOutStream); 
                }

                //Reading link layer (Ver >=8.6.1)
                if (link.m.lLayer != "Default")
                {
                    xyOutStream.WriteLine("lLayer " + link.m.lLayer);
                }

                // write graphics.
                if (link.graphics != null)
                {
                    if (link.graphics.points != null)
                    {
                        if (link.graphics.points.Length > 0)
                        {
                            //save xy coordinates for a link
                            xyOutStream.WriteLine("lcoords");
                            for (i = 0; i < link.graphics.points.Length; i++)
                            {
                                xyOutStream.WriteLine(i * 2 + " " + link.graphics.points[i].X.ToString("F4"));
                                xyOutStream.WriteLine(i * 2 + 1 + " " + link.graphics.points[i].Y.ToString("F4"));
                            }
                        }
                    }
                    if (!link.graphics.visible)
                    {
                        xyOutStream.WriteLine("IsVisible " + Convert.ToInt32(link.graphics.visible).ToString());
                    }
                }
                if (!link.m.selected)
                {
                    XYFileWriter.WriteInteger("select", xyOutStream, 0, 1);
                }
                // data for ownership links only
                if (link.m.accrualLink != null)
                {
                    if (link.number == link.m.accrualLink.number)
                    {
                        throw new Exception("Parent link cannot equal itself");
                    }
                    if (link.m.lnkallow > 0)
                    {
                        throw new Exception(" Ownership links cannot have lnkallow set");
                    }
                    XYFileWriter.WriteInteger("capown", xyOutStream, link.m.capacityOwned, 0);
                    XYFileWriter.WriteInteger("stglft", xyOutStream, link.m.initialStglft, 0);
                    xyOutStream.WriteLine("lparent " + link.m.accrualLink.number);
                    XYFileWriter.WriteInteger("relativeUseOrder", xyOutStream, link.m.relativeUseOrder, 0);
                    XYFileWriter.WriteInteger("lhydtable", xyOutStream, link.m.hydTable, 0);
                    XYFileWriter.WriteIndexedIntList("lrentlim", link.m.rentLimit, 0, xyOutStream);
                    // lastfill flag on ownership links only
                    // we need to set the resLastFillLink in the Node reader/writer
                    XYFileWriter.WriteInteger("llastfill", xyOutStream, link.m.lastFill, 0);
                    XYFileWriter.WriteInteger("groupnumber", xyOutStream, link.m.groupNumber, 0);
                    if (link.m.upsOwner)
                    {
                        XYFileWriter.WriteInteger("upsowner", xyOutStream, 1, 0);
                    }
                    if (link.m.linkConstraintUPS != null)
                    {
                        XYFileWriter.WriteInteger("llinkconstr", xyOutStream, link.m.linkConstraintUPS.number, DefineConstants.NODATAVALUE);
                    }
                    if (link.m.linkConstraintDWS != null)
                    {
                        XYFileWriter.WriteInteger("llinkconstD", xyOutStream, link.m.linkConstraintDWS.number, 0);
                    }
                    if (link.m.linkChannelLoss != null)
                    {
                        XYFileWriter.WriteInteger("llinkchloss", xyOutStream, link.m.linkChannelLoss.number, DefineConstants.NODATAVALUE);
                    }
                }
                else
                {
                    // any standard link should be able of have seasonal capacity
                    XYFileWriter.WriteInteger("lnkallow", xyOutStream, link.m.lnkallow, 0);
                    // the stuff below can not pertain to ownership links
                    if (link.IsAccrualLink || link.IsNaturalFlowLink() || link.IsLastFillLink())
                    {
                        // should have seasonal capacity for accrual links in xy file
                        // separate command and meaning than lnkallow
                        if (link.m.waterRightsDate != TimeManager.missingDate)
                        {
                            xyOutStream.WriteLine("WaterRightDate " + link.m.waterRightsDate.ToString(TimeManager.DateFormat));
                        }
                        if (link.IsAccrualLink)
                        {
                            XYFileWriter.WriteInteger("numgroups", xyOutStream, link.m.numberOfGroups, 0);
                            if (link.m.initStglft.Length > 0)
                            {
                                XYFileWriter.WriteIndexedIntList("initstglft", link.m.initStglft, 0, xyOutStream);
                            }
                            if (link.m.stgAmount.Length > 0)
                            {
                                XYFileWriter.WriteIndexedIntList("stgamount", link.m.stgAmount, 0, xyOutStream);
                            }
                            // we should not need this command; set it in setnet
                            XYFileWriter.WriteBoolean("accrualLink", xyOutStream, link.IsAccrualLink, false);
                        }
                    }
                    else
                    {
                        // the rest of this cannot be on ownership, accrual, or natural flow links
                        if (link.m.exchangeLimitLinks != null)
                        {
                            xyOutStream.WriteLine("lplstrm " + link.m.exchangeLimitLinks.number);
                        }
                        XYFileWriter.WriteFloat("xlcf", xyOutStream, link.m.loss_coef, 0);
                        XYFileWriter.WriteFloat("spyldc", xyOutStream, link.m.spyldc, 0);
                        // specific yield
                        XYFileWriter.WriteFloat("transc", xyOutStream, link.m.transc, 0);
                        // transmissivity
                        XYFileWriter.WriteFloat("distc", xyOutStream, link.m.distc, 0);
                        if ((link.m.returnNode != null))
                        {
                            xyOutStream.WriteLine("idstrc " + link.m.returnNode.number);
                        }
                        XYFileWriter.WriteIndexedFloatList("llagfact", link.m.lagfactors, 0, xyOutStream);
                        XYFileWriter.WriteInteger("lstg_only", xyOutStream, link.m.flagStorageStepOnly, 0);
                        //If Not link.m.linkChannelLoss Is Nothing Then
                        //    XYFileWriter.WriteInteger("llinkchloss", xyOutStream, link.m.linkChannelLoss.number, -999)
                        //End If
                        XYFileWriter.WriteInteger("lstg_eq_nf", xyOutStream, link.m.flagSTGeqNF, 0);
                        XYFileWriter.WriteInteger("l2ndstgonly", xyOutStream, link.m.flagSecondStgStepOnly, 0);
                        // we should not need accrualLink in the xy file
                        XYFileWriter.WriteBoolean("accrualLink", xyOutStream, link.IsAccrualLink, false);
                    }
                }
            }
        }
    }
}
