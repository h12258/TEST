using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace FortifySCAAutomation
{
    public partial class Program
    {
        /// <summary>
        /// 將傳入的方案檔進行Fortify掃描。
        /// </summary>
        /// <param name="inputSLNs">待掃描方案</param>
        public static void FortifyScan(List<FortifyScannedItem> inputSLNs)
        {
            /*
             * 0.	初始化Process
             * 1.	進行單一SLN掃描
             * 2.	分析Log存到/AnalyzeLog/日期/資料夾內
             * 3.	分析完畢後檢查Log是否有Analysis completed字樣，有的話代表成功，沒有代表失敗
             * 4.	產生fpr檔及pdf檔
             * 5.	通知負責人
             */
            //建置 & 掃描 & 產檔

            foreach (FortifyScannedItem item in inputSLNs)
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
                //開啟VS CMD
                using (StreamWriter sw = p.StandardInput)
                {
                    if (sw.BaseStream.CanWrite)
                    {
                        sw.WriteLine(thisConfiguration.VSConsolePath.AddQuotes());
                    }
                   item.Analyze(p);
                   item.ReportGenerate();
                }                
                MSGReturnModel execResult = new MSGReturnModel();
                execResult = item.CheckCompleted();
                if (execResult.RETURN_FLAG)
                {
                    item.CheckReportGenerated();
                }
                item.StatusUpdate();
                item.Notify();
            }
        }

        /// <summary>
        /// 印出執行指令
        /// </summary>
        /// <param name="sendingProcess"></param>
        /// <param name="outLine"></param>
        private static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            Console.WriteLine(outLine.Data);
        }
    }
}
