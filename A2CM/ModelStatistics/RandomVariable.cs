using System;
using System.Data;

namespace ASquared.ModelStatistics
{
    // For counting crossing
    /// <summary>The type of crossing counting used in calculations.</summary>
    public enum CrossingType
    {
        Crossings_Count = 0,
        Crossings_Mean = 1,
        upCrossings_Count = 2,
        upCrossings_Mean = 3,
        downCrossings_Count = 4,
        downCrossings_Mean = 5
    }
    /// <summary>The distribution type.</summary>
    public enum DistType
    {
        // Continuous
        distNormal = 0,
        distLogNormalII = 1,
        distLogNormalIII = 2,
        distLogPearsonIII = 3,
        distBeta = 4,
        distExponential = 5,
        distGamma = 6,
        distGenExtremeVal = 7,
        distGumbel = 8,
        distLogGumbel = 9,
        distWakeby = 10,
        distWeibull = 11,

        // Discrete Distributions
        distBernoulli = 12,
        distBinomial = 13,
        distGeometric = 14,
        distHypergeometric = 15,
        distMultiNomial = 16,
        distNegBinomial = 17,
        distPoisson = 18
    }
    /// <summary>The method used to fit statistical distributions to data.</summary>
    public enum FitMethod
    {
        fitMethodOfMoments = 0,
        fitProbWeightedMoments = 1,
        fitMaxLikelihood = 2,
        fitLeastSquares = 3,
        fitIndirectMOM = 4
    }
    /// <summary>A class defining a random variable as defined in probability theory. Has some usual parameters.</summary>
    public class RandomVariable
    {
        #region Static instance variables

        public static Double Tolerance = 0.001;
        public static Int32 MaxRecursiveDepth = 200;

        #endregion
        #region Instance variables

        // Statistical parameters
        public Double mean, stdev, skew, Xo, logBase, alpha, beta, Yo, k;

        // Realizations
        /// <summary>An array of realizations that PDFs and CDFs will be calculated on.</summary>
        private Double[] X;
        private Double[] pdf, cdf;
        private Double[] bins_Beg, bins_Middle, bins_End; 
        private Double[] epdf, ecdf;

        // Distribution information
        private DistType distType;
        private FitMethod fitType;

        // Other instance variables
        private Int32 seed = Int32.MaxValue; 
        private bool isFitted;
        private Double xMin, xMax;

        #endregion
        #region Properties

        /// <summary>Gets and sets an array of realizations that PDFs and CDFs will be calculated on.</summary>
        public Double[] Realizations { get { return this.X; } set { Array.Sort(value); this.X = value; } }
        public Double[] PDF { get { return this.pdf; } }
        public Double[] CDF { get { return this.cdf; } }
        public Double[] Bins_Beg { get { return this.bins_Beg; } }
        public Double[] Bins_Middle { get { return this.bins_Middle; } }
        public Double[] Bins_End { get { return this.bins_End; } }
        public Double[] EmpiricalPDF { get { return this.epdf; } }
        public Double[] EmpiricalCDF { get { return this.ecdf; } }
        /// <summary>Gets and sets the seed when using a random number generator.</summary>
        /// <remarks>If the seed == Double.NaN, a seed based on the current time is used instead.</remarks>
        public Int32 Seed { get { return this.seed; } set { this.seed = value; } }

        #endregion

        #region Constructor

        public RandomVariable(DistType theDistribution, FitMethod theFittingMethod)
        {
            this.Initialize(theDistribution, theFittingMethod);
        }
        public void Initialize(DistType theDistribution, FitMethod theFittingMethod)
        {
            //pdfHeader = mf.MyTranspose(Strings.Split("Sorted Data,Model PDF", ","), true);
            //epdfHeader = mf.MyTranspose(Strings.Split("Middle of Bin,End of Bin,Empirical PDF", ","), true);
            //cdfHeader = mf.MyTranspose(Strings.Split("Sorted Data,Model CDF", ","), true);
            //ecdfHeader = mf.MyTranspose(Strings.Split("Middle of Bin,End of Bin,Empirical CDF", ","), true);
            this.distType = theDistribution;
            this.fitType = theFittingMethod;
            isFitted = false;
        }

        #endregion

        #region Generate Data to a specific distribution with a specific method

        /// <summary>Generates data based on this distribution.</summary>
        /// <param name="X">The original data.</param>
        public DataTable GenData()
        {
            return GenData(1);
        }
        /// <summary>Generates data based on this distribution.</summary>
        /// <param name="X">The original data.</param>
        /// <param name="NumOfSets">The number of generated sets.</param>
        public DataTable GenData(Int32 NumOfSets)
        {
            return GenData(NumOfSets, 0);
        }
        /// <summary>Generates data based on this distribution.</summary>
        /// <param name="X">The original data.</param>
        /// <param name="NumOfSets">The number of generated sets.</param>
        /// <param name="LengthOfSets">The length of generated sets.</param>
        public DataTable GenData(Int32 NumOfSets, Int32 LengthOfSets)
        {
            Int32 i, j;

            // Initialize variables
            if (!isFitted) FitData();
            if (LengthOfSets <= 0) LengthOfSets = X.Length;

            // Initialize random number generator
            Random rng;
            if (this.seed != Int32.MaxValue)
                rng = new Random(this.seed);
            else
                rng = new Random();

            // Loop through each set, and generate the number
            DataTable dt = new DataTable("Generated Data");
            for (j = 0; j < NumOfSets; j++)
            {
                dt.Columns.Add("Set " + j.ToString(), typeof(Double));
                for (i = 0; i < LengthOfSets; i++)
                {
                    Double RndNum = rng.NextDouble();
                    switch (this.distType)
                    {
                        case DistType.distGenExtremeVal:
                            dt.Rows[i][j] = this.GetQuantile(RndNum);
                            break;
                        default:
                            throw new NotImplementedException("Distribution type: " + this.distType.ToString() + " is not implemented.");
                    }
                }
            }
            return dt;
        }

        #endregion

        #region Fit Data to a specific distribution with a specific method

        /// <summary>Fits data to a distribution and fills instance variables within this distribution.</summary>
        /// <param name="X">The data.</param>
        public void FitToDistribution()
        {
            switch (this.distType)
            {
                case DistType.distNormal:
                    FitNormal();
                    break;
                case DistType.distLogNormalII:
                    FitLogNormal(false);
                    break;
                case DistType.distLogNormalIII:
                    FitLogNormal(true);
                    break;
                case DistType.distLogPearsonIII:
                    FitLogPearson();
                    break;
                case DistType.distGenExtremeVal:
                    FitGEV(); 
                    break;
                default:
                    throw new NotImplementedException("Distribution type: " + this.distType.ToString() + " is not implemented in FitToDistribution().");
            }
            this.isFitted = true;
        }
        /// <summary>Fits data to a distribution and gets the quantile at a specified non-exceedence value.</summary>
        /// <param name="X">The data.</param>
        /// <param name="NonExcProb">The non-exceedence probability.</param>
        public Double FitAndGetQuantile(ref Double NonExcProb)
        {
            this.FitToDistribution();
            return this.GetQuantile(NonExcProb);
        }
        /// <summary>Fits data to a distribution and returns a table containing sorted data, PDF, and CDF values.</summary>
        /// <param name="X">The data.</param>
        public DataTable FitData()
        {
            // Fit the data to a distribution
            FitToDistribution();

            // Place the fitted data into a nice format. 
            DataTable dt = new DataTable("Fitted Data PDF and CDF");
            dt.Columns.Add("Sorted Data", typeof(Double));
            dt.Columns.Add("Fitted PDF", typeof(Double));
            dt.Columns.Add("Fitted CDF", typeof(Double));
            for (Int32 i = 0; i < pdf.GetLength(0); i++)
                dt.Rows.Add(new object[] { X[i], pdf[i], cdf[i] });

            // Exit while indicating that the data has been fitted
            return dt;
        }

        #endregion
        #region Integrating functions to get CDF from PDF

        public void GetBounds()
        {
            // Gets the bounds on the variables for the distributions
            switch (this.distType)
            {
                case DistType.distNormal:
                    xMin = Double.NegativeInfinity;
                    xMax = Double.PositiveInfinity;
                    break;
                case DistType.distLogNormalII:
                case DistType.distLogNormalIII:
                    xMin = Xo + Tolerance / 1000;
                    xMax = Double.PositiveInfinity;
                    break;
                case DistType.distLogPearsonIII:
                    k = 1 / Math.Log(logBase);
                    if (alpha >= 0)
                    {
                        xMin = Math.Pow(logBase, (Yo / k));
                        xMax = Double.PositiveInfinity;
                    }
                    else
                    {
                        xMin = Tolerance / 1000;
                        xMax = Math.Pow(logBase, (Yo / k));
                    }
                    break;
                case DistType.distGenExtremeVal:
                    // Explicit form...
                    if (beta == 0)
                    {
                        xMin = Double.NegativeInfinity;
                        xMax = Double.PositiveInfinity;
                    }
                    else if (beta < 0)
                    {
                        xMin = Xo + alpha / beta;
                        xMax = Double.PositiveInfinity;
                    }
                    else
                    {
                        xMin = Double.NegativeInfinity;
                        xMax = Xo + alpha / beta;
                    }
                    break;
                default:
                    throw new NotImplementedException("Distribution type: " + this.distType.ToString() + " is not implemented.");
            }
        }
        public Double[] retrieveCDF()
        {
            if (X == null) throw new NullReferenceException("The data array cannot be null.");

            // Build CDF
            this.GetBounds();
            this.cdf = new Double[X.Length];

            cdf[0] = this.evalIntegral(this.xMin, X[0]);
            for (Int32 i = 1; i < X.Length; i++)
                this.cdf[i] = this.cdf[i - 1] + this.evalIntegral(X[i - 1], X[i]);
            return this.cdf;
        }
        public Double GetQuantile()
        {
            return this.GetQuantile(-999);
        }
        public Double GetQuantile(Double NonExcProb)
        {
            Int32 i = 0;
            Double QVal = 0;
            Double dQ = 0;

            // Define variables
            GetBounds();
            QVal = X[X.Length - 1];
            dQ = X[X.Length - 1] - X[X.Length - 2];

            // Search filled cdf for beginning point and direction
            for (i = 1; i < X.Length; i++)
            {
                if (NonExcProb != -999 && NonExcProb < cdf[i])
                {
                    QVal = X[i];
                    dQ = X[i - 1] - X[i];
                    break;
                }
            }

            // Find quantile with explicit or non-explicit form
            if (0 < NonExcProb && NonExcProb < 1)
            {
                switch (this.distType)
                {
                    // For the distributions that have an explicit quantile value...
                    case DistType.distGenExtremeVal:
                        QVal = this.evalGEV_Quantile(NonExcProb);
                        break;

                    // For the distributions that do not have an explicit quantile value...
                    default:
                        while (!(Math.Abs(dQ) <= Tolerance))
                        {
                            Double F1 = 0;
                            Double F2 = 0;

                            // Evaluate the CDF (the nonexceedence probability) minus the desired quantile value
                            F1 = this.evalIntegral(xMin, QVal) - NonExcProb;
                            F2 = this.evalIntegral(xMin, QVal + dQ) - NonExcProb;

                            // If the guess crosses over the desired value, change the time step.
                            QVal = QVal + dQ;
                            if (F1 * F2 < 0)
                                dQ = -dQ / 2;
                            else if (Math.Abs(F2) > Math.Abs(F1))
                                dQ = -dQ / 2;
                        }
                        break;
                }
                return QVal;
            }
            else
            {
                throw new Exception("Non-exceedence probability " + NonExcProb.ToString() + " needs to be in between 0 and 1.");
            }
        }
        /// <summary>Evaluates the function known as Simpson's Rule.</summary>
        public Double evalSimpsonsRule(Double xMin, Double xMax)
        {
            // Found here: http://en.wikipedia.org/wiki/Adaptive_Simpson%27s_method
            Double xMid = 0;
            Double h = 0;
            xMid = (xMin + xMax) / 2;
            h = Math.Abs(xMax - xMin) / 6;
            return h * (evalPDF(xMin) + 4 * evalPDF(xMid) + evalPDF(xMax));
        }
        public Double evalIntegral_Sub(Double xMin, Double xMax, Double FullIntegral, Double Tolerance, Int32 MaxRecursiveDepth)
        {
            Double functionReturnValue = 0;
            // Recursive implementation of Simpson's rule
            Double xMid = 0, theLeft = 0, theRight = 0;
            xMid = (xMin + xMax) / 2;
            theLeft = evalSimpsonsRule(xMin, xMid);
            theRight = evalSimpsonsRule(xMid, xMax);
            if (MaxRecursiveDepth <= 0 | Math.Abs(theLeft + theRight - FullIntegral) <= 15 * Tolerance)
                functionReturnValue = theLeft + theRight + (theLeft + theRight - FullIntegral) / 15;
            else
                functionReturnValue = evalIntegral_Sub(xMin, xMid, theLeft, Tolerance / 2, MaxRecursiveDepth - 1) + evalIntegral_Sub(xMid, xMax, theRight, Tolerance / 2, MaxRecursiveDepth - 1);
            return functionReturnValue;
        }
        public Double evalIntegral(Double xMin, Double xMax)
        {
            // Uses the recursive implementation of Simpson's rule
            //  found here: http://en.wikipedia.org/wiki/Adaptive_Simpson%27s_method
            return evalIntegral_Sub(xMin, xMax, evalSimpsonsRule(xMin, xMax), Tolerance, MaxRecursiveDepth);
        }

        #endregion
        #region Distribution fitting functions

        // Evaluates any PDF
        /// <summary>Evaluates the PDF of a particular distribution function.</summary>
        /// <param name="X">The value to evaluate.</param>
        public Double evalPDF(Double X)
        {
            // Evaluate the pdf of a function
            switch (this.distType)
            {
                case DistType.distNormal:
                    return evalNormal(X);
                case DistType.distLogNormalII:
                case DistType.distLogNormalIII:
                    return evalLogNormal(X);
                case DistType.distLogPearsonIII:
                    return evalLogPearson(X);
                case DistType.distGenExtremeVal:
                    return evalGEV_PDF(X);
                default:
                    throw new NotImplementedException("Distribution type: " + this.distType.ToString() + " is not implemented.");
            }
        }

        // Individual distributions
        #region Normal Distribution

        public Double evalNormal(Double X)
        {
            return (1 / ((Math.Pow((2 * Math.PI), 0.5)) * stdev)) * Math.Exp(-0.5 * Math.Pow(((X - mean) / stdev), 2));
        }
        public void FitNormal()
        {
            // Returns the PDF for the data
            switch (this.fitType)
            {
                case FitMethod.fitProbWeightedMoments:
                    mean = ProbWeightedMoment(0);
                    stdev = Math.Pow(Math.PI, 0.5) * (2 * ProbWeightedMoment(1) - mean);
                    break;
                default:
                    throw new NotImplementedException("Fitting method: " + this.fitType.ToString() + " is not implemented.");
            }
            this.pdf = new Double[X.Length];

            for (Int32 i = 0; i < X.Length; i++)
                pdf[i] = this.evalNormal(X[i]);
            this.cdf = retrieveCDF();
        }

        #endregion
        #region Lognormal III Distribution

        public Double evalLogNormal(Double X)
        {
            Double k = 0;
            k = 1 / Math.Log(logBase);
            return k / ((Math.Pow((2 * Math.PI), 0.5)) * (X - Xo) * stdev) * Math.Exp(-0.5 * Math.Pow(((Math.Log(X - Xo) / Math.Log(logBase) - mean) / stdev), 2));
        }
        public Double GetXoLogNormal()
        {
            return this.GetXoLogNormal(-999);
        }
        public Double GetXoLogNormal(Double Xo)
        {
            Int32 i = 0, N = X.Length;
            Double a = 0, B = 0, C = 0, d = 0, e = 0, f = 0, k = 1 / Math.Log(logBase);
            Double bigF = 0, bigFprime = 0, tempa = 0, tempb = 0;
            Double Xold = 0;
            if (Xo == -999)
                Xo = Statistics.Min(X) - 5;

            // log base ten
            for (i = 0; i <= N - 1; i++)
            {
                tempa = Math.Pow((X[i] - Xo), (-1));
                tempb = Math.Log(X[i] - Xo, logBase);
                a += tempa;
                B += Math.Pow(tempb, 2);
                C += tempb;
                d += tempa * tempb;
                e += Math.Pow(tempa, 2) * tempb;
                f += Math.Pow(tempa, 2);
            }
            B = B / N;
            C = C / N;
            // Solved using Newton-Raphson method (Press et al. 1986)
            bigF = a * (B - Math.Pow(C, 2) - k * C) + k * d;
            bigFprime = f * (B - Math.Pow(C, 2) - k * C - Math.Pow(k, 2)) + (a * k / N) * (-2 * d + 2 * a * C + k * a) + k * e;
            Xold = Xo;
            Xo = Xo - bigF / bigFprime;
            if (Math.Abs((Xo - Xold) / Xo) <= Tolerance)
                return Xo;
            else
                return GetXoLogNormal(Xo);
        }
        public void FitLogNormal()
        {
            this.FitLogNormal(false);
        }
        public void FitLogNormal(bool TypeIII)
        {
            Int32 i, N = X.Length;
            switch (this.fitType)
            {
                case FitMethod.fitMaxLikelihood:
                    // Xo and the base of the logs
                    if (TypeIII)
                    {
                        logBase = 10;
                        Xo = GetXoLogNormal();
                    }
                    else
                    {
                        logBase = Math.Exp(1);
                        Xo = 0;
                    }

                    // Mean
                    mean = Statistics.LogMoment(X, Xo, 1, logBase);

                    // Standard deviation
                    stdev = 0;
                    for (i = 0; i < N; i++)
                        stdev = stdev + Math.Pow((Math.Log(X[i] - Xo) / Math.Log(logBase) - mean), 2);

                    stdev = Math.Pow(stdev / N, 0.5);
                    break;
                default:
                    throw new NotImplementedException("Fitting method: " + this.fitType.ToString() + " is not implemented.");
            }

            this.pdf = new Double[N];
            for (i = 0; i < N; i++)
                pdf[i] = evalLogNormal(X[i]);
            this.cdf = retrieveCDF();
        }

        #endregion
        #region LogPearson III Distribution

        public Double evalLogPearson(Double X)
        {
            Double k = 0;
            k = 1 / Math.Log(logBase);
            return (k / (Math.Abs(alpha) * GammaFunction(beta) * X)) * (Math.Pow(((Math.Log(X) / Math.Log(logBase) - Yo) / alpha), (beta - 1))) * Math.Exp(-(Math.Log(X) / Math.Log(logBase) - Yo) / alpha);
        }
        public void FitLogPearson()
        {
            // Returns the PDF for the data
            Int32 N = X.Length;
            Double[] Y = null;
            switch (this.fitType)
            {
                case FitMethod.fitIndirectMOM:
                    logBase = Math.E;
                    // use natural log
                    Y = Statistics.Log(X, logBase);
                    mean = Statistics.Avg(Y);
                    stdev = Statistics.StdDev(Y);
                    skew = Statistics.Skewness(Y);
                    alpha = stdev * skew / 2;
                    beta = Math.Pow((2 / skew), 2);
                    Yo = mean - alpha * beta;
                    break;
                default:
                    throw new NotImplementedException("Fitting method: " + this.fitType.ToString() + " is not implemented.");
            }
            this.pdf = new Double[N];
            for (Int32 i = 0; i < N; i++)
                pdf[i] = evalLogPearson(X[i]);
            this.cdf = retrieveCDF(); 
        }

        #endregion
        #region General Extreme Value Distribution

        public Double evalGEV_Quantile(Double NonExcProb)
        {
            return Xo + alpha / beta * (1 - Math.Pow((-Math.Log(NonExcProb)), beta));
        }
        public Double evalGEV_CDF(Double X)
        {
            return Math.Exp(-Math.Pow((1 - beta * (X - Xo) / alpha), (1 / beta)));
        }
        public Double evalGEV_PDF(Double X)
        {
            return evalGEV_CDF(X) / alpha * Math.Pow((1 - beta * (X - Xo) / alpha), (1 / beta - 1));
        }
        private Double BetaDiff(Double B, Double RHS)
        {
            return (1 - Math.Pow(3, (-B))) / (1 - Math.Pow(2, (-B))) - RHS;
        }
        private Double Beta_Right(Double B0, Double B1, Double B2)
        {
            return (3 * B2 - B0) / (2 * B1 - B0);
        }
        public Double retrieveBeta_GEV(Double B0, Double B1, Double B2)
        {
            Double B = 0, dB = 0, C = 0, RHS = 0, F1 = 0, F2 = 0;
            RHS = Beta_Right(B0, B1, B2);
            C = 1 / RHS - Math.Log(2) / Math.Log(3);
            B = 7.859 * C + 2.9554 * Math.Pow(C, 2);
            dB = Tolerance * 100;
            do
            {
                F1 = BetaDiff(B, RHS);
                F2 = BetaDiff(B + dB, RHS);
                if (Math.Abs(F2) < Math.Abs(F1))
                    B = B + dB;
                else
                    dB = -dB / 2;
            } while (!(dB < Tolerance));
            return B;
        }
        public Double[] FitGEV()
        {
            // Returns the PDF for the data
            Int32 N = X.Length;
            Double B0 = 0, B1 = 0, B2 = 0;
            switch (this.fitType)
            {
                case FitMethod.fitProbWeightedMoments:
                    // Beta must be solved for numerically
                    B0 = ProbWeightedMoment(0);
                    B1 = ProbWeightedMoment(1);
                    B2 = ProbWeightedMoment(2);
                    beta = retrieveBeta_GEV(B0, B1, B2);
                    alpha = (2 * B1 - B0) * beta / (GammaFunction(beta + 1) * (1 - Math.Pow(2, (-beta))));
                    Xo = B0 + (alpha / beta) * (GammaFunction(beta + 1) - 1);
                    break;
                default:
                    throw new NotImplementedException("Fitting method: " + this.fitType.ToString() + " is not implemented.");
            }
            this.pdf = new Double[N];
            this.cdf = new Double[N];
            for (Int32 i = 0; i < N; i++)
            {
                pdf[i] = evalGEV_PDF(X[i]);
                cdf[i] = evalGEV_CDF(X[i]);
            }
            return pdf;
        }

        #endregion

        #endregion

        #region Helper methods

        /// <summary>Approximates the Gamma function... Given by Lanczos (1964). Eq. 4.104 in Salas's book.</summary>
        /// <param name="Beta">The parameter for the Gamma function.</param>
        public Double GammaFunction(Double Beta)
        {
            Double Ag = 0;
            Double[] C = new Double[] { 76.18009173, -86.50532033, 24.01409822, -1.231739516, 0.00120858003, -5.36382E-06 };
            for (Int32 i = 0; i <= 5; i++)
                Ag += C[i] / (Beta + i);
            Ag++;
            return Math.Pow((Beta + 4.5), (Beta - 0.5)) * Math.Exp(-Beta - 4.5) * Math.Pow((2 * Math.PI), 0.5) * Ag;
        }
        /// <summary>Calculates a probability weighted moment according to Salas's book.</summary>
        /// <param name="X">The data.</param>
        /// <param name="r">The order of the moment.</param>
        public Double ProbWeightedMoment(Int32 r)
        {
            // Using equation 3.4 from Salas's book
            Int32 N = X.Length;
            Double pwm = 0;
            for (Int32 j = r + 1; j <= N; j++)
                pwm += Statistics.nChooseK(j - 1, r) * X[j - 1];
            return pwm / (N * Statistics.nChooseK(N - 1, r));
        }

        #endregion

        #region Count / Average Crossings

        public bool[] ValsAboveThreshold(Double theThreshold)
        {
            bool[] Xt = new bool[X.Length];
            if (theThreshold == -999)
                theThreshold = Statistics.Avg(X);
            for (Int32 i = 0; i < X.Length; i++)
            {
                if (X[i] >= theThreshold)
                    Xt[i] = true;
                else
                    Xt[i] = false;
            }
            return Xt;
        }
        public Double CrossingRate(Double theThreshold)
        {
            return this.CrossingRate(theThreshold, CrossingType.Crossings_Count);
        }
        public Double CrossingRate(Double theThreshold, CrossingType crossType)
        {
            Double retVal = 0;
            Int32 i = 0;
            bool takeMean = false;
            if (theThreshold == -999)
                theThreshold = Statistics.Avg(X);
            bool[] Xt = ValsAboveThreshold(theThreshold);
            switch (crossType)
            {
                case CrossingType.Crossings_Mean:
                    takeMean = true;
                    crossType = CrossingType.Crossings_Count;
                    break;
                case CrossingType.upCrossings_Mean:
                    takeMean = true;
                    crossType = CrossingType.upCrossings_Count;
                    break;
                case CrossingType.downCrossings_Mean:
                    takeMean = true;
                    crossType = CrossingType.downCrossings_Count;
                    break;
                default:
                    takeMean = false;
                    break;
            }
            switch (crossType)
            {
                case CrossingType.Crossings_Count:
                    for (i = 1; i < Xt.Length; i++)
                        if (Xt[i - 1] != Xt[i])
                            retVal++;
                    break;
                case CrossingType.upCrossings_Count:
                    for (i = 1; i < Xt.Length; i++)
                        if (!Xt[i - 1] && Xt[i])
                            retVal++;
                    break;
                case CrossingType.downCrossings_Count:
                    for (i = 1; i < Xt.Length; i++)
                        if (Xt[i - 1] && !Xt[i])
                            retVal++;
                    break;
            }
            if (takeMean)
                retVal /= X.Length;
            return retVal;
        }

        #endregion

        #region Empirical Distribution Function

        public DataTable Histogram()
        {
            return this.Histogram(0);
        }
        public DataTable Histogram(Int32 NumberOfClasses)
        {
            Int32 i, N = X.Length;
            Int32 cindex = 0;
            Double xMax = 0;
            Double xMin = 0;
            Double dx = 0;

            // Determine the number of classes if a number is not given
            if (NumberOfClasses <= 0)
                NumberOfClasses = Convert.ToInt32(1 + 3.322 * Math.Log(N));

            // Set up the output table 
            DataTable dt = new DataTable();
            dt.Columns.Add("Middle of bin", typeof(Double));
            dt.Columns.Add("End of bin", typeof(Double));
            dt.Columns.Add("Count within bin", typeof(Int32));
            dt.Columns[2].DefaultValue = 0; 
            dt.Columns.Add("Relative frequency distribution", typeof(Double));
            dt.Columns.Add("Empirical CDF", typeof(Double));
            dt.Columns.Add("Empirical PDF", typeof(Double)); 

            // Determine the class interval
            xMax = Statistics.Max(X);
            xMin = Statistics.Min(X);
            dx = (xMax - xMin) / (NumberOfClasses - 1);

            // Determine the class marks
            this.bins_Beg = new Double[NumberOfClasses];
            this.bins_Middle = new Double[NumberOfClasses];
            this.bins_End = new Double[NumberOfClasses];
            for (i = 1; i <= NumberOfClasses; i++)
            {
                DataRow dr = dt.NewRow();
                this.bins_Beg[i - 1] = xMin + (i - 3 / 2) * dx;
                dr[0] = this.bins_Middle[i - 1] = xMin + (i - 1) * dx;
                dr[1] = this.bins_End[i - 1] = xMin + (i - 1 / 2) * dx; 
                dt.Rows.Add(dr); 
            }

            // Count the number of observations that fall within each class interval
            for (i = 0; i < X.Length; i++)
            {
                cindex = Convert.ToInt32((NumberOfClasses - 1) / (xMax - xMin) * (X[i] - xMin) + 1);
                dt.Rows[cindex][2] = (Int32)dt.Rows[cindex][2] + 1;
            }

            // Empirical PDF
            this.epdf = new Double[NumberOfClasses];
            this.ecdf = new Double[NumberOfClasses]; 
            for (i = 1; i <= NumberOfClasses; i++)
            {
                // Determine relative frequencies
                dt.Rows[i][3] = ((Double)dt.Rows[i][2]) / N; 

                // Determine cumulative frequencies
                if (i == 1)
                    dt.Rows[i][4] = this.ecdf[i - 1] = (Double)dt.Rows[i][3];
                else
                    dt.Rows[i][4] = this.ecdf[i - 1] = (Double)dt.Rows[i - 1][4] + (Double)dt.Rows[i][3];

                // Determine empirical density function
                dt.Rows[i][5] = this.epdf[i - 1] = (Double)dt.Rows[i][3] / dx;
            }

            return dt;
        }

        #endregion

    }


}

