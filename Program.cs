using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CopyCat
{
    public class Program
    {

        enum ERROR_CODES
        {
            SOURCE_NOT_FOUND = -806,
            OS_MISMATCH = -805,
            DIRECTORY_NOT_FOUND = -804,
            FILE_NOT_FOUND = -803,
            ARGUMENT_NULL_EXCEPTION = -802,
            ARGUMENT_EXCEPTION = -801,
            UNAUTHORIZED_EXCEPTION = -800,
            HTTP_INTERNAL_SERVER_ERROR = -500,
            HTTP_NOT_FOUND = -404,
            HTTP_FORBIDDEN = -403,
            HTTP_UNAUTHORIZED = -401,
            HTT_BAD_REQUEST = -400,
            FAILED = -1,
            SUCCESS = 1,
        }

        public static char file_separator;
        public static bool overwrite_mode = true;


        public interface Operation
        {
            int Execute();
            string GetDesc();
        }
        public class CopyOperation : Operation
        {
            private string _source;
            private string _destination;
            public CopyOperation(string source, string destination)
            {
                _source = source;
                _destination = destination;
            }

            public string GetDesc()
            {
                if (Directory.Exists(_source))
                {
                    return "CopyDirectory, " + _source + " => " + _destination;
                }
                else
                {
                    return "CopyFile, " + _source + " => " + _destination;
                }
            }
            /// <summary>
            /// Return just the file name form a file path. Ex: C:\Users\Frank\someFile.txt => someFile.txt
            /// </summary>
            /// <param name="filePath"></param>
            /// <returns></returns>
            private string _GetFileName(string filePath)
            {
                if (filePath.Contains(file_separator))
                    return filePath.Substring(filePath.LastIndexOf(file_separator) + 1);
                return filePath;
            }

            //Ref: https://docs.microsoft.com/en-us/dotnet/api/system.io.directoryinfo?redirectedfrom=MSDN&view=netframework-4.7.2
            /// <summary>
            /// Copy a directory and all it's subdirectories to specified destination
            /// </summary>
            /// <param name="source"></param>
            /// <param name="destination"></param>
            private void _CopyDirectory(string source, string destination)
            {
                DirectoryInfo sourceDirInfo = new DirectoryInfo(source);
                DirectoryInfo destDirInfo = new DirectoryInfo(destination);
                _CopyAll(sourceDirInfo, destDirInfo);
            }

            //Ref: https://docs.microsoft.com/en-us/dotnet/api/system.io.directoryinfo?redirectedfrom=MSDN&view=netframework-4.7.2
            /// <summary>
            /// Copy directory and all its subdirectories recursively
            /// </summary>
            /// <param name="source"></param>
            /// <param name="destination"></param>
            private void _CopyAll(DirectoryInfo source, DirectoryInfo destination)
            {
                Directory.CreateDirectory(destination.FullName);

                // Copy each file into the new directory.
                foreach (FileInfo fi in source.GetFiles())
                {
                    //Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                    fi.CopyTo(Path.Combine(destination.FullName, fi.Name), true);
                }

                // Copy each subdirectory using recursion.
                foreach (DirectoryInfo di in source.GetDirectories())
                {
                    DirectoryInfo nextSubDirInfo = destination.CreateSubdirectory(di.Name);
                    _CopyAll(di, nextSubDirInfo);
                }
            }
            /// <summary>
            /// Copy source file or folder to destination
            /// </summary>
            /// <returns></returns>
            public int Execute()
            {
                int ret;
                if (File.Exists(_source))
                {
                    //File operation
                    if (Directory.Exists(_destination)) //Operation entails copying file to a directory
                    {
                        File.Copy(_source, _destination + file_separator + _GetFileName(_source), overwrite_mode);
                    }
                    else //No directory found, destination is file name
                    {
                        try
                        {
                            if (File.Exists(_source))
                            {
                                File.Copy(_source, _destination, overwrite_mode);
                            }
                            else
                            {
                                return (int)ERROR_CODES.FILE_NOT_FOUND;
                            }
                        }
                        catch (Exception ex)
                        {
                            return Program.ExceptionToErrCode(ex);
                        }
                    }
                }
                else if (Directory.Exists(_source))
                {
                    try
                    {
                        _CopyDirectory(_source, _destination);
                    }
                    catch (Exception ex)
                    {
                        return Program.ExceptionToErrCode(ex);
                    }
                }
                else
                {
                    ret = (int)ERROR_CODES.SOURCE_NOT_FOUND;
                }
                ret = (int)ERROR_CODES.SUCCESS;
                return ret;
            }
        }
        public class DownloadOperation : Operation
        {

            private string _url;
            private string _localFile;

            public DownloadOperation(string url, string localFile)
            {
                _url = url;
                _localFile = localFile;
            }

            /// <summary>
            /// Send HTTP Web request to download requested resource
            /// Return an error code which will indicate whether the operation
            /// succeeded and if not, why it has failed.
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            private int _PerformDownload(out byte[] data)
            {
                int ret = (int)ERROR_CODES.FAILED;
                data = null;
                try
                {
                    HttpWebRequest htreq = (HttpWebRequest)WebRequest.Create(_url);
                    HttpWebResponse htresp = (HttpWebResponse)htreq.GetResponse();
                    MemoryStream ms = new MemoryStream();
                    Stream stream = htresp.GetResponseStream();
                    stream.CopyTo(ms);
                    stream.Close();
                    ms.Close();
                    data = ms.ToArray();
                    ret = (int)ERROR_CODES.SUCCESS;
                }
                catch (WebException wex)
                {
                    HttpWebResponse err_resp = wex.Response as HttpWebResponse;
                    ret = ((int)err_resp.StatusCode) * -1; //Ex: 404 => -404
                }
                catch
                {
                    ret = (int)ERROR_CODES.FAILED;
                }
                return ret;
            }
            public int Execute()
            {
                //Download("http://xyz.com/resouce.txt", "C:\Users\XYZ\resource.txt")
                byte[] rx;
                int ret = _PerformDownload(out rx);
                if (ret > 0)
                {
                    //save file
                    File.WriteAllBytes(_localFile, rx);
                    return ret;
                }
                return ret;
            }

            public string GetDesc()
            {
                return "Download," + _url + " => " + _localFile;
            }

        }
        public class ExecOperation : Operation
        {
            private bool _wait;
            private string _exec_str;

            public string GetDesc()
            {
                if (_wait)
                {
                    return "ExecWait, " + _exec_str;
                }
                else
                {
                    return "Exec, " + _exec_str;
                }
            }

            /// <summary>
            /// Break apart string containing path to executable and various arguments
            /// Ex: "C:\Program Files (x86)\SomePath\SomeProgram.exe" --someTag someValue
            /// </summary>
            /// <param name="exec_str"></param>
            /// <returns></returns>
            private string[] _spl_with_quotations(string exec_str)
            {
                bool encl = false;
                List<string> args = new List<string>();
                string curr_str = string.Empty;
                for(int i = 0; i < exec_str.Length; i++)
                {
                    char cur_char = exec_str[i];
                    if (cur_char == '"')
                    {
                        encl = !encl;
                    }
                    else if(cur_char == ' ')
                    {
                        if(!encl)
                        {
                            args.Add(curr_str);
                            curr_str = string.Empty;
                        }
                        else
                        {
                            curr_str += cur_char;
                        }
                    }
                    else
                    {
                        curr_str += cur_char;
                    }
                }
                if(!string.IsNullOrEmpty(curr_str))
                {
                    args.Add(curr_str);
                }
                return args.ToArray();
            }

            /// <summary>
            /// Concatinate string array with spaces starting from specified start index
            /// </summary>
            /// <param name="args"></param>
            /// <param name="start_index"></param>
            /// <returns></returns>
            private string _concatinate_from(string[] args, int start_index)
            {
                string ret = string.Empty;
                for(int i = start_index; i < args.Length; i++)
                {
                    ret += args[i] + " ";
                }
                ret = ret.Substring(0, ret.Length - 1); //trim last space
                return ret;
            }
        
            public ExecOperation(string exec_str)
            {
                _exec_str = exec_str;
                _wait = false;
            }
            public ExecOperation(string exec_str, bool wait)
            {
                _exec_str = exec_str;
                _wait = wait;
            }

            /// <summary>
            /// Execute process without waiting for its completion
            /// </summary>
            /// <param name="exec_str"></param>
            private Process _Exec(string exec_str)
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                string[] arr = _spl_with_quotations(exec_str);
                psi.FileName = arr[0];
                if (arr.Length > 1)
                {
                    psi.Arguments = _concatinate_from(arr, 1);
                }
                return Process.Start(psi);
            }

            /// <summary>
            /// Execute process and wait for its completion
            /// </summary>
            /// <param name="exec_str"></param>
            private void _ExecWait(string exec_str)
            {
                _Exec(exec_str).WaitForExit();
            }

            public int Execute()
            {
                try
                {
                    if (_wait)
                    {
                        _ExecWait(_exec_str);
                    }
                    else
                    {
                        _Exec(_exec_str);
                    }

                    return (int)ERROR_CODES.SUCCESS;
                }
                catch (Exception ex)
                {
                    return Program.ExceptionToErrCode(ex);
                }
            }
        }


        public class CopyCatScript
        {
            private Dictionary<string, string> variables = new Dictionary<string, string>();
            List<Operation> operations = new List<Operation>();
            private string _os;

            public bool IsForOS(string os)
            {
                if (_os == null || os == null) return true; //Unable to compare
                return _os.ToLower().Equals(os.ToLower());
            }
            /// <summary>
            /// Replace Date/Time wildcards with current date time Ex: #YYYY# => 2019
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            private static string _ReplaceDateTimeStr(string value)
            {
                string ret = string.Empty;
                if (value.Contains("#"))
                {
                    int start = -1;
                    for (int i = 0; i < value.Length; i++)
                    {
                        if (value[i] == '#')
                        {
                            if (start == -1)
                            {
                                start = i;
                            }
                            else
                            {
                                //Process
                                string dt_format = value.Substring(start + 1, (i - 1) - start);
                                ret += DateTime.Now.ToString(dt_format);
                            }
                        }
                        else if (start != -1)
                        {
                            //Currently inside date/time wildcard, don't output chars
                        }
                        else
                        {
                            ret += value[i];
                        }
                    }
                    return ret;
                }
                else
                {
                    return value;
                }
            }
            public CopyCatScript(string filePath)
            {
                StreamReader scriptReader = new StreamReader(filePath);
                List<string> _intermediate = new List<string>();
                while (!scriptReader.EndOfStream)
                {
                    string line = scriptReader.ReadLine();
                    if (line.StartsWith("//")) continue; //skip comment line
                    if (string.IsNullOrEmpty(line.Trim())) continue; //skip blank line
                    line = _ReplaceDateTimeStr(line);
                    if (line.Contains("=") && !(line.Contains("=>") || line.Contains("<="))) //variable
                    {
                        string tag, val;
                        tag = line.Split('=')[0].Trim();
                        val = line.Split('=')[1].Trim();
                        variables.Add(tag, val);
                    }
                    else
                    {
                        _intermediate.Add(line);
                    }
                }
                scriptReader.Close();

                //Second Pass
                //Process variables
                _os = _ValueFromDict(variables, "os");
                foreach (string line in _intermediate)
                {
                    string fline = line;
                    //First replace vars
                    foreach (string tag in variables.Keys)
                    {
                        string var = "%" + tag + "%";
                        if (fline.Contains(var))
                        {
                            fline = line.Replace(var, variables[tag]);
                        }
                    }
                    operations.Add(_OperationFromLine(fline));
                }


            }

            /// <summary>
            /// Parse a line of the script and return the corresponding Operation
            /// </summary>
            /// <param name="line"></param>
            /// <returns></returns>
            private Operation _OperationFromLine(string line)
            {
                Operation ret = null;
                string source, destination;
                string tag, value;
                if (line.Contains("=>"))
                {
                    source = line.Substring(0, line.IndexOf("=>")).Trim();
                    destination = line.Substring(line.IndexOf("=>") + 2).Trim();
                    ret = new CopyOperation(source, destination);
                }
                else if (line.Contains("<="))
                {
                    destination = line.Substring(0, line.IndexOf("<=")).Trim();
                    source = line.Substring(line.IndexOf("<=") + 1).Trim();
                    ret = (new CopyOperation(source, destination));
                }
                else if (line.Contains("(")) //exec(...), exec_wait(...)
                {
                    tag = line.Substring(0, line.IndexOf("(")).Trim(); //get exec type (wait or no wait)
                    value = line.Substring(line.IndexOf("(") + 1).Trim(')').Trim(); //remove enclosing parenthesis
                    if (tag.ToLowerInvariant().Equals("exec_wait"))
                    {
                        ret = new ExecOperation(value, true);
                    }
                    else if (tag.ToLowerInvariant().Equals("exec"))
                    {
                        ret = new ExecOperation(value);
                    }
                }
                return ret;
            }

            /// <summary>
            /// Get value from dictionary with key specified (case-insensitive)
            /// </summary>
            /// <param name="dict"></param>
            /// <param name="key"></param>
            /// <returns></returns>
            private string _ValueFromDict(Dictionary<string, string> dict, string key)
            {
                foreach (string ckey in dict.Keys)
                    if (ckey.ToLowerInvariant().Equals(key))
                        return dict[ckey];
                return null;
            }
            public int Execute()
            {
                //If script is not intender for current operating system, return OS mismatch
                if (!IsForOS(Program.GetOS_Str()))
                    return (int)ERROR_CODES.OS_MISMATCH;

                int success_count = 0;
                for (int i = 0; i < operations.Count; i++)
                {
                    int ec = operations[i].Execute();
                    if (ec > 0)
                    {
                        ++success_count;
                        _WriteSuccess(operations[i].GetDesc() + " Succeeded");
                    }
                    else
                    {
                        _WriteError(operations[i].GetDesc() + " Failed: [" + (ERROR_CODES)ec + "]");
                    }
                }
                return (success_count == operations.Count) ? (int)ERROR_CODES.SUCCESS : (int)ERROR_CODES.FAILED;
            }
        }

        /// <summary>
        /// Get string for Operating Sytem
        /// </summary>
        /// <returns></returns>
        public static string GetOS_Str()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "mac";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "linux";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "windows";
            return null;
        }

        public static void Main(string[] args)
        {

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                file_separator = '/';
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                file_separator = '\\';
            }
            else
            {
                _WriteError("CopyCat is unsupported on your operating system :(");
            }

            _PrintEnclosed("CopyCat", '=', ConsoleColor.White);
            _PrintAsciiCat();

            Dictionary<int, string> scripts = _GetScripts();
            if (scripts.Count == 0)
            {
                _WriteWarning("No scripts were available, exiting program...");
                if (Debugger.IsAttached)
                {
                    _WaitKey("Exit", ConsoleColor.White);
                }
                return;
            }
            foreach (int key in scripts.Keys)
            {
                string short_file = scripts[key];
                if (short_file.Contains(file_separator))
                {
                    short_file = short_file.Substring(short_file.LastIndexOf(file_separator) + 1);
                }
                Console.WriteLine("{0} - {1}", key, short_file);
            }
            Console.Write("Enter the name or number of a script: ");
            string user_input = Console.ReadLine();
            string script = _GetSelectedScript(scripts, user_input);
            if (script == null)
            {
                _WriteError("Please enter a valid number or script name...");
            }
            else
            {
                CopyCatScript ccs = new CopyCatScript(script);
                ccs.Execute();
            }
            if (Debugger.IsAttached)
            {
                _WaitKey("Exit", ConsoleColor.White);
            }

        }


        /// <summary>
        /// Return a dictionary of all Copy Cat Scripts (.ccs) files
        /// </summary>
        /// <returns></returns>
        private static Dictionary<int, string> _GetScripts()
        {
            Dictionary<int, string> ret = new Dictionary<int, string>();
            int index = 0;
            string copyCatPath = Assembly.GetExecutingAssembly().Location;
            copyCatPath = copyCatPath.Substring(0, copyCatPath.LastIndexOf(file_separator));
            foreach (string file in Directory.GetFiles(copyCatPath, "*.ccs"))
            {
                ret.Add(index++, file);
            }
            return ret;
        }

        /// <summary>
        /// Get selected script based on number or string entered by user
        /// </summary>
        /// <param name="scripts"></param>
        /// <param name="user_input"></param>
        /// <returns></returns>
        public static string _GetSelectedScript(Dictionary<int, string> scripts, string user_input)
        {
            int number = -1;
            int.TryParse(user_input, out number);
            if (number >= 0 && number < scripts.Count)
            {
                return scripts[number];
            }
            else //do loose search on values
            {
                foreach (string script in scripts.Values)
                {
                    if (script.ToLower().Contains(user_input.ToLower()))
                    {
                        return script;
                    }
                }
            }
            return null; //not found

        }


        /// <summary>
        /// Return the error code corresponding to the exception given
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static int ExceptionToErrCode(Exception ex)
        {
            int ret;
            if (ex is UnauthorizedAccessException)
            {
                ret = (int)ERROR_CODES.UNAUTHORIZED_EXCEPTION;
            }
            else if (ex is DirectoryNotFoundException)
            {
                ret = (int)ERROR_CODES.DIRECTORY_NOT_FOUND;
            }
            else if (ex is FileNotFoundException)
            {
                ret = (int)ERROR_CODES.FILE_NOT_FOUND;
            }
            else //General
            {
                ret = (int)ERROR_CODES.FAILED;
            }
            return ret;
        }

        #region ConsoleFunctions

        /// <summary>
        /// Print "Press any key to 'action" in console, then wait for key press
        /// </summary>
        /// <param name="action"></param>
        /// <param name="fore_color"></param>
        private static void _WaitKey(string action, ConsoleColor fore_color)
        {
            Console.ForegroundColor = fore_color;
            Console.Write("Press any key to {0}...", action);
            Console.ReadKey();
            Console.ResetColor();
        }

        /// <summary>
        /// Print text enclosed by character specified
        /// </summary>
        /// <param name="text"></param>
        /// <param name="enclose_char"></param>
        /// <param name="fore_color"></param>
        private static void _PrintEnclosed(string text, char enclose_char, ConsoleColor fore_color)
        {
            Console.ForegroundColor = fore_color;
            int start_char = (Console.WindowWidth / 2) - (text.Length / 2);
            for (int i = 0; i < Console.WindowWidth; i++)
            {
                if (i >= start_char && i < (start_char + text.Length))
                {
                    Console.Write(text[i - start_char]);
                }
                else
                {
                    Console.Write(enclose_char);
                }
            }
            Console.ResetColor();
        }

        /// <summary>
        /// Print ascii cat art to console
        /// </summary>
        private static void _PrintAsciiCat()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "CopyCat.ascii-cat.txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                reader.ReadLine(); //Skip credit line
                string result = reader.ReadToEnd();
                _WriteSuccess(result);
            }
        }

        /// <summary>
        /// Print info string to console with white foreground
        /// </summary>
        /// <param name="info_str"></param>
        public static void _WriteInfo(string info_str)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(info_str);
            Console.ResetColor();
        }

        /// <summary>
        /// Print success string to console with red foreground
        /// </summary>
        /// <param name="success_str"></param>
        public static void _WriteSuccess(string success_str)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(success_str);
            Console.ResetColor();
        }

        /// <summary>
        /// Print warning string to console with yellow foreground
        /// </summary>
        /// <param name="warn_str"></param>
        public static void _WriteWarning(string warn_str)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(warn_str);
            Console.ResetColor();
        }

        /// <summary>
        /// Print error string to console with red foreground
        /// </summary>
        /// <param name="err_str"></param>
        public static void _WriteError(string err_str)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(err_str);
            Console.ResetColor();
        }
        #endregion ConsoleFunctions
    }
}
