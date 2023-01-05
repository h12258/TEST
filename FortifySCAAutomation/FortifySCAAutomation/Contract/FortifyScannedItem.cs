using Microsoft.Build.BuildEngine;
using Microsoft.Build.Execution;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FortifySCAAutomation
{
    /// <summary>
    /// Fortify待掃描項目
    /// </summary>
    public class FortifyScannedItem
    {
        /// <summary>
        /// Solution路徑
        /// </summary>
        public string SolutionPath { get; set; }

        /// <summary>
        /// Solution名稱
        /// </summary>
        public string SolutionName { get; set; }

        /// <summary>
        /// 掃描狀態
        /// </summary>
        public Status Status { get; set; }

        /// <summary>
        /// 信箱
        /// </summary>
        public List<string> NotifyMailAddress { get; set; }

        /// <summary>
        /// Build ID，for掃描使用，掃描時再產生
        /// </summary>
        public string BuildID { get; set; }

        /// <summary>
        /// 分析log的路徑，掃描時新增
        /// </summary>
        public string AnalyzeLogPath { get; set; }

        /// <summary>
        /// FPR檔的路徑，產生時更新
        /// </summary>
        public string FPRPath { get; set; }

        /// <summary>
        /// PDF檔的路徑，產生時更新
        /// </summary>
        public string ReportPath { get; set; }

        /// <summary>
        /// 是否建置成功，預設false
        /// </summary>
        public bool IsBuildSuccess { get; set; }

        /// <summary>
        /// 是否分析成功，預設false
        /// </summary>
        public bool IsAnalyzeSuccess { get; set; }

        /// <summary>
        /// 是否產報表成功，預設false
        /// </summary>
        public bool IsReportGenerateSuccess { get; set; }

        #region 建置
        /// <summary>
        /// 建置
        /// </summary>
        /// <param name="solutionFullPath">若有輸入，則projectFilePath直接使用傳入值</param>
        public void Build(string solutionFullPath = "")
        {
            
            Configuration thisConfiguration = Program.thisConfiguration;
            if (!string.IsNullOrWhiteSpace(solutionFullPath))
            {
                SolutionPath = Utility.FilePathNormalize(Path.GetDirectoryName(solutionFullPath));
                SolutionName = Path.GetFileName(solutionFullPath);
            }
            Console.WriteLine("Start Building " + SolutionPath + SolutionName);
            string logFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\buildLog\\" +
                                    thisConfiguration.ExcutionDate.ToString("yyyyMMddHHmmss") + "\\" +
                                    SolutionName + ".log";
            string projectFilePath = SolutionPath + SolutionName;
            FileLogger logger = new FileLogger();
            logger.Parameters = @"logfile=" + logFilePath;
            Microsoft.Build.Evaluation.ProjectCollection pc =
                    new Microsoft.Build.Evaluation.ProjectCollection();
            BuildRequestData buildRequest = new BuildRequestData(projectFilePath,
                            thisConfiguration.GlobalProperty, null, new string[] { "Rebuild" }, null,
                            BuildRequestDataFlags.IgnoreExistingProjectState);
            //register file logger using BuildParameters
            BuildParameters bp = new BuildParameters(pc);
            bp.Loggers = new List<Microsoft.Build.Framework.ILogger> { logger }.AsEnumerable();
            bp.Culture = new CultureInfo("en-US");
            bp.UICulture = new CultureInfo("en-US");

            //build solution
            BuildManager.DefaultBuildManager.Build(bp, buildRequest);
            //Unregister all loggers to close the log file               
            pc.UnregisterAllLoggers();
            IsBuildSuccess = BuildResultCheck(logFilePath).RETURN_FLAG;
        }

        /// <summary>
        /// 透過LOG確認建置是否成功
        /// </summary>
        /// <param name="logFilePath">Log路徑</param>
        /// <returns>建置結果</returns>
        private MSGReturnModel BuildResultCheck(string logFilePath)
        {
            //檢查Log是否有0 Error(s)字樣，有的話代表成功，沒有代表失敗
            MSGReturnModel result = new MSGReturnModel();

            //先確認是否有產出LOG檔 => 沒有=> 失敗
            if (File.Exists(logFilePath) == false)
            {
                result.RETURN_FLAG = false;
                result.DESCRIPTION = "Build Log Cannot be Found! (" + logFilePath + ")";
                return result;
            }

            //若LOG檔中有找到Analysis completed，就回傳成功
            foreach (string line in File.ReadAllLines(logFilePath))
            {
                if (line.Contains(" 0 Error(s)"))
                {
                    IsAnalyzeSuccess = true;
                    result.RETURN_FLAG = true;
                    return result;
                }
            }
            //都沒有回傳失敗
            result.RETURN_FLAG = false;
            this.Status = Status.F;
            result.DESCRIPTION = "Build Failed!";
            return result;
        }
        #endregion 建置

        #region SCA 分析 =>產FPR
        /// <summary>
        /// 進行Fortify Build & Scan –產生FPR檔
        /// </summary>
        /// <param name="p">正在執行的Process</param>
        /// <returns>執行結果</returns>
        public void Analyze(Process p)
        {
            Console.WriteLine("Start Analyzing " + SolutionPath + SolutionName);
            Configuration thisConfiguration = Program.thisConfiguration;
            #region Build ID & Build String
            //1. 先產生Build ID
            BuildID = SolutionName + DateTime.Now.ToString("yyyyMMddHHmm");
            //2. 串build string
            string buildString = "sourceanalyzer -b " + BuildID + " devenv " +
                SolutionPath + SolutionName + " /Rebuild Release";
            #endregion  Build ID & Build String
            #region Analyze Log & FPR Path & Analyze String
            //(Log存到 /AnalyzeLog/日期/資料夾內)
            AnalyzeLogPath = AppDomain.CurrentDomain.BaseDirectory +
                "\\AnalyzeLog\\" + thisConfiguration.ExcutionDate.ToString("yyyyMMdd") +
                "\\" + SolutionName + "_" + DateTime.Now.ToString("yyyyMMddHHmm") + ".log";

            FPRPath = thisConfiguration.FortifyFPRPath +
                 Regex.Replace(SolutionName, "sln", "fpr", RegexOptions.IgnoreCase);

            string analyzeString = "sourceanalyzer -b " + BuildID + " -scan -f " +
                FPRPath.AddQuotes() + " -logfile " + AnalyzeLogPath.AddQuotes();
            #endregion Analyze Log & FPR Path & Analyze String
            #region 執行build和analyze 把相關指令送到cmd中
            using (StreamWriter sw = p.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    Console.WriteLine(buildString);
                    sw.WriteLine(buildString);
                }
                if (sw.BaseStream.CanWrite)
                {
                    Console.WriteLine(analyzeString);
                    sw.WriteLine(analyzeString);
                }
            }
            #endregion 執行build和analyze 把相關指令送到cmd中
            p.WaitForExit();
        }

        /// <summary>
        /// 確認分析是否完成
        /// </summary>
        /// <returns></returns>
        public MSGReturnModel CheckCompleted()
        {
            //檢查Log是否有Analysis completed字樣，有的話代表成功，沒有代表失敗
            MSGReturnModel result = new MSGReturnModel();

            //先確認是否有產出LOG檔 => 沒有=> 失敗
            if (File.Exists(AnalyzeLogPath) == false)
            {
                result.RETURN_FLAG = false;
                this.Status = Status.F;
                result.DESCRIPTION = "Analyze Log Cannot be Found! (" + AnalyzeLogPath + ")";
                return result;
            }

            //若LOG檔中有找到Analysis completed，就回傳成功
            foreach (string line in File.ReadAllLines(AnalyzeLogPath))
            {
                if (line.Contains("Analysis completed"))
                {
                    IsAnalyzeSuccess = true;
                    result.RETURN_FLAG = true;
                    return result;
                }
            }
            //都沒有回傳失敗
            result.RETURN_FLAG = false;
            this.Status = Status.F;
            result.DESCRIPTION = "Analyze Failed!";
            return result;
        }
        #endregion SCA 分析 =>產FPR

        #region 產生報表
        /// <summary>
        /// 產生報表
        /// </summary>
        /// <param name="p">正在執行的Process</param>
        /// <returns>執行結果</returns>
        public void ReportGenerate()
        {
            Console.WriteLine("Start Generating Report : " + SolutionPath + SolutionName);
            Configuration thisConfiguration = Program.thisConfiguration;
            #region 初始化Process
            Process p = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "cmd.exe";
            info.RedirectStandardInput = true;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            p.StartInfo = info;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            #endregion 初始化Process
            #region Report Path & Report Generate String
            //(Log存到 /AnalyzeLog/日期/資料夾內)

            ReportPath = thisConfiguration.FortifyReportPath + thisConfiguration.SystemName +
                thisConfiguration.Version + "/" + thisConfiguration.ExcutionDate.ToString("yyyyMMddHHmm") + "/" +
                thisConfiguration.Version + "_" + SolutionName + ".pdf";

            string reportGeneratorString = "ReportGenerator -format pdf -f " +
                ReportPath.Replace("\\", "\\\\").AddQuotes() + " -source " + FPRPath.AddQuotes() +
                "  -template " + thisConfiguration.FortifyTemplatePath.Replace("\\", "\\\\").AddQuotes() +
                " -filterSet \"Security View\"";

            #endregion Report Path & Report Generate String
            #region 執行build和analyze 把相關指令送到cmd中
            using (StreamWriter sw = p.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine(thisConfiguration.VSConsolePath.AddQuotes());
                }
                if (sw.BaseStream.CanWrite)
                {
                    Console.WriteLine(reportGeneratorString);
                    sw.WriteLine(reportGeneratorString);
                }
            }
            p.WaitForExit();
            #endregion 執行build和analyze 把相關指令送到cmd中
        }

        /// <summary>
        /// 確認pdf是否有產生出來
        /// </summary>
        /// <returns></returns>
        public MSGReturnModel CheckReportGenerated()
        {
            //確認是否有產出PDF檔 => 沒有=> 失敗
            MSGReturnModel result = new MSGReturnModel();

            if (File.Exists(ReportPath) == false)
            {
                result.RETURN_FLAG = false;
                this.Status = Status.F;
                result.DESCRIPTION = "Generate Report Failed! (" + ReportPath + ")";
                return result;
            }

            result.RETURN_FLAG = true;
            IsReportGenerateSuccess = true;
            this.Status = Status.C;
            return result;
        }
        #endregion 產生報表

        #region Notify
        /// <summary>
        /// 傳送通知給負責人
        /// </summary>
        public void Notify()
        {
            Configuration thisConfiguration = Program.thisConfiguration;
            if (!NotifyMailAddress.IsAny())
            {
                return;
            }
            #region 宣告與初始化
            string mailAddress = string.Join(",", NotifyMailAddress);
            string subject = string.Empty;
            string content = string.Empty;
            #endregion 宣告與初始化

            #region 串內容
            //成功
            if (IsBuildSuccess && IsAnalyzeSuccess && IsReportGenerateSuccess)
            {
                subject = SolutionName + " Fortify Scan Successfully (" + 
                          thisConfiguration.ExcutionDate.ToString("yyyy/MM/dd") + ")";
                
            }
            else
            {
                //失敗
                if (IsBuildSuccess == false)
                {
                    subject = SolutionName + " Build Failed (" +
                              thisConfiguration.ExcutionDate.ToString("yyyy/MM/dd") + ")";
                }
                else if (IsAnalyzeSuccess == false)
                {
                    subject = SolutionName + " Analyze Failed (" +
                              thisConfiguration.ExcutionDate.ToString("yyyy/MM/dd") + ")";
                }
                else if (IsReportGenerateSuccess == false)
                {
                    subject = SolutionName + "  Report Generate Failed (" +
                              thisConfiguration.ExcutionDate.ToString("yyyy/MM/dd") + ")";
                }
            }
            content = subject;
            #endregion 串內容
            try
            {
                if (thisConfiguration.IsPDFAttachMail)
                {
                    Utility.MailSend(mailAddress, subject, content, new List<string> { ReportPath });
                }
                else
                {
                    Utility.MailSend(mailAddress, subject, content);
                }
            }
            catch 
            {
                //do nothing
            }
        }
        #endregion Notify

        #region 更新excel的Status欄位
        /// <summary>
        /// 更新excel的Status欄位
        /// </summary>
        /// <returns></returns>
        public bool StatusUpdate()
        {
            Configuration thisConfiguration = Program.thisConfiguration;
            string connectionString = "Provider = Microsoft.ACE.OLEDB.12.0; Data Source =" +
                                      thisConfiguration.FilePath + ";Extended Properties=Excel 12.0 XML;";
            bool result = false;
            if (File.Exists(Program.thisConfiguration.FilePath) == false)
            {
                Program.LogError("Can't find Excel file:" + thisConfiguration.FilePath);
                return result;
            }

            OleDbConnection connection = new OleDbConnection(connectionString);
            using (connection)
            {
                connection.Open();

                DataTable xslx = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                if (xslx.Rows == null)
                {
                    return result;
                }
                string sheetName = xslx.Rows[0]["TABLE_NAME"].ToString();

                string commandString = String.Format(
                        "UPDATE [{0}] set [Status] = '{1}' where [Solution Name]='{2}'",
                        sheetName,
                        Status.ToString(),
                        SolutionName);
                OleDbCommand commande = new OleDbCommand(commandString, connection);
                commande.ExecuteNonQuery();
                commande.ToString();
            }

            connection.Close();
            result = true;
            return result;
        }
        #endregion 更新excel的Status欄位
    }
}
