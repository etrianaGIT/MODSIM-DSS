using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace Csu.Modsim.ModsimModel
{
    public class DataImporter
    {
        //Tab separated columns with the following pattern 
        //("StartDate" must be in the first column, first row of data):
        //	StartDate|NodeName1_TimeSeriesType|Node2_TimeSeriesType|Link1_TimeSeriesType|...
        //	1/1/2010|50.0|60.0|80.0	...
        //	1/2/2010|60.0|80.0|20.0	...
        //	:
        // 
        //Timeseries Types:
        // 
        //	VariableCapacity   long     link Link
        //	NonStorage         long     node NonStorage
        //	Targets            long     node Reservoir
        //	Generating_Hours   double   node Reservoir
        //	Evaporation        double   node Reservoir
        //	Forecast           long     node Reservoir
        //	Infiltration       double   node Reservoir or Demand
        //	Power_Target       double   node Demand
        //	Demand             long     node Demand
        //	Sink               long     node Demand

        private readonly static string[] delims = new string[] { Link.ForbiddenStringInName };
        private string _file;
        private TimeSeriesType[] _allTStypes;
        private Model _model;
        private int[] _columns;
        private object[] _objects;
        private TimeSeriesType[] _colTypes;
        private DataTable[] _tables;

        public DataImporter(Model model, string file)
        {
            _file = file;
            _model = model;
            _allTStypes = (TimeSeriesType[])Enum.GetValues(typeof(TimeSeriesType));
        }

        private TimeSeriesType getTSTypeFromEnd(string s)
        {
            for (int i = 0; i < _allTStypes.Length; i++)
                if (s.EndsWith(_allTStypes[i].ToString()))
                    return _allTStypes[i];
            return TimeSeriesType.Undefined;
        }

        private void ReadColumnNames(StreamReader sr)
        {
            List<int> columnList = new List<int>();
            List<object> nodeList = new List<object>();
            List<TimeSeriesType> typeList = new List<TimeSeriesType>();
            while (!sr.EndOfStream)
            {
                string s = sr.ReadLine().Trim();
                if (!s.StartsWith("StartDate"))
                    continue;

                // split the columns
                string[] cols = s.Split(delims, StringSplitOptions.None);
                for (int i = 1; i < cols.Length; i++)
                {
                    // Get the type info
                    TimeSeriesType type = getTSTypeFromEnd(cols[i]);
                    if (type == TimeSeriesType.Undefined)
                        continue;

                    // Get the name of the node / other object
                    string name = cols[i].Substring(0, cols[i].Length - type.ToString().Length - 1);
                    object obj = null;
                    switch (type)
                    {
                        case TimeSeriesType.Power_Target:
                            obj = _model.hydro.GetHydroTarget(name);
                            break;
                        case TimeSeriesType.VariableCapacity:
                            obj = _model.FindLink(name);
                            break;
                        default:
                            obj = _model.FindNode(name);
                            break;
                    }

                    // Add the items to the lists
                    if (obj != null)
                    {
                        columnList.Add(i);
                        nodeList.Add(obj);
                        typeList.Add(type);
                    }
                }
            }
            _columns = columnList.ToArray();
            _objects = nodeList.ToArray();
            _colTypes = typeList.ToArray();
        }

        private TimeSeries getTS(TimeSeriesType type, object obj)
        {
            HydropowerTarget targ = null;
            Node node = null;
            Link link = null;

            // Fill the right type of object
            if (type == TimeSeriesType.Power_Target)
                targ = (HydropowerTarget)obj;
            else if (type == TimeSeriesType.VariableCapacity)
                link = (Link)obj;
            else
                node = (Node)obj;

            // Clone the correct tables
            switch (type)
            {
                case TimeSeriesType.Demand:
                    return node.m.adaDemandsM;
                case TimeSeriesType.Evaporation:
                    return node.m.adaEvaporationsM;
                case TimeSeriesType.Forecast:
                    return node.m.adaForecastsM;
                case TimeSeriesType.Generating_Hours:
                    return node.m.adaGeneratingHrsM;
                case TimeSeriesType.Infiltration:
                    return node.m.adaInfiltrationsM;
                case TimeSeriesType.NonStorage:
                    return node.m.adaInflowsM;
                case TimeSeriesType.Power_Target:
                    return targ.PowerTargetsTS;
                case TimeSeriesType.Sink:
                    return node.m.adaDemandsM;
                case TimeSeriesType.Targets:
                    return node.m.adaTargetsM;
                case TimeSeriesType.VariableCapacity:
                    return link.m.maxVariable;
                default:
                    throw new NotImplementedException("The type " + type.ToString() + " is not a type implemented in the tab-delimited importer.");
            }
        }

        private double getScaleFactor(TimeSeriesType type) 
        {
            switch (type)
            {
                case TimeSeriesType.NonStorage:
                case TimeSeriesType.Targets:
                case TimeSeriesType.Demand:
                case TimeSeriesType.VariableCapacity:
                    return _model.ScaleFactor;
                case TimeSeriesType.Forecast:
                case TimeSeriesType.Generating_Hours:
                case TimeSeriesType.Infiltration:
                case TimeSeriesType.Evaporation:
                case TimeSeriesType.Power_Target:
                case TimeSeriesType.Sink:
                    return 1.0; 
                default:
                    throw new NotImplementedException("The type " + type.ToString() + " is not a type implemented in the tab-delimited importer.");
            }
        }

        private double scaledValue(double val, TimeSeriesType type)
        {
            return (Math.Abs(val - _model.defaultMaxCap) < 0.01) ? val : val / getScaleFactor(type); 
        }

        private void SetupDataTables()
        {
            _tables = new DataTable[_objects.Length];
            for (int i = 0; i < _objects.Length; i++)
                _tables[i] = getTS(_colTypes[i], _objects[i]).dataTable.Clone(); 
        }

        private void FillDataTables(StreamReader sr)
        {
            this.SetupDataTables(); 
            
            while (!sr.EndOfStream)
            {
                string s = sr.ReadLine().Trim();
                if (s.Equals(""))
                    continue;

                // split the columns 
                string[] cols = s.Split(delims, StringSplitOptions.None);
                if (cols.Length != _columns.Length)
                    continue; 

                // parse the data 
                DateTime date = Convert.ToDateTime(cols[0]); 
                for (int i = 0; i < _columns.Length; i++) 
                {
                    double val;
                    if (cols[_columns[i]].Equals("") || !double.TryParse(cols[_columns[i]], out val))
                        continue;

                    DataRow dr = _tables[i].NewRow();
                    dr[0] = date;
                    dr[1] = scaledValue(val, _colTypes[i]);
                    _tables[i].Rows.Add(dr);  
                }
            }
        }

        private void SetTimeSeries()
        {
            for (int i = 0; i < _colTypes.Length; i++)
                getTS(_colTypes[i], _objects[i]).dataTable = _tables[i]; 
        }

        public void Import()
        {
            try
            {

                // Open the file 
                StreamReader sr = new StreamReader(_file);

                // Read column names and retrieve nodes and timeseries types
                this.ReadColumnNames(sr);

                // Get a datatable of the data 
                this.FillDataTables(sr);

                // Close the reader 
                sr.Close();

                // Fill the TimeSeries objects with the new data
                this.SetTimeSeries();

            }
            catch (Exception ex)
            {
                _model.FireOnError("\n\nError importing data: \n" + ex.ToString() + "\n");
            }
        }
    }
}
