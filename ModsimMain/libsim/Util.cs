using System;

namespace Csu.Modsim.ModsimModel
{
    public static class Util
    {
        public static Boolean IsNumeric(Object expression)
        {
            if (expression == null || expression is DateTime)
                return false;

            if (expression is Int16 || expression is Int32 || expression is Int64 || expression is Decimal || expression is Single || expression is Double || expression is Boolean)
                return true;

            try
            {
                if (expression is string)
                    Double.Parse(expression as string);
                else
                    Double.Parse(expression.ToString());
                return true;

            }
            catch { } // just dismiss errors but return false
            return false;
        }

        public static Boolean IsDate(Object expression)
        {
            if (expression == null)
                return false;

            if (expression is DateTime)
                return true;

            try
            {
                if (expression is string)
                    DateTime.Parse(expression as string);
                else
                    DateTime.Parse(expression.ToString());
                return true;
            }
            catch { } // just dismiss errors but return false
            return false;
        }

        public static Boolean IsDBNull(Object expression)
        {
            return expression == DBNull.Value;
        }
    }
}
