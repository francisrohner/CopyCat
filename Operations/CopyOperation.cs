using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using CopyCat.Parser;
using CopyCat.Utils;

namespace CopyCat.Operations
{

    public class CopyOperation : BaseOperation
    {
        //Copy C:\Some\Dir <= C:\Other\Dir
        //Copy C:\Some\Dir C:\Other\Dir [Assume left to right]
        //Copy "C:\Some\Dir With Space" => C:\Other\Dir
        public new static bool CommandMatch(string line) => line.TrimStart().StartsWith("copy", StringComparison.InvariantCultureIgnoreCase);

        private string Source { get; set; }
        private string Destination { get; set; }

        public CopyOperation(CopyCatScript script, string line) : base(script, line)
        {
            string[] segments = Utility.SplitWithQuotations(line); //line.Trim().Split();
            //bool flipDirection = segments.Any(segment => segment.Equals("<="));
            bool flipDirection = false;
            if (segments.Length == 4)
            {
                Source = segments[1];
                flipDirection = segments[2] == "<=";
                Destination = segments[3];
            }
            else
            {
                Source = segments[1];
                Destination = segments[2];
            }

            if (flipDirection)
            {
                string tmp = (string)Source.Clone();
                Source = (string)Destination.Clone();
                Destination = tmp;
            }

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
        public override ResultCode Execute()
        {
            if (File.Exists(Source))
            {
                //File operation
                if (Directory.Exists(Destination)) //IOperation entails copying file to a directory
                {
                    File.Copy(Source, Destination + Path.DirectorySeparatorChar + Path.GetFileName(Source), Program.OverwriteMode);
                }
                else //No directory found, destination is file name
                {
                    try
                    {
                        if (File.Exists(Source))
                        {
                            File.Copy(Source, Destination, Program.OverwriteMode);
                        }
                        else
                        {
                            return ResultCode.FILE_NOT_FOUND;
                        }
                    }
                    catch (Exception ex)
                    {
                        return Utility.ExceptionToErrCode(ex);
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
                    return Utility.ExceptionToErrCode(ex);
                }
            }
            else
            {
                return ResultCode.SOURCE_NOT_FOUND;
            }
            return ResultCode.SUCCESS;
        }


    }
}
