using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Nyan.Core.Modules.Environment
{
    public static class Shell
    {
        public static string Execute(string pCommand, string pParameters)
        {
            var ret = new StringBuilder();

            var procShell = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    FileName = pCommand,
                    Arguments = pParameters,

                    UseShellExecute = false,
                    CreateNoWindow = true,

                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            procShell.Start();

            procShell.OutputDataReceived += (s, e) => { ret.Append(e.Data + "\r\n"); };

            procShell.BeginOutputReadLine();
            procShell.BeginErrorReadLine();

            procShell.WaitForExit();

            //var streamReader = new StreamReader(procShell.StandardOutput.BaseStream, procShell.StandardOutput.CurrentEncoding);



            //do
            //{
            //    var line = streamReader.ReadLine();
            //    if (line == null) break;

            //    ret.Append(line + "\r\n");
            //} while (true);

            //streamReader.Close();

            procShell.Close();

            return ret.ToString();
        }
    }
}