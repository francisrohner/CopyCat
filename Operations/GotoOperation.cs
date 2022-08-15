using System;
using CopyCat.Parser;
using CopyCat.Utils;

namespace CopyCat.Operations
{
    public class GotoOperation : BaseOperation
    {
        public new static bool CommandMatch(string line) => line.StartsWith("goto", StringComparison.InvariantCultureIgnoreCase);
        public GotoOperation(CopyCatScript script, string line) : base(script, line)
        {
        }

        private string TargetLabel => Line.Replace("goto", string.Empty, StringComparison.InvariantCultureIgnoreCase).Trim();

        public override ResultCode Execute()
        {
            int lineNo = Script.GetLabelLineNo(TargetLabel);
            if(lineNo > -1)
            {
                Script.JumpToLine(lineNo);
                return ResultCode.SUCCESS;
            }
            return ResultCode.LABEL_NOT_FOUND;
        }
    }
}

