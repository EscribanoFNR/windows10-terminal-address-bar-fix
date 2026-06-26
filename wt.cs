using System;
using System.Diagnostics;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        // Ruta absoluta al ejecutable REAL de Windows Terminal.
        // ¡Cámbiala por la ruta que obtuviste con fsutil en tu equipo!
        string realWt = @"C:\Program Files\WindowsApps\"
            + @"Microsoft.WindowsTerminal_1.24.11321.0_x64__8wekyb3d8bbwe\"
            + "wt.exe";

        var sb = new StringBuilder();
        bool hasDir = false;

        foreach (var a in args)
        {
            if (sb.Length > 0)
                sb.Append(' ');

            if (a.Contains(" ") || a.Contains("\""))
                sb.Append("\"").Append(a.Replace("\"", "\\\"")).Append("\"");
            else
                sb.Append(a);

            if (a == "-d" || a == "--directory")
                hasDir = true;
        }

        if (!hasDir)
        {
            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append("-d .");
        }

        var psi = new ProcessStartInfo(realWt, sb.ToString())
        {
            UseShellExecute = false,
            WorkingDirectory = Environment.CurrentDirectory
        };

        Process.Start(psi);
    }
}
