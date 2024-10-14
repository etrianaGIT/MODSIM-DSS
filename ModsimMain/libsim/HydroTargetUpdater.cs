using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace Csu.Modsim.ModsimModel
{
    public class HydroTargetUpdater
    {
        private static char[] s_delim = new char[] { ',' };

        public static void AddToTargets(Model model, TimeSeries toAdd)
        {
            AddToTargets(model, model.hydro.HydroTargets, toAdd);
        }

        public static void AddToTargets(Model model, HydropowerTarget[] targets, TimeSeries toAdd)
        {
            // split the data to add into fractions based on the capacity of the units within a particular hydropower target object
            double[] fractions;
            SumCapacity(targets, out fractions);

            for (int i = 0; i < targets.Length; i++)
            {
                // Copy the timeseries to add... 
                TimeSeries fracToAdd = toAdd.Copy();

                // Multiply each element by the fraction for this hydropower target object
                for (int col = 1; col < fracToAdd.dataTable.Columns.Count; col++)
                    for (int row = 0; row < fracToAdd.dataTable.Rows.Count; row++)
                        fracToAdd.dataTable.Rows[row][col] = fractions[i] * (double)fracToAdd.dataTable.Rows[row][col];

                // Add this new data to the hydropower target object 
                AddToTargets(model, targets[i], fracToAdd);
            }
        }

        public static void AddToTargets(Model model, HydropowerTarget target, TimeSeries toAdd)
        {
            if (toAdd == null)
                return;

            target.PowerTargetsTS.FillTable(model, 0.0, model.EnergyUnits);
            toAdd.FillTable(model, 0.0, model.EnergyUnits);

            DataTable dt = target.PowerTargetsTS.dataTable;
            for (int i = 0; i < dt.Rows.Count; i++)
                for (int j = 1; j < dt.Columns.Count; j++)
                    dt.Rows[i][j] = (double)dt.Rows[i][j] + (double)toAdd.dataTable.Rows[i][j];
        }

        public static void AddToTargets(Model model, HydropowerTarget target, string timeSeriesFile)
        {
            TimeSeries ts = ReadPowerFile(timeSeriesFile, model);
            AddToTargets(model, target, ts);
        }

        public static TimeSeries ReadPowerFile(string file, Model model)
        {
            StreamReader sr = new StreamReader(file);
            TimeSeries t = new TimeSeries(TimeSeriesType.Power_Target);
            DataTable dt = t.dataTable.Clone();
            DataRow dr;
            while (!sr.EndOfStream)
            {
                string lineString = sr.ReadLine();
                string[] line = lineString.Split(s_delim);
                if (line.Length != 4)
                    continue;

                int yr, day, hr;
                double val;
                if (Int32.TryParse(line[0], out yr)
                    && Int32.TryParse(line[1], out day)
                    && Int32.TryParse(line[2], out hr)
                    && Double.TryParse(line[3], out val))
                {
                    dr = dt.NewRow();
                    DateTime date = new DateTime(model.TimeStepManager.startingDate.Year, 1, 1);
                    dr[0] = date.AddDays(day - 1).AddHours(hr - 1);
                    dr[1] = val;
                    dt.Rows.Add(dr);
                }
            }
            sr.Close();
            t.dataTable = dt;
            t.VariesByYear = false;
            t.units = new ModsimUnits(EnergyUnitsType.MWh, ModsimTimeStepType.Hourly);
            t.FillTable(model, 0.0, new ModsimUnits(EnergyUnitsType.MWh));
            return t;
        }

        /// <summary>Gets the energy difference calculated within HydropowerTarget (energy produced minus energy target)</summary>
        /// <param name="left">Specifies whether to sum the energy diff on the left side or the right side of the bus.</param>
        public static double SumEnergy(HydropowerTarget[] targs, bool target, bool takeAbsolute)
        {
            double sum = 0;
            double val;
            foreach (HydropowerTarget targ in targs)
            {
                val = target ? targ.EnergyTarget : targ.Energy;
                sum += takeAbsolute ? Math.Abs(val) : val;
            }
            return sum;
        }

        public static double SumEnergyDiff(HydropowerTarget[] targs, bool takeAbsolute)
        {
            double sum = 0.0;
            foreach (HydropowerTarget targ in targs)
                sum += takeAbsolute ? Math.Abs(targ.EnergyDiff) : targ.EnergyDiff;
            return sum;
        }

        public static double SumCapacity(HydropowerTarget[] targets)
        {
            double[] a;
            return SumCapacity(targets, out a);
        }

        public static double SumCapacity(HydropowerTarget[] targets, out double[] fractions)
        {
            double totalsum = 0.0, cap;
            fractions = new double[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                cap = targets[i].PowerCapacity;
                fractions[i] = cap;
                totalsum += cap;
            }

            for (int i = 0; i < targets.Length; i++)
                fractions[i] /= totalsum;

            return totalsum;
        }

        public static double SumReserveCapacity(HydropowerTarget[] targets, bool downReserve, out double[] fractions)
        {
            double totalsum = 0.0, cap;
            fractions = new double[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                cap = downReserve ? targets[i].DownReserveCapacity : targets[i].UpReserveCapacity;
                fractions[i] = cap;
                totalsum += cap;
            }

            if (totalsum == 0.0)
                for (int i = 0; i < targets.Length; i++)
                    fractions[i] = 1.0 / (double)targets.Length;
            else
                for (int i = 0; i < targets.Length; i++)
                    fractions[i] /= totalsum;

            return totalsum;
        }

    }
}
