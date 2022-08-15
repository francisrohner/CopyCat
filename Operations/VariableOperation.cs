using System;
using System.Collections.Generic;
using System.Text;
using CopyCat.Parser;
using CopyCat.Utils;

namespace CopyCat.Operations
{
    public class VariableOperation : BaseOperation
    {
        public new static bool CommandMatch(string line) => line.Trim().StartsWith("setvar", StringComparison.InvariantCultureIgnoreCase);

        public VariableOperation(CopyCatScript script, string line) : base(script, line)
        {

        }
        public override ResultCode ScriptLoadTimeExecute()
        {
            string line = Line.TrimStart().Substring("setvar".Length);
            string[] segments = line.Trim().Split('=');
            string tag = segments[0].Trim();
            string value = segments[1];
            Script.SetVariable(tag, value);

            //Special var
            if (tag.Equals("os", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!Script.CheckOSMatch(value))
                {
                    return ResultCode.OS_MISMATCH;
                }
            }
            return ResultCode.SUCCESS;
        }

        public override ResultCode Execute()
        {
            return ResultCode.SUCCESS;
        }
    }
}
