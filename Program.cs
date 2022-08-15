using CopyCat.Operations;
using CopyCat.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using CopyCat.Parser;

namespace CopyCat
{
    public class Program
    {
        //TODO Run mode - EXIT_ON_FAIL
        //TODO Run mode - CONTINUE_ON_FAIL
        //TODO Logic operation (if fail, ...)
        //TODO Goto operation
        
        
        public static bool OverwriteMode = true;


        public static void Main(string[] args)
        {
            ////Console.WriteLine(new ComputerInfo().OSVersion);
            //Utility.WaitKey("exit");
            //return;
            //Console.WriteLine(CopyCatScript.GetOS_Str());
            //Utility.WaitKey("exit");

            Utility.PrintEnclosed("CopyCat", '=', ConsoleColor.White);
            Utility.PrintAsciiCat();

            Dictionary<int, string> scripts = GetScripts();
            if (scripts.Count == 0)
            {
                Utility.WriteWarning("No scripts were available, exiting program...");
                if (Debugger.IsAttached)
                {
                    Utility.WaitKey("Exit");
                }
                return;
            }
            foreach (int key in scripts.Keys)
            {
                Console.WriteLine($"{key} - {Path.GetFileName(scripts[key])}");
            }
            Console.Write("Enter the name or number of a script: ");
            string userInput = Console.ReadLine();
            string script = GetSelectedScript(scripts, userInput);
            if (script == null)
            {
                Utility.WriteError("Please enter a valid number or script name...");
            }
            else
            {
                var result = CopyCatScript.Execute(script);
                if(result > 0)
                {
                    Utility.WriteInfo($"Script finished successfully [{result}]");
                }
                else
                {
                    Utility.WriteError($"Script finished with one or more errors [{result}]");
                }

            }
            //if (Debugger.IsAttached)
            //{
            //    Utility.WaitKey("Exit", ConsoleColor.White);
            //}

        }


        /// <summary>
        /// Return a dictionary of all Copy Cat Scripts (.ccs) files
        /// </summary>
        /// <returns></returns>
        private static Dictionary<int, string> GetScripts()
        {
            Dictionary<int, string> ret = new Dictionary<int, string>();
            int index = 0;
            string copyCatPath = Assembly.GetExecutingAssembly().Location;
            copyCatPath = copyCatPath.Substring(0, copyCatPath.LastIndexOf(Path.DirectorySeparatorChar)); //TODO validate on linux
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
        /// <param name="userInput"></param>
        /// <returns></returns>
        public static string GetSelectedScript(Dictionary<int, string> scripts, string userInput)
        {

            if (int.TryParse(userInput, out int number) && number >= 0 && number < scripts.Count)
            {
                return scripts[number];
            }
            else //do loose search on values
            {
                foreach (string script in scripts.Values)
                {
                    if (script.ToLower().Contains(userInput.ToLower()))
                    {
                        return script;
                    }
                }
            }
            return null; //not found

        }



        
    }
}
