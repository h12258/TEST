using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FortifySCAAutomation;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace UnitTest
{
    [TestClass]
    public class UnitTest
    {
        /// <summary>
        /// 測試寄信功能
        /// </summary>
        [TestMethod]
        public void TestMailSend()
        {
            #region case 1 - 寄信 + 附件
            //string mailAddress = "minghunghsu@evaair.com, damychou@evaair.com";
            string mailAddress = "minghunghsu@evaair.com";
            string subject = "[Test] NCW V21.05.26 EVA.NCW.REFS.Customer.Service.sln Fortify Scan Successfully ("
                                + DateTime.Now.ToString("yyyy/MM/dd") + ")";
            string content = "NCW V21.05.26 EVA.NCW.REFS.Customer.Service.sln Fortify Scan Successfully ("
                                + DateTime.Now.ToString("yyyy/MM/dd") + ")";
            List<string> attachmentPaths = new List<string> { @"C:\Document\NCW\版本文件\2021\V21.05.17 - 20210517\QA Fortify\V210517_EVA.NCW.REFS.Customer.ServiceFortifyReport.pdf" };
            Utility.MailSend(mailAddress, subject, content, attachmentPaths);
            #endregion case 1
        }

        /// <summary>
        /// 測試初始化CONFIG
        /// </summary>
        [TestMethod]
        public void TestConfiguration()
        {
            #region 讀取app.config
            Configuration thisConfiguration = new Configuration();
            #endregion
            #region 設定Version & System Name
            thisConfiguration.VersionSet("V210531");
            thisConfiguration.SystemNameSet("NCW");
            #endregion
            Assert.AreEqual(thisConfiguration.Version, "V210531");
        }

        /// <summary>
        ///測試讀EXCEL
        /// </summary>
        [TestMethod]
        public void TestScannedItemRead()
        {
            Program.thisConfiguration = new Configuration();
            List<FortifyScannedItem> items = Program.ScannedItemRead();
            Assert.IsFalse(items[0].Status == Status.S);
        }

        /// <summary>
        /// 測試取新版
        /// </summary>
        [TestMethod]
        public void TestGetLatestVersion()
        {
            Program.thisConfiguration = new Configuration();
            bool result = Program.GetLatestVersion();
            Assert.IsTrue(result);
        }

        /// <summary>
        /// 測試建置
        /// </summary>
        [TestMethod]
        public void TestBuild()
        {
            Program.thisConfiguration = new Configuration();
            List<FortifyScannedItem> items = Program.ScannedItemRead();
            Program.SolutionsBuild(items);
            Assert.IsTrue(items[0].IsBuildSuccess);
        }

        [TestMethod]
        public void TestCheckCompleted()
        {
            MSGReturnModel result = new MSGReturnModel();

            FortifyScannedItem Item = new FortifyScannedItem();
            Item.SolutionPath = @"C:\EVA\NCW\QA6";
            Item.SolutionName = "EVA.NCW.ACR.Service.sln";
            Item.AnalyzeLogPath = @"C:\EVA\NCW\FortifySCAAutomation\FortifySCAAutomation\UnitTest\EVA.NCW.ACR.Service.sln_202106090251.log";
            result = Item.CheckCompleted();
            //正確
            Assert.IsTrue(result.RETURN_FLAG);
            //找不到
            Item.AnalyzeLogPath = "";
            result = Item.CheckCompleted();
            Assert.IsFalse(result.RETURN_FLAG);
            //錯誤
            Item.AnalyzeLogPath = @"C:\EVA\NCW\FortifySCAAutomation\FortifySCAAutomation\UnitTest\TEST.txt";
            result = Item.CheckCompleted();
            Assert.IsFalse(result.RETURN_FLAG);
        }

        /// <summary>
        /// 測試更新Excel
        /// </summary>
        [TestMethod]
        public void TestUpdateExcel()
        {
            Program.thisConfiguration = new Configuration();
            List<FortifyScannedItem> items = Program.ScannedItemRead();
            foreach (FortifyScannedItem item in items)
            {
                item.Status = Status.C;
                bool result = item.StatusUpdate();
                Assert.IsTrue(result);
            }
        }

        /// <summary>
        /// 測試彙整結果
        /// </summary>
        [TestMethod]
        public void TestScanResultNotify()
        {
            Program.thisConfiguration = new Configuration();
            List<FortifyScannedItem> inputSLNs = Program.ScannedItemRead();
            List<FortifyScannedItem> allSLNs = Program.ScannedItemRead();

            #region 1. all pass
            inputSLNs[0].IsBuildSuccess = true;
            inputSLNs[0].IsAnalyzeSuccess = true;
            inputSLNs[0].IsReportGenerateSuccess = true;
            inputSLNs[0].Status = Status.C;
            inputSLNs[1].IsBuildSuccess = true;
            inputSLNs[1].IsAnalyzeSuccess = true;
            inputSLNs[1].IsReportGenerateSuccess = true;
            inputSLNs[1].Status = Status.C;
            Program.ScanResultNotify(inputSLNs, allSLNs);
            #endregion 1. all pass
            #region 2. build & anaylysis failed
            //build failed
            inputSLNs[0].IsBuildSuccess = false;
            inputSLNs[0].IsAnalyzeSuccess = false;
            inputSLNs[0].IsReportGenerateSuccess = false;
            inputSLNs[0].Status = Status.F;
            //buianaylysisld failed
            inputSLNs[1].IsBuildSuccess = true;
            inputSLNs[1].IsAnalyzeSuccess = false;
            inputSLNs[1].IsReportGenerateSuccess = false;
            inputSLNs[1].Status = Status.F;
            Program.ScanResultNotify(inputSLNs, allSLNs);
            #endregion 2. build & anaylysis failed
            #region 3. one success & report failed
            //build failed
            inputSLNs[0].IsBuildSuccess = false;
            inputSLNs[0].IsAnalyzeSuccess = false;
            inputSLNs[0].IsReportGenerateSuccess = false;
            inputSLNs[0].Status = Status.F;
            //buianaylysisld failed
            inputSLNs[1].IsBuildSuccess = true;
            inputSLNs[1].IsAnalyzeSuccess = false;
            inputSLNs[1].IsReportGenerateSuccess = false;
            inputSLNs[0].Status = Status.F;
            Program.ScanResultNotify(inputSLNs, allSLNs);
            #endregion 3. build & anaylysis failed
            #region 4. no solution
            List<FortifyScannedItem> noSLN = Program.ScannedItemRead();
            Program.ScanResultNotify(noSLN, allSLNs);
            #endregion 4. no solution
        }

        /// <summary>
        ///測試讀EXCEL
        /// </summary>
        [TestMethod]
        public void TestSLNNotify()
        {
            Program.thisConfiguration = new Configuration();
            List<FortifyScannedItem> items = Program.ScannedItemRead();
            items[0].ReportPath = (@"C:\Fortify Reports\NCWV210531\202106211008\V210531_EVA.NCW.ACR.Service.sln.pdf");
            #region 1. sucess
            items[0].IsBuildSuccess = true;
            items[0].IsAnalyzeSuccess = true;
            items[0].IsReportGenerateSuccess = true;
            items[0].Notify();
            #endregion 1. sucess
            #region 2. build faild
            items[0].IsBuildSuccess = false;
            items[0].IsAnalyzeSuccess = false;
            items[0].IsReportGenerateSuccess = false;
            items[0].ReportPath = "";
            items[0].Notify();
            #endregion 2. build faild
            #region 3. Analyze faild
            items[0].IsBuildSuccess = true;
            items[0].IsAnalyzeSuccess = false;
            items[0].IsReportGenerateSuccess = false;
            items[0].ReportPath = "";
            items[0].Notify();
            #endregion 3. Analyze faild
            #region 4. Report Generate faild
            items[0].IsBuildSuccess = true;
            items[0].IsAnalyzeSuccess = true;
            items[0].IsReportGenerateSuccess = false;
            items[0].ReportPath = "";
            items[0].Notify();
            #endregion 4. Report Generate faild

        }
    }
}
