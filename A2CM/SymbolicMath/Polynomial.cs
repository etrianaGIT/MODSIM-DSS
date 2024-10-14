using System;
using ASquared.ModelStatistics;

namespace ASquared.SymbolicMath
{
	public static partial class Sym
	{
		public static Int32 MaximumFittedPolynomialOrder = 10;

		#region Polynomial building methods

		/// <summary>Builds a polynomial like so: a0 + a1 * x + a2 * x ^ 2 + ...</summary>
		/// <param name="a">The array of polynomial coefficients</param>
		/// <param name="x">The symbol x.</param>
		public static Symbol BuildPolynomial(Double[] a, Symbol x)
		{
			if (a == null || a.Length == 0) return new Symbol();
			Symbol s = new Symbol(a[0]);
			for (Int32 i = 1; i < a.Length; i++)
				s += a[i] * (x ^ i);
			return s;
		}

		/// <summary>Fits a polynomial to data defined by xVals and yVals.</summary>
		/// <param name="xVals">The abscissa values</param>
		/// <param name="yVals">The ordinate values</param>
		/// <param name="x">The variable used as 'x' in the fitted polynomial</param>
		/// <param name="targetR2">The target coefficient of determination (R-squared) value</param>
		public static Symbol FitPolynomial(Double[] xVals, Double[] yVals, Symbol x, Double targetR2)
		{
			ModelPerformance perf;
			return FitPolynomial(xVals, yVals, x, targetR2, out perf);
		}
		
		/// <summary>Fits a polynomial to data defined by xVals and yVals.</summary>
		/// <param name="xVals">The abscissa values</param>
		/// <param name="yVals">The ordinate values</param>
		/// <param name="x">The variable used as 'x' in the fitted polynomial</param>
		/// <param name="targetR2">The target coefficient of determination (R-squared) value</param>
		/// <param name="perf">Defines the performance of the fitted polynomial</param>
		public static Symbol FitPolynomial(Double[] xVals, Double[] yVals, Symbol x, Double targetR2, out ModelPerformance perf)
		{
			for (Int32 order = 0; order <= MaximumFittedPolynomialOrder; order++)
			{
				Symbol poly = FitPolynomial(xVals, yVals, x, order);
				perf = new ModelPerformance(yVals, poly.Eval(xVals));
				if (perf.Rsquared() >= targetR2)
					return poly;
			}
			throw new Exception("Could not match the target R² with any polynomial order up to " + MaximumFittedPolynomialOrder.ToString());
		}
		
		/// <summary>Fits a polynomial to data defined by xVals and yVals.</summary>
		/// <param name="xVals">The abscissa values</param>
		/// <param name="yVals">The ordinate values</param>
		/// <param name="x">The variable used as 'x' in the fitted polynomial</param>
		/// <param name="order">The specified order of the fitted polynomial</param>
		public static Symbol FitPolynomial(Double[] xVals, Double[] yVals, Symbol x, Int32 order)
		{
            if (xVals == null || yVals == null) return new Symbol(); 
			if (xVals.Length != yVals.Length) throw new ArgumentException("xVals and yVals need to have the same number of elements in them.");
            if (xVals.Length == 0) return new Symbol(); 
			Matrix X = xVals;
			Matrix y = yVals;

			// Build columns (first column of 1's) 
			Matrix CurrCol = new Matrix(xVals.Length, 1.0);
			Matrix A = CurrCol;
			for (Int32 j = 0; j < order; j++)
				A.AppendCols(CurrCol = CurrCol.ElementwiseMult(X));

			// Transpose 
			Matrix T = A.Transpose();

			// Least squares approx. fitting method
			Double[] a = (T * A).Inverse() * T * y;
			return BuildPolynomial(a, x);
		}
		
		/// <summary>Returns a string showing a fitted polynomial and its goodness of fit.</summary>
		/// <param name="xVals">The abscissa values</param>
		/// <param name="yVals">The ordinate values</param>
		/// <param name="x">The variable used as 'x' in the fitted polynomial</param>
		/// <param name="targetR2">The target coefficient of determination (R-squared) value</param>
		public static string PolynomialGoodnessOfFit(Double[] xVals, Double[] yVals, Symbol x, Double targetR2)
		{
			if (xVals.Length != yVals.Length) throw new Exception("The length in each of the input arrays must be the same.");
			Symbol p = FitPolynomial(xVals, yVals, x, targetR2);
			ModelPerformance perf = new ModelPerformance(yVals, p.Eval(xVals));
			return "Goodness of fit for " + p.ToString() + ": \n" + perf.ToString();
		}
		
		/// <summary>Returns a string showing a fitted polynomial and its goodness of fit.</summary>
		/// <param name="xVals">The abscissa values</param>
		/// <param name="yVals">The ordinate values</param>
		/// <param name="x">The variable used as 'x' in the fitted polynomial</param>
		/// <param name="order">The specified order of the fitted polynomial</param>
		public static string PolynomialGoodnessOfFit(Double[] xVals, Double[] yVals, Symbol x, Int32 order)
		{
			if (xVals.Length != yVals.Length) throw new Exception("The length in each of the input arrays must be the same.");
			Symbol p = FitPolynomial(xVals, yVals, x, order);
			ModelPerformance perf = new ModelPerformance(yVals, p.Eval(xVals));
			return "Goodness of fit for " + p.ToString() + ": \n" + perf.ToString();
		}
		
		/// <summary>Returns a string showing a fitted polynomial and its goodness of fit.</summary>
		/// <param name="xVals">The abscissa values</param>
		/// <param name="yVals">The ordinate values</param>
		/// <param name="x">The variable used as 'x' in the fitted polynomial</param>
		/// <param name="targetR2">The target coefficient of determination (R-squared) value</param>
		public static string PolynomialGoodnessOfFit(Double[] xVals, Double[] yVals, Symbol fxn, Symbol x)
		{
			if (xVals.Length != yVals.Length) throw new Exception("The length in each of the input arrays must be the same.");
			ModelPerformance perf = new ModelPerformance(yVals, fxn.Eval(xVals));
			return "Goodness of fit for " + fxn.ToString() + ": \n" + perf.ToString();
		}

		#endregion
	}
}