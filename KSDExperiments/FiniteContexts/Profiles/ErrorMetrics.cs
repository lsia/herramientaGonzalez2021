using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using KSDExperiments.Latex;


namespace KSDExperiments.FiniteContexts.Profiles
{
    public class ErrorMetrics
    {
        public string Name { get; private set; }
        public double EER { get; private set; }
        public double Threshold { get; private set; }

        public double FAR { get; private set; }
        public double FRR { get; private set; }

        public double[] LegitimateValues { get; private set; }
        public double[] ImpostorValues { get; private set; }

        public ErrorMetrics(string name, double eer)
        {
            Name = name;
            EER = eer;

            FAR = FRR = Threshold = double.NaN;
            LegitimateValues = ImpostorValues = null;
        }

        public ErrorMetrics(string name, double eer, double far, double frr)
        {
            Name = name;
            EER = eer;
            FAR = far;
            FRR = frr;

            Threshold = double.NaN;
            LegitimateValues = ImpostorValues = null;
        }

        public ErrorMetrics(string name, double[] legitimate_values, double[] impostor_values)
        {
            Name = name;
            LegitimateValues = legitimate_values;
            ImpostorValues = impostor_values;

            CalculateMetrics();
        }

        void CalculateMetrics()
        {
            Array.Sort(LegitimateValues);
            Array.Sort(ImpostorValues);

            if (LegitimateValues[LegitimateValues.Length - 1] < ImpostorValues[0])
            {
                EER = 0;
                Threshold = (LegitimateValues[LegitimateValues.Length - 1] + ImpostorValues[0]) / 2.0;
                return;
            }

            int ipos = 0;
            for (int i = 0; i < LegitimateValues.Length; i++)
            {
                while (ipos < ImpostorValues.Length && ImpostorValues[ipos] <= LegitimateValues[i])
                    ipos++;

                FRR = (double)(LegitimateValues.Length - i) / (double)LegitimateValues.Length;
                FAR = (double)ipos / (double)ImpostorValues.Length;

                if (FAR >= FRR)
                {
                    EER = (FAR + FRR) / 2.0;

                    if (ipos >= ImpostorValues.Length) ipos = ImpostorValues.Length - 1;
                    Threshold = (LegitimateValues[i] + ImpostorValues[ipos]) / 2.0;
                    return;
                }
            }

            throw new ArgumentException("Unable to calculate EER");
        }

        public override string ToString()
        {
            return Math.Round(100.0 * EER, 2) + "%/" + Math.Round(Threshold, 2);
        }

        public LatexDocument GenerateLatexPlot(string filename = null)
        {
            if (LegitimateValues == null || ImpostorValues == null)
                return null;

            StringBuilder sb = new StringBuilder();
            Array.Sort(LegitimateValues);
            Array.Sort(ImpostorValues);

            for (int i = 0; i < LegitimateValues.Length; i++)
            {
                int j = i + 1;
                for (; j < LegitimateValues.Length && LegitimateValues[j] == LegitimateValues[i]; j++) ;

                if (LegitimateValues[i] != 0 && sb.Length == 0)
                    sb.Append("(0,100)");

                i = j - 1;
                sb.Append("(");
                sb.Append(LegitimateValues[i]);
                sb.Append(",");
                sb.Append(100.0 * (LegitimateValues.Length - i - 1) / LegitimateValues.Length);
                sb.Append(")");
            }

            sb.AppendLine();
            string frr = sb.ToString();

            sb.Clear();
            for (int i = 0; i < ImpostorValues.Length; i++)
            {
                sb.Append("(");
                sb.Append(ImpostorValues[i]);
                sb.Append(",");
                sb.Append(100.0 * (i+1) / ImpostorValues.Length);
                sb.Append(")");
            }

            if (LegitimateValues[LegitimateValues.Length - 1] > ImpostorValues[ImpostorValues.Length - 1])
                sb.Append("(" + LegitimateValues[LegitimateValues.Length - 1] + ", 100)");

            sb.AppendLine();
            string far = sb.ToString();

            string xmax = Math.Max(LegitimateValues.Max(), ImpostorValues.Max()).ToString();

            string tex_filename = filename == null ? Path.GetTempFileName() : filename;
            LatexDocument latex = new LatexDocument("Latex/Templates/FARFRRERR.template", tex_filename);
            latex.Replace("name", Name);
            latex.Replace("xmax", xmax);
            latex.Replace("eerx", Threshold.ToString());
            latex.Replace("eery", (100.0 * EER).ToString());
            latex.Replace("frr", frr);
            latex.Replace("far", far);
            return latex;
        }
    }
}
