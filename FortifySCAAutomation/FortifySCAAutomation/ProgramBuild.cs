using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FortifySCAAutomation
{
    public partial class Program
    {
        //一次變更改兩個檔案programBuild
        #region 取新版
        /// <summary>
        /// 將指定路徑底下的檔案全部取新版。
        /// </summary>
        /// <returns>取新版是否成功</returns>
        public static bool GetLatestVersion()
        {
            try
            {
                if (thisConfiguration.IsGetLastestVersion == false)
                {
                    return true;
                }
                //A. 先讀取TFS Server路徑
                Uri tfsUri = new Uri(thisConfiguration.TFSServerUri);
                TfsTeamProjectCollection myTfsTeamProjectCollection =
                    new TfsTeamProjectCollection(tfsUri);
                VersionControlServer vsStore =
                    myTfsTeamProjectCollection.GetService<VersionControlServer>();
                //B. 抓出TFS Source路徑底下的檔案清單並取新版
                //C. 將取版過程顯示在Console及記錄在Log中
                // 取得該路徑下所有的檔案
                ItemSet items = vsStore.GetItems(thisConfiguration.TFSSourcePath,
                                                    VersionSpec.Latest, RecursionType.Full);
                if (items.Items.Count() == 0)
                {
                    return false;
                }

                //取得被本機簽出的檔案 => 未來這些不要覆蓋
                PendingSet[] pendingSet = PendingSetGet(vsStore);

                Console.WriteLine("Start Getting Files..");
                foreach (Item item in items.Items)
                {
                    // 若被本機簽出 => 跳過
                    if (pendingSet.IsAny(x => x.PendingChanges.IsAny(y => y.ServerItem == item.ServerItem)))
                    {
                        Console.WriteLine(item.ServerItem + " is Checked Out");
                        continue;
                    }
                    string relativePath = item.ServerItem.Replace("$", thisConfiguration.RootPath);
                    Console.WriteLine("Get " + relativePath);
                    if (item.ItemType == ItemType.Folder)
                    {
                        Directory.CreateDirectory(relativePath);
                    }
                    else
                    {
                        item.DownloadFile(relativePath);
                    }
                }
                // 待補
                //D. 若全部下載都沒發生錯誤，回傳true
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 取得暫止的變更
        /// </summary>
        /// <param name="vsStore">TFS Server</param>
        /// <returns></returns>
        private static PendingSet[] PendingSetGet(VersionControlServer vsStore) 
        {
            ItemSpec[] itemSpecs = new ItemSpec[1];
            itemSpecs[0] = new ItemSpec(thisConfiguration.TFSSourcePath, RecursionType.Full);
            bool includeDownloadInfo = true;
            PendingSet[] pendingSet =
                vsStore.QueryPendingSets(itemSpecs, thisConfiguration.WorkSpaceName, 
                                         thisConfiguration.CheckOutUser, includeDownloadInfo);

            return pendingSet;
        }
        #endregion 取新版
        #region 建置
        /// <summary>
        /// 根據BuildMode決定要全建置或依序建置，
        /// (依照建置結果更新傳入的inputSLNs的IsBuildSuccess)
        /// </summary>
        /// <param name="inputSLNs">要建置的SLN目錄</param>
        public static void SolutionsBuild(List<FortifyScannedItem> inputSLNs)
        {
            /*
             * 根據BuildMode決定要全建置或依序建置，依照建置結果更新傳入的inputSLNs的IsBuildSuccess。
             */
            try
            {
                //A. [初始化]
                // Log相關
                string logDir = AppDomain.CurrentDomain.BaseDirectory + "\\buildLog";
                Directory.CreateDirectory(logDir);
                string thisLogDir = AppDomain.CurrentDomain.BaseDirectory + "\\buildLog\\" +
                                    thisConfiguration.ExcutionDate.ToString("yyyyMMddHHmmss");
                Directory.CreateDirectory(thisLogDir);
                if (!inputSLNs.IsAny())
                {
                    return;
                }
                //B. 判斷傳入之BuildMode
                switch (thisConfiguration.BuildMode)
                {
                    case BuildCategory.All:
                        #region All
                        // 若完整的都是 空 => 就算建置失敗
                        if (string.IsNullOrWhiteSpace(thisConfiguration.WebSLNPath) &&
                            string.IsNullOrWhiteSpace(thisConfiguration.ServiceSLNPath))
                        {
                            inputSLNs.ForEach(x => x.IsBuildSuccess = false);
                            break;
                        }
                        //建置SERVICE SLN
                        if (!string.IsNullOrWhiteSpace(thisConfiguration.ServiceSLNPath))
                        {

                            FortifyScannedItem serviceSLN = new FortifyScannedItem();
                            serviceSLN.Build(thisConfiguration.ServiceSLNPath);
                            if (serviceSLN.IsBuildSuccess)
                            {
                                inputSLNs.ForEach(x => x.IsBuildSuccess = true);
                            }
                            else
                            {
                                inputSLNs.ForEach(x => x.IsBuildSuccess = false);
                                break;
                            }
                        }
                        // 建置WEB SLN
                        if (!string.IsNullOrWhiteSpace(thisConfiguration.WebSLNPath))
                        {
                            FortifyScannedItem webSLN = new FortifyScannedItem();
                            webSLN.Build(thisConfiguration.WebSLNPath);
                            if (webSLN.IsBuildSuccess)
                            {
                                inputSLNs.ForEach(x => x.IsBuildSuccess = true);
                            }
                            else
                            {
                                inputSLNs.ForEach(x => x.IsBuildSuccess = false);
                                break;
                            }
                        }
                        break;
                        #endregion All
                    case BuildCategory.Each:
                        foreach (FortifyScannedItem item in inputSLNs)
                        {
                            item.Build();
                        }
                        break;
                    case BuildCategory.None:
                    default:
                        inputSLNs.ForEach(x => x.IsBuildSuccess = true);
                        break;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion 建置
    }
}
