using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using CopyCat.Parser;
using CopyCat.Utils;

namespace CopyCat.Operations
{

    public class DownloadOperation : BaseOperation
    {

        public new static bool CommandMatch(string line) => line.Trim().StartsWith("download", StringComparison.InvariantCultureIgnoreCase);

        private string Url { get; }
        private string LocalFile { get; }

        public DownloadOperation(CopyCatScript script, string line) : base(script, line)
        {
            //download https://abc.com/some%20resource.txt "C:\Some\Path with space\resource.txt"
            string[] segments = Utility.SplitWithQuotations(line);
            Url = segments[1];
            LocalFile = segments[2];
        }

        /// <summary>
        /// Send HTTP Web request to download requested resource
        /// Return an error code which will indicate whether the operation
        /// succeeded and if not, why it has failed.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private ResultCode PerformDownload(out byte[] data)
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
                return ResultCode.SUCCESS;
            }
            catch (Exception ex)
            {
                return Utility.ExceptionToErrCode(ex);
            }
        }
        public override ResultCode Execute()
        {
            //Download("http://xyz.com/resouce.txt", "C:\Users\XYZ\resource.txt")
            ResultCode ret = PerformDownload(out byte[] rx);
            if (ret > 0)
            {
                //save file
                File.WriteAllBytes(LocalFile, rx);
                return ret;
            }
            return ret;
        }



    }
}
