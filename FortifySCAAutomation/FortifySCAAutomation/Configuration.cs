using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace FortifySCAAutomation
{
    /// <summary>
    /// 存放初始化之組態設定，因此都只能get不能set
    /// </summary>
    public class Configuration
    {
        #region private attribute
        private string _FilePath;
        private DateTime _ExcutionDate;
        private string _Version;
        private string _SystemName;
        private string _WebSLNPath;
        private string _ServiceSLNPath;
        private string _TFSSourcePath;
        private string _RootPath;
        private string _VSConsolePath;
        private BuildCategory _BuildMode;
        private string _TFSServerUri;
        private string _WorkSpaceName;
        private string _CheckOutUser;
        private string _FortifyTemplatePath;
        private string _FortifyReportPath;
        private string _FortifyFPRPath;
        private bool _IsPDFAttachMail;
        private Dictionary<string, string> _GlobalProperty;
        private string _ManagerMailAddress;
        private bool _IsGetLastestVersion;

        #endregion
        /// <summary>
        /// 建構子，從app.config中讀取設定
        /// </summary>
        public Configuration()
        {
            //add comment
            //what??
            Console.WriteLine("Initializing...");

            Console.Write("Manager Mail Address: ");
            _ManagerMailAddress = ConfigurationManager.AppSettings["ManagerMailAddress"].Trim();
            Console.Write(_ManagerMailAddress + Environment.NewLine);

            #region Excel File Path
            Console.Write("File Path: ");
            _FilePath = ConfigurationManager.AppSettings["FilePath"].Trim();
            if (File.Exists(_FilePath) == false)
            {
                throw new Exception(_FilePath + " doesn't exist!");
            }
            Console.Write(_FilePath + Environment.NewLine);
            #endregion Excel File Path

            _ExcutionDate = DateTime.Now;

            #region 大SLN
            Console.Write("Web SLN Path: ");
            _WebSLNPath = ConfigurationManager.AppSettings["WebSLNPath"];
            if (string.IsNullOrEmpty(_WebSLNPath) == false)
            {
                _WebSLNPath = _WebSLNPath.Trim();
                if (File.Exists(_WebSLNPath) == false)
                {
                    throw new Exception(_WebSLNPath + " doesn't exist!");
                }
            }
            Console.Write(_WebSLNPath + Environment.NewLine);
            Console.Write("Service SLN Path: ");
            _ServiceSLNPath = ConfigurationManager.AppSettings["ServiceSLNPath"];
            if (string.IsNullOrEmpty(_ServiceSLNPath) == false)
            {
                _ServiceSLNPath = _ServiceSLNPath.Trim();
                if (File.Exists(_ServiceSLNPath) == false)
                {
                    throw new Exception(_ServiceSLNPath + " doesn't exist!");
                }
            }
            Console.Write(_ServiceSLNPath + Environment.NewLine);
            #endregion 大SLN

            #region TFS 本機目錄
            Console.Write("TFS Source Path: ");
            _TFSSourcePath = ConfigurationManager.AppSettings["TFSSourcePath"].Trim();
            Utility.CheckPathExist(_TFSSourcePath, true);
            Console.Write(_TFSSourcePath + Environment.NewLine);
            #endregion TFS本機目錄

            Console.Write("Root Path: ");
            _RootPath = ConfigurationManager.AppSettings["RootPath"].Trim();
            Console.Write(_RootPath + Environment.NewLine);

            #region VS Console路徑
            Console.Write("VS Console Path: ");
            _VSConsolePath = ConfigurationManager.AppSettings["VSConsolePath"].Trim();
            if (File.Exists(_VSConsolePath) == false)
            {
                throw new Exception(_VSConsolePath + " doesn't exist!");
            }
            Console.Write(_VSConsolePath + Environment.NewLine);
            #endregion VS Console路徑

            #region Build Mode - 沒設定預設就是Each
            Console.Write("Build Mode: ");
            string loadBuildMode = ConfigurationManager.AppSettings["BuildMode"];
            _BuildMode = string.IsNullOrEmpty(loadBuildMode) ?
                BuildCategory.Each :
                FortifySCAAutomation.Extension.GetAssignEnum<BuildCategory>(loadBuildMode.Trim());
            Console.Write(ConfigurationManager.AppSettings["BuildMode"].Trim() + Environment.NewLine);
            #endregion Build Mode

            #region Build 全域參數
            _GlobalProperty = new Dictionary<string, string>();
            _GlobalProperty.Add("Configuration", "Debug");
            _GlobalProperty.Add("Platform", "Any CPU");
            //不產生PDB檔
            _GlobalProperty.Add("DebugSymbols", "false");
            _GlobalProperty.Add("DebugType", "None");
            #endregion Build 全域參數

            #region 取新版相關
            Console.Write("TFS Server Uri: ");
            _TFSServerUri = ConfigurationManager.AppSettings["TFSServerUri"].Trim();
            Console.Write(_TFSServerUri + Environment.NewLine);
            Console.Write("Is Get Lastest Version: ");
            string IsGetLastestVersionChar = ConfigurationManager.AppSettings["IsGetLastestVersion"];
            _IsGetLastestVersion = string.IsNullOrEmpty(IsGetLastestVersionChar) ?
                                true :
                                IsGetLastestVersionChar.Trim() == "Y";
            Console.Write((_IsPDFAttachMail ? "Y" : "N") + Environment.NewLine);
            Console.Write("Work Space Name: ");
            _WorkSpaceName = ConfigurationManager.AppSettings["WorkSpaceName"].Trim();
            Console.Write(_WorkSpaceName + Environment.NewLine);
            Console.Write("Check Out User: ");
            _CheckOutUser = ConfigurationManager.AppSettings["CheckOutUser"].Trim();
            Console.Write(_CheckOutUser + Environment.NewLine);
            //沒設定工作區或電腦名稱，就中止 => 以免有人誤設被蓋掉
            if (string.IsNullOrWhiteSpace(_WorkSpaceName) || string.IsNullOrWhiteSpace(_CheckOutUser))
            {
                throw new Exception("WorkerSpace or Check Out User is empty!");
            }
            #endregion 取新版相關

            #region Fortify Template Path
            Console.Write("Fortify Template Path: ");
            _FortifyTemplatePath = ConfigurationManager.AppSettings["FortifyTemplatePath"].Trim();
            if (File.Exists(_FortifyTemplatePath) == false)
            {
                throw new Exception(_FortifyTemplatePath + " doesn't exist!");
            }
            Console.Write(_FortifyTemplatePath + Environment.NewLine);
            #endregion Fortify Template Path

            #region Fortify 產生報表及fpr檔目標路徑
            Console.Write("Fortify Report Path: ");
            _FortifyReportPath = ConfigurationManager.AppSettings["FortifyReportPath"].Trim();
            Utility.CheckPathExist(_FortifyReportPath, true);
            Console.Write(_FortifyReportPath + Environment.NewLine);
            Console.Write("Fortify FPR Path: ");
            _FortifyFPRPath = ConfigurationManager.AppSettings["FortifyFPRPath"].Trim();
            Utility.CheckPathExist(_FortifyFPRPath, true);
            Console.Write(_FortifyFPRPath + Environment.NewLine);
            #endregion Fortify 產生報表及fpr檔目標路徑

            #region 信件附檔設定 - 沒設定就是Y
            Console.Write("IsPDFAttachMail: ");
            string IsPDFAttachMailChar = ConfigurationManager.AppSettings["IsPDFAttachMail"];
            _IsPDFAttachMail = string.IsNullOrEmpty(IsPDFAttachMailChar) ?
                                true :
                                IsPDFAttachMailChar.Trim() == "Y";
            Console.Write((_IsPDFAttachMail ? "Y" : "N") + Environment.NewLine);
            #endregion 信件附檔設定 - 沒設定就是Y
            ExecutionLog = NLog.LogManager.GetLogger("ExecutionLog");
            ErrorLog = NLog.LogManager.GetLogger("errorLog");
            Console.WriteLine(Environment.NewLine);
            FilePathNormalize();
        }

        /// <summary>
        /// 所有Path前後加上\\
        /// </summary>
        private void FilePathNormalize()
        {
            _RootPath = Utility.FilePathNormalize(_RootPath);
            _TFSSourcePath = Utility.FilePathNormalize(_TFSSourcePath);
            _FortifyReportPath = Utility.FilePathNormalize(_FortifyReportPath);
            _FortifyFPRPath = Utility.FilePathNormalize(_FortifyFPRPath);
        }

        /// <summary>
        /// Excel路徑
        /// </summary>
        public string FilePath
        {
            get { return this._FilePath; }
        }

        /// <summary>
        /// 執行日期
        /// </summary>
        public DateTime ExcutionDate
        {
            get { return this._ExcutionDate; }
        }

        /// <summary>
        /// 上線版本(Report串檔名用)
        /// </summary>
        public string Version
        {
            get { return this._Version; }
        }

        /// <summary>
        /// 系統代碼，寄信用
        /// </summary>
        public string SystemName
        {
            get { return this._SystemName; }
        }

        /// <summary>
        /// Web Sln檔案路徑
        /// </summary>
        public string WebSLNPath
        {
            get { return this._WebSLNPath; }
        }

        /// <summary>
        /// Service Sln檔案路徑
        /// </summary>
        public string ServiceSLNPath
        {
            get { return this._ServiceSLNPath; }
        }

        /// <summary>
        /// TFS分支路徑，例如：C:\\EVA\\NCW\\QA6
        /// </summary>
        public string TFSSourcePath
        {
            get { return this._TFSSourcePath; }
        }

        /// <summary>
        /// 對應的根目錄路徑，例如：C:\\EVA
        /// </summary>
        public string RootPath
        {
            get { return this._RootPath; }
        }

        /// <summary>
        /// VS命令提示字元路徑，例如：C:\\Program Files (x86)\\Microsoft Visual Studio 12.0\\Common7\\Tools\\VsDevCmd.bat
        /// </summary>
        public string VSConsolePath
        {
            get { return this._VSConsolePath; }
        }

        /// <summary>
        /// 建置模式(不建置/全建置/只建需掃描之Partial SLN)
        /// </summary>
        public BuildCategory BuildMode
        {
            get { return this._BuildMode; }
        }

        /// <summary>
        /// TFS Server網址，例如：http://cargotfs02:8080/tfs/NCW
        /// </summary>
        public string TFSServerUri
        {
            get { return this._TFSServerUri; }
        }

        /// <summary>
        /// 是否要取新版
        /// </summary>
        public bool IsGetLastestVersion
        {
            get { return this._IsGetLastestVersion; }
        }

        /// <summary>
        /// 工作區名稱(電腦名稱)，例如 : C143264
        /// </summary>
        public string WorkSpaceName
        {
            get { return this._WorkSpaceName; }
        }

        /// <summary>
        /// 電腦使用者名稱，AD帳號，例如：E73058
        /// </summary>
        public string CheckOutUser
        {
            get { return this._CheckOutUser; }
        }

        /// <summary>
        /// Fortify Template的路徑，預設在程式目錄下
        /// </summary>
        public string FortifyTemplatePath
        {
            get { return this._FortifyTemplatePath; }
        }

        /// <summary>
        /// 產生之Report們的放置路徑
        /// </summary>
        public string FortifyReportPath
        {
            get { return this._FortifyReportPath; }
        }

        /// <summary>
        /// 產生之FPR檔們的放置路徑
        /// </summary>
        public string FortifyFPRPath
        {
            get { return this._FortifyFPRPath; }
        }

        /// <summary>
        /// 是否要把PDF夾在附件中
        /// </summary>
        public bool IsPDFAttachMail
        {
            get { return this._IsPDFAttachMail; }
        }

        /// <summary>
        /// 管理者的信箱地址
        /// </summary>
        public string ManagerMailAddress
        {
            get { return this._ManagerMailAddress; }
        }

        /// <summary>
        /// 執行Log
        /// </summary>
        public Logger ExecutionLog { get; set; }

        /// <summary>
        /// 錯誤Log
        /// </summary>
        public Logger ErrorLog { get; set; }

        /// <summary>
        /// 錯誤Log
        /// </summary>
        public Dictionary<string, string> GlobalProperty
        {
            get { return this._GlobalProperty; }
        }

        /// <summary>
        /// 設定Version
        /// </summary>
        /// <param name="value"></param>
        public void VersionSet(string value)
        {
            _Version = value;
        }

        /// <summary>
        /// 設定System Name
        /// </summary>
        /// <param name="value"></param>
        public void SystemNameSet(string value)
        {
            _SystemName = value;
        }
    }
}
