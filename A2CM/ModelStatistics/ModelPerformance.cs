using System;
using System.Text;

namespace ASquared.ModelStatistics
{
    public class ModelPerformance
    {
        // Instance variables
        private Double[] observed, modeled;
        private Double obsAvg = 0, modAvg = 0;

        // Properties 
        public Double AverageObserved { get { return this.obsAvg; } }
        public Double AverageModeled { get { return this.modAvg; } }
        public Double[] Observed { get { return this.observed; } set { this.observed = value; } }
        public Double[] Modeled { get { return this.modeled; } set { this.modeled = value; } }

        // Constructor
        /// <summary>A class that calculates performance measures for specified data.</summary>
        /// <param name="observed">Observed data</param>
        /// <param name="modeled">Modeled data</param>
        /// <remarks>Observed and Modeled data must have the same number of elements.</remarks>
        public ModelPerformance(Double[] observed, Double[] modeled)
        {
            if (observed == null || modeled == null || observed.Length != modeled.Length)
                throw new Exception("Cannot calculate performance of data that does not exist or observed and modeled arrays of different sizes.");
            this.observed = observed;
            this.modeled = modeled;
            this.obsAvg = Statistics.Avg(this.observed);
            this.modAvg = Statistics.Avg(this.modeled);
        }

        // Overrides
        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.Append("BIAS = " + this.BIAS().ToString());
            s.Append("\nDEVIATION = " + this.DEVIATION().ToString());
            s.Append("\nSAE = " + this.SAE().ToString());
            s.Append("\nMAE = " + this.MAE().ToString());
            s.Append("\nMRE = " + this.MRE().ToString());
            s.Append("\nSSE = " + this.SSE().ToString());
            s.Append("\nMSE = " + this.MSE().ToString());
            s.Append("\nRMSE = " + this.RMSE().ToString());
            s.Append("\nPWRMSE = " + this.PWRMSE().ToString());
            s.Append("\nCorrelation = " + this.Corr().ToString());
            s.Append("\nR² = " + this.Rsquared().ToString());
            s.Append("\nNSCE = " + this.NSCE().ToString());
            s.Append("\nMCE = " + this.MCE().ToString());
            return s.ToString();
        }

        public string ToSingleLineString(string delimiter)
        {
            StringBuilder s = new StringBuilder();
            s.Append(this.BIAS().ToString());
            s.Append(delimiter + this.DEVIATION().ToString());
            s.Append(delimiter + this.SAE().ToString());
            s.Append(delimiter + this.MAE().ToString());
            s.Append(delimiter + this.MRE().ToString());
            s.Append(delimiter + this.SSE().ToString());
            s.Append(delimiter + this.MSE().ToString());
            s.Append(delimiter + this.RMSE().ToString());
            s.Append(delimiter + this.PWRMSE().ToString());
            s.Append(delimiter + this.Corr().ToString());
            s.Append(delimiter + this.Rsquared().ToString());
            s.Append(delimiter + this.NSCE().ToString());
            s.Append(delimiter + this.MCE().ToString());
            return s.ToString();
        }

        public static string SingleLineStringHeader(string delimiter)
        {
            StringBuilder s = new StringBuilder();
            s.Append("BIAS" + delimiter);
            s.Append("DEVIATION" + delimiter);
            s.Append("SAE" + delimiter);
            s.Append("MAE" + delimiter);
            s.Append("MRE" + delimiter);
            s.Append("SSE" + delimiter);
            s.Append("MSE" + delimiter);
            s.Append("RMSE" + delimiter);
            s.Append("PWRMSE" + delimiter);
            s.Append("Correlation" + delimiter);
            s.Append("R²" + delimiter);
            s.Append("NSCE" + delimiter);
            s.Append("MCE");
            return s.ToString();
        }

        #region Calculations

        // Under/over Metrics
        /// <summary>Model bias. If positive, observed is greater than modeled.</summary>
        public Double BIAS()
        {
            return Statistics.Sum(this.observed) - Statistics.Sum(this.modeled);
        }
        /// <summary>Model deviation. If greater than 1, modeled is greater than observed. If less than 1, modeled is less than observed.</summary>
        public Double DEVIATION()
        {
            return Statistics.Sum(this.modeled) / Statistics.Sum(this.observed);
        }

        // Absolute errors
        /// <summary>Sum of absolute error</summary>
        public Double SAE()
        {
            Double sum = 0;
            for (Int32 i = 0; i < this.observed.Length; i++)
                sum += Math.Abs(observed[i] - modeled[i]);
            return sum;
        }
        /// <summary>Mean absolute error</summary>
        public Double MAE()
        {
            return this.SAE() / this.observed.Length;
        }

        // Relative error
        public Double MRE()
        {
            Double sum = 0;
            for (Int32 i = 0; i < this.observed.Length; i++)
                sum += Math.Abs(this.observed[i] - this.modeled[i]) / this.observed[i];
            return sum / this.observed.Length;
        }

        // Squared errors (emphasize large errors)
        /// <summary>Sum of squared error</summary>
        public Double SSE()
        {
            Double sum = 0;
            for (Int32 i = 0; i < this.observed.Length; i++)
                sum += Math.Pow(observed[i] - modeled[i], 2);
            return sum;
        }
        /// <summary>Mean squared error</summary>
        public Double MSE()
        {
            return this.SSE() / this.observed.Length;
        }
        /// <summary>Root mean squared error</summary>
        public Double RMSE()
        {
            return Math.Sqrt(this.SSE() / this.observed.Length);
        }
        /// <summary>Peak weighted root mean squared error</summary>
        public Double PWRMSE()
        {
            Double sum = 0;
            for (Int32 i = 0; i < this.observed.Length; i++)
                sum += Math.Pow(observed[i] - modeled[i], 2) * (observed[i] + obsAvg) / (2 * obsAvg);
            return Math.Sqrt(sum / this.observed.Length);
        }

        // Consider variance
        /// <summary>Cross-correlation coefficient</summary>
        public Double Corr()
        {
            return Statistics.CrossCorrelation(observed, modeled); 
        }
        /// <summary>Coefficient of determination (R^2)</summary>
        public Double Rsquared()
        {
            return Statistics.Rsquared(observed, modeled); 
        }
        /// <summary>Nash-Sutcliffe coefficient of efficiency</summary>
        public Double NSCE()
        {
            Double sum = 0;
            for (Int32 i = 0; i < this.observed.Length; i++)
                sum += Math.Pow(observed[i] - obsAvg, 2);
            return 1 - this.SSE() / sum;
        }
        /// <summary>Modified coefficient of efficiency</summary>
        public Double MCE()
        {
            Double sum = 0;
            for (Int32 i = 0; i < this.observed.Length; i++)
                sum += Math.Abs(observed[i] - obsAvg);
            return 1 - this.SAE() / sum;
        }

        #endregion
    }
}
