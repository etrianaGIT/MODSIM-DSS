using System;
using System.Data;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.ModsimIO
{
    public class Ver8Upgrade
    {
        public static void TSLoopForConversion(Model mi)
        {
            Node aNode = mi.firstNode;
            Link aLink = mi.firstLink;
            bool zero = true;
            DataRow row = null;
            DataRow row3 = null;
            DataRow[] row2 = null;
            string[] str = null;
            int i = 0;
            int j = 0;

            while (aNode != null)
            {
                if (aNode.nodeType == NodeType.Reservoir)
                {
                    if (aNode.m.adaTargetsM.units == null)
                    {
                        aNode.m.adaTargetsM.units = mi.FlowUnits;
                    }
                    if (aNode.m.adaTargetsM.dataTable.Rows.Count > 0)
                    {
                        // Check to see if the first row is all zeros
                        row = aNode.m.adaTargetsM.dataTable.Rows[0];
                        for (j = 1; j < aNode.m.adaTargetsM.dataTable.Columns.Count; j++)
                        {
                            if (Convert.ToDouble(row[j]) > 0)
                            {
                                zero = false;
                            }
                        }

                        // If the first row is not all zeros and the date in the first row is the data start date, update the dates to the end-of-period dates (instead of start-of-period dates)
                        //  AndAlso mi.timeStep.TSType <> ModsimTimeStepType.Daily Then
                        if (zero == false && Convert.ToDateTime(row[0]) >= mi.TimeStepManager.dataStartDate)
                        {
                            for (i = 0; i < aNode.m.adaTargetsM.dataTable.Rows.Count; i++)
                            {
                                row = aNode.m.adaTargetsM.dataTable.Rows[i];
                                row2 = mi.TimeStepManager.timeStepsList.Select("IniDate = #" + Convert.ToDateTime(row[0]).ToString() + "#");
                                if (row2.Length > 0)
                                {
                                    row3 = row2[0];
                                    row[0] = row3[1];
                                }
                            }
                        }
                    }
                    zero = true;
                    if (aNode.m.adaGeneratingHrsM.units != null)
                    {
                        str = aNode.m.adaGeneratingHrsM.units.Label.Split('/');
                        if (str.Length == 1)
                            aNode.m.adaGeneratingHrsM.units = aNode.m.adaGeneratingHrsM.units.Label + "/" + mi.timeStep.Label;
                    }
                    else
                    {
                        aNode.m.adaGeneratingHrsM.units = mi.TimeRateUnits;
                    }
                    if (aNode.m.adaEvaporationsM.units != null)
                    {
                        str = aNode.m.adaEvaporationsM.units.Label.Split(Convert.ToChar("/"));
                        if (str.Length == 1)
                            aNode.m.adaEvaporationsM.units = aNode.m.adaEvaporationsM.units.Label + "/" + mi.timeStep.Label;
                    }
                    else
                    {
                        aNode.m.adaEvaporationsM.units = mi.LengthRateUnits;
                    }
                }
                else if (aNode.nodeType == NodeType.NonStorage)
                {
                    if (aNode.m.adaInflowsM.units != null)
                    {
                        str = aNode.m.adaInflowsM.units.Label.Split(Convert.ToChar("/"));
                        if (str.Length == 1)
                            aNode.m.adaInflowsM.units = aNode.m.adaInflowsM.units.Label + "/" + mi.timeStep.Label;
                    }
                    else
                    {
                        aNode.m.adaInflowsM.units = mi.FlowUnits;
                    }
                }
                else if (aNode.nodeType == NodeType.Demand)
                {
                    if (aNode.m.adaDemandsM.units != null)
                    {
                        str = aNode.m.adaDemandsM.units.Label.Split(Convert.ToChar("/"));
                        if (str.Length == 1)
                            aNode.m.adaDemandsM.units = aNode.m.adaDemandsM.units.Label + "/" + mi.timeStep.Label;
                    }
                    else
                    {
                        aNode.m.adaDemandsM.units = mi.FlowUnits;
                    }
                }
                aNode = aNode.next;
            }

            while (aLink != null)
            {
                if ((aLink.m.maxVariable.units != null))
                {
                    str = aLink.m.maxVariable.units.Label.Split(Convert.ToChar("/"));
                    if (str.Length == 1)
                        aLink.m.maxVariable.units = aLink.m.maxVariable.units.Label + "/" + mi.timeStep.Label;
                }
                else
                {
                    aLink.m.maxVariable.units = mi.FlowUnits;
                }
                aLink = aLink.next;
            }
        }
    }
}
