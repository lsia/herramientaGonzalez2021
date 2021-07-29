using KSDExperiments.Datasets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSDExperiments.Reports
{
    class NumericTableReport
    {
        public int Columns { get; private set; }
        public NumericTableReport(int columns)
        {
            Columns = columns;
            Rows = new List<KeyValuePair<Session, double[]>>();
        }

        public List<KeyValuePair<Session, double[]>> Rows { get; private set; }

        public KeyValuePair<Session,double[]> GetMaxByColumn(int column)
        {
            KeyValuePair<Session, double[]>? retval = null;
            foreach (var kv in Rows)
                if (retval == null || retval?.Value[column] < kv.Value[column])
                    retval = kv;

            return (KeyValuePair<Session, double[]>) retval;
        }

        public void AddSession(Session session, double[] row)
        {
            Debug.Assert(row.Length == Columns);
            
            KeyValuePair<Session, double[]> kv = new KeyValuePair<Session, double[]>(session, row);
            Rows.Add(kv);
        }

        public double[] GetAverage()
        {
            double[] retval = new double[Columns];

            foreach (var kv in Rows)
                for (int i = 0; i < Columns; i++)
                    retval[i] += kv.Value[i];

            for (int i = 0; i < Columns; i++)
                retval[i] /= Rows.Count;

            return retval;
        }

        public double[] GetVariance()
        {
            double[] retval = new double[Columns];
            double[] mean = GetAverage();

            foreach (var kv in Rows)
                for (int i = 0; i < Columns; i++)
                {
                    double tmp = kv.Value[i] - mean[i];
                    retval[i] += tmp * tmp;
                }

            for (int i = 0; i < Columns; i++)
                retval[i] /= Rows.Count;

            return retval;
        }

        public void GetDistribution(out double[] mean, out double[] variance)
        {
            mean = GetAverage();
            variance = new double[Columns];

            foreach (var kv in Rows)
                for (int i = 0; i < Columns; i++)
                {
                    double tmp = kv.Value[i] - mean[i];
                    variance[i] += tmp * tmp;
                }

            for (int i = 0; i < Columns; i++)
                variance[i] /= Rows.Count;
        }
    }
}
