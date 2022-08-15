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
        public int LineNumber { get; set; }
        public CopyCatScript Script { get; }
        public string Line { get; set; }
        public virtual bool PrintResultMessage => true;

        protected BaseOperation(CopyCatScript script, string line)
        {
            Script = script;
            Line = line;
        }

        public static bool CommandMatch(string line) => false;

        public void InterpretVariables()
        {
            Line = Script.ReplaceDateTimeStr(Line);
            Line = Script.ReplaceVariables(Line);
        }

        public abstract ResultCode Execute();
        public override string ToString() => Line;

        public static BaseOperation InstantiateOperation(int lineNo, CopyCatScript script, string line)
        {
            foreach (Type childOperation in Assembly.GetAssembly(typeof(BaseOperation)).GetTypes()
                .Where(type => !type.IsAbstract && type.BaseType == typeof(BaseOperation)))
            {
                MethodInfo method = childOperation.GetMethod("CommandMatch");
                //Console.WriteLine($"Checking {method.DeclaringType}");
                if (method != null)
                {
                    if ((bool)method.Invoke(null, new[] { line }))
                    {
                        try
                        {
                            var ret = (BaseOperation) Activator.CreateInstance(childOperation, script, line);
                            ret.LineNumber = lineNo;
                            return ret;
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
