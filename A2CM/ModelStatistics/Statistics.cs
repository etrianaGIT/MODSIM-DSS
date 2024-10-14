using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASquared.ModelStatistics
{
    public static class Statistics
    {
        public static Double Sum(Double[] vals)
        {
            Double sum = 0;
            for (Int32 i = 0; i < vals.Length; i++)
                sum += vals[i];
            return sum;
        }
        public static Double Avg(Double[] vals)
        {
            return Sum(vals) / vals.Length;
        }
        public static Double Max(Double[] vals)
        {
            int i; 
            return Max(vals, out i); 
        }
        public static Double Max(Double[] vals, out int index)
        {
            Double max = Double.MinValue;
            index = -1; 
            for (int i = 0; i < vals.Length; i++)
                if (max < vals[i])
                {
                    max = vals[i];
                    index = i; 
                }
            return max; 
        }
        public static Double Min(Double[] vals)
        {
            int i;
            return Min(vals, out i); 
        }
        public static Double Min(Double[] vals, out int index)
        {
            Double min = Double.MaxValue;
            index = -1; 
            for (int i = 0; i < vals.Length; i++)
                if (min > vals[i])
                {
                    min = vals[i];
                    index = i; 
                }
            return min; 
        }
        public static Double SumMinusAvg(Double[] vals, Int32 order)
        {
            return SumMinusAvg(vals, Avg(vals), order); 
        }
        public static Double SumMinusAvg(Double[] vals, Double avg, Int32 order)
        {
            Double sum = 0;
            for (Int32 i = 0; i < vals.Length; i++)
                sum += Math.Pow(vals[i] - avg, order);
            return sum; 
        }
        public static Double Var(Double[] vals)
        {
            return Var(vals, Avg(vals));
        }
        public static Double Var(Double[] vals, Double avg)
        {
            return Cov(vals, vals, avg, avg); 
        }
        public static Double Cov(Double[] X, Double[] Y)
        {
            return Cov(X, Y, Avg(X), Avg(Y));
        }
        public static Double Cov(Double[] X, Double[] Y, Double Xavg, Double Yavg)
        {
            Double sum = 0;
            for (Int32 i = 0; i < X.Length; i++)
                sum += (X[i] - Xavg) * (Y[i] - Yavg);
            return sum / X.Length;
        }
        public static Double Moment(Double[] X, Int32 order)
        {
            return Moment(X, 0, order);
        }
        public static Double Moment(Double[] X, Double centerVal, Int32 order)
        {
            Double sum = 0;
            for (Int32 i = 0; i < X.Length; i++)
                sum += Math.Pow(X[i] - centerVal, order);
            return sum / X.Length; 
        }
        public static Double[] Log(Double[] X)
        {
            return Log(X, Math.E); 
        }
        public static Double[] Log(Double[] X, Double newBase)
        {
            Double[] retVal = new Double[X.Length];
            for (Int32 i = 0; i < X.Length; i++)
                retVal[i] = Math.Log(X[i], newBase);
            return retVal; 
        }
        public static Double LogMoment(Double[] X, Int32 order)
        {
            return LogMoment(X, 0, order, Math.E); 
        }
        public static Double LogMoment(Double[] X, Int32 order, Double newBase)
        {
            return LogMoment(X, 0, order, newBase);
        }
        public static Double LogMoment(Double[] X, Double centerVal, Int32 order)
        {
            return LogMoment(X, centerVal, order, Math.E); 
        }
        public static Double LogMoment(Double[] X, Double centerVal, Int32 order, Double newBase)
        {
            Double sum = 0;
            for (Int32 i = 0; i < X.Length; i++) 
                sum += Math.Pow(Math.Log(X[i] - centerVal, newBase), order);
            return sum / X.Length; 
        }
        public static Double StdDev(Double[] X)
        {
            return StdDev(X, true); 
        }
        public static Double StdDev(Double[] X, bool Unbiased)
        {
            return Math.Sqrt(SumMinusAvg(X, 2) / (Unbiased ? X.Length - 1 : X.Length));
        }
        public static Double Skewness(Double[] X)
        {
            return Skewness(X, true);
        }
        public static Double Skewness(Double[] X, bool Unbiased) 
        {
            Double N = X.Length;
            N =  Unbiased ? N / ((N - 1) * (N - 2)) : 1 / N;
            return SumMinusAvg(X, 3) * N / Math.Pow(StdDev(X, Unbiased), 3); 
        }
        public static Double Kurtosis(Double[] X)
        {
            return Kurtosis(X, true);
        }
        public static Double Kurtosis(Double[] X, bool Unbiased) 
        {
            Double N = X.Length;
            N = Unbiased ? Math.Pow(N, 2) / ((N - 1) * (N - 2) * (N - 3)) : 1 / N; 
            return SumMinusAvg(X, 4) * N / Math.Pow(StdDev(X, Unbiased), 4);
        }
        public static Double SerialCorrelation(Double[] X, Double avg)
        {
            return SerialCorrelation(X, avg, 1);
        }
        public static Double SerialCorrelation(Double[] X, Int32 lags)
        {
            return SerialCorrelation(X, Avg(X), lags);
        }
        public static Double SerialCorrelation(Double[] X, Double avg, Int32 lags)
        {
            return SerialCorrelation(X, avg, lags, false); 
        }
        public static Double SerialCorrelation(Double[] X, Double avg, Int32 lags, bool Unbiased)
        {
            if (Unbiased && lags > 1)
                throw new NotImplementedException("Unbiased coefficients are undefined for lags greater than one.");
            Int32 N = X.Length; 
            Double sum = 0;
            for (Int32 i = 0; i < N - lags; i++)
                sum += (X[i + lags] - avg) * (X[i] - avg);
            sum /= SumMinusAvg(X, 2);
            if (Unbiased && N > 4)
                return (1 + N * sum) / (N - 4);
            return sum; 
        }
        public static Double CrossCorrelation(Double[] X, Double[] Y)
        {
            return CrossCorrelation(X, Y, (Int32)0, (Int32)0); 
        }
        public static Double CrossCorrelation(Double[] X, Double[] Y, Int32 Xlags, Int32 Ylags)
        {
            return CrossCorrelation(X, Y, Avg(X), Avg(Y), Xlags, Ylags); 
        }
        public static Double CrossCorrelation(Double[] X, Double[] Y, Double Xavg, Double Yavg, Int32 Xlags, Int32 Ylags)
        {
            Int32 N = X.Length;
            Double sum = 0;
            Int32 maxlags = Math.Max(Xlags, Ylags); 
            for (Int32 i = 0; i < N - maxlags; i++)
                sum += (X[i + Xlags] - Xavg) * (Y[i + Ylags] - Yavg);
            Double Sx = SumMinusAvg(X, Xavg, 2), Sy = SumMinusAvg(Y, Yavg, 2);
            if (Sx == 0 && Sy == 0)
                return 1;
            else if (Sx == 0 || Sy == 0)
                return 0;
            else
                sum /= (Math.Sqrt(SumMinusAvg(X, Xavg, 2)) * Math.Sqrt(SumMinusAvg(Y, Yavg, 2)));
            return sum; 
        }
        public static Double Rsquared(Double[] X, Double[] Y)
        {
            return Math.Pow(CrossCorrelation(X, Y), 2);
        }
        /// <summary>n!/(k! * (n-k)!)</summary>
        /// <param name="n">The number of trials</param>
        /// <param name="k">The number of successful trials</param>
        public static Double nChooseK(long n, long k)
        {
            if (k > n)
                throw new Exception("k must be less than n"); 
            // can do: return factorial(n) / (factorial(k) * factorial(n-k))
            // but, it is more efficient this way: 
            Double retVal = 1;
            for (Int32 i = 1; i <= k; i++)
                retVal *= (n - k + i) / i;
            return retVal; 
        }
        /// <summary>Calculates the factorial for the specified value.</summary>
        /// <param name="val">The value on which to calculate the factorial.</param>
        public static Double factorial(long val)
        {
            Double retVal = 1;
            for (Int32 i = 2; i <= val; i++)
                retVal *= i;
            return retVal; 
        }
    }
}
