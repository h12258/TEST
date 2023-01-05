using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FortifySCAAutomation;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace UnitTest
{
    [TestClass]
    public class ReportTest
    {
        /// <summary>
        /// 印出執行指令
        /// </summary>
        /// <param name="sendingProcess"></param>
        /// <param name="outLine"></param>
        private static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            Console.WriteLine(outLine.Data);
        }

        [TestMethod]
        public void TestCheckCompleted()
        {
            #region 初始化Process
            Process p = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "cmd.exe";
            info.RedirectStandardInput = true;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;

            p.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            p.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
            p.StartInfo = info;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            #endregion 初始化Process
            #region 讀取app.config
            Configuration thisConfiguration = new Configuration();
            #endregion
            #region 設定Version & System Name
            thisConfiguration.VersionSet("V210531");
            thisConfiguration.SystemNameSet("NCW");
            #endregion
            string pdf = @"\\C:\\Fortify Reports\\NCWV210531/202106111124/V210531_EVA.NCW.ACR.Service.sln.pdf";
            string fpr = @"\C:\EVA\NCW\QA6\EVA.NCW.ACR.Service.fpr";
            string myString = "ReportGenerator -format pdf –f " + pdf.AddQuotes() + " -source " +
                fpr.AddQuotes() + " -template " + thisConfiguration.VSConsolePath.AddQuotes() + 
                   " -filterSet \"Security View\"";

            using (StreamWriter sw = p.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine(thisConfiguration.VSConsolePath.AddQuotes());
                }
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine(myString);
                }
                p.WaitForExit();
            }
        }

    }

    public static class Extension
    { 
        /// <summary>
        /// 傳入字串加上前後加上雙引號後回傳
        /// </summary>
        /// <param name="sourceString"></param>
        /// <returns></returns>
        public static string AddQuotes(this string sourceString)
        {
            string retVal = sourceString == null ? "\"\"" : 
                                                   "\"" + sourceString + "\"";
            return retVal;
        }
    }
}
