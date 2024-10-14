using System;
using System.Collections.Generic;

/* In this class partition, new symbol types can be created.
* 
* Every symbol method:
* 
*   Evaluate		takes:	 Double value
*					returns: Double
*					
*   Substitue		takes:	 String[] variables, Object[] values;
*					   values can be number, Symbol, or Char/String
*					returns: Symbol
*					
*   Differentiate   takes:   String variable
*					returns: Symbol
*					
*   Output			takes:   void
*					returns: String
* 
* must be private and implemented for every symbol type.
* The header for each method must be of the form:
* 
*      SymbolType_SymbolMethod(parameters)
* 
* In addition all new symbol types must:
* 
*    - Be added to the SymbolType enum list above
*    
*	  - If a function is to be associated with the new
*	    symbol type (such as 'Sin()'), the following must be met:
*    
*			> Designate a string to be used for parsing by having
*			  a key in the dictionary below that designates the
*			  number of arguments the function expects
*			  
*			> Designate a public static method in the region
*			  "Symbol Type Functions" below that performs
*			  the desired function
*			  
*			> Designate a private static handler of the form:
*			
*			  Object Handler_{Functionname}(Object[] args)
*			  
*			  in the "Parser Handlers" region below that wraps
*			  its respective function method, casting the
*			  objects in 'args' appropriately
*			  
*			  Note: {Functionname} designates the expected
*			  function name as one word with the first
*			  letter capitalized
* 
* It is best to group methods for a symbol type together
* by wrapping them in an appropriately-named #region.
*/

namespace ASquared.SymbolicMath
{
	public enum SymbolType
	{
		// Built-in
		Null,
		Error,
		Constant,
		Negate,
		Variable,
		Add,
		Subtract,
		Multiply,
		Divide,
		Power,
		Log,
		NaturalLog,
		Exponential,

		// Extensions
		Sine,
		Cosine,
		Tangent,
		Cosecant,
		Secant,
		Cotangent
	};

    public partial class Symbol
    {
		private enum SymbolMethod
		{
			Evaluate,
			Substitute,
			Differentiate,
			Output
		};

		private static Dictionary<String, Int32> _functions = new Dictionary<String, Int32>
		{
			// Built-in
			{"log",  2},
			{"ln",	 1},
			{"exp",  1},
			{"neg",  1},
			{"diff", 3},
			{"root", 2},
			{"sqrt", 1},
			{"cbrt", 1},

			// Extensions
			{"sin",	 1},
			{"cos",  1},
			{"tan",  1},
			{"csc",  1},
			{"sec",  1},
			{"cot",  1}
		};

        #region Sine Symbol Methods

        private Double Sine_Evaluate(Double value)
        {
            return Math.Sin(_operand.Eval(value));
        }

        private Symbol Sine_Substitute(String[] variables, Object[] values)
        {
            return Sym.Sin(_operand.Subs(variables, values));
        }

        private Symbol Sine_Differentiate(String variable)
        {
			return Sym.Cos(_operand) * Sym.Diff(_operand, variable);
        }

        private String Sine_Output()
        {
            return "sin(" + _operand.ToString() + ")";
        }

        #endregion

        #region Cosine Symbol Methods

        private Double Cosine_Evaluate(Double value)
        {
            return Math.Cos(_operand.Eval(value));
        }

        private Symbol Cosine_Substitute(String[] variables, Object[] values)
        {
			return Sym.Cos(_operand.Subs(variables, values));
        }

        private Symbol Cosine_Differentiate(String variable)
        {
            return -Sym.Sin(_operand) * Sym.Diff(_operand, variable);
        }

        private String Cosine_Output()
        {
            return "cos(" + _operand.ToString() + ")";
        }

        #endregion

        #region Tangent Symbol Methods

        private Double Tangent_Evaluate(Double value)
        {
            return Math.Tan(_operand.Eval(value));
        }

        private Symbol Tangent_Substitute(String[] variables, Object[] values)
        {
			return Sym.Tan(_operand.Subs(variables, values));
        }

        private Symbol Tangent_Differentiate(String variable)
        {
            return (Sym.Sec(_operand) ^ 2) * Sym.Diff(_operand, variable);
        }

        private String Tangent_Output()
        {
            return "tan(" + _operand.ToString() + ")";
        }

        #endregion

        #region Cosecant Symbol Methods

        private Double Cosecant_Evaluate(Double value)
        {
            return 1 / Math.Sin(_operand.Eval(value));
        }

        private Symbol Cosecant_Substitute(String[] variables, Object[] values)
        {
			return Sym.Csc(_operand.Subs(variables, values));
        }

        private Symbol Cosecant_Differentiate(String variable)
        {
			return -this * Sym.Cot(_operand) * Sym.Diff(_operand, variable);
        }

        private String Cosecant_Output()
        {
            return "csc(" + _operand.ToString() + ")";
        }

        #endregion

        #region Secant Symbol Methods

        private Double Secant_Evaluate(Double value)
        {
            return 1 / Math.Cos(_operand.Eval(value));
        }

        private Symbol Secant_Substitute(String[] variables, Object[] values)
        {
			return Sym.Sec(_operand.Subs(variables, values));
        }

        private Symbol Secant_Differentiate(String variable)
        {
			return this * Sym.Tan(_operand) * Sym.Diff(_operand, variable);
        }

        private String Secant_Output()
        {
            return "sec(" + _operand.ToString() + ")";
        }

        #endregion

        #region Cotangent Symbol Methods

        private Double Cotangent_Evaluate(Double value)
        {
            return 1 / Math.Tan(_operand.Eval(value));
        }

        private Symbol Cotangent_Substitute(String[] variables, Object[] values)
        {
			return Sym.Cot(_operand.Subs(variables, values));
        }

        private Symbol Cotangent_Differentiate(String variable)
        {
			return -(Sym.Csc(_operand) ^ 2) * Sym.Diff(_operand, variable);
        }

        private String Cotangent_Output()
        {
            return "cot(" + _operand.ToString() + ")";
        }

        #endregion

		#region Parser Handlers

		private static Object Handler_Sin(Object[] args)
		{
			return Sym.Sin((Symbol)args[0]);
		}

		private static Object Handler_Cos(Object[] args)
		{
			return Sym.Cos((Symbol)args[0]);
		}

		private static Object Handler_Tan(Object[] args)
		{
			return Sym.Tan((Symbol)args[0]);
		}

		private static Object Handler_Csc(Object[] args)
		{
			return Sym.Csc((Symbol)args[0]);
		}

		private static Object Handler_Sec(Object[] args)
		{
			return Sym.Sec((Symbol)args[0]);
		}

		private static Object Handler_Cot(Object[] args)
		{
			return Sym.Cot((Symbol)args[0]);
		}

		#endregion
    }

	public static partial class Sym
	{
		#region Symbol Type Functions

		public static Symbol Sin(Symbol operand)
		{
			return new Symbol(operand, SymbolType.Sine);
		}

		public static Symbol Cos(Symbol operand)
		{
			return new Symbol(operand, SymbolType.Cosine);
		}

		public static Symbol Tan(Symbol operand)
		{
			return new Symbol(operand, SymbolType.Tangent);
		}

		public static Symbol Csc(Symbol operand)
		{
			return new Symbol(operand, SymbolType.Cosecant);
		}

		public static Symbol Sec(Symbol operand)
		{
			return new Symbol(operand, SymbolType.Secant);
		}

		public static Symbol Cot(Symbol operand)
		{
			return new Symbol(operand, SymbolType.Cotangent);
		}
		#endregion
	}
}