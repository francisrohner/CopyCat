using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace CopyCat
{
    public class Program
    {

        public enum ERROR_CODES
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

        public static char FileSeparator;
        public static bool OverwriteMode = true;

        public interface IOperation
        {
            ERROR_CODES Execute();
            string GetDesc();
        }

        public class CopyOperation : IOperation
        {
            public string Source { get; set; }
            public string Destination { get; private set; }
            public CopyOperation(string source, string destination)
            {
                Source = source;
                Destination = destination;
            }

            public string GetDesc()
            {
                if (Directory.Exists(Source))
                {
                    return "CopyDirectory, " + Source + " => " + Destination;
                }
                else
                {
                    return "CopyFile, " + Source + " => " + Destination;
                }
            }
            /// <summary>
            /// Return just the file name form a file path. Ex: C:\Users\Frank\someFile.txt => someFile.txt
            /// </summary>
            /// <param name="filePath"></param>
            /// <returns></returns>
            private string GetFileName(string filePath)
            {
                if (filePath.Contains(FileSeparator))
                    return filePath.Substring(filePath.LastIndexOf(FileSeparator) + 1);
                return filePath;
            }

            //Ref: https://docs.microsoft.com/en-us/dotnet/api/system.io.directoryinfo?redirectedfrom=MSDN&view=netframework-4.7.2
            /// <summary>
            /// Copy a directory and all it's subdirectories to specified destionation
            /// </summary>
            /// <param name="source"></param>
            /// <param name="destination"></param>
            private void CopyDirectory(string source, string destination)
            {
                DirectoryInfo sourceDirInfo = new DirectoryInfo(source);
                DirectoryInfo destDirInfo = new DirectoryInfo(destination);
                CopyAll(sourceDirInfo, destDirInfo);
            }

            //Ref: https://docs.microsoft.com/en-us/dotnet/api/system.io.directoryinfo?redirectedfrom=MSDN&view=netframework-4.7.2
            /// <summary>
            /// Copy directory and all its subdirectories recursively
            /// </summary>
            /// <param name="source"></param>
            /// <param name="destination"></param>
            private void CopyAll(DirectoryInfo source, DirectoryInfo destination)
            {
                Directory.CreateDirectory(destination.FullName);

                // Copy each file into the new directory.
                foreach (FileInfo fileInfo in source.GetFiles())
                {
                    fileInfo.CopyTo(Path.Combine(destination.FullName, fileInfo.Name), true);
                }

                // Copy every sub directory recursively.
                foreach (DirectoryInfo di in source.GetDirectories())
                {
                    DirectoryInfo nextSubDirInfo = destination.CreateSubdirectory(di.Name);
                    CopyAll(di, nextSubDirInfo);
                }
            }
            /// <summary>
            /// Copy source file or folder to destination
            /// </summary>
            /// <returns></returns>
            public ERROR_CODES Execute()
            {
                if (File.Exists(Source))
                {
                    //File operation
                    if (Directory.Exists(Destination)) //IOperation entails copying file to a directory
                    {
                        File.Copy(Source, Destination + FileSeparator + GetFileName(Source), OverwriteMode);
                    }
                    else //No directory found, destination is file name
                    {
                        try
                        {
                            if (File.Exists(Source))
                            {
                                File.Copy(Source, Destination, OverwriteMode);
                            }
                            else
                            {
                                return ERROR_CODES.FILE_NOT_FOUND;
                            }
                        }
                        catch (Exception ex)
                        {
                            return Program.ExceptionToErrCode(ex);
                        }
                    }
                }
                else if (Directory.Exists(Source))
                {
                    try
                    {
                        CopyDirectory(Source, Destination);
                    }
                    catch (Exception ex)
                    {
                        return Program.ExceptionToErrCode(ex);
                    }
                }
                else
                {
                    return ERROR_CODES.SOURCE_NOT_FOUND;
                }
                return ERROR_CODES.SUCCESS;
            }
        }
        public class DownloadOperation : IOperation
        {

            private string Url { get; }
            private string LocalFile { get; }

            public DownloadOperation(string url, string localFile)
            {
                Url = url;
                LocalFile = localFile;
            }

            /// <summary>
            /// Send HTTP Web request to download requested resource
            /// Return an error code which will indicate whether the operation
            /// succeeded and if not, why it has failed.
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            private ERROR_CODES PerformDownload(out byte[] data)
            {
                data = null;
                try
                {
                    HttpWebRequest htreq = (HttpWebRequest)WebRequest.Create(Url);
                    HttpWebResponse htresp = (HttpWebResponse)htreq.GetResponse();
                    MemoryStream ms = new MemoryStream();
                    Stream stream = htresp.GetResponseStream();
                    stream.CopyTo(ms);
                    stream.Close();
                    ms.Close();
                    data = ms.ToArray();
                    return ERROR_CODES.SUCCESS;
                }
                //Todo work into ExceptionToErrCode
                //catch (WebException wex)
                //{
                //    HttpWebResponse err_resp = wex.Response as HttpWebResponse;
                //    ret = ((int)err_resp.StatusCode) * -1; //Ex: 404 => -404
                //}
                catch(Exception ex)
                {
                    return ExceptionToErrCode(ex);
                }
            }
            public ERROR_CODES Execute()
            {
                //Download("http://xyz.com/resouce.txt", "C:\Users\XYZ\resource.txt")
                byte[] rx;
                ERROR_CODES ret = PerformDownload(out rx);
                if (ret > 0)
                {
                    //save file
                    File.WriteAllBytes(LocalFile, rx);
                    return ret;
                }
                return ret;
            }

            public string GetDesc()
            {
                return "Download," + Url + " => " + LocalFile;
            }

        }
        public class ExecOperation : IOperation
        {
            private bool Wait { get; }
            private string ExecStr { get; }

            public string GetDesc()
            {
                if (Wait)
                {
                    return "ExecWait, " + ExecStr;
                }
                else
                {
                    return "Exec, " + ExecStr;
                }
            }

            /// <summary>
            /// Break apart string containing path to executable and various arguments
            /// Ex: "C:\Program Files (x86)\SomePath\SomeProgram.exe" --someTag someValue
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            private string[] SplitWithQuotations(string value)
            {
                bool enclosed = false;
                List<string> args = new List<string>();
                StringBuilder buffer = new StringBuilder();
                foreach (char current in value)
                {
                    if (current == 34) //34 - "
                    {
                        enclosed = !enclosed;
                    }
                    else if(current == ' ')
                    {
                        if(!enclosed)
                        {
                            args.Add(buffer.ToString());
                            buffer.Length = 0;
                        }
                        else
                        {
                            buffer.Append(current);
                        }
                    }
                    else
                    {
                        buffer.Append(current);
                    }
                }
                if(buffer.Length > 0)
                {
                    args.Add(buffer.ToString());
                }
                return args.ToArray();
            }

            /// <summary>
            /// Concatenate string array with spaces starting from specified start index
            /// </summary>
            /// <param name="args"></param>
            /// <param name="startIndex"></param>
            /// <returns></returns>
            private string ConcatenateFrom(string[] args, int startIndex)
            {
                string ret = string.Empty;
                for(int i = startIndex; i < args.Length; i++)
                {
                    ret += args[i] + " ";
                }
                ret = ret.Substring(0, ret.Length - 1); //trim last space
                return ret;
            }
        
            public ExecOperation(string execStr)
            {
                ExecStr = execStr;
                Wait = false;
            }
            public ExecOperation(string execStr, bool wait)
            {
                ExecStr = execStr;
                Wait = wait;
            }

            /// <summary>
            /// Execute process without waiting for its completion
            /// </summary>
            /// <param name="value"></param>
            private Process Exec(string value)
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                string[] arr = SplitWithQuotations(value);
                psi.FileName = arr[0];
                if (arr.Length > 1)
                {
                    psi.Arguments = ConcatenateFrom(arr, 1);
                }
                return Process.Start(psi);
            }

            /// <summary>
            /// Execute process and wait for its completion
            /// </summary>
            /// <param name="value"></param>
            private void ExecWait(string value)
            {
                Exec(value).WaitForExit();
            }

            public ERROR_CODES Execute()
            {
                try
                {
                    if (Wait)
                    {
                        ExecWait(ExecStr);
                    }
                    else
                    {
                        Exec(ExecStr);
                    }
                    return ERROR_CODES.SUCCESS;
                }
                catch (Exception ex)
                {
                    return ExceptionToErrCode(ex);
                }
            }
        }


        public class CopyCatScript
        {
            private Dictionary<string, string> Variables { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            List<IOperation> Operations { get; } = new List<IOperation>();
            private string OperatingSystem { get; }

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
                                string dateTimeFormat = value.Substring(start + 1, (i - 1) - start);
                                ret += DateTime.Now.ToString(dateTimeFormat);
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
                List<string> intermediate = new List<string>();
                while (!scriptReader.EndOfStream)
                {
                    string line = scriptReader.ReadLine();
                    if (line.StartsWith("//")) continue; //skip comment line
                    if (string.IsNullOrEmpty(line.Trim())) continue; //skip blank line
                    line = ReplaceDateTimeStr(line);
                    if (line.Contains("=") && !(line.Contains("=>") || line.Contains("<="))) //variable
                    {
                        string tag, val;
                        tag = line.Split('=')[0].Trim();
                        val = line.Split('=')[1].Trim();
                        Variables.Add(tag, val);
                    }
                    else
                    {
                        intermediate.Add(line);
                    }
                }
                scriptReader.Close();

                //Second Pass
                //Process Variables
                OperatingSystem = Variables["os"];
                foreach (string line in intermediate)
                {
                    string cline = (string)line.Clone();
                    //First replace vars
                    foreach (string tag in Variables.Keys)
                    {
                        string var = "%" + tag + "%";
                        if (cline.Contains(var))
                        {
                            cline = line.Replace(var, Variables[tag]);
                        }
                    }
                    Operations.Add(OperationFromLine(cline));
                }


            }

            /// <summary>
            /// Parse a line of the script and return the corresponding IOperation
            /// </summary>
            /// <param name="line"></param>
            /// <returns></returns>
            private IOperation OperationFromLine(string line)
            {
                IOperation ret = null;
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

            public ERROR_CODES Execute()
            {
                //If script is not intended for current operating system, return OS mismatch
                if (!CheckOSMatch(Program.GetOS_Str()))
                    return ERROR_CODES.OS_MISMATCH;

                int successCount = 0;
                foreach (var operation in Operations)
                {
                    ERROR_CODES ec = operation.Execute();
                    if (ec > 0)
                    {
                        ++successCount;
                        WriteSuccess(operation.GetDesc() + " Succeeded");
                    }
                    else
                    {
                        WriteError(operation.GetDesc() + " Failed: [" + ec + "]");
                    }
                }
                return (successCount == Operations.Count) ? ERROR_CODES.SUCCESS : ERROR_CODES.FAILED;
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
                FileSeparator = '/';
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                FileSeparator = '\\';
            }
            else
            {
                WriteError("CopyCat is unsupported on your operating system :(");
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
                if (short_file.Contains(FileSeparator))
                {
                    short_file = short_file.Substring(short_file.LastIndexOf(FileSeparator) + 1);
                }
                Console.WriteLine("{0} - {1}", key, short_file);
            }
            Console.Write("Enter the name or number of a script: ");
            string user_input = Console.ReadLine();
            string script = _GetSelectedScript(scripts, user_input);
            if (script == null)
            {
                WriteError("Please enter a valid number or script name...");
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
            copyCatPath = copyCatPath.Substring(0, copyCatPath.LastIndexOf(FileSeparator));
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
        public static ERROR_CODES ExceptionToErrCode(Exception ex)
        {
            ERROR_CODES ret = ERROR_CODES.FAILED;
            if (ex is UnauthorizedAccessException)
            {
                ret = ERROR_CODES.UNAUTHORIZED_EXCEPTION;
            }
            else if (ex is DirectoryNotFoundException)
            {
                ret = ERROR_CODES.DIRECTORY_NOT_FOUND;
            }
            else if (ex is FileNotFoundException)
            {
                ret = ERROR_CODES.FILE_NOT_FOUND;
            }
            else //General
            {
                ret = ERROR_CODES.FAILED;
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
                WriteSuccess(result);
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
        public static void WriteSuccess(string success_str)
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
        public static void WriteError(string err_str)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(err_str);
            Console.ResetColor();
        }
        #endregion ConsoleFunctions
    }
}
