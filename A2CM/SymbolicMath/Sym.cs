using System;

namespace ASquared.SymbolicMath
{
    public static partial class Sym
    {
		#region Mathematical Methods

		// finds the given-order derivative for the given variable.
		// omit arguments to do a first-order derivative, treating
		// all variabls the same.
		public static Symbol Diff(Symbol operand, String variable, Int32 order)
		{
			return operand.Diff(variable, order);
		}

		public static Symbol Diff(Symbol operand)
		{
			return Diff(operand, String.Empty, 1);
		}

		public static Symbol Diff(Symbol operand, String variable)
		{
			return Diff(operand, variable, 1);
		}

		public static Symbol Diff(Symbol operand, Int32 order)
		{
			return Diff(operand, String.Empty, order);
		}

        /// <summary>Calculates the gradient of a symbolic math object.</summary>
        /// <param name="operand">The math object from which to calculate the gradient.</param>
        /// <param name="variables">The variables to include in the gradient.</param>
        public static Symbol[] Gradient(Symbol operand, String[] variables)
        {
            return Array.ConvertAll(variables, variable => Diff(operand, variable));
        }

		public static Symbol Root(Symbol operand, Symbol root)
		{
			return operand ^ (1 / root);
		}

		public static Symbol SquareRoot(Symbol operand)
		{
			return Root(operand, 2);
		}

		public static Symbol CubeRoot(Symbol operand)
		{
			return Root(operand, 3);
		}

		#endregion
	}
}