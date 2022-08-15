using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace CopyCat.Utils
{
    public static class Utility
    {
        /// <summary>
        /// Return the error code corresponding to the exception given
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static ResultCode ExceptionToErrCode(Exception ex)
        {
            ResultCode ret = ResultCode.FAILED;
            if (ex is UnauthorizedAccessException)
            {
                ret = ResultCode.UNAUTHORIZED_EXCEPTION;
            }
            else if (ex is DirectoryNotFoundException)
            {
                ret = ResultCode.DIRECTORY_NOT_FOUND;
            }
            else if (ex is FileNotFoundException)
            {
                ret = ResultCode.FILE_NOT_FOUND;
            }
            else //General
            {
                ret = ResultCode.FAILED;
            }
            return ret;
        }

        /// <summary>
        /// Concatenate string array with spaces starting from specified start index
        /// </summary>
        /// <param name="args"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static string ConcatenateFrom(string[] args, int startIndex)
        {
            StringBuilder ret = new StringBuilder();
            for (int i = startIndex; i < args.Length; i++)
            {
                ret.Append(args[i]);
                if (startIndex < args.Length - 1)
                {
                    ret.Append(StringConstants.Space);
                }
            }
            return ret.ToString();
        }

        /// <summary>
        /// Break apart string containing path to executable and various arguments
        /// Ex: "C:\Program Files (x86)\SomePath\SomeProgram.exe" --someTag someValue
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string[] SplitWithQuotations(string value, bool noTrim = false)
        {
            if (!noTrim)
            {
                value = value.Trim();
            }
            bool enclosed = false;
            List<string> args = new List<string>();
            StringBuilder buffer = new StringBuilder();

            foreach (char current in value)
            {
                if (current == StringConstants.Quote)
                {
                    enclosed = !enclosed;
                }
                else if (current == StringConstants.Space)
                {
                    if (!enclosed)
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

            if (buffer.Length > 0)
            {
                args.Add(buffer.ToString());
            }
            return args.ToArray();
        }



        #region ConsoleFunctions

        /// <summary>
        /// Print "Press any key to 'action" in console, then wait for key press
        /// </summary>
        /// <param name="action"></param>
        /// <param name="foreColor"></param>
        public static void WaitKey(string action, ConsoleColor foreColor = ConsoleColor.White)
        {
            Console.ForegroundColor = foreColor;
            Console.Write("Press any key to {0}...", action);
            Console.ReadKey();
            Console.ResetColor();
        }

        /// <summary>
        /// Print text enclosed by character specified
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encloseChar"></param>
        /// <param name="foreColor"></param>
        public static void PrintEnclosed(string text, char encloseChar, ConsoleColor foreColor)
        {
            Console.ForegroundColor = foreColor;
            int start_char = (Console.WindowWidth / 2) - (text.Length / 2);
            for (int i = 0; i < Console.WindowWidth; i++)
            {
                if (i >= start_char && i < (start_char + text.Length))
                {
                    Console.Write(text[i - start_char]);
                }
                else
                {
                    Console.Write(encloseChar);
                }
            }
            Console.ResetColor();
        }

        /// <summary>
        /// Print ascii cat art to console
        /// </summary>
        public static void PrintAsciiCat()
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
        /// <param name="output"></param>
        public static void WriteInfo(string output, bool excludeConsole = false)
        {
            if (!excludeConsole)
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(output);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Print success string to console with red foreground
        /// </summary>
        /// <param name="output"></param>
        public static void WriteSuccess(string output, bool excludeConsole = false)
        {
            if (!excludeConsole)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(output);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Print warning string to console with yellow foreground
        /// </summary>
        /// <param name="output"></param>
        public static void WriteWarning(string output, bool excludeConsole = false)
        {
            if(!excludeConsole)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(output);
                Console.ResetColor();
            }

        }

        /// <summary>
        /// Print error string to console with red foreground
        /// </summary>
        /// <param name="output"></param>
        public static void WriteError(string output, bool excludeConsole = false)
        { 
            if (!excludeConsole)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(output);
                Console.ResetColor();
            }
        }
        #endregion ConsoleFunctions
    }
}
