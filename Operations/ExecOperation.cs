using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CopyCat.Parser;
using CopyCat.Utils;

namespace CopyCat.Operations
{
    //Consider allowing storage of process output in variable
    public class ExecOperation : BaseOperation
    {
        public new static bool CommandMatch(string line) => line.Trim().StartsWith("exec", StringComparison.InvariantCultureIgnoreCase);

        private bool Wait { get; }
        private string ExecStr { get; }

        public ExecOperation(CopyCatScript script, string line) : base(script, line)
        {
            string[] segments = Utility.SplitWithQuotations(line);
            string mode = segments[0];
            ExecStr = Utility.ConcatenateFrom(segments, 1);
            Wait = mode.Contains("wait", StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Execute process without waiting for its completion
        /// </summary>
        /// <param name="value"></param>
        private Process Exec(string value)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            string[] arr = Utility.SplitWithQuotations(value);
            psi.FileName = arr[0];
            if (arr.Length > 1)
            {
                string[] args = new string[arr.Length - 1];
                Array.Copy(arr, 1, args, 0, args.Length);
                psi.Arguments = string.Join(StringConstants.Space, args);
            }
            return Process.Start(psi);
        }

        /// <summary>
        /// Execute process and wait for its completion
        /// </summary>
        /// <param name="value"></param>
        private void ExecWait(string value)
        {
            Exec(value).WaitForExit();
        }

        public override ResultCode Execute()
        {
            try
            {
                if (Wait)
                {
                    ExecWait(ExecStr);
                }
                else
                {
                    Exec(ExecStr);
                }
                return ResultCode.SUCCESS;
            }
            catch (Exception ex)
            {
                return Utility.ExceptionToErrCode(ex);
            }
        }

        //Exec(..) or ExecWait(...)
        //private string[] Formats = { "Exec(", "ExecWait(" };
        //public bool CommandMatch(string line) => Formats.Any(format => line.Contains(format, StringComparison.InvariantCultureIgnoreCase));
    }
}
