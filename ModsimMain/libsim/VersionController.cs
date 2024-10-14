using System;
using System.Collections.Generic;

namespace Csu.Modsim.ModsimModel
{
    // Enumerators for the MODSIM model version control
    /// <summary>Defines the input versions that can be read into the current version of MODSIM.</summary>
    public enum InputVersionType : int
    {
        Undefined = 0,
        V056 = 1,
        V8_0 = 2,
        V8_1 = 3,
        V8_2 = 4,
        V8_2_1 = 5,
        V8_2_2 = 6,
        V8_3_0 = 7,
        V8_3_1 = 8,
        V8_3_2 = 9,
        V8_4_0 = 10,
        V8_4_1 = 11,
        V8_4_2 = 12,
        V8_4_3 = 13,
        V8_4_4 = 14,
        V8_4_5 = 15,
        V8_5_0 = 16,
        V8_5_1 = 17,
        V8_6 = 18
    }

    /// <summary>Defines the output versions that can be written from the current version of MODSIM.</summary>
    public enum OutputVersionType : int
    {
        Undefined = 0,
        V8_1 = 3,
        V8_2 = 4,
        V8_2_1 = 5,
        V8_2_2 = 6,
        V8_3_0 = 7,
        V8_3_1 = 8,
        V8_3_2 = 9,
        V8_4_0 = 10,
        V8_4_1 = 11,
        V8_4_2 = 12,
        V8_4_3 = 13,
        V8_4_4 = 14,
        V8_4_5 = 15,
        V8_5_0 = 16,
        V8_5_1 = 17,
        V8_6 = 18,
        LatestVersion = 100000 // Keep this the largest number
    }

    // Classes that perform MODSIM version control.
    /// <summary>Contains methods and information about the version of MODSIM to read from.</summary>
    public class InputVersion
    {
        // Instance variables
        private InputVersionType privInputVersion = InputVersionType.Undefined;

        // Properties
        /// <summary>Gets or sets the version of MODSIM from which the model derived.</summary>
        public InputVersionType Type
        {
            get
            {
                return privInputVersion;
            }
            set
            {
                privInputVersion = value;
            }
        }

        /// <summary>Constructs an instance of InputVersion given a specified version string</summary>
        /// <param name="version">The version type (e.g., V8_1, V8_2, etc.).</param>
        public InputVersion(InputVersionType version)
        {
            privInputVersion = version;
        }
        /// <summary>Constructs an instance of InputVersion given a specified version string</summary>
        /// <param name="version">The version string (e.g., 8.1.0 or 8.2.0, etc.).</param>
        public InputVersion(string version)
        {
            privInputVersion = GetType(version);
        }
        public static implicit operator InputVersion(InputVersionType version)
        {
            return new InputVersion(version);
        }
        public static implicit operator InputVersion(string version)
        {
            return new InputVersion(version);
        }

        // Publicly shared methods over [Input|Output]VersionTypes
        /// <summary>Retrieves the InputVersionType associated with the name or label of the corresponding version of MODSIM.</summary>
        /// <param name="version">A label specifying the version of MODSIM (e.g., 8.1.0 or 8.2.0).</param>
        /// <returns>Returns the InputVersionType corresponding with the label.</returns>
        public static InputVersionType GetType(string version)
        {
            switch (version)
            {
                case "056":
                    return InputVersionType.V056;
                case "8.0":
                case "8.0.0":
                    return InputVersionType.V8_0;
                case "8.1":
                case "8.1.0":
                    return InputVersionType.V8_1;
                case "8.2":
                case "8.2.0":
                    return InputVersionType.V8_2;
                default:
                    string vers;
                    if (Enum.IsDefined(typeof(InputVersionType), vers = "V" + version.Replace('.',
                                       '_')) || Enum.IsDefined(typeof(InputVersionType),
                                               vers = vers.TrimEnd(new char[] { '0' }).TrimEnd(new char[] { '.' })))
                    {
                        return (InputVersionType)Enum.Parse(typeof(InputVersionType), vers);
                    }
                    else
                    {
                        return InputVersionType.Undefined;
                    }
            }
        }
    }
    /// <summary>Contains methods and information about the version of MODSIM to write to.</summary>
    public class OutputVersion
    {
        // Instance variables
        private OutputVersionType privType = OutputVersionType.LatestVersion;
        private string privLabel = "";

        // Properties
        /// <summary>Gets or sets the version of MODSIM to which the model will be written.</summary>
        /// <remarks>If the current instance of output version is OutputVersionType.LatestVersion or OutputVersionType.Undefined, this property returns the latest version defined by OutputVersionType.</remarks>
        public OutputVersionType Type
        {
            get
            {
                if (privType == OutputVersionType.LatestVersion
                        || privType == OutputVersionType.Undefined)
                {
                    return privType = GetLatest();
                }
                else
                {
                    return privType;
                }
            }
            set
            {
                if (value == OutputVersionType.LatestVersion
                        || value == OutputVersionType.Undefined)
                {
                    privType = GetLatest();
                }
                else
                {
                    privType = value;
                }
                privLabel = GetLabel(privType);
            }
        }
        /// <summary>Gets the label for the Type of the current Instance.</summary>
        public string Label
        {
            get
            {
                return privLabel;
            }
        }

        /// <summary>Constructs an instance of Output Version for the latest version of MODSIM.</summary>
        public OutputVersion() : this(OutputVersionType.LatestVersion)
        {

        }
        /// <summary>Constructs an instance of Output Version for the latest version of MODSIM.</summary>
        /// <param name="version">The version of the inputs. If version is 8.1.0 or later, OutputVersion is the same by default.</param>
        public OutputVersion(InputVersionType version) : this(GetType(version))
        {

        }
        /// <summary>Constructs an instance of Output Version for the latest version of MODSIM.</summary>
        /// <param name="value">A value corresponding to the OutputVersionType.</param>
        public OutputVersion(int value) : this(GetType(value))
        {

        }
        /// <summary>Constructs an instance of Output Version for the latest version of MODSIM.</summary>
        /// <param name="value">A value corresponding to the OutputVersionType.</param>
        /// <param name="IsFilterIndex">Specifies whether 'value' corresponds directly to OutputVersionType, or is actually a open/save file dialog filter index.</param>
        public OutputVersion(int value, bool IsFilterIndex) : this(GetType(value,
                IsFilterIndex))
        {

        }
        /// <summary>Constructs an instance of OutputVersion for the latest version of MODSIM.</summary>
        /// <param name="version">The output version.</param>
        public OutputVersion(OutputVersionType version)
        {
            privType = version;
            privLabel = GetLabel(version);
        }

        // Privately shared methods
        /// <summary>Retrieves the latest MODSIM output version defined within the enumerator OutputVersionType.</summary>
        /// <returns>Returns the latest MODSIM output version.</returns>
        private static OutputVersionType GetLatest()
        {
            OutputVersionType[] vals = GetTypes();
            Array.Sort(vals);
            return vals[vals.Length - 2];
        }
        /// <summary>Converts a specified OutputVersionType to its label.</summary>
        /// <param name="version">Specifies the OutputVersionType.</param>
        /// <returns>Returns the label for the specified OutputVersionType.</returns>
        private static string GetLabel(OutputVersionType version)
        {
            if (version == OutputVersionType.LatestVersion)
            {
                return GetLabel(GetLatest());
            }

            if ((int)version >= (int)OutputVersionType.V8_2_1)
            {
                return version.ToString().Replace("V", "").Replace('_', '.');
            }
            else
            {
                return version.ToString().Replace("V", "").Replace('_', '.') + ".0";
            }
        }

        // Publicly shared methods over [Input|Output]VersionTypes
        /// <summary>Converts an InputVersionType enum to an OutputVersionType enum.</summary>
        /// <param name="version">Specifies the InputVersionType.</param>
        /// <returns>Returns an equivalent OutputVersionType to the InputVersionType if it exists; otherwise, it returns the latest version.</returns>
        /// <remarks>Older versions from InputVersionType and the 'undefined' enum are converted to the latest version of OutputVersionType.</remarks>
        public static OutputVersionType GetType(InputVersionType version)
        {
            OutputVersionType latest = GetLatest();
            if ((int)version < (int)InputVersionType.V8_1 || (int)version > (int)latest)
            {
                return GetLatest();
            }
            else
            {
                return (OutputVersionType)((int)version);
            }
        }
        /// <summary>Retrieves the OutputVersionType associated with the specified integer value.</summary>
        /// <param name="value">The integer value to convert to OutputVersionType.</param>
        /// <returns>Returns the OutputVersionType associated with the specified integer value.</returns>
        public static OutputVersionType GetType(int value)
        {
            return GetType(value, false);
        }
        /// <summary>Retrieves the OutputVersionType associated with the specified integer value.</summary>
        /// <param name="value">The integer value to convert to OutputVersionType.</param>
        /// <param name="IsFilterIndex">Specifies whether 'value' refers to a filter index or to the corresponding value of the OutputVersionType enum.</param>
        /// <returns>Returns the OutputVersionType associated with the specified integer value.</returns>
        public static OutputVersionType GetType(int value, bool IsFilterIndex)
        {
            if (!IsFilterIndex)
            {
                // Values directly correspond with OutputVersionType enum.
                if (Enum.IsDefined(typeof(OutputVersionType), value))
                {
                    return (OutputVersionType)value;
                }
                else
                {
                    return OutputVersionType.Undefined;
                }
            }
            else
            {
                // Values correspond with a dialog filter index.
                OutputVersionType retVal = OutputVersionType.LatestVersion;
                OutputVersionType[] AllVals = GetTypes(false);
                if (value <= AllVals.Length)
                {
                    retVal = AllVals[AllVals.Length - value];
                }
                if (retVal == OutputVersionType.LatestVersion
                        || retVal ==
                        OutputVersionType.Undefined) // It should never equal undefined at this point, but why not put it here any way?
                {
                    retVal = GetLatest();
                }
                return retVal;
            }
        }
        /// <summary>Builds a sorted array of all elements in the OutputVersionType enum.</summary>
        /// <returns>Returns a sorted array of all elements in the OutputVersionType enum.</returns>
        public static OutputVersionType[] GetTypes()
        {
            return GetTypes(true);
        }
        /// <summary>Builds a sorted array of either all elements in the OutputVersionType enum or all that aren't 'LatestVersion' and 'Undefined'.</summary>
        /// <param name="GetAllElements">Specifies whether to retrieve all the elements or just those that aren't 'LatestVersion' or 'Undefined'.</param>
        /// <returns>Returns a sorted array of all elements in the OutputVersionType enum.</returns>
        public static OutputVersionType[] GetTypes(bool GetAllElements)
        {
            // Get the values into an integer array.
            Array allVals = Enum.GetValues(typeof(OutputVersionType));
            int[] intVals = new int[allVals.Length];
            allVals.CopyTo(intVals, 0);
            Array.Sort(intVals);

            // Get the desired list of elements (either all elements, or just the ones that aren't LatestVersion and Undefined)
            List<OutputVersionType> aList = new List<OutputVersionType>();
            OutputVersionType elem;
            foreach (int val in intVals)
            {
                elem = (OutputVersionType)val;
                if (GetAllElements || (elem != OutputVersionType.LatestVersion
                                       && elem != OutputVersionType.Undefined))
                {
                    aList.Add(elem);
                }
            }

            // Place list of values into the return value array
            OutputVersionType[] retVal = new OutputVersionType[aList.Count];
            aList.CopyTo(retVal);
            return retVal;
        }
        /// <summary>Builds an array of labels for all OutputVersionTypes.</summary>
        /// <returns>Returns the array of labels for all OutputVersionTypes.</returns>
        public static string[] GetLabels()
        {
            return GetLabels(true);
        }
        /// <summary>Builds an array of labels for all OutputVersionTypes.</summary>
        /// <param name="GetAllElements">Specifies whether to retrieve all the elements or just those that aren't 'LatestVersion' or 'Undefined'.</param>
        /// <returns>Returns the array of labels for all OutputVersionTypes.</returns>
        public static string[] GetLabels(bool GetAllElements)
        {
            OutputVersionType[] elements = GetTypes(GetAllElements);
            string[] versionStrings = new string[elements.Length];
            return Array.ConvertAll(elements, element => GetLabel(element));
        }

        // Open and save file dialog filter methods
        /// <summary>Builds an open file or save file dialog filter for MODSIM files using the OutputVersionType enum.</summary>
        /// <param name="version">The output version type to build a dialog filter for.</param>
        /// <returns>Returns the dialog filter for the specified output version.</returns>
        public static string GetDialogFilter(OutputVersionType version)
        {
            return "MODSIM " + GetLabel(version) + " XY file (*.xy)|*.xy";
        }
        /// <summary>Builds all open file or save file dialog filter strings for all MODSIM OutputVersionTypes.</summary>
        /// <returns>Returns a string containing all the filters for output versions of MODSIM.</returns>
        public static string GetDialogFilters()
        {
            OutputVersionType[] elements = GetTypes(
                                               false); // Don't want the LatestVersion or Undefined values
            string retVal = "";
            for (int i = elements.Length - 1; i >= 0; i--)
            {
                retVal += GetDialogFilter(elements[i]) + "|";
            }
            retVal += "All Files(*.*)|*.*";
            return retVal;
        }

    }
}
