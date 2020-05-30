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
        public Dictionary<string, string> Variables { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        //List<BaseOperation> Operations { get; } = new List<BaseOperation>();
        public string OperatingSystem { get; set; }
        public string FilePath { get; }

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
        private static string ReplaceDateTimeStr(string value)
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

        protected CopyCatScript(string filePath)
        {
            FilePath = filePath;
        }

        private string ReplaceVariables(string line)
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

        public ResultCode Execute()
        {
            if (!File.Exists(FilePath))
            {
                return ResultCode.FILE_NOT_FOUND;
            }
            int successCount = 0;
            int totalOperations = 0;

            //Note: This interpet/execute on the fly view may make logical eval and jump ops harder
            int lineNumber = 0;
            using (StreamReader scriptReader = new StreamReader(FilePath))
            {
                //Pass 1 -- Read script, decipher variables
                while (!scriptReader.EndOfStream)
                {
                    string line = scriptReader.ReadLine();
                    ++lineNumber;

                    if (line == null || line.StartsWith("//") || string.IsNullOrEmpty(line.Trim()))
                    {
                        continue;
                    }

                    line = ReplaceDateTimeStr(line);
                    line = ReplaceVariables(line);
                    var operation = BaseOperation.InstantiateOperation(lineNumber, this, line);
                    if (operation == null)
                    {
                        continue; //invalid op
                    }
                    ++totalOperations;

                    ResultCode ec = ResultCode.FAILED;
                    try
                    {
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
                        ++successCount;
                        Utility.WriteSuccess($"{operation} Succeeded");
                    }
                    else
                    {
                        Utility.WriteError($"{operation} Failed: [{ec}]");
                    }
                }
            }

            return (successCount == totalOperations) ? ResultCode.SUCCESS : ResultCode.FAILED;
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
