using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CopyCat.Operations;
using CopyCat.Utils;

namespace CopyCat.Parser
{

    public class CopyCatScript
    {
        private Dictionary<string, int> Labels { get; } = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
        private Dictionary<string, string> Variables { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private string OperatingSystem { get; set; }
        private string FilePath { get; }
        private List<BaseOperation> Operations { get; set; }
        private int ExecutionIndex { get; set; }
        public int ErrorCount { get; private set; }

        public string GetVariableStr(string key) => Variables.TryGetValue(key, out string value) ? value : string.Empty;

        public bool SetLabelLineNo(string label, int lineNumber)
        {
            if(Labels.ContainsKey(label))
            {
                return false;
            }
            Labels[label] = lineNumber;
            return true;
        }
        public int GetLabelLineNo(string label) => Labels.TryGetValue(label, out int index) ? index : -1;

        public void SetVariable(string key, string value) => Variables[key] = value;

        public bool CheckOSMatch(string os)
        {
            if (OperatingSystem == null || os == null) return true; //Unable to compare
            return OperatingSystem.ToLower().Equals(os.ToLower());
        }

        /// <summary>
        /// Replace Date/Time wildcards with current date time Ex: #YYYY# => 2019
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string ReplaceDateTimeStr(string value)
        {

            StringBuilder ret = new StringBuilder();
            if (!value.Contains('#'))
            {
                return value;
            }

            int? start = null;
            int i = 0;
            for (i = 0; i < value.Length; i++)
            {
                if (value[i] == '#')
                {
                    if (start.HasValue) //end
                    {
                        string dateTimeFormat = value.Substring(start.Value + 1, (i - 1) - start.Value);
                        ret.Append(DateTime.Now.ToString(dateTimeFormat));
                        start = null;
                    }
                    else
                    {
                        start = i;
                    }
                }
                else if (!start.HasValue)
                {
                    ret.Append(value[i]);
                }
            }

            //TODO refactor function, come up with better solution
            if (start != null) //not d/t format
            {
                ret.Append("#");
                ret.Append(value.Substring(start.Value + 1, (i - 1) - start.Value));
            }

            return ret.ToString();
        }

        public static ResultCode Execute(string filePath)
        {
            return new CopyCatScript(filePath).Execute();
        }

        public CopyCatScript(string filePath)
        {
            FilePath = filePath;
        }

        public string ReplaceVariables(string line)
        {
            string cline = (string)line.Clone();
            foreach (string tag in Variables.Keys)
            {
                string var = $"%{tag}%";
                if (cline.Contains(var))
                {
                    cline = line.Replace(var, Variables[tag]);
                }
            }
            return cline;
        }

        public void JumpToLine(int lineNo)
        {
            ExecutionIndex = lineNo;
        }

        public (bool Success, List<BaseOperation> Operations) ParseScript()
        {
            if (!File.Exists(FilePath))
            {
                return (false, null);
            }
            List<BaseOperation> operations = new List<BaseOperation>();

            int lineNumber = 0;
            using (StreamReader scriptReader = new StreamReader(FilePath))
            {
                while (!scriptReader.EndOfStream)
                {
                    string line = scriptReader.ReadLine().Trim();
                    if (line == null || line.StartsWith("//") || string.IsNullOrEmpty(line.Trim()))
                    {
                        continue; //comment, or other negligble line
                    }

                    //line = ReplaceDateTimeStr(line);
                    //line = ReplaceVariables(line);
                    var operation = BaseOperation.InstantiateOperation(lineNumber++, this, line);
                    if (operation == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Unrecognized command: {line}");
                        Console.ResetColor();
                        continue; //invalid op
                    }
                    operation.ScriptLoadTimeExecute(); //TODO handle load time failures
                    operations.Add(operation);
                }
            }

            return (true, operations);

        }

        public ResultCode Execute()
        {
            if (!File.Exists(FilePath))
            {
                return ResultCode.FILE_NOT_FOUND;
            }

            var result = ParseScript();
            Operations = result.Operations;

            ErrorCount = 0;
            int count = Operations.Count();
            for (ExecutionIndex = 0; ExecutionIndex < count; ExecutionIndex++)
            {
                var operation = Operations[ExecutionIndex];
                ResultCode ec;
                try
                {
                    operation.InterpretVariables();
                    ec = operation.Execute();
                }
                catch (Exception ex)
                {
                    ec = Utility.ExceptionToErrCode(ex);
                    if (Variables.TryGetValue("exit_on_error", out string exitOnErr) &&
                        exitOnErr.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    {
                        break;
                    }
                }

                if (ec == ResultCode.EXIT_REQUESTED)
                {
                    return ec; //early abort
                }

                if (ec > 0)
                {
                    Utility.WriteSuccess($"{operation} Succeeded", !operation.PrintResultMessage);
                }
                else
                {
                    ++ErrorCount;
                    Utility.WriteError($"{operation} Failed: [{ec}]", !operation.PrintResultMessage);
                }

            }

            return (ErrorCount == 0) ? ResultCode.SUCCESS : ResultCode.FAILED;
        }


        /// <summary>
        /// Get string for Operating System
        /// </summary>
        /// <returns></returns>
        public static string GetOS_Str()
        {
            foreach (PropertyInfo platform in typeof(OSPlatform).GetProperties())
            {
                if (platform.PropertyType == typeof(OSPlatform))
                {
                    var value = platform.GetValue(null);
                    if (RuntimeInformation.IsOSPlatform((OSPlatform)value))
                    {
                        return platform.Name.ToLower();
                    }
                }
            }
            return null;
            //if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            //    return "mac";
            //else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            //    return "linux";
            //else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            //    return "windows";
            //return null;
        }
    }
}
