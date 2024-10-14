using System;
using System.Linq;

namespace ASquared.SymbolicMath
{
    public partial class Symbol
	{
		#region Error Symbol Methods

		private Double Error_Evaluate(Double val)
		{
			return Double.NaN;
		}

		private Symbol Error_Substitute(String[] variables, Object[] values)
		{
			return this;
		}

		private Symbol Error_Differentiate(String variable)
		{
			return this;
		}

		private String Error_Output()
		{
			return "{Error}";
		}

		#endregion

		#region Constant Symbol Methods

		private Double Constant_Evaluate(Double val)
        {
            return _constVal;
        }

        private Symbol Constant_Substitute(String[] variables, Object[] values)
        {
            return this;
        }

        private Symbol Constant_Differentiate(String variable)
        {
            return 0;
        }

        private String Constant_Output()
        {
            // Check if this constant is a special one
            if (_constants.ContainsValue(_constVal))
                return _constants.First(c => c.Value == _constVal).Key;

            return _constVal.ToString();
        }

        #endregion

        #region Negate Symbol Methods

        private Double Negate_Evaluate(Double value)
        {
            return -_operand.Eval(value);
        }

        private Symbol Negate_Substitute(String[] variables, Object[] values)
        {
            return -_operand.Subs(variables, values);
        }

        private Symbol Negate_Differentiate(String variable)
        {
            return -Sym.Diff(_operand, variable);
        }

        private String Negate_Output()
        {
            return "(-" + _operand.ToStringInternal() + ")";
        }

        #endregion

        #region Variable Symbol Methods

        private Double Variable_Evaluate(Double value)
        {
            return value;
        }

        private Symbol Variable_Substitute(String[] variables, Object[] values)
        {
            // search for a matching variable in variable list
            for (Int32 v = 0; v < variables.Length; v++)
            {
                // perform suitable substitution based on object type
                if (variables[v] == _varVal)
                {
                    Object val = values[v];
                    Type objType = values[v].GetType();

                    if (val is Char || val is String)
                        return new Symbol(val.ToString());
                    else if (val is Symbol)
                        return (Symbol)val;
                    else
                        return new Symbol(Convert.ToDouble(val));  // assume it's a number type
                }
            }

            // if none found, leave unevaluated
            return this;
        }

        private Symbol Variable_Differentiate(String variable)
        {
            if (variable == string.Empty || variable == _varVal)
                return 1;

            return 0;  // treat as constant
        }

        private String Variable_Output()
        {
            return _varVal.ToString();
        }

        #endregion

        #region Add Symbol Methods

        private Double Add_Evaluate(Double value)
        {
            return _operand.Eval(value) + _rhs.Eval(value);
        }

        private Symbol Add_Substitute(String[] variables, Object[] values)
        {
            return _operand.Subs(variables, values) + _rhs.Subs(variables, values);
        }

        private Symbol Add_Differentiate(String variable)
        {
            return Sym.Diff(_operand, variable) + Sym.Diff(_rhs, variable);
        }

        private String Add_Output()
        {
            return "(" + _operand.ToString() + " + " + _rhs.ToString() + ")";
        }

        #endregion

        #region Subtract Symbol Methods

        private Double Subtract_Evaluate(Double value)
        {
            return _operand.Eval(value) - _rhs.Eval(value);
        }

        private Symbol Subtract_Substitute(String[] variables, Object[] values)
        {
            return _operand.Subs(variables, values) - _rhs.Subs(variables, values);
        }

        private Symbol Subtract_Differentiate(String variable)
        {
			return Sym.Diff(_operand, variable) - Sym.Diff(_rhs, variable);
        }

        private String Subtract_Output()
        {
            return "(" + _operand.ToString() + " - " + _rhs.ToStringInternal() + ")";
        }

        #endregion

        #region Multiply Symbol Methods

        private Double Multiply_Evaluate(Double value)
        {
            return _operand.Eval(value) * _rhs.Eval(value);
        }

        private Symbol Multiply_Substitute(String[] variables, Object[] values)
        {
            return _operand.Subs(variables, values) * _rhs.Subs(variables, values);
        }

        private Symbol Multiply_Differentiate(String variable)
        {
			return _operand * Sym.Diff(_rhs, variable) + Sym.Diff(_operand, variable) * _rhs;
        }

        private String Multiply_Output()
        {
            return "(" + _operand.ToStringInternal() + " * " + _rhs.ToStringInternal() + ")";
        }

        #endregion

        #region Divide Symbol Methods

        private Double Divide_Evaluate(Double value)
        {
            return _operand.Eval(value) / _rhs.Eval(value);
        }

        private Symbol Divide_Substitute(String[] variables, Object[] values)
        {
            return _operand.Subs(variables, values) / _rhs.Subs(variables, values);
        }

        private Symbol Divide_Differentiate(String variable)
        {
			return (Sym.Diff(_operand, variable) * _rhs - Sym.Diff(_rhs, variable) * _operand) / (_rhs ^ 2);
        }

        private String Divide_Output()
        {
            String rhsStr = _rhs.ToStringInternal();

            return "(" + _operand.ToStringInternal() + " / " + _rhs.ToStringInternal() + ")";
        }

        #endregion

        #region Power Symbol Methods

        private Double Power_Evaluate(Double value)
        {
            return Math.Pow(_operand.Eval(value), _rhs.Eval(value));
        }

        private Symbol Power_Substitute(String[] variables, Object[] values)
        {
            return _operand.Subs(variables, values) ^ _rhs.Subs(variables, values);
        }

        private Symbol Power_Differentiate(String variable)
        {
			return (_operand ^ _rhs) * Sym.Diff(_rhs, variable) * Sym.Ln(_operand) + _rhs * (_operand ^ (_rhs - 1)) * Sym.Diff(_operand, variable);
        }

        private String Power_Output()
        {
            return "(" + _operand.ToStringInternal() + "^" + _rhs.ToStringInternal() + ")";
        }

        #endregion

        #region Log Symbol Methods

        private Double Log_Evaluate(Double value)
        {
            return Math.Log(_operand.Eval(value), _rhs.Eval(value));
        }

        private Symbol Log_Substitute(String[] variables, Object[] values)
        {
			return Sym.Log(_operand.Subs(variables, values), _rhs.Subs(variables, values));
            //return new Symbol(_val.Subs(variables, values), SymbolType.Log, _rhs.Subs(variables, values));
        }

        private Symbol Log_Differentiate(String variable)
        {
			return Sym.Diff(Sym.Ln(_operand) / Sym.Ln(_rhs), variable);
        }

        private String Log_Output()
        {
            return "log(" + _operand.ToString() + ", " + _rhs.ToString() + ")";
        }

        #endregion

        #region Natural Log Symbol Methods

        private Double NaturalLog_Evaluate(Double value)
        {
            return Math.Log(_operand.Eval(value));
        }

        private Symbol NaturalLog_Substitute(String[] variables, Object[] values)
        {
			return Sym.Ln(Subs(variables, values));
        }

        private Symbol NaturalLog_Differentiate(String variable)
        {
			return (1 / _operand) * Sym.Diff(_operand, variable);
        }

        private String NaturalLog_Output()
        {
            return "ln(" + _operand.ToStringInternal() + ")";
        }

        #endregion

        #region Exponential Symbol Methods

        private Double Exponential_Evaluate(Double value)
        {
            return Math.Exp(_operand.Eval(value));
        }

        private Symbol Exponential_Substitute(String[] variables, Object[] values)
        {
			return Sym.Exp(_operand.Subs(variables, values));
        }

        private Symbol Exponential_Differentiate(String variable)
        {
			return this * Sym.Diff(_operand, variable);
        }

        private String Exponential_Output()
        {
            return "exp(" + _operand.ToString() + ")";
        }

        #endregion

		#region Parser Handlers

		private static Object Handler_Log(Object[] args)
		{
			return Sym.Log((Symbol)args[0], (Symbol)args[1]);
		}

		private static Object Handler_Ln(Object[] args)
		{
			return Sym.Ln((Symbol)args[0]);
		}

		private static Object Handler_Exp(Object[] args)
		{
			return Sym.Exp((Symbol)args[0]);
		}

		#endregion
    }
    
    public static partial class Sym
	{
		#region Symbol Type Functions

		public static Symbol Log(Symbol operand, Symbol logBase)
		{
			if (operand.SymbolType == SymbolType.Constant && operand.ToNumber() == 1)
				return 0;

			return new Symbol(operand, SymbolType.Log, logBase);
		}

		public static Symbol Log(Symbol operand)
		{
			return Log(operand, 10);
		}

		public static Symbol Ln(Symbol operand)
		{
			if (operand.SymbolType == SymbolType.Constant && operand.ToNumber() == 1)
				return 0;

			return new Symbol(operand, SymbolType.NaturalLog);
		}

		public static Symbol Exp(Symbol operand)
		{
			if (operand.IsZero())
				return 1;

			return new Symbol(operand, SymbolType.Exponential);
		}

		#endregion
	}
}