using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CopyCat
{
    public class Program
    {

        enum ERROR_CODES
        {            
            OS_MISMATCH = -606,
            DIRECTORY_NOT_FOUND = -505,
            FILE_NOT_FOUND = -404,
            ARGUMENT_NULL_EXCEPTION = -300,
            ARGUMENT_EXCEPTION = -200,
            UNAUTHORIZED_EXCEPTION = -100,
            FAILED = -1,
            SUCCESS = 1,
        }

        public static char file_separator;
        public static bool overwrite_mode = true;


        public interface Operation
        {
            int Execute();
            void Resolve(Dictionary<string, string> variables);
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

            public void Resolve(Dictionary<string, string> variables)
            {
                foreach(string tag in variables.Keys)
                {
                    if(_source.Contains("%" + tag + "%"))
                    {
                        _source = _source.Replace("%" + tag + "%", variables[tag]);
                    }
                    else if(_destination.Contains("%" + tag + "%"))
                    {
                        _destination = _destination.Replace("%" + tag + "%", variables[tag]);
                    }
                }
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
                ret = (int)ERROR_CODES.SUCCESS;
                return ret;
            }
        }

        public class ExecOperation : Operation
        {
            private bool _wait;
            private string _exec_str;

            public string GetDesc()
            {
                if(_wait)
                {
                    return "ExecWait, " + _exec_str;
                }
                else
                {
                    return "Exec, " + _exec_str;
                }
            }
            private string[] _spl_exe_args(string exec_str)
            {
                string exe;
                string args;
                //TODO handle quotes encapsulation
                if (exec_str.Contains(' '))
                {
                    exe = exec_str.Substring(0, exec_str.IndexOf(' '));
                    args = exec_str.Substring(exec_str.IndexOf(' ') + 1);
                }
                else //just exe
                {
                    exe = exec_str;
                    args = "";
                }
                return new string[] { exe, args };
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
                string[] arr = _spl_exe_args(exec_str);
                psi.FileName = arr[0];
                psi.Arguments = arr[1];
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
                catch(Exception ex)
                {
                    return Program.ExceptionToErrCode(ex);
                }
            }

            public void Resolve(Dictionary<string, string> variables)
            {
                foreach(string tag in variables.Keys)
                {
                    if(_exec_str.Contains("%" + tag + "%"))
                    {
                        _exec_str = _exec_str.Replace("%" + tag + "%", variables[tag]);
                    }
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
                if(value.Contains("#"))
                {
                    int start = -1;
                    for(int i = 0; i < value.Length; i++)
                    {
                        if(value[i] == '#')
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
                        else if(start != -1)
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
                string source, destination;
                string tag, value;
                while(!scriptReader.EndOfStream)
                {
                    string line = scriptReader.ReadLine();
                    if (line.StartsWith("//")) continue;
                    line = _ReplaceDateTimeStr(line);
                    if(line.Contains("=>"))
                    {
                        source = line.Substring(0, line.IndexOf("=>")).Trim();
                        destination = line.Substring(line.IndexOf("=>") + 2).Trim();
                        operations.Add(new CopyOperation(source, destination));
                    }
                    else if(line.Contains("<="))
                    {
                        destination = line.Substring(0, line.IndexOf("<=")).Trim();
                        source = line.Substring(line.IndexOf("<=") + 1).Trim();
                        operations.Add(new CopyOperation(source, destination));
                    }                    
                    else //Tag/Value pair
                    {
                        tag = line.Split('=')[0];
                        tag = tag.ToLower();

                        value = line.Split('=')[1];
                        if (tag.StartsWith("exec_wait"))
                        {
                            operations.Add(new ExecOperation(value, true));
                        }
                        else if (tag.StartsWith("exec"))
                        {
                            operations.Add(new ExecOperation(value));
                        }
                        else
                        {
                            variables.Add(tag, value);
                        }
                    }
                }
                scriptReader.Close();

                //Process variables
                if(variables.ContainsKey("os"))
                {
                    _os = variables["os"];
                }
            }
            public int Execute()
            {
                if (!IsForOS(Program.GetOS_Str()))
                    return (int)ERROR_CODES.OS_MISMATCH;

                int success_count = 0;
                for(int i = 0; i < operations.Count; i++)
                {
                    if(operations[i].Execute() > 0)
                    {
                        ++success_count;
                    }
                }
                return (success_count == operations.Count) ? (int)ERROR_CODES.SUCCESS : (int)ERROR_CODES.FAILED;
            }
        }

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

            //Console.WriteLine(CopyCatScript.ReplaceDateTimeStr("FUCK_YEAH#yyyy-MM-dd#"));
            //_WaitKey("test", ConsoleColor.White);
            //return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                file_separator = '/';
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
            if(scripts.Count == 0)
            {
                _WriteWarning("No scripts were available, exiting program...");
                if (Debugger.IsAttached)
                {
                    _WaitKey("Exit", ConsoleColor.White);
                }
                return;
            }
            foreach(int key in scripts.Keys)
            {
                string short_file = scripts[key];
                if(short_file.Contains(file_separator))
                {
                    short_file = short_file.Substring(short_file.LastIndexOf(file_separator) + 1);
                }
                Console.WriteLine("{0} - {1}", key, short_file);
            }
            Console.Write("Enter the name or number of a script: ");
            string user_input = Console.ReadLine();
            string script = _GetSelectedScript(scripts, user_input);
            if(script == null)
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
            foreach(string file in Directory.GetFiles(copyCatPath, "*.ccs"))
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
            if(number >= 0 && number < scripts.Count)
            {
                return scripts[number];
            }
            else //do loose search on values
            {
                foreach(string script in scripts.Values)
                {
                    if(script.ToLower().Contains(user_input.ToLower()))
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
            if(ex is UnauthorizedAccessException)
            {
                ret = (int)ERROR_CODES.UNAUTHORIZED_EXCEPTION;
            }
            else if(ex is DirectoryNotFoundException)
            {
                ret = (int)ERROR_CODES.DIRECTORY_NOT_FOUND;
            }
            else if(ex is FileNotFoundException)
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
