using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using CopyCat.Parser;
using CopyCat.Utils;

namespace CopyCat.Operations
{
    public abstract class BaseOperation
    {
        public CopyCatScript Script { get; }
        public string Raw { get; }
        protected BaseOperation(CopyCatScript script, string line)
        {
            Script = script;
            Raw = line;
        }
        public static bool CommandMatch(string line) => false;

        public virtual ResultCode Execute() => ResultCode.NOT_IMPLEMENTED;
        public override string ToString() => Raw;



        public static BaseOperation InstantiateOperation(int lineNo, CopyCatScript script, string line)
        {
            foreach (Type childOperation in Assembly.GetAssembly(typeof(BaseOperation)).GetTypes()
                .Where(type => !type.IsAbstract))
            {
                MethodInfo? method = childOperation.GetMethod("CommandMatch");
                if (method != null)
                {
                    if ((bool)method.Invoke(null, new[] { line }))
                    {
                        try
                        {
                            return (BaseOperation) Activator.CreateInstance(childOperation, script, line);
                        }
                        catch
                        {
                            Utility.WriteError($"Failed to parse Ln{lineNo} - {line} // Invalid format for {childOperation.Name}");
                        }
                    }
                }
            }
            return null;
        }
    }
}
