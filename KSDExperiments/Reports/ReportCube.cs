using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using KSDExperiments.Experiments;
using KSDExperiments.Pipelines;
using KSDExperiments.Util;


namespace KSDExperiments.Reports
{
    public class ReportCubeRow
    {
        public ReportCube Cube { get; private set; }

        public object[] Values { get; private set; }
        public ReportCubeRow(ReportCube cube)
        {
            Cube = cube;
            Values = new object[Cube.Columns.Length];
        }

        public ReportCubeRow(ReportCube cube, object[] values)
        {
            Cube = cube;

            if (values.Length == Cube.Columns.Length)
                Values = values;
            else
            {
                Values = new object[Cube.Columns.Length];
                Array.Copy(values, 0, Values, 0, values.Length);
                CurrentPosition = values.Length;
            }
        }

        public int CurrentPosition { get; private set; }
        public void Append(object value)
        {
            if (CurrentPosition >= Values.Length)
                throw new InvalidOperationException("All column values have been appended.");

            Values[CurrentPosition] = value;
            CurrentPosition++;
        }

        public void Append(params object[] values)
        {
            if ((CurrentPosition + values.Length) > Values.Length)
                throw new InvalidOperationException("Too many values to append.");

            for (int i = 0; i < values.Length; i++, CurrentPosition++)
                Values[CurrentPosition] = values[i];
        }

        public object this[string column]
        {
            get
            {
                if (!Cube.ColumnsDictionary.ContainsKey(column))
                    throw new ArgumentException("Column not found.");

                return Values[Cube.ColumnsDictionary[column]];
            }

            set
            {
                if (!Cube.ColumnsDictionary.ContainsKey(column))
                    throw new ArgumentException("Column not found.");

                Values[Cube.ColumnsDictionary[column]] = value;
            }
        }

        public object this[int column]
        {
            get
            {
                return Values[column];
            }

            set
            {
                Values[column] = value;
            }
        }
    }

    public class ReportCube
    {
        public Dictionary<string,int> ColumnsDictionary { get; private set; }
        public string[] Columns { get; private set; }
        public ReportCube(params string[] columns)
        {
            Columns = columns;
            ColumnsDictionary = new Dictionary<string, int>();
            Rows = new List<ReportCubeRow>();

            for (int i = 0; i < columns.Length; i++)
                ColumnsDictionary.Add(columns[i], i);
        }

        public string Basename { get; set; }

        object giant_lock = new object();
        public List<ReportCubeRow> Rows { get; private set; }
        public void Clear()
        {
            Rows.Clear();
        }

        public ReportCubeRow AddRow(params object[] values)
        {
            ReportCubeRow retval = null;

            if (initial_values == null)
            {
                if (values.Length != Columns.Length)
                    throw new ArgumentException("Invalid number of columns.");

                retval = new ReportCubeRow(this, values);
            }
            else
            {
                if ((values.Length + initial_values.Length) != Columns.Length)
                    throw new ArgumentException("Invalid number of columns.");

                retval = new ReportCubeRow(this);
                retval.Append(initial_values);
                retval.Append(values);
            }

            lock (giant_lock)
                Rows.Add(retval);
            
            return retval;
        }

        public ReportCubeRow CreateRow()
        {
            ReportCubeRow retval = new ReportCubeRow(this);
            lock (giant_lock)
                Rows.Add(retval);
            
            return retval;
        }

        public ReportCubeRow CreateRow(params object[] initial_values)
        {
            ReportCubeRow retval = new ReportCubeRow(this);
            retval.Append(initial_values);
            lock (giant_lock)
                Rows.Add(retval);
            
            return retval;
        }

        public ReportCube AggregateSum(string target_column, params string[] grouping_columns)
        {
            int target_column_pos = ColumnsDictionary[target_column];

            StringBuilder group = new StringBuilder();
            Dictionary<string, double> groups = new Dictionary<string, double>();
            foreach (var row in Rows)
            {
                group.Clear();
                for (int i = 0; i < grouping_columns.Length; i++)
                {
                    group.Append(row[grouping_columns[i]]);
                    group.Append("|");
                }

                string tmp = group.ToString();
                if (!groups.ContainsKey(tmp))
                    groups.Add(tmp, (double) row[target_column_pos]);
                else
                    groups[tmp] += (double) row[target_column_pos];
            }

            List<string> new_columns = new List<string>();
            new_columns.AddRange(grouping_columns);
            new_columns.Add(target_column);
            ReportCube retval = new ReportCube(new_columns.ToArray());
            retval.Basename = Basename + "-AGGREGATE";
            foreach (var kv in groups)
            {
                string[] fields = kv.Key.Split('|');

                object[] values = new object[grouping_columns.Length + 1];
                Array.Copy(fields, 0, values, 0, grouping_columns.Length);
                values[grouping_columns.Length] = kv.Value;
                retval.AddRow(values);
            }

            return retval;
        }

        object[] initial_values = null;
        public void SetFixedInitialValues(params object[] values)
        {
            initial_values = values;
        }

        public ReportCube AggregateMean(string target_column, params string[] grouping_columns)
        {
            int target_column_pos = ColumnsDictionary[target_column];

            StringBuilder group = new StringBuilder();
            Dictionary<string, int> count = new Dictionary<string, int>();
            Dictionary<string, double> groups = new Dictionary<string, double>();
            foreach (var row in Rows)
            {
                group.Clear();
                for (int i = 0; i < grouping_columns.Length; i++)
                {
                    group.Append(row[grouping_columns[i]]);
                    group.Append("|");
                }

                string tmp = group.ToString();
                if (!groups.ContainsKey(tmp))
                {
                    groups.Add(tmp, (double)row[target_column_pos]);
                    count.Add(tmp, 1);
                }
                else
                {
                    groups[tmp] += (double)row[target_column_pos];
                    count[tmp]++;
                }
            }

            foreach (var key in count.Keys)
                groups[key] /= count[key];

            List<string> new_columns = new List<string>();
            new_columns.AddRange(grouping_columns);
            new_columns.Add(target_column);
            ReportCube retval = new ReportCube(new_columns.ToArray());
            retval.Basename = Basename + "-AGGREGATE";
            foreach (var kv in groups)
            {
                string[] fields = kv.Key.Split('|');

                object[] values = new object[grouping_columns.Length + 1];
                Array.Copy(fields, 0, values, 0, grouping_columns.Length);
                values[grouping_columns.Length] = kv.Value;
                retval.AddRow(values);
            }

            return retval;
        }

        public void Save(string suffix = null)
        {
            string FOLDER = ExperimentUtil.OutputFolder;
            if (!Directory.Exists(FOLDER))
                Directory.CreateDirectory(FOLDER);

            string filename = FOLDER;
            if (Basename == null)
            {
                if (suffix == null)
                    throw new ArgumentException("You must provide a basename or a suffix.");
                else
                    filename += suffix + ".csv";
            }
            else
            {
                if (suffix == null)
                    filename += Basename + ".csv";
                else
                    filename += Basename + "-" + suffix + ".csv";
            }

            CsvWriter csv = new CsvWriter(filename, Columns.Length);
            csv.WriteLine(Columns);

            foreach (var row in Rows)
                csv.WriteLine(row.Values);

            csv.Flush();
            csv.Dump();
        }
    }
}
