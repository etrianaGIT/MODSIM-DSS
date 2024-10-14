using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ASquared.SymbolicMath
{
    public partial class Symbol
    {
        #region Static Members

        private static Dictionary<String, Double> _constants = new Dictionary<String, Double>
		{
			{"_e",  A2.E },
			{"_pi", A2.Pi }
		};

        private static Dictionary<String, SymbolType> _operators = new Dictionary<String, SymbolType>
		{
			{"+", SymbolType.Add},
			{"-", SymbolType.Subtract},
			{"*", SymbolType.Multiply},
			{"/", SymbolType.Divide},
			{"^", SymbolType.Power}
		};

        #endregion

        #region Instance Variables

        private Symbol _operand;   // symbol's primary (left-hand side) operand
        private Double _constVal;  // valid only for constant types
        private String _varVal;	   // valid only for variable types
        private SymbolType _type;  // type of this symbol
        private Symbol _rhs;       // valid only for binary operator types

        #endregion

        #region Properties

        public SymbolType SymbolType
        {
            get
            {
                return _type;
            }
        }

        #endregion

        #region Lifecycle Methods

        public Symbol()
            : this(0)
        { }

        public Symbol(Double val)
        {
            if (val < 0)
            {
                _operand = -val;
                _type = SymbolType.Negate;
            }
            else
            {
                _constVal = val;
                _type = SymbolType.Constant;
            }
        }

        public Symbol(Char val)
            : this(val.ToString())
        { }

        public Symbol(String val)
        {
            _varVal = val;
            _type = SymbolType.Variable;
        }

        public Symbol(Symbol val, SymbolType type)
            : this(val, type, null)
        { }

        public Symbol(Symbol val, SymbolType type, Symbol rhs)
        {
            _operand = val;
            _type = type;
            _rhs = rhs;
        }

        #endregion

        #region Conversion Operators

        public static implicit operator Symbol(Double val)
        {
            return new Symbol(val);
        }

        public static implicit operator Symbol(Char val)
        {
            return new Symbol(val);
        }

        public static implicit operator Symbol(String val)
        {
            return new Symbol(val);
        }

        #endregion

        #region Math Operators

        public static Symbol operator +(Symbol lhs, Symbol rhs)
        {
            if (lhs._type == SymbolType.Constant && rhs._type == SymbolType.Constant)
                return new Symbol(lhs._constVal + rhs._constVal);

            if (lhs._type == SymbolType.Negate && rhs._type != SymbolType.Negate)
                return rhs - lhs._operand;

            if (rhs._type == SymbolType.Negate)
                return lhs - rhs._operand;

            if (lhs.IsZero())
                return rhs;

            if (rhs.IsZero())
                return lhs;

            return new Symbol(lhs, SymbolType.Add, rhs);
        }

        public static Symbol operator -(Symbol lhs, Symbol rhs)
        {
            if (lhs._type == SymbolType.Constant && rhs._type == SymbolType.Constant)
                return new Symbol(lhs._constVal - rhs._constVal);

            if (rhs._type == SymbolType.Negate)
                return lhs + rhs._operand;

            if (lhs.IsZero())
                return -rhs;

            if (rhs.IsZero())
                return lhs;

            return new Symbol(lhs, SymbolType.Subtract, rhs);
        }

        public static Symbol operator -(Symbol val)
        {
            if (val.IsZero())
                return 0;

            // Double-negative
            if (val._type == SymbolType.Negate)
                return val._operand;

            return new Symbol(val, SymbolType.Negate);
        }

        public static Symbol operator *(Symbol lhs, Symbol rhs)
        {
            if (lhs.IsZero() || rhs.IsZero())
                return 0;

            if (lhs._type == SymbolType.Constant && lhs._constVal == 1)
                return rhs;

            if (rhs._type == SymbolType.Constant && rhs._constVal == 1)
                return lhs;

            if (lhs._type == SymbolType.Negate && rhs._type != SymbolType.Negate)
                return -(lhs._operand * rhs);

            if (lhs._type != SymbolType.Negate && rhs._type == SymbolType.Negate)
                return -(lhs * rhs._operand);

            if (lhs._type == SymbolType.Negate && rhs._type == SymbolType.Negate)
                return lhs._operand * rhs._operand;

            return new Symbol(lhs, SymbolType.Multiply, rhs);
        }

        public static Symbol operator /(Symbol lhs, Symbol rhs)
        {
            if (rhs._type == SymbolType.Constant && rhs._constVal == 1)
                return lhs;

            if (lhs._type == SymbolType.Negate && rhs._type != SymbolType.Negate)
                return -(lhs._operand / rhs);

            if (lhs._type != SymbolType.Negate && rhs._type == SymbolType.Negate)
                return -(lhs / rhs._operand);

            if (lhs._type == SymbolType.Negate && rhs._type == SymbolType.Negate)
                return lhs._operand / rhs._operand;

            if (rhs.IsZero())
                return Double.PositiveInfinity;

            if (lhs.IsZero())
                return 0;

            return new Symbol(lhs, SymbolType.Divide, rhs);
        }

        public static Symbol operator ^(Symbol val, Symbol exp)
        {
            if (val.IsZero())
                return 0;

            if (val._type == SymbolType.Constant && val._constVal == 1)
                return 1;

            if (exp.IsZero())
                return 1;

            if (exp._type == SymbolType.Constant && exp._constVal == 1)
                return val;

            return new Symbol(val, SymbolType.Power, exp);
        }

        // finds the first derivative of the single-variable symbol.
        // treats all variables the same, regardless of name.
        // for multivariate and/or higher-order derivatives, use method
        // Diff(String variable, Int32 order).
        public static Symbol operator ~(Symbol operand)
        {
            return Sym.Diff(operand);
        }

        public static bool operator ==(Symbol lhs, Symbol rhs)
        {
            if ((Object)lhs == null && (Object)rhs == null) return true; 
            else if ((Object)lhs == null || (Object)rhs == null) return false;
            return lhs._constVal == rhs._constVal
                && lhs._type == rhs._type
                && lhs._varVal == rhs._varVal 
                && lhs._operand == rhs._operand
                && lhs._rhs == rhs._rhs; 
        }

        public static bool operator !=(Symbol lhs, Symbol rhs)
        {
            return !(lhs == rhs); 
        }

        #endregion

        #region Mathematical Methods

        internal Symbol Diff(String variable, Int32 order)
        {
            Symbol deriv = this;

            if (order < 1)
                return deriv;

            for (Int32 n = 0; n < order; n++)
            {
                deriv = (Symbol)(deriv.RunMethod(deriv.SymbolType, Symbol.SymbolMethod.Differentiate, new Object[] { variable }));
            }

            return deriv;
        }

        // evaluates symbolic variable at given value or array of values (corresponding to each of the variables in the function).
        // 'value' gets substituted for every variable, regardless of name.
        // use to convert constant expression to Double by omitting 'value'.
        [CLSCompliant(false)]
        public Double Eval()
        {
            return Eval(0);
        }
        [CLSCompliant(false)]
        public Double Eval(Double value)
        {
            return (Double)RunMethod(SymbolMethod.Evaluate, new Object[] { value });
        }

        /// <summary>Evaluates the current instance specifying specific values for each variable (the user must specify every variable found within this instance).</summary>
        /// <param name="variables">The variables that the nested arrays in the values array are assigned to (in order)</param>
        /// <param name="values">Array of realizations. Each nested array represents one substitution of variables into the associated variables</param>
        [CLSCompliant(false)]
        public Double Eval(String[] variables, Double[] values)
        {
            return Subs(variables, values).ToNumber();
        }

        /// <summary>Evaluates the current instance for multiple realizations (substitutes each realization value in for every variable).</summary>
        /// <param name="values">Array of realizations.</param>
        [CLSCompliant(false)]
        public Double[] Eval(Double[] values)
        {
            return Array.ConvertAll(values, value => Eval(value));
        }

        /// <summary>Evaluates the current instance for multiple realizations (the user must specify every variable found within this instance).</summary>
        /// <param name="variables">The variables that the nested arrays in the values array are assigned to (in order)</param>
        /// <param name="values">Array of realizations. Each nested array represents one substitution of variables into the associated variables</param>
        [CLSCompliant(false)]
        public Double[] Eval(String[] variables, Double[][] values)
        {
            return Array.ConvertAll(values, value => Eval(variables, value));
        }

        /// <summary>Evaluates the current instance for multiple functions (the user must specify every variable found within this instance).</summary>
        /// <param name="fxns">An array of various functions.</param>
        /// <param name="variables">The variables that the nested arrays in the values array are assigned to (in order)</param>
        /// <param name="values">Array of realizations. Each nested array represents one substitution of variables into the associated variables</param>
        [CLSCompliant(false)]
        public static Double[] Eval(Symbol[] fxns, String[] variables, Double[][] values)
        {
            if (fxns.Length != values.Length || fxns.Length == 0)
                throw new Exception("The length of the values array must have the same length as the fxns array and there must be at least one function to evaluate.");
            if (variables.Length != values[0].Length)
                throw new Exception("The length of the variables array must be the same length as each nested array within the values array.");

            Double[] retVal = new Double[fxns.Length];
            for (Int32 i = 0; i < fxns.Length; i++)
                retVal[i] = fxns[i].Eval(variables, values[i]);
            return retVal;
        }

        /// <summary>Evaluates the current instance for multiple functions (the user must specify every variable found within this instance).</summary>
        /// <param name="fxns">An array of various functions.</param>
        /// <param name="variables">The variables that the values array are assigned to (in order)</param>
        /// <param name="values">Array of values used in each realization.</param>
        [CLSCompliant(false)]
        public static Double[] Eval(Symbol[] fxns, String[] variables, Double[] values)
        {
            if (fxns.Length != values.Length || fxns.Length == 0)
                throw new Exception("The length of the values array must have the same length as the fxns array and there must be at least one function to evaluate.");
            if (variables.Length != values.Length)
                throw new Exception("The length of the variables array must be the same length as each nested array within the values array.");

            return Array.ConvertAll(fxns, fxn => fxn.Eval(variables, values));
        }

        // evaluates symbol for given variables at their corresponding values.
        // objects in 'values' can be Doubles, Strings/Chars representing other variable names, or other Symbols.
        // returns a new Symbol object, which can be converted to a Double by subsequently calling 'Eval(void)'
        // if all variables were substituted for Doubles.
        [CLSCompliant(false)]
        public Symbol Subs(String[] variables, Object[] values)
        {
            return (Symbol)RunMethod(SymbolMethod.Substitute, new Object[] { variables, values });
        }
        [CLSCompliant(false)]
        public Symbol Subs(String[] variables, Double[] values)
        {
            return Subs(variables, Array.ConvertAll(values, val => (Object)val));
        }
        [CLSCompliant(false)]
        public Symbol Subs(String[] variables, Symbol s)
        {
            return Subs(variables, Array.ConvertAll<String, Object>(variables, v => s));
        }

        // overload 'Subs()' for a single variable and a single Double
        [CLSCompliant(false)]
        public Symbol Subs(String variable, Double value)
        {
            return Subs(new String[] { variable }, new Object[] { value });
        }

        // overload 'Subs()' for a single variable and a single String
        [CLSCompliant(false)]
        public Symbol Subs(String variable, String value)
        {
            return Subs(new String[] { variable }, new Object[] { value });
        }

        // overload 'Subs()' for a single variable and a single Symbol
        [CLSCompliant(false)]
        public Symbol Subs(String variable, Symbol value)
        {
            return Subs(new String[] { variable }, new Object[] { value });
        }

        /// <summary>Substitutes values in for variables for multiple realizations.</summary>
        /// <param name="variables">The variables that the nested arrays in the values array are assigned to (in order)</param>
        /// <param name="values">Array of realizations. Each nested array represents one substitution of variables into the associated variables</param>
        [CLSCompliant(false)]
        public Symbol[] Subs(String[] variables, Object[][] values)
        {
            return Array.ConvertAll(values, value => Subs(variables, value));
        }

        /// <summary>Substitutes values in for variables for multiple realizations.</summary>
        /// <param name="variables">The variables that the nested arrays in the values array are assigned to (in order)</param>
        /// <param name="values">Array of realizations. Each nested array represents one substitution of variables into the associated variables</param>
        [CLSCompliant(false)]
        public Symbol[] Subs(String[] variables, Double[][] values)
        {
            return Array.ConvertAll(values, value => Subs(variables, value));
        }

        #endregion

        #region Output Methods

        public static String operator +(String s, Symbol rhs)
        {
            if (rhs == null) return s;
            return s + rhs.ToString();
        }

        public static String operator +(Symbol lhs, String s)
        {
            if (lhs == null) return s;
            return lhs.ToString() + s;
        }

        // Displays symbolic expression
        public override String ToString()
        {
            String output = ToStringInternal();

            // remove extraneous outer parentheses
            if (output.First() == '(')
            {
                Int32 paren = 0;
                for (Int32 i = 0; i < output.Length; i++)
                {
                    Char c = output[i];

                    switch (c)
                    {
                        case '(': paren++; break;
                        case ')': paren--; break;
                        default: continue;
                    }

                    // Matching brace found.
                    // If at end of string, return string with outer
                    // parentheses removed.
                    // If in middle of string, return original string.
                    if (paren == 0)
                    {
                        if (i == output.Length - 1)
                            return output.Substring(1, output.Length - 2);
                        else
                            return output;
                    }
                }
            }

            return output;
        }

        // For internal use.
        // Using this, most symbol types display themselves wrapped in parentheses.
        private String ToStringInternal()
        {
            return (String)RunMethod(SymbolMethod.Output, null);
        }

        #endregion

        #region Auxilary Methods

        public Boolean IsZero()
        {
            return _type == SymbolType.Constant && _constVal == 0;
        }

        /// <summary>Gets an array of all the variables within this Symbol.</summary>
        public List<String> Variables()
        {
            List<String> list = new List<String>();

            // Add variables from _operand
            List<String> sublist;
            if (_operand != null)
            {
                sublist = _operand.Variables();
                foreach (String v in sublist)
                    if (!list.Contains(v))
                        list.Add(v);
            }

            // Add variables from _rhs
            if (_rhs != null)
            {
                sublist = _rhs.Variables();
                foreach (String v in sublist)
                    if (!list.Contains(v))
                        list.Add(v);
            }

            // Add variables from this instance
            if (_type == SymbolType.Variable && !list.Contains(_varVal))
                list.Add(_varVal);

            return list;
        }

        // If the symbol contains a variable, this method
        // returns NaN.  Otherwise, the symbol is evaluated.
        public Double ToNumber()
        {
            if (IsConstant())
                return Eval();
            else
                return Double.NaN;
        }

        // Note:  An expression's variables may
        // have been eliminated through simple
        // simplifications performed automatically.
        public Boolean ContainsOneOrMoreVariables()
        {
            return _type == SymbolType.Variable || (_operand != null && _operand.ContainsOneOrMoreVariables()) || (_rhs != null && _rhs.ContainsOneOrMoreVariables());
        }

        // Returns whether or not the symbol evaluates
        // to a constant value (vs. containing variables)
        public Boolean IsConstant()
        {
            return !ContainsOneOrMoreVariables();
        }

        // returns a MethodInfo object that can be invoked with 'Invoke'.
        // method return is of the form 'SymbolType_SymbolMethod'.
        // omit type to use this instance's type
        private MethodInfo GetMethod(SymbolMethod method)
        {
            return GetMethod(SymbolType.Null, method);
        }

        private MethodInfo GetMethod(SymbolType type, SymbolMethod method)
        {
            if (type == SymbolType.Null)
                type = _type;

            String methodName = type.ToString() + "_" + method.ToString();
            MethodInfo info = typeof(Symbol).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (info == null)
                throw new System.Exception("Method \'" + methodName + "\' doesn't exist in class Symbol");

            return info;
        }

        private Object RunMethod(SymbolMethod method, Object[] parameters)
        {
            return RunMethod(SymbolType.Null, method, parameters);
        }

        private Object RunMethod(SymbolType type, SymbolMethod method, Object[] parameters)
        {
            if (type == SymbolType.Null)
                type = _type;

            switch (method)
            {
                case SymbolMethod.Differentiate:
                    String paramDiff = (String)parameters[0];
                    switch (type)
                    {
                        case SymbolType.Add: return this.Add_Differentiate(paramDiff);
                        case SymbolType.Constant: return this.Constant_Differentiate(paramDiff);
                        case SymbolType.Cosecant: return this.Cosecant_Differentiate(paramDiff);
                        case SymbolType.Cosine: return this.Cosine_Differentiate(paramDiff);
                        case SymbolType.Cotangent: return this.Cotangent_Differentiate(paramDiff);
                        case SymbolType.Divide: return this.Divide_Differentiate(paramDiff);
                        case SymbolType.Error: return this.Error_Differentiate(paramDiff);
                        case SymbolType.Exponential: return this.Exponential_Differentiate(paramDiff);
                        case SymbolType.Log: return this.Log_Differentiate(paramDiff);
                        case SymbolType.Multiply: return this.Multiply_Differentiate(paramDiff);
                        case SymbolType.NaturalLog: return this.NaturalLog_Differentiate(paramDiff);
                        case SymbolType.Negate: return this.Negate_Differentiate(paramDiff);
                        case SymbolType.Power: return this.Power_Differentiate(paramDiff);
                        case SymbolType.Secant: return this.Secant_Differentiate(paramDiff);
                        case SymbolType.Sine: return this.Sine_Differentiate(paramDiff);
                        case SymbolType.Subtract: return this.Subtract_Differentiate(paramDiff);
                        case SymbolType.Tangent: return this.Tangent_Differentiate(paramDiff);
                        case SymbolType.Variable: return this.Variable_Differentiate(paramDiff);
                        default: return GetMethod(type, method).Invoke(this, parameters);
                    }

                case SymbolMethod.Evaluate:
                    Double paramEval = (Double)parameters[0];
                    switch (type)
                    {
                        case SymbolType.Add: return this.Add_Evaluate(paramEval);
                        case SymbolType.Constant: return this.Constant_Evaluate(paramEval);
                        case SymbolType.Cosecant: return this.Cosecant_Evaluate(paramEval);
                        case SymbolType.Cosine: return this.Cosine_Evaluate(paramEval);
                        case SymbolType.Cotangent: return this.Cotangent_Evaluate(paramEval);
                        case SymbolType.Divide: return this.Divide_Evaluate(paramEval);
                        case SymbolType.Error: return this.Error_Evaluate(paramEval);
                        case SymbolType.Exponential: return this.Exponential_Evaluate(paramEval);
                        case SymbolType.Log: return this.Log_Evaluate(paramEval);
                        case SymbolType.Multiply: return this.Multiply_Evaluate(paramEval);
                        case SymbolType.NaturalLog: return this.NaturalLog_Evaluate(paramEval);
                        case SymbolType.Negate: return this.Negate_Evaluate(paramEval);
                        case SymbolType.Power: return this.Power_Evaluate(paramEval);
                        case SymbolType.Secant: return this.Secant_Evaluate(paramEval);
                        case SymbolType.Sine: return this.Sine_Evaluate(paramEval);
                        case SymbolType.Subtract: return this.Subtract_Evaluate(paramEval);
                        case SymbolType.Tangent: return this.Tangent_Evaluate(paramEval);
                        case SymbolType.Variable: return this.Variable_Evaluate(paramEval);
                        default: return GetMethod(type, method).Invoke(this, parameters);
                    }
                case SymbolMethod.Output:
                    switch (type)
                    {
                        case SymbolType.Add: return this.Add_Output();
                        case SymbolType.Constant: return this.Constant_Output();
                        case SymbolType.Cosecant: return this.Cosecant_Output();
                        case SymbolType.Cosine: return this.Cosine_Output();
                        case SymbolType.Cotangent: return this.Cotangent_Output();
                        case SymbolType.Divide: return this.Divide_Output();
                        case SymbolType.Error: return this.Error_Output();
                        case SymbolType.Exponential: return this.Exponential_Output();
                        case SymbolType.Log: return this.Log_Output();
                        case SymbolType.Multiply: return this.Multiply_Output();
                        case SymbolType.NaturalLog: return this.NaturalLog_Output();
                        case SymbolType.Negate: return this.Negate_Output();
                        case SymbolType.Power: return this.Power_Output();
                        case SymbolType.Secant: return this.Secant_Output();
                        case SymbolType.Sine: return this.Sine_Output();
                        case SymbolType.Subtract: return this.Subtract_Output();
                        case SymbolType.Tangent: return this.Tangent_Output();
                        case SymbolType.Variable: return this.Variable_Output();
                        default: return GetMethod(type, method).Invoke(this, parameters);
                    }
                case SymbolMethod.Substitute:
                    String[] paramVars = (String[])parameters[0];
                    Object[] paramVals = (Object[])parameters[1];
                    switch (type)
                    {
                        case SymbolType.Add: return this.Add_Substitute(paramVars, paramVals);
                        case SymbolType.Constant: return this.Constant_Substitute(paramVars, paramVals);
                        case SymbolType.Cosecant: return this.Cosecant_Substitute(paramVars, paramVals);
                        case SymbolType.Cosine: return this.Cosine_Substitute(paramVars, paramVals);
                        case SymbolType.Cotangent: return this.Cotangent_Substitute(paramVars, paramVals);
                        case SymbolType.Divide: return this.Divide_Substitute(paramVars, paramVals);
                        case SymbolType.Error: return this.Error_Substitute(paramVars, paramVals);
                        case SymbolType.Exponential: return this.Exponential_Substitute(paramVars, paramVals);
                        case SymbolType.Log: return this.Log_Substitute(paramVars, paramVals);
                        case SymbolType.Multiply: return this.Multiply_Substitute(paramVars, paramVals);
                        case SymbolType.NaturalLog: return this.NaturalLog_Substitute(paramVars, paramVals);
                        case SymbolType.Negate: return this.Negate_Substitute(paramVars, paramVals);
                        case SymbolType.Power: return this.Power_Substitute(paramVars, paramVals);
                        case SymbolType.Secant: return this.Secant_Substitute(paramVars, paramVals);
                        case SymbolType.Sine: return this.Sine_Substitute(paramVars, paramVals);
                        case SymbolType.Subtract: return this.Subtract_Substitute(paramVars, paramVals);
                        case SymbolType.Tangent: return this.Tangent_Substitute(paramVars, paramVals);
                        case SymbolType.Variable: return this.Variable_Substitute(paramVars, paramVals);
                        default: return GetMethod(type, method).Invoke(this, parameters);
                    }
                default: return GetMethod(type, method).Invoke(this, parameters);
            }
        }

        //private static String GetOutputToken(SymbolType type)
        //{
        //    return _functionTokens.FirstOrDefault(pair => pair.Value == type).Key;
        //}

        #endregion

        #region Overrides 

        public override bool Equals(object obj)
        {
            if (obj is Symbol)
                return (this == (Symbol)obj);
            else
                return false; 
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion 

        #region Parsing Methods

        public static Symbol Parse(String expression)
        {
            try
            {
                return PostfixEval(ToPostfix(CreateTokens(expression)));
            }
            catch
            {
                return new Symbol(null, SymbolType.Error);
            }
        }

        private static List<String> CreateTokens(String expression)
        {
            List<String> tokens = new List<String>();

            String currStr = "";

            foreach (Char c in expression)
            {
                switch (c)
                {
                    case ',':
                    case ' ':
                    case '+':
                    case '-':
                    case '*':
                    case '/':
                    case '^':
                    case '(':
                    case ')':
                        // When coming across an operator (or delimiter), add the built-up
                        // string to the list and clear the string, then add the operator
                        // or delimiter (that is not a space) to the list
                        if (currStr != "")
                        {
                            tokens.Add(currStr);
                            currStr = "";
                        }

                        if (c != ' ')
                            tokens.Add(c.ToString());
                        break;

                    // If didn't see an operator, continue to
                    // build up the string of characters
                    default:
                        if (c != ' ')
                            currStr += c;
                        break;
                }
            }

            // Add any final built-up string to the list
            if (currStr != "")
                tokens.Add(currStr);

            //Console.WriteLine("TOKENS:");
            //foreach (String t in tokens)
            //    Console.WriteLine(t);

            return tokens;
        }

        private static List<Object> ToPostfix(List<String> tokens)
        {
            List<Object> process = new List<Object>();
            Stack<String> ops = new Stack<String>();
            Double n;

            for (Int32 i = 0; i < tokens.Count; i++)
            {
                String t = tokens[i];

                // token is a number
                if (Double.TryParse(t, out n))
                    process.Add(new Symbol(n));

                // token is an operator or function
                else if (IsOperatorOrFunction(t))
                {
                    // if operator is a negative sign, check to see
                    // if it is subtraction or negation
                    if (t == "-")
                    {
                        // if previous token is null or an operator
                        // the minus sign is a negation.
                        // otherwise, assume it is subtraction.
                        if (i < 1 || tokens[i - 1] == "," || IsOperatorOrFunction(tokens[i - 1]))
                            t = "neg";
                    }

                    // while operator at top of operator stack has higher
                    // or equal precedence than the current token, pop
                    // it and push it onto the process stack
                    //
                    // left parentheses will simply get pushed onto
                    // the process stack without being checked for
                    // precedence
                    while (t != "(" && ops.Count > 0 && GetPrecedence(ops.Peek()) >= GetPrecedence(t))
                        process.Add(ops.Pop());

                    ops.Push(t);
                }

                // if token is a right parenthesis, pop operators
                // off the operator stack until a left parenthesis
                // is found, then pop the parenthesis
                else if (t == ")")
                {
                    while (ops.Peek() != "(")
                        process.Add(ops.Pop());

                    ops.Pop();  // pop the left parenthesis
                }

                // if token is a comma, pop operators off the
                // operator stack until a left parenthesis is
                // found, but do not pop the parenthesis
                else if (t == ",")
                {
                    while (ops.Peek() != "(")
                        process.Add(ops.Pop());
                }

                // token is a string describing
                // a variable, such as ""x"" or "'x'"
                else if (t.First() == '\"' && t.Last() == '\"' || t.First() == '\'' && t.Last() == '\'')
                    process.Add(t.Substring(1, t.Length - 2));

                // token is a constant like "_e" or "_pi"
                else if (_constants.ContainsKey(t))
                {
                    process.Add(new Symbol(_constants[t]));
                }

                // if nothing else, treat as a variable
                else
                    process.Add(new Symbol(t));
            }

            // pop the remaining operators onto the
            // process stack
            while (ops.Count > 0)
                process.Add(ops.Pop());

            //Console.WriteLine("\nOUTPUT:");
            //foreach (Object s in process)
            //    Console.WriteLine(s);
            //Console.WriteLine("\nOPERATORS:");
            //foreach (String o in ops)
            //    Console.WriteLine(o);

            return process;
        }

        private static Symbol PostfixEval(List<Object> toProcess)
        {
            //foreach (Object o in toProcess)
            //{
            //    Console.Write(o.ToString() + ' ');
            //}
            //Console.WriteLine();

            for (Int32 i = 0; i < toProcess.Count; i++)
            {
                Object obj = toProcess[i];

                if (obj.GetType() == typeof(String))
                {
                    String t = obj.ToString();

                    if (_functions.ContainsKey(t))
                    {
                        Int32 numArgs = _functions[t];
                        Object[] args = new Object[numArgs];

                        for (Int32 n = 0; n < numArgs; n++)
                        {
                            args[numArgs - n - 1] = toProcess[i - 1];
                            toProcess.RemoveAt(--i);  // remove argument
                        }

                        // Make function name upper case at first letter and lower case elsewhere
                        String name = Char.ToUpper(t[0]) + t.ToLower().Substring(1, t.Length - 1);

                        // Find function handler
                        MethodInfo method = typeof(Symbol).GetMethod("Handler_" + name, BindingFlags.NonPublic | BindingFlags.Static);

                        // Replace function with result
                        Object result = method.Invoke(null, new Object[] { args });

                        toProcess.RemoveAt(i);
                        toProcess.Insert(i, result);
                    }
                    else if (_operators.ContainsKey(t))
                    {
                        Symbol rhs = (Symbol)toProcess[i - 1];
                        toProcess.RemoveAt(--i);

                        Symbol lhs = (Symbol)toProcess[i - 1];
                        toProcess.RemoveAt(--i);

                        // replace operator with result
                        toProcess.RemoveAt(i);
                        toProcess.Insert(i, new Symbol(lhs, _operators[t], rhs));
                    }
                }
            }
            return (Symbol)toProcess.First();
        }

        private static Boolean IsOperand(String token)
        {
            Double n;
            if (Double.TryParse(token, out n))
                return true;

            if (token.Length == 1 && Char.IsLetter(token.First()))
                return true;

            return token == ")";  // special case
        }

        private static Boolean IsOperatorOrFunction(String token)
        {
            switch (token)
            {
                case "+":
                case "-":
                case "*":
                case "/":
                case "^":
                case "(":  // special case
                    return true;
                default:
                    return _functions.ContainsKey(token);
            }
        }

        private static Int32 GetPrecedence(String token)
        {
            switch (token)
            {
                case "(":  // special case
                    return -1;
                case "+":
                case "-":
                    return 0;
                case "*":
                case "/":
                    return 1;
                case "^":
                    return 2;
                case "neg":
                    return 3;
                default:
                    return 4;
            }
        }

        #endregion

        #region Parsing Handlers

        private static Object Handler_Neg(Object[] args)
        {
            return -((Symbol)args[0]);
        }

        private static Object Handler_Diff(Object[] args)
        {
            return Sym.Diff((Symbol)args[0], args[1].ToString(), (Int32)((Symbol)args[2]).ToNumber());
        }

        private static Object Handler_Root(Object[] args)
        {
            return Sym.Root((Symbol)args[0], (Symbol)args[1]);
        }

        private static Object Handler_Sqrt(Object[] args)
        {
            return Sym.SquareRoot((Symbol)args[0]);
        }

        private static Object Handler_Cbrt(Object[] args)
        {
            return Sym.CubeRoot((Symbol)args[0]);
        }

        #endregion
    }
}
