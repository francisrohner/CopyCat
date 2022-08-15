using System;
using CopyCat.Parser;
using CopyCat.Utils;

namespace CopyCat.Operations
{
    public class LabelOperation : BaseOperation 
    {
        public new static bool CommandMatch(string line) => line.EndsWith(":");
        public LabelOperation(CopyCatScript script, string line) : base(script, line)
        {
        }

        private string Label => Line.TrimEnd(':').Trim();

        public override ResultCode Execute()
        {
            if(Script.SetLabelLineNo(Label, LineNumber))
            {
                return ResultCode.SUCCESS;
            }
            return ResultCode.LABEL_DUPLICATE;
        }
    }
}

