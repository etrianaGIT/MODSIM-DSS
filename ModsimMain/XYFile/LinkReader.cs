using System;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.ModsimIO
{
    public class LinkReader
    {
        // reads through xy file and creates all links
        public static void CreateLinks(Model mi, TextFile file)
        {
            XYFileReader.LinkArray = new Link[XYFileReader.LinkCount + 1];
            string[] MainCmds = null;
            MainCmds = XYCommands.MainCommands;
            int lineCounter = 0;
            int startIndex = 0;
            // line where string 'link' is found
            int endIndex = 0;
            // last line for this link 

            while ((lineCounter < file.Count))
            {
                startIndex = file.Find("link", lineCounter, file.Count - 1);
                endIndex = file.FindAny(MainCmds, startIndex + 1);
                if (startIndex == -1)
                    return;

                int fromNodeNum = XYFileReader.ReadInteger("fromnum", -1, file, startIndex, endIndex);
                int toNodeNum = XYFileReader.ReadInteger("tonum", -1, file, startIndex, endIndex);

                Node fromNode = XYFileReader.NodeArray[fromNodeNum];
                //mi.FindNode(fromNodeNum)
                Node toNode = XYFileReader.NodeArray[toNodeNum];
                //mi.FindNode(toNodeNum)
                if (fromNode == null | toNode == null)
                {
                    throw new Exception("Error:  Link is not connected to valid nodes.  xy file line number =" + startIndex);
                }

                Link link = mi.AddNewLink(true);
                link.@from = fromNode;
                link.to = toNode;
                link.name = XYFileReader.ReadString("lname", "", file, startIndex, endIndex);
                string uidString = XYFileReader.ReadString("luid", "", file, startIndex, endIndex);
                //for compatibility with older files the new link has a default guid asssigned to be saved with this version.
                if (uidString !="")
                    link.uid = new Guid(uidString);
                link.description = XYFileReader.ReadString("ldescription", "", file, startIndex, endIndex);
                link.number = XYFileReader.ReadInteger("lnum", -1, file, startIndex, endIndex);
                if (link.number == -1)
                {
                    throw new Exception("Error: link is missing link number : xy file line number " + startIndex);
                }
                XYFileReader.LinkArray[link.number] = link;
                if (endIndex < 0)
                    return;
                lineCounter = endIndex;
            }

        }

        // ReadLinks reads the details for all Links.  
        // you should have called CreateLinks before calling ReadLinks
        public static void ReadLinks(Model mi, TextFile file)
        {
            string[] MainCmds = null;
            MainCmds = XYCommands.MainCommands;
            int lineCounter = 0;
            int startIndex = 0;
            // line where string 'link' is found
            int endIndex = 0;
            // last line for this link 
            while ((lineCounter < file.Count))
            {
                startIndex = file.Find("link", lineCounter, file.Count - 1);
                endIndex = file.FindAny(MainCmds, startIndex + 1);
                if (startIndex == -1)
                    break; // TODO: might not be correct. Was : Exit While
                int linkNumber = XYFileReader.ReadInteger("lnum", -1, file, startIndex, endIndex);
                if ((linkNumber == -1))
                {
                    throw new Exception("Error: link number not found line number " + startIndex);
                }
                Link link = XYFileReader.LinkArray[linkNumber];
                if (link == null)
                {
                    throw new Exception("Error: link was not found:  link num = " + linkNumber);
                }
                ReadLinkDetails(mi, link, file, startIndex, endIndex);
                if (endIndex < 0)
                    break; // TODO: might not be correct. Was : Exit While
                lineCounter = endIndex;
            }
            if (mi.inputVersion.Type == InputVersionType.V056)
            {
                int i = 0;
                for (i = 1; i < XYFileReader.LinkArray.Length; i++)
                {
                    Link link = XYFileReader.LinkArray[i];
                    if (!link.IsAccrualLink)
                    {
                        CheckAccrualLinkStatus(link);
                    }
                }
            }
        }
        private static void CheckAccrualLinkStatus(Link link)
        {
            int i = 0;
            for (i = 1; i < XYFileReader.LinkArray.Length; i++)
            {
                if (!(XYFileReader.LinkArray[i].number == link.number))
                {
                    if ((XYFileReader.LinkArray[i].m.accrualLink != null))
                    {
                        if (XYFileReader.LinkArray[i].m.accrualLink.number == link.number)
                        {
                            link.IsAccrualLink = true;
                            return;
                        }
                    }
                }
            }
        }

        //ReadNode function reads link details from the xy file
        private static void ReadLinkDetails(Model mi, Link link, TextFile file, int startIndex, int endIndex)
        {
            int tmpLinkNumber = 0;
            link.m = new Mlink();
            link.m.min = XYFileReader.ReadLong("lmin", 0, file, startIndex, endIndex);
            link.m.cost = XYFileReader.ReadLong("lcost", 0, file, startIndex, endIndex);
            link.graphics = new Glink();
            double[] coords = null;
            coords = XYFileReader.ReadIndexedFloatList("lcoords", 0, file, startIndex, endIndex);
            //If (coords.Length > 0) Then
            int len = Convert.ToInt32(coords.Length / 2);
            System.Drawing.PointF[] points = new System.Drawing.PointF[len];
            // copy from one dimensional alternating x,y to array of PointF
            for (int i = 0; i < points.Length; i++)
            {
                points[i].X = Convert.ToSingle(coords[i * 2]);
                points[i].Y = Convert.ToSingle(coords[i * 2 + 1]);
            }
            link.graphics.points = points;
            int tempbool = XYFileReader.ReadInteger("IsVisible", 1, file, startIndex, endIndex);
            if (tempbool == 0)
                link.graphics.visible = false;
            link.m.selected = Convert.ToBoolean(XYFileReader.ReadInteger("select", 1, file, startIndex, endIndex));
            if (mi.inputVersion.Type == InputVersionType.V056)
            {
                long[] tempMaxVarCapacity = null;
                tempMaxVarCapacity = XYFileReader.ReadIndexedIntegerList("cmaxv", 12, 0, file, startIndex, endIndex);
                Ver7Upgrade.UpgradeTomaxCapacity(mi, link, tempMaxVarCapacity);
            }
            else
            {
                link.m.maxVariable = XYFileReader.ReadTimeSeries(mi, "maxCap", file, startIndex, endIndex);
            }
            //Reading link measured flows 
            link.m.adaMeasured = XYFileReader.ReadTimeSeries(mi, "measured", file, startIndex, endIndex);

            //Reading link layer (Ver >=8.6.1)
            link.m.lLayer = XYFileReader.ReadString("lLayer", "Default", file, startIndex, endIndex);

            long tempmax = XYFileReader.ReadLong("lmax", DefineConstants.NODATAVALUE, file, startIndex, endIndex);
            //99999999, file, startIndex, endIndex)
            //This fixes a bug in the old xyfile written - there are big numbers in the saved xyfile.
            // 99999999 Then
            if (tempmax != DefineConstants.NODATAVALUE)
            {
                if (tempmax > DefineConstants.NODATAVALUE)
                    tempmax = DefineConstants.NODATAVALUE;
                // 2099999999 Then tempmax = 2099999999
                // if there is a data in the maxVariable link max will not be used.
                if (link.m.maxVariable.getSize() == 0)
                {
                    link.m.maxVariable.setDate(0, mi.TimeStepManager.Index2Date(0, TypeIndexes.DataIndex));
                    link.m.maxVariable.setDataL(0, tempmax);
                }
            }
            // if lparent is not nothing then this is an ownership link
            tmpLinkNumber = XYFileReader.ReadInteger("lparent", -1, file, startIndex, endIndex);
            if (tmpLinkNumber != -1)
            {
                if (mi.inputVersion.Type == InputVersionType.V056 & tmpLinkNumber == link.number)
                {
                    if (link.to.nodeType == NodeType.Reservoir)
                    {
                        if (link.to.m.lastFillLink == null)
                        {
                            link.to.m.lastFillLink = link;
                        }
                        else
                        {
                            if ((!object.ReferenceEquals(link.to.m.lastFillLink, link)))
                                mi.FireOnError("More than one last fill link to reservoir " + link.to.name + ".");
                        }
                        link.m.accrualLink = null;
                    }
                    if (link.@from.nodeType == NodeType.Reservoir)
                    {
                        if (link.@from.m.resOutLink == null)
                        {
                            link.@from.m.resOutLink = link;
                        }
                        else
                        {
                            if ((!object.ReferenceEquals(link.@from.m.resOutLink, link)))
                                mi.FireOnError("More than one outflow link from reservoir " + link.@from.name + ".");
                        }
                        link.m.accrualLink = null;
                    }
                }
                else
                {
                    if (tmpLinkNumber == link.number)
                    {
                        throw new Exception("Parent link is for ownership links ONLY \\n" + "link " + link.number);
                    }
                    link.m.accrualLink = XYFileReader.LinkArray[tmpLinkNumber];
                }
            }
            if (link.m.accrualLink != null)
            {
                // read storage ownership link data
                if (mi.inputVersion.Type == InputVersionType.V056)
                {
                    link.m.initialStglft = link.m.min;
                    link.m.min = 0;
                    if (link.m.maxVariable.getSize() > 1)
                    {
                        throw new Exception("Ownership links cannot have variable capacity");
                    }
                    link.m.capacityOwned = link.m.maxVariable.getDataL(0);
                    link.m.maxVariable.setDataL(0, 0);
                }
                else
                {
                    link.m.capacityOwned = XYFileReader.ReadLong("capown", 0, file, startIndex, endIndex);
                    link.m.initialStglft = XYFileReader.ReadLong("stglft", 0, file, startIndex, endIndex);
                }
                link.m.hydTable = XYFileReader.ReadInteger("lhydtable", 0, file, startIndex, endIndex);
                if (mi.inputVersion.Type == InputVersionType.V056)
                {
                    if (mi.runType == ModsimRunType.Explicit_Targets)
                    {
                        link.m.hydTable = 0;
                    }
                    else
                    {
                        if (link.m.hydTable == 0)
                        {
                            link.m.hydTable = 1;
                        }
                    }
                }
                link.m.relativeUseOrder = XYFileReader.ReadLong("relativeUseOrder", 0, file, startIndex, endIndex);
                //AndAlso link.m.accrualLink IsNot link AndAlso link.to.nodeType = NodeType.Demand Then
                if (mi.inputVersion.Type <= InputVersionType.V8_3_1)
                {
                    link.m.relativeUseOrder = link.m.cost;
                }
                link.m.rentLimit = XYFileReader.ReadIndexedIntegerList("lrentlim", 7, 0, file, startIndex, endIndex);
                link.m.lastFill = XYFileReader.ReadLong("llastfill", 0, file, startIndex, endIndex);
                link.m.groupNumber = XYFileReader.ReadInteger("groupnumber", 0, file, startIndex, endIndex);
                link.m.upsOwner = Convert.ToBoolean(XYFileReader.ReadInteger("upsowner", 0, file, startIndex, endIndex));
                tmpLinkNumber = XYFileReader.ReadInteger("llinkconstr", -1, file, startIndex, endIndex);
                if (tmpLinkNumber != -1)
                {
                    link.m.linkConstraintUPS = XYFileReader.LinkArray[tmpLinkNumber];
                    // current storage limit link
                }
                tmpLinkNumber = XYFileReader.ReadInteger("llinkconstD", -1, file, startIndex, endIndex);
                if (tmpLinkNumber != -1)
                {
                    link.m.linkConstraintDWS = XYFileReader.LinkArray[tmpLinkNumber];
                    // current storage limit link
                }
                tmpLinkNumber = XYFileReader.ReadInteger("llinkchloss", -1, file, startIndex, endIndex);
                if (tmpLinkNumber != -1)
                {
                    link.m.linkChannelLoss = XYFileReader.LinkArray[tmpLinkNumber];
                    // current channel loss link
                }
            }
            // this stuff cannot apply to ownership links
            if (link.m.accrualLink == null)
            {
                link.m.loss_coef = XYFileReader.ReadFloat("xlcf", 0, file, startIndex, endIndex);
                link.m.spyldc = XYFileReader.ReadFloat("spyldc", 0, file, startIndex, endIndex);
                link.m.transc = XYFileReader.ReadFloat("transc", 0, file, startIndex, endIndex);
                link.m.distc = XYFileReader.ReadFloat("distc", 0, file, startIndex, endIndex);
                int tmpNodeNumber = XYFileReader.ReadInteger("idstrc", -1, file, startIndex, endIndex);
                if (tmpNodeNumber != -1)
                {
                    link.m.returnNode = XYFileReader.NodeArray[tmpNodeNumber];
                }
                link.m.lnkallow = XYFileReader.ReadLong("lnkallow", 0, file, startIndex, endIndex);
                link.m.lagfactors = XYFileReader.ReadIndexedFloatList("llagfact", 0, file, startIndex, endIndex);
                //TODO: Make sense to have the size variable but code need to be fixed.
                //If link.m.lagfactors.Length < mi.nlags Then
                Array.Resize(ref link.m.lagfactors, 1201);
                // mi.nlags) ' Note: upper bounds is +1
                //End If
                link.m.waterRightsDate = TimeManager.missingDate;
                // default
                if (mi.inputVersion.Type == InputVersionType.V056)
                {
                    long[] water_rd = XYFileReader.ReadIndexedIntegerList("water_rd", 0, file, startIndex, endIndex);
                    if (water_rd.Length > 0)
                    {
                        if (water_rd.Length != 3)
                        {
                            mi.FireOnError("Error: the water rights date is not complete in xy file: link number " + link.number + " at line number " + startIndex + 1);
                        }
                        else
                        {
                            // convert to date.
                            try
                            {
                                link.m.waterRightsDate = new DateTime(Convert.ToInt32(water_rd[2]), Convert.ToInt32(water_rd[0]), Convert.ToInt32(water_rd[1]));
                            }
                            catch
                            {
                                mi.FireOnError("Error parsing water rights date is not complete in xy file: link number " + link.number + " at line number " + startIndex + 1);
                            }
                        }
                    }

                    // version 8
                }
                else
                {
                    link.m.waterRightsDate = XYFileReader.ReadDateTime("WaterRightDate", file, TimeManager.missingDate, startIndex, endIndex);
                }
                link.m.flagStorageStepOnly = XYFileReader.ReadLong("lstg_only", 0, file, startIndex, endIndex);
                //tmpLinkNumber = XYFileReader.ReadInteger("llinkconstr", -1, file, startIndex, endIndex)
                //If tmpLinkNumber <> -1 Then
                //    link.m.linkConstraintUPS = XYFileReader.LinkArray(tmpLinkNumber)  ' current storage limit link
                //End If
                //tmpLinkNumber = XYFileReader.ReadInteger("llinkchloss", -1, file, startIndex, endIndex)
                //If tmpLinkNumber <> -1 Then
                //    link.m.linkChannelLoss = XYFileReader.LinkArray(tmpLinkNumber)  ' current channel loss link
                //End If
                link.m.flagSTGeqNF = XYFileReader.ReadLong("lstg_eq_nf", 0, file, startIndex, endIndex);
                link.m.numberOfGroups = XYFileReader.ReadInteger("numgroups", 0, file, startIndex, endIndex);
                long[] tmpIntList = XYFileReader.ReadIndexedIntegerList("initstglft", 0, file, startIndex, endIndex);
                Array.Resize(ref tmpIntList, link.m.numberOfGroups);
                link.m.initStglft = tmpIntList;
                tmpIntList = XYFileReader.ReadIndexedIntegerList("stgamount", 0, file, startIndex, endIndex);
                Array.Resize(ref tmpIntList, link.m.numberOfGroups);
                link.m.stgAmount = tmpIntList;
                link.m.flagSecondStgStepOnly = XYFileReader.ReadLong("l2ndstgonly", 0, file, startIndex, endIndex);
                tmpLinkNumber = XYFileReader.ReadInteger("lplstrm", -1, file, startIndex, endIndex);
                if (tmpLinkNumber != -1)
                {
                    link.m.exchangeLimitLinks = XYFileReader.LinkArray[tmpLinkNumber];
                }
            }
            // end of stuff that does not pertain to ownership links

            // command accrualLink is in version8 xy files; need the CheckAccrualLinkStatus function for old xy files
            link.IsAccrualLink = XYFileReader.ReadBoolean("accrualLink", false, file, startIndex, endIndex);

        }
    }
}
