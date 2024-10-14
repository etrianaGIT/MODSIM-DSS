using System;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.ModsimIO
{
    // Input or Output
    public enum DirectionType
    {
        Input,
        Output
    }
}

namespace Csu.Modsim.ModsimIO
{
    public class XYFile
    {
        // Instance variables
        public readonly int DataNumOfSpaces;

        public readonly char[] DataDivider;
        // Constructor
        public XYFile(Model mi, DirectionType dirType)
        {
            this.DataNumOfSpaces = GetNumberOfSpaces(mi, dirType);
            this.DataDivider = GetDataDivider(mi, dirType);
        }

        // Shared functions
        public static char[] GetDataDivider(Model mi, DirectionType dirType)
        {
            switch (dirType)
            {
                case DirectionType.Input:
                    if (mi.inputVersion.Type >= InputVersionType.V8_2)
                        return new char[3] { Convert.ToChar("\t"), ' ', '|' };
                    else
                        return new char[1] { ' ' };
                case DirectionType.Output:
                    if (mi.outputVersion.Type >= OutputVersionType.V8_2)
                        return new char[1] { Convert.ToChar("\t") };
                    else
                        return new char[1] { ' ' };
                default:
                    throw new Exception("Cannot get the data divider for an undefined direction type: " + dirType.ToString());
            }
        }
        public static int GetNumberOfSpaces(Model mi, DirectionType dirType)
        {
            if (mi == null)
                return 0;
            switch (dirType)
            {
                case DirectionType.Input:
                    if (mi.inputVersion == null)
                        return 0;
                    if (mi.inputVersion.Type >= InputVersionType.V8_2)
                        return 0;
                    else
                        return 10;
                case DirectionType.Output:
                    if (mi.outputVersion == null)
                        return 0;
                    if (mi.outputVersion.Type >= OutputVersionType.V8_2)
                        return 0;
                    else
                        return 10;
                default:
                    throw new Exception("Cannot get the data divider for an undefined direction type: " + dirType.ToString());
            }
        }
    }
}
