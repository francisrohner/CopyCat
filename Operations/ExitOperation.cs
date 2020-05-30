using CopyCat.Parser;
using CopyCat.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace CopyCat.Operations
{
    public class ExitOperation : BaseOperation
    {
        public new static bool CommandMatch(string line) => line.Trim().StartsWith("exit", StringComparison.InvariantCultureIgnoreCase);
        public ExitOperation(CopyCatScript script, string line) : base(script, line)
        {

        }
        public override ResultCode Execute()
        {
            return ResultCode.EXIT_REQUESTED;
        }

    }
}
