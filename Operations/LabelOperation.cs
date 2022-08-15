using System;
using CopyCat.Parser;
using CopyCat.Utils;

namespace CopyCat.Operations
{
    public class LabelOperation : BaseOperation 
    {
        public new static bool CommandMatch(string line) => line.StartsWith(":");
        public LabelOperation(CopyCatScript script, string line) : base(script, line)
        {
        }

        private string Label => Line.Trim(':').Trim();

        public override ResultCode ScriptLoadTimeExecute()
        {
            if(Script.SetLabelLineNo(Label, LineNumber))
            {
                return ResultCode.SUCCESS;
            }
            return ResultCode.LABEL_DUPLICATE;
        }

        public override ResultCode Execute()
        {
            return ResultCode.SUCCESS;
        }
    }
}

