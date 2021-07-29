using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using System.IO;


namespace KSDExperiments.Latex
{
    public class LatexDocument
    {
        public static string SharedPreamble { get; private set; }
        public static DirectoryInfo Folder { get; private set; }
        public static void Initialize(string folder, string shared_preamble = null)
        {
            Folder = new DirectoryInfo(folder);

            if (!Folder.Exists)
                Folder.Create();

            SharedPreamble = shared_preamble;
        }

        public LatexCompiler Compiler { get; private set; }

        public LatexDocument(string template_file, string name, LatexCompiler compiler = LatexCompiler.PDF)
        {
            Name = name;
            Compiler = compiler;

            Template = File.ReadAllText(template_file);
            Text = Template;

            ShowWindow = false;
        }

        public string Name { get; private set; }

        public string Template { get; private set; }
        public string Text { get; private set; }

        public void Replace(string tag, string text, bool raw = false)
        {
            string ftag = "<%=" + tag + "%>";
            if (!Text.Contains(ftag))
                throw new ArgumentException("Tag " + tag + " is not contained in template.");

            if (raw)
                Text = Text.Replace(ftag, text);
            else
                Text = Text.Replace(ftag, text.Replace("_", @"\_"));
        }

        static List<Process> pending_processes = new List<Process>();
        public static void ClosePendingProcesses()
        {
            foreach (Process p in pending_processes)
                try
                {
                    p.Kill();
                }
                catch
                {
                }

            foreach (Process p in Process.GetProcesses().Where(p => p.ProcessName.Contains("Foxit")))
                try
                {
                    p.Kill();
                }
                catch
                {
                }

            foreach (Process p in Process.GetProcesses().Where(p => p.ProcessName.Contains("pdflatex")))
                try
                {
                    p.Kill();
                }
                catch
                {
                }

            foreach (Process p in Process.GetProcesses().Where(p => p.ProcessName.Contains("lualatex")))
                try
                {
                    p.Kill();
                }
                catch
                {
                }
        }

        void PrecompileHeader(string source)
        {
            string preamble_file = Path.Combine(Folder.FullName, Name) + ".preamble";
            if (SharedPreamble != null)
                preamble_file = Path.Combine(Folder.FullName, SharedPreamble) + ".preamble";

            string tex_file = Path.Combine(Folder.FullName, Name) + ".tex";

            int pos = source.IndexOf(@"\endofdump");
            if (pos == -1)
            {
                File.WriteAllText(tex_file, source);
                return;
            }

            string current_preamble = source.Substring(0, pos);
            string old_preamble = "";

            if (File.Exists(preamble_file))
                old_preamble = File.ReadAllText(preamble_file);

            FileInfo fmt = new FileInfo(Path.Combine(Folder.FullName, Name) + ".fmt");
            if ( SharedPreamble != null)
                fmt = new FileInfo(Path.Combine(Folder.FullName, SharedPreamble) + ".fmt");

            string new_fmt = Path.Combine(Folder.FullName, Name) + ".fmt";

            if (fmt.Exists && current_preamble == old_preamble)
            {
            }
            else
            {
                File.WriteAllText(preamble_file, current_preamble);
                File.WriteAllText(tex_file, source);

                Process p = RunLatexProcess(GetPrecompilerName(), "-ini -jobname=\"" + Name +
                                                      "\" \"&pdflatex\" mylatexformat.ltx " + Name + ".tex");

                if (p.ExitCode != 0 && p.ExitCode != 1)
                    throw new Exception("UNABLE TO PRECOMPILE HEADER.");

                // if (fmt.Exists) fmt.Delete();
                // File.Copy(new_fmt, fmt.FullName);
            }

            pos += 10;
            string tmp = "%&" + Name + Environment.NewLine + source.Substring(pos);
            File.WriteAllText(tex_file, tmp);
        }

        public bool ShowWindow { get; set; }

        string GetMiktexPath()
        {
            string retval = @"C:\Program Files\MiKTeX 2.9\miktex\bin\x64\";
            if (!Directory.Exists(retval))
            {
                retval = @"C:\Program Files\MiKTeX\miktex\bin\x64\";

                if (!Directory.Exists(retval))
                    throw new FileNotFoundException("Cannot find Miktex distribution.");
            }

            return retval;
        }

        Process RunLatexProcess(string name, string arguments)
        {
            string PATH = GetMiktexPath();

            Process p = new Process();
            p.StartInfo.FileName = Path.Combine(PATH, name);
            p.StartInfo.Arguments = "-interaction=nonstopmode " + arguments;
            p.StartInfo.WorkingDirectory = Folder.FullName;

            if (!ShowWindow)
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            p.Start();
            p.WaitForExit();

            if (p.ExitCode != 0 && p.ExitCode != 1)
            {
                Console.WriteLine("ERROR IN " + Name);
                throw new ArgumentException("BLGBLG");
            }

            return p;
        }

        public bool Compile(string output_filename)
        {
            string filename = Folder.FullName + "/" + Name + ".pdf";
            if (File.Exists(filename)) File.Delete(filename);
            if (!Compile())
                return false;

            string new_filename = Folder.FullName + "/" + output_filename + ".pdf";
            if (File.Exists(new_filename)) File.Delete(new_filename);
            File.Move(filename, new_filename);
            return true;
        }

        public bool Compile()
        {
            return Compile(false);
        }

        string GetPrecompilerName()
        {
            if (Compiler == LatexCompiler.PDF)
                return "pdftex";
            else
                return "luatex";
        }

        string GetCompilerName()
        {
            if (Compiler == LatexCompiler.PDF)
                return "pdflatex";
            else
                return "lualatex";
        }

        public virtual bool Compile(bool show)
        {
            string tmp = Text.Replace("<%=defines%>", defines.ToString());
            PrecompileHeader(tmp);

            Process p = RunLatexProcess(GetCompilerName(), Name + ".tex");

            if (p.ExitCode == 0 || p.ExitCode == 1)
            {
                if (show)
                    Show();

                return true;
            }
            else
            {
                throw new Exception("ERROR!!!");
            }
        }

        public void Show()
        {
            Process p = new Process();
            p.StartInfo.FileName = Path.Combine(Folder.FullName, Path.GetFileName(Name) + ".pdf");
            p.StartInfo.WorkingDirectory = Folder.FullName;
            p.Start();
            pending_processes.Add(p);
        }

        StringBuilder defines = new StringBuilder();
        public void Define(string define_name)
        {
            defines.AppendLine(@"\newcommand{\" + define_name + "}{}");
        }
    }
}
