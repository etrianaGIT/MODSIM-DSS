using System;
using System.Data;

namespace Csu.Modsim.ModsimModel
{
    public class ModelSettings
    {
        private Model ModsimModel;
        private Node ModsimNode = null;
        private Link ModsimLink = null;
        private string errMessage = string.Empty;
        private string warningMessage = string.Empty;

        /// <summary>Gets an error message that occured when changing any of the settings. Returns an empty string if there has been no error message.</summary>
        public string ErrorMessage { get { return this.errMessage; } }
        /// <summary>Gets the last warning message that occured when changing any of the settings. Returns an empty string if there has been no warning message.</summary>
        public string WarningMessage { get { return this.warningMessage; } }

        // Constructor
        public ModelSettings(Model mi)
        {
            this.ModsimModel = mi;
        }

        // Methods controlling messages 
        public void ResetMessages()
        {
            this.errMessage = string.Empty;
            this.warningMessage = string.Empty; 
        } 

        // Change accuracy
        private long scaleFactorOld, scaleFactorNew;
        private void scaleValues(ref long[] OldValue, bool ChangeValues)
        {
            for (int i = 0; i < OldValue.Length; i++)
                scaleValue(ref OldValue[i], ChangeValues);
        }
        private void scaleValues(ref double[] OldValue, bool ChangeValues)
        {
            for (int i = 0; i < OldValue.Length; i++)
                scaleValue(ref OldValue[i], ChangeValues);
        }
        private void scaleValue(ref long OldValue, bool ChangeValues)
        {
            if (OldValue == ModsimModel.defaultMaxCap)
                return;
            long tmp = Convert.ToInt64(OldValue * scaleFactorNew / scaleFactorOld);
            if (tmp > ModsimModel.defaultMaxCap)
            {
                ValueTooLargeMessage(tmp);
            }
            if (ChangeValues)
                OldValue = tmp;
        }
        private void scaleValue(ref double OldValue, bool ChangeValues)
        {
            if (Convert.ToInt64(OldValue) == ModsimModel.defaultMaxCap)
                return;
            double tmp = OldValue * scaleFactorNew / scaleFactorOld;
            if (tmp > ModsimModel.defaultMaxCap)
            {
                ValueTooLargeMessage(tmp);
            }
            if (ChangeValues)
                OldValue = tmp;
        }
        private void scaleValue(ref object OldValue, bool ChangeValues)
        {
            if (OldValue is double)
            {
                double tmp = (double)OldValue;
                scaleValue(ref tmp, ChangeValues);
                OldValue = tmp;
            }
            else if (OldValue is long)
            {
                long tmp = (long)OldValue;
                scaleValue(ref tmp, ChangeValues);
                OldValue = tmp;
            }
        }
        private void ValueTooLargeMessage(double tmp)
        {
            string name = "";
            if (this.ModsimNode != null)
                name = " in node '" + this.ModsimNode.name + "'";
            else if (this.ModsimLink != null)
                name = " in link '" + this.ModsimLink.name + "'";
            this.warningMessage = "In order to run the solver, MODSIM needs to be able to convert all floating point values to integers. To maintain accuracy, all numbers are multiplied by 10 to the given accuracy (i.e., 10^accuracy). There was a scaled value that became too large when doing this scaling operation: \n\nScaled value (= " + tmp.ToString() + ")" + name + " is greater than default maximum link capacity (" + (ModsimModel.defaultMaxCap/ ModsimModel.ScaleFactor).ToString() + ").";
        }
        private void changeTSAccuracy(TimeSeries tsData, bool ChangeValues)
        {
            DataRow row = null;
            int i = 0;
            int j = 0;
            DataTable tbl = null;
            if (!ChangeValues)
                tbl = tsData.GetTable().Copy();
            else
                tbl = tsData.GetTable();
            if (tsData.IsFloatType == false)
            {
                for (i = 0; i < tbl.Rows.Count; i++)
                {
                    row = tbl.Rows[i];
                    for (j = 1; j < tbl.Columns.Count; j++)
                    {
                        object val = row[j];
                        scaleValue(ref val, ChangeValues);
                        row[j] = val;
                    }
                }
            }
        }
        private void changeAccuracy(int accuracyOld, int accuracyNew)
        {
            changeAccuracy(accuracyOld, accuracyNew, false);
        }
        private void changeAccuracy(int accuracyOld, int accuracyNew, bool ChangeValues)
        {
            this.ModsimNode = ModsimModel.firstNode;
            bool warningAtBeg = false;
            scaleFactorOld = ModsimModel.CalcScaleFactor(accuracyOld);
            scaleFactorNew = ModsimModel.CalcScaleFactor(accuracyNew);

            while (!(ModsimNode == null))
            {
                warningAtBeg = (this.warningMessage != "");
                if (ModsimNode.nodeType == NodeType.Demand)
                {
                    changeTSAccuracy(ModsimNode.m.adaDemandsM, ChangeValues);
                    changeTSAccuracy(ModsimNode.m.adaInfiltrationsM, ChangeValues);
                    scaleValue(ref ModsimNode.m.pcap, ChangeValues);
                }
                else if (ModsimNode.nodeType == NodeType.Reservoir)
                {
                    changeTSAccuracy(ModsimNode.m.adaTargetsM, ChangeValues);
                    changeTSAccuracy(ModsimNode.m.adaGeneratingHrsM, ChangeValues);
                    changeTSAccuracy(ModsimNode.m.adaEvaporationsM, ChangeValues);
                    changeTSAccuracy(ModsimNode.m.adaForecastsM, ChangeValues);
                    scaleValue(ref ModsimNode.m.max_volume, ChangeValues);
                    scaleValue(ref ModsimNode.m.min_volume, ChangeValues);
                    scaleValue(ref ModsimNode.m.starting_volume, ChangeValues);
                    scaleValues(ref ModsimNode.m.hpoints, ChangeValues);
                    scaleValues(ref ModsimNode.m.cpoints, ChangeValues);
                }
                else if (ModsimNode.nodeType == NodeType.NonStorage)
                {
                    changeTSAccuracy(ModsimNode.m.adaInflowsM, ChangeValues);
                }
                ModsimNode = ModsimNode.next;
            }

            this.ModsimLink = ModsimModel.firstLink;
            while (!(ModsimLink == null))
            {
                changeTSAccuracy(ModsimLink.m.maxVariable, ChangeValues);
                scaleValue(ref ModsimLink.m.lnkallow, ChangeValues);
                scaleValue(ref ModsimLink.m.min, ChangeValues);
                scaleValue(ref ModsimLink.m.capacityOwned, ChangeValues);
                scaleValue(ref ModsimLink.m.initialStglft, ChangeValues);
                scaleValues(ref ModsimLink.m.stgAmount, ChangeValues);
                scaleValues(ref ModsimLink.m.initStglft, ChangeValues);
                scaleValues(ref ModsimLink.m.rentLimit, ChangeValues);
                ModsimLink = ModsimLink.next;
            }
            for (int i = 0; i < ModsimModel.hydro.EfficiencyCurves.Length; i++)
			{
                PowerEfficiencyCurve pcurve = ModsimModel.hydro.EfficiencyCurves[i];
                double[] tmp = pcurve.Flows; 
                scaleValues(ref tmp, ChangeValues);
                pcurve.Flows = tmp;
			}
        }
        public bool ChangeAccuracy(int accuracyOld, int accuracyNew)
        {
            try
            {
                changeAccuracy(accuracyOld, accuracyNew, false);
            }
            catch (Exception ex)
            {
                this.errMessage = ex.ToString();
                return false;
            }

            changeAccuracy(accuracyOld, accuracyNew, true);
            return true;
        }

        // Change default max. link capacity
        private long defMaxCapOld, defMaxCapNew;
        private void changeCapacity(ref long[] OldValue)
        {
            for (int i = 0; i < OldValue.Length; i++)
            {
                changeCapacity(ref OldValue[i]);
            }
        }
        private void changeCapacity(ref long OldValue)
        {
            if (OldValue == defMaxCapOld)
                OldValue = defMaxCapNew;
        }
        private void changeCapacity(ref object OldValue)
        {
            long tmp = 0;
            if (long.TryParse(OldValue.ToString(), out tmp))
            {
                if (tmp == defMaxCapOld)
                    OldValue = defMaxCapNew;
            }
        }
        private void changeTSCapacity(TimeSeries tsData)
        {
            changeTSCapacity(tsData, false);
        }
        private void changeTSCapacity(TimeSeries tsData, bool CompressTableOn1Capacity)
        {
            DataRow row = null;
            int i = 0;
            int j = 0;
            DataTable tbl = tsData.GetTable();

            // If the table only has one row and it is equal to the default max. capacity... Delete it.
            if (CompressTableOn1Capacity && tbl.Rows.Count == 1)
            {
                bool delRow = true;
                for (j = 1; j < tbl.Columns.Count; j++)
                {
                    long lng = Convert.ToInt64(tbl.Rows[0][j]);
                    if (lng != this.defMaxCapOld && lng != this.defMaxCapNew)
                        delRow = false;
                }
                if (delRow)
                {
                    tbl.Rows[0].Delete();
                    return;
                }
            }

            // Change the capacity in all the rows and columns of data.
            for (i = 0; i < tbl.Rows.Count; i++)
            {
                row = tbl.Rows[i];
                for (j = 1; j < tbl.Columns.Count; j++)
                {
                    object val = row[j]; 
                    changeCapacity(ref val);
                    row[j] = val;
                }
            }
        }
        public bool ChangeCapacity(long defaultMaxCapOld, long defaultMaxCapNew)
        {
            try
            {
                this.ModsimNode = ModsimModel.firstNode;
                this.defMaxCapOld = defaultMaxCapOld;
                this.defMaxCapNew = defaultMaxCapNew;

                while (!(ModsimNode == null))
                {
                    if (ModsimNode.nodeType == NodeType.Demand)
                    {
                        changeTSCapacity(ModsimNode.m.adaDemandsM);
                        changeTSCapacity(ModsimNode.m.adaInfiltrationsM);
                        changeCapacity(ref ModsimNode.m.pcap);
                    }
                    else if (ModsimNode.nodeType == NodeType.Reservoir)
                    {
                        changeTSCapacity(ModsimNode.m.adaTargetsM);
                        changeTSCapacity(ModsimNode.m.adaGeneratingHrsM);
                        changeTSCapacity(ModsimNode.m.adaEvaporationsM);
                        changeTSCapacity(ModsimNode.m.adaForecastsM);
                        changeCapacity(ref ModsimNode.m.max_volume);
                        changeCapacity(ref ModsimNode.m.min_volume);
                        changeCapacity(ref ModsimNode.m.starting_volume);
                        changeCapacity(ref ModsimNode.m.hpoints);
                    }
                    else if (ModsimNode.nodeType == NodeType.NonStorage)
                    {
                        changeTSCapacity(ModsimNode.m.adaInflowsM);
                    }
                    ModsimNode = ModsimNode.next;
                }

                this.ModsimLink = ModsimModel.firstLink;
                while (!(ModsimLink == null))
                {
                    changeTSCapacity(ModsimLink.m.maxVariable, true);
                    changeCapacity(ref ModsimLink.m.lnkallow);
                    changeCapacity(ref ModsimLink.m.min);
                    changeCapacity(ref ModsimLink.m.capacityOwned);
                    changeCapacity(ref ModsimLink.m.initialStglft);
                    changeCapacity(ref ModsimLink.m.stgAmount);
                    changeCapacity(ref ModsimLink.m.initStglft);
                    ModsimLink = ModsimLink.next;
                }
                return true;
            }
            catch (Exception ex)
            {
                string name = "";
                if (this.ModsimNode != null)
                    name = " in node '" + this.ModsimNode.name + "'";
                else if (this.ModsimLink != null) 
                    name = " in link '" + this.ModsimLink.name + "'";
                this.errMessage = "Error occurred when changing a capacity value" + name + ":\n" + ex.ToString(); 
                return false;
            }
        }
    }
}
