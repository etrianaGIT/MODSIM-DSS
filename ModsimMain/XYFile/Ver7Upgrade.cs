using System;
using System.Data;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.ModsimIO
{
    public class Ver7Upgrade
    {
        public static void TruncateTimeManager(Model mi)
        {
            if (mi.TimeStepManager.startingDate > mi.TimeStepManager.dataStartDate)
            {
                mi.FireOnError(" TimeStepManager.dataStartDate is being set to TimeStepManager.startingDate. If you do not wish this to happen, please set startingDate to dataStartDate to preserve the data, otherwise if you continue and write out an xy file the data before startingDate will be lost.");
                mi.TimeStepManager.dataStartDate = mi.TimeStepManager.startingDate;
                mi.TimeStepManager.UpdateTimeStepsInfo(mi.timeStep);
            }
        }
        public static void LinksLoopForConversion(Model model)
        {
            OwnershipLinksRelativeUseConversion(model);
        }
        public static void NodesLoopForConversion(Model model)
        {
            //This is a temporal conversion for Ver7 networks with demand nodes named "SINKXXX"
            TempCreationOfSinks(model);
            AdjustXYPointsToVer8(model);
            AdjustIncrementalPrioritiesToCost(model);
        }
        //Links loop
        public static void OwnershipLinksRelativeUseConversion(Model model)
        {
            Link myLink = model.firstLink;
            for (int i = 1; i <= model.LinkCount; i++)
            {
                if (myLink.IsStorageLink())
                {
                    myLink.m.relativeUseOrder = myLink.m.cost;
                }
                myLink = myLink.next;
            }
        }
        public static void TempCreationOfSinks(Model model)
        {
            Node myNode = model.firstNode;
            for (int i = 1; i <= model.NodeCount; i++)
            {
                string bString = null;
                if (myNode.name.Length >= 7)
                {
                    bString = myNode.name.Substring(0, 7);
                    if (bString.ToUpper() == "BR_SINK")
                    {
                        myNode.nodeType = NodeType.Sink;
                    }
                }
                myNode = myNode.next;
            }
        }
        public static void AdjustXYPointsToVer8(Model model)
        {
            Node myNode = model.firstNode;
            while (myNode != null)
            {
                myNode.graphics.nodeLoc.X /= 2;
                myNode.graphics.nodeLoc.Y /= 2;
                if (!myNode.graphics.nodeLabelLoc.IsEmpty)
                {
                    myNode.graphics.nodeLabelLoc.X /= 2;
                    myNode.graphics.nodeLabelLoc.Y /= 2;
                }
                myNode = myNode.next;
            }
            for (int i = 0; i < model.Annotations.Count; i++)
            {
                model.Annotations.Item1(i).x = Convert.ToInt32(model.Annotations.Item1(i).x / 2);
                model.Annotations.Item1(i).y = Convert.ToInt32(model.Annotations.Item1(i).y / 2);
            }
            Link myLink = model.firstLink;
            while (myLink != null)
            {
                if (myLink.graphics.points.Length > 0)
                {
                    for (int i = 0; i < myLink.graphics.points.Length; i++)
                    {
                        myLink.graphics.points[i].X /= 2;
                        myLink.graphics.points[i].Y /= 2;
                    }
                }
                myLink = myLink.next;
            }
        }
        public static void CorrectDailyTimeSeriesIndexes(int NumYears, TimeSeries ts)
        {
            if (ts.getSize() <= 7)
                return;
            int yearIndex = 0;
            int monIndex = 0;
            int prevTimeStep = 0;
            int newTimeStep = 0;
            for (yearIndex = 0; yearIndex < NumYears; yearIndex++)
            {
                for (monIndex = 0; monIndex <= 6; monIndex++)
                {
                    prevTimeStep = yearIndex * 12 + monIndex;
                    newTimeStep = yearIndex * 7 + monIndex;
                    if (ts.IsFloatType == true)
                    {
                        ts.setDataF(newTimeStep, ts.getDataF(prevTimeStep));
                    }
                    else
                    {
                        ts.setDataL(newTimeStep, ts.getDataL(prevTimeStep));
                    }
                }
            }
        }
        public static void TruncateOldTimeSeries(Model mi, TimeSeries ts)
        {
            // we have to figure out where in the time sereis we wish to start
            // if we need to delete rows ahead of the beginning MODEL startingDate do that
            // then delete anything at the end
            DataTable table = ts.GetTable();

            // fill only the data that's needed 
            if (ts.getSize() > mi.TimeStepManager.noDataTimeSteps)
            {
                DataTable newtbl = SetUpColumnsForDT(table.Columns);
                for (int i = 0; i < mi.TimeStepManager.noDataTimeSteps; i++)
                {
                    newtbl.Rows.Add(table.Rows[i].ItemArray);
                }
                ts.dataTable = newtbl;
            }
        }

        public static void CorrectOldDailyTimeSeries(Model model, Node node)
        {
            int yearIndex = 0;
            int monIndex = 0;
            for (yearIndex = 0; yearIndex < model.Nyears; yearIndex++)
            {
                for (monIndex = 0; monIndex < 12; monIndex++)
                {
                    int prevTimeStep = yearIndex * 12 + monIndex;
                    int newTimeStep = yearIndex * 7 + monIndex;
                    if (node.m.adaDemandsM.getSize() > 7)
                        node.m.adaDemandsM.setDataL(newTimeStep, node.m.adaDemandsM.getDataL(prevTimeStep));
                    if (node.m.adaEvaporationsM.getSize() > 7)
                        node.m.adaEvaporationsM.setDataF(newTimeStep, node.m.adaEvaporationsM.getDataF(prevTimeStep));
                    if (node.m.adaForecastsM.getSize() > 7)
                        node.m.adaForecastsM.setDataL(newTimeStep, node.m.adaForecastsM.getDataL(prevTimeStep));
                    if (node.m.adaGeneratingHrsM.getSize() > 7)
                        node.m.adaGeneratingHrsM.setDataF(newTimeStep, node.m.adaGeneratingHrsM.getDataF(prevTimeStep));
                    if (node.m.adaInfiltrationsM.getSize() > 7)
                        node.m.adaInfiltrationsM.setDataF(newTimeStep, node.m.adaInfiltrationsM.getDataF(prevTimeStep));
                    if (node.m.adaInflowsM.getSize() > 7)
                        node.m.adaInflowsM.setDataL(newTimeStep, node.m.adaInflowsM.getDataL(prevTimeStep));
                    if (node.m.adaTargetsM.getSize() > 7)
                        node.m.adaTargetsM.setDataL(newTimeStep, node.m.adaTargetsM.getDataL(prevTimeStep));
                }
            }
            if (node.m.adaDemandsM.getSize() > 7)
                FixTSSize(model, node.m.adaDemandsM);
            if (node.m.adaEvaporationsM.getSize() > 7)
                FixTSSize(model, node.m.adaEvaporationsM);
            if (node.m.adaForecastsM.getSize() > 7)
                FixTSSize(model, node.m.adaForecastsM);
            if (node.m.adaGeneratingHrsM.getSize() > 7)
                FixTSSize(model, node.m.adaGeneratingHrsM);
            if (node.m.adaInfiltrationsM.getSize() > 7)
                FixTSSize(model, node.m.adaInfiltrationsM);
            if (node.m.adaInflowsM.getSize() > 7)
                FixTSSize(model, node.m.adaInflowsM);
            if (node.m.adaTargetsM.getSize() > 7)
                FixTSSize(model, node.m.adaTargetsM);
        }
        public static void FixTSSize(Model mi, TimeSeries ts)
        {
            if (ts.getSize() > mi.TimeStepManager.noModelTimeSteps)
            {
                for (int i = mi.TimeStepManager.noModelTimeSteps; i < ts.getSize(); i++)
                {
                    ts.GetTable().Rows[i].Delete();
                }
            }
        }
        public static void ReArrangeDailyTimeSeries(Model model)
        {
            //This routine removes the blanks from version 7 arrays in the daily timeseries
            int yearIndex = 0;
            int monIndex = 0;
            Node myNode = model.firstNode;
            for (int i = 1; i <= model.NodeCount; i++)
            {
                for (yearIndex = 1; yearIndex <= model.Nyears; yearIndex++)
                {
                    for (monIndex = 0; monIndex < 12; monIndex++)
                    {
                        int prevTimeStep = (yearIndex - 1) * (12) + monIndex;
                        int newTimeStep = (yearIndex - 1) * (7) + monIndex;
                        if (myNode.m.adaDemandsM.getSize() > 0)
                            myNode.m.adaDemandsM.setDataL(newTimeStep, myNode.m.adaDemandsM.getDataL(prevTimeStep));
                        if (myNode.m.adaEvaporationsM.getSize() > 0)
                            myNode.m.adaEvaporationsM.setDataF(newTimeStep, myNode.m.adaEvaporationsM.getDataF(prevTimeStep));
                        if (myNode.m.adaForecastsM.getSize() > 0)
                            myNode.m.adaForecastsM.setDataL(newTimeStep, myNode.m.adaForecastsM.getDataL(prevTimeStep));
                        if (myNode.m.adaGeneratingHrsM.getSize() > 0)
                            myNode.m.adaGeneratingHrsM.setDataF(newTimeStep, myNode.m.adaGeneratingHrsM.getDataF(prevTimeStep));
                        if (myNode.m.adaInfiltrationsM.getSize() > 0)
                            myNode.m.adaInfiltrationsM.setDataF(newTimeStep, myNode.m.adaInfiltrationsM.getDataF(prevTimeStep));
                        if (myNode.m.adaInflowsM.getSize() > 0)
                            myNode.m.adaInflowsM.setDataL(newTimeStep, myNode.m.adaInflowsM.getDataL(prevTimeStep));
                        //myNode.m.adaResTrgM.setData(newTimeStep, myNode.m.adaResTrgM.getData(prevTimeStep))
                        if (myNode.m.adaTargetsM.getSize() > 0)
                            myNode.m.adaTargetsM.setDataL(newTimeStep, myNode.m.adaTargetsM.getDataL(prevTimeStep));
                    }
                }
                myNode = myNode.next;
            }
        }
        public static void UpgradeTomaxCapacity(Model mi, Link m_link, long[] maxVarCapacity)
        {
            int lper = mi.timeStep.NumOfTSsForV7Output;
            bool isActive = false;
            for (int i = 0; i < maxVarCapacity.Length; i++)
            {
                if (maxVarCapacity[i] > 0)
                {
                    isActive = true;
                    break; // TODO: might not be correct. Was : Exit For
                }
            }
            if (isActive)
            {
                if (mi.timeStep.TSType != ModsimTimeStepType.Monthly)
                {
                    mi.FireOnError("Maximum Variable Capacity for Time Step other than Monthly is not supported in Version 7.");
                    mi.FireOnError("Import could misunderstand your data.  Check the imported values");
                    m_link.m.maxVariable.VariesByYear = true;
                    int i = 0;
                    for (int j = 0; j < mi.TimeStepManager.noDataTimeSteps; j++)
                    {
                        m_link.m.maxVariable.setDate(j, mi.TimeStepManager.Index2Date(j, TypeIndexes.DataIndex));
                        m_link.m.maxVariable.setDataL(j, maxVarCapacity[i]);
                        i++;

                        if (i >= lper)
                            i = 0;
                    }
                }
                else
                {
                    m_link.m.maxVariable.VariesByYear = false;
                    for (int i = 0; i < maxVarCapacity.Length; i++)
                    {
                        m_link.m.maxVariable.setDate(i, mi.TimeStepManager.Index2Date(i, TypeIndexes.DataIndex));
                        m_link.m.maxVariable.setDataL(i, maxVarCapacity[i]);
                    }
                }
            }
        }
        public static void AdjustIncrementalPrioritiesToCost(Model model)
        {
            Node myNode = model.firstNode;
            bool first = true;
            while (myNode != null)
            {
                if (myNode.nodeType == NodeType.Reservoir)
                {
                    if (myNode.m.resBalance != null)
                    {
                        if (first)
                            model.FireOnMessage("Converting MODSIM v7 Reservoir Layers incremental priority to incremental cost (values will be multiplied times 10)");
                        for (int i = 0; i < myNode.m.resBalance.incrPriorities.Length; i++)
                        {
                            //Adjust old incremental priorities to incremental cost.
                            myNode.m.resBalance.incrPriorities[i] *= 10;
                        }
                        first = false;
                    }
                }
                myNode = myNode.next;
            }
        }
        private static DataTable SetUpColumnsForDT(DataColumnCollection theColumns)
        {
            DataTable newtbl = new DataTable();
            for (int j = 0; j < theColumns.Count; j++)
            {
                newtbl.Columns.Add(theColumns[j].ColumnName).DataType = theColumns[j].DataType;
            }
            return newtbl;
        }
    }
}
