using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;

namespace FortifySCAAutomation
{
    public partial class Program
    {
        public static Configuration thisConfiguration { get; set; }
        private static string logDetail = "\r\n";
        //private static bool ShowDetailLog;
        private static int errorCount = 0;

        #region 主流程
        static void Main(string[] args)
        {
            /*
             *  A. 組態設定初始化 –呼叫Initialize以初始化Configuration (定義為全域變數)
                B. 讀取Excel讀取需掃描的SLN檔 –呼叫ScannedItemRead
                C. 程式取新版–呼叫GetLatestVersion
                D. 建置–呼叫SolutionsBuild
                E. 掃描與產生報表及通知各sln負責人-呼叫FortifyScan
                F. 寄出彙整信件 – 呼叫ScanResultNotify
             */
            try
            {
                // A. 組態設定初始化 –呼叫Initialize以初始化Configuration
                thisConfiguration = new Configuration();
                // B. 讀取Excel讀取需掃描的SLN檔
                List<FortifyScannedItem> allSLNs = ScannedItemRead();
                if (!allSLNs.IsAny())
                {
                    Console.WriteLine("There is no SLN in Excel!");
                    Console.ReadLine();
                    return;
                }
                // C. 程式取新版–呼叫GetLatestVersion
                Console.WriteLine("Get Latest Version Start!");
                bool getResult = GetLatestVersion();
                if (!getResult)
                {
                    //bye
                    Console.WriteLine("Get Latest Version Failed!");
                    Console.ReadLine();
                    return;
                }
                // D. 建置–呼叫SolutionsBuild
                // 抓出待掃的檔案
                List<FortifyScannedItem> inputSLNs = allSLNs.DeepCopy()
                                                            .Where(x => x.Status == Status.S)
                                                            .ToList();
                Console.WriteLine("Build Start!");
                SolutionsBuild(inputSLNs);
                // E. 掃描與產生報表及通知各sln負責人-呼叫FortifyScan
                Console.WriteLine("Fortify Scan Start!");
                FortifyScan(inputSLNs.Where(x => x.IsBuildSuccess == true).ToList());
                // F. 寄出彙整信件 – 呼叫ScanResultNotify
                Console.WriteLine("Fortify Result Notify PIC!");
                ScanResultNotify(inputSLNs, allSLNs);
                Console.WriteLine("Fortify FSA Finish");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                //記LOG & 顯示
                Console.WriteLine(ex.Message);
                LogError(ex.ToString());
            }
            finally
            {
                Logger loggerDetail = NLog.LogManager.GetLogger("detailLog");
                loggerDetail.Info(logDetail);
                Console.WriteLine(logDetail);

                thisConfiguration.ErrorLog.Info("Error : " + errorCount);
                thisConfiguration.ErrorLog.Info("***************  Task End  ***************");
                Console.WriteLine("Error : " + errorCount);
                Console.WriteLine("Task done. Press any key to continue.");
                Console.ReadLine();
            }
        }
        #endregion 主流程

        #region 讀EXCEL
        /// <summary>
        /// 讀取Excel
        /// </summary>
        /// <returns>要讀取的Item</returns>
        public static List<FortifyScannedItem> ScannedItemRead()//回傳值 METHOD_NAME (傳入值)
        {
            // 把excel當成db進行讀取，並把需要掃描的sln檔案回存  
            // 初始化
            List<FortifyScannedItem> FortifyScannedList = new List<FortifyScannedItem>();
            // 宣告變數 string=變數,此行用意是把不同字串串起來
            string connectionString = "Provider = Microsoft.ACE.OLEDB.12.0; Data Source =" +
                thisConfiguration.FilePath + ";Extended Properties=Excel 12.0 XML;";
            DataTable LLCF = new DataTable();

            // 讀excel
            OleDbConnection connection = new OleDbConnection(connectionString);//設定連線 
            using (connection)
            {
                connection.Open();//開檔案
                //讀出整個excel檔案
                DataTable xslx = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                //取得Excel所有的工作表(頁簽)
                foreach (DataRow row in xslx.Rows)
                {
                    OleDbCommand command = new OleDbCommand("select * from [" + row["TABLE_NAME"].ToString() + "]", connection);
                    using (OleDbDataReader dr = command.ExecuteReader())
                    {
                        //讀每個欄位的值
                        if (dr.Read())
                        {
                            LLCF.Load(dr);
                        }
                    }
                }
            }
            connection.Close();
            foreach (DataRow x in LLCF.Rows)
            {
                // 如果有空行就跳過
                if (string.IsNullOrWhiteSpace(x.ItemArray[0].ToString()))
                {
                    continue;
                }
                if (x.ItemArray[0].ToString() == "系統名稱")
                {
                    string systemName = x.ItemArray.Count() <= 1 ? "Default" :
                                                                    x.ItemArray[1].ToString();
                    thisConfiguration.SystemNameSet(systemName);
                    continue;
                }
                if (x.ItemArray[0].ToString() == "Version")
                {
                    string version = x.ItemArray.Count() <= 1 ? "DefaultVersion" :
                                                                 x.ItemArray[1].ToString();
                    thisConfiguration.VersionSet(version);
                    continue;
                }
                // 標頭
                if (string.IsNullOrWhiteSpace(x.ItemArray[0].ToString()) ||
                    x.ItemArray[0].ToString() == "Path" ||
                    string.IsNullOrWhiteSpace(x.ItemArray[1].ToString()) ||
                    x.ItemArray[1].ToString() == "Solution Name" ||
                    string.IsNullOrWhiteSpace(x.ItemArray[2].ToString()) ||
                    x.ItemArray[2].ToString() == "Is Scanned" ||
                    string.IsNullOrWhiteSpace(x.ItemArray[3].ToString()) ||
                    x.ItemArray[3].ToString() == "Mail Address")
                {
                    continue;
                }

                // 欲掃描之專案
                FortifyScannedItem item = new FortifyScannedItem();
                item.SolutionPath = Utility.FilePathNormalize(x.ItemArray[0].ToString());
                item.SolutionName = x.ItemArray[1].ToString();

                string loadStatus = x.ItemArray[2].ToString();
                item.Status = string.IsNullOrEmpty(loadStatus) ?
                                Status.E :
                                Extension.GetAssignEnum<Status>(loadStatus.Trim());

                item.NotifyMailAddress = x.ItemArray[3].ToString().Split(',').ToList();
                FortifyScannedList.Add(item);
            }
            return FortifyScannedList;
        }

        #endregion 讀EXCEL

        #region 寄彙整結果
        /// <summary>
        /// 整理本次所有方案檔掃描結果給負責人，只列出統整，不寄附件。
        /// </summary>
        /// <param name="inputSLNs">本次掃描的SLN</param>
        /// <param name="allSLNs">所有該版本的SLN</param>
        public static void ScanResultNotify(List<FortifyScannedItem> inputSLNs,
                                            List<FortifyScannedItem> allSLNs)
        {
            #region 宣告與初始化
            if (!inputSLNs.IsAny() || !allSLNs.IsAny())
            {
                return;
            }

            //更新ALL的Status
            allSLNs.ForEach(x =>
            {
                FortifyScannedItem sln = inputSLNs.FirstOrDefault(y =>
                                                        y.SolutionName == x.SolutionName);
                if (sln != null)
                {
                    x.Status = sln.Status;
                }
            });

            string mailAddress = thisConfiguration.ManagerMailAddress;
            string subject = string.Format("{0} {1} Fortify SCA Result - {2}",
                                            thisConfiguration.SystemName,
                                            thisConfiguration.Version,
                                            thisConfiguration.ExcutionDate.ToString("yyyy/MM/dd"));
            string content = string.Empty;

            List<FortifyScannedItem> successSLNs = new List<FortifyScannedItem>();
            List<FortifyScannedItem> buildFailedSLNs = new List<FortifyScannedItem>();
            List<FortifyScannedItem> analyzeFailedSLNs = new List<FortifyScannedItem>();
            List<FortifyScannedItem> reportFailedSLNs = new List<FortifyScannedItem>();

            #endregion 宣告與初始化

            #region 分類
            successSLNs = inputSLNs.Where(x => x.IsBuildSuccess &&
                                               x.IsAnalyzeSuccess &&
                                               x.IsReportGenerateSuccess).ToList();
            reportFailedSLNs = inputSLNs.Where(x => x.IsBuildSuccess &&
                                                    x.IsAnalyzeSuccess &&
                                                    !x.IsReportGenerateSuccess).ToList();
            analyzeFailedSLNs = inputSLNs.Except(reportFailedSLNs)
                                         .Where(x => x.IsBuildSuccess &&
                                                     !x.IsAnalyzeSuccess).ToList();
            buildFailedSLNs = inputSLNs.Except(reportFailedSLNs)
                                       .Except(analyzeFailedSLNs)
                                       .Where(x => !x.IsBuildSuccess).ToList();
            #endregion 分類
            #region 串內容
            // Total的結論。
            string versionString = string.Format(
                "<h2>{0} {1} Fortify SCA : {2} / {3} ( scaned SLN / all SLN for this version )</h2>",
                thisConfiguration.SystemName, thisConfiguration.Version,
                allSLNs.Count(x => x.Status == Status.C), allSLNs.Count);
            // Total的結論。
            string totalString = "<h2>Total Scanned Solutions：" + inputSLNs.Count + "</h2>";
            // 成功的結論&細項
            string successString = ContentStringGenerate("Scanned Successfully", successSLNs);
            // 建置失敗的結論&細項
            string buildFailedString = ContentStringGenerate("Build Failed", buildFailedSLNs);
            // 分析失敗的結論&細項
            string analyzeFailedString = ContentStringGenerate("Analyze Failed", analyzeFailedSLNs);
            // 產報表失敗的結論&細項
            string reportFailedString = ContentStringGenerate("Generate Report Failed", reportFailedSLNs);

            content = versionString + totalString + successString + buildFailedString +
                      analyzeFailedString + reportFailedString;
            #endregion 串內容
            Utility.MailSend(mailAddress, subject, content);
        }
        /// <summary>
        /// 產生串信件的內容
        /// </summary>
        /// <param name="title">信件內容中的title，大小h2</param>
        /// <param name="SLNs">分類過的方案們</param>
        /// <returns></returns>
        private static string ContentStringGenerate(string title, List<FortifyScannedItem> SLNs)
        {
            string result = string.Empty;
            string titleString = "<h2>" + title + " : " + SLNs.Count + "</h2>";
            string detail = string.Join("<br/>", SLNs.Select(x => x.SolutionName)) + "<br/>";
            result = titleString + detail;

            return result;
        }
        #endregion 寄彙整結果
        /// <summary>
        /// 記錄error 訊息及errorCount +1
        /// </summary>
        /// <param name="msg">error message</param>
        public static void LogError(string msg)
        {
            logDetail += msg;
            thisConfiguration.ErrorLog.Error(msg);
            Console.WriteLine(msg);
            errorCount += 1;
        }

        ///ytujtryrgdrtg drgdrg drg drg d

    }
}
