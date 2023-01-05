using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FortifySCAAutomation
{
    #region enum定義
    /// <summary>
    /// 建置模式
    /// </summary>
    public enum BuildCategory
    {
        /// <summary>
        /// None - 不建置
        /// </summary>
        None,
        /// <summary>
        /// All - 全建置
        /// </summary>
        All,
        /// <summary>
        /// Each - 建置需掃描之方案
        /// </summary>
        Each
    }

    /// <summary>
    /// 方案目前狀態
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// C : Completed 已完成掃描
        /// </summary>
        C,
        /// <summary>
        /// F : Failed 掃描失敗
        /// </summary>
        F,
        /// <summary>
        /// S : Standby 等待掃描
        /// </summary>
        S,
        /// <summary>
        /// P : Pending 暫不掃描
        /// </summary>
        P,
        /// <summary>
        /// E : Exception 輸入不該輸入的東西
        /// </summary>
        E
    }
    #endregion
    /// <summary>
    /// 共用元件
    /// </summary>
    public class Utility
    {
        #region 路徑相關
        /// <summary>
        /// 路徑正規化 => 後加上\\
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string FilePathNormalize(string filePath)
        {
            Regex rgx2 = new Regex(@"\\$");
            if (rgx2.IsMatch(filePath) == false)
            {
                filePath = filePath + "\\";
            }

            filePath = filePath.Replace("\\\\", "\\");

            return filePath;
        }

        /// <summary>
        /// 確認路徑是否存在
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool CheckPathExist(string filePath, bool createFlag = false)
        {
            DirectoryInfo di = new DirectoryInfo(filePath);
            if (di.Exists)
            {
                return true;
            }

            if (createFlag)
            {
                di = Directory.CreateDirectory(filePath);
            }
            return di.Exists;
        }
        #endregion
        #region 寄信
        /// <summary>
        /// 根據傳入的地址、主旨、內文、附件寄信。
        /// </summary>
        /// <param name="mailAddress">收件者</param>
        /// <param name="subject">主旨</param>
        /// <param name="content">內容</param>
        /// <param name="attachmentPaths">附件</param>
        public static void MailSend(string mailAddress, string subject, string content, List<string> attachmentPaths = null)
        {
            MailMessage MyMail = new MailMessage();
            MyMail.From = new MailAddress("FSA@evaair.com"); //寄件者名稱
            MyMail.To.Add(mailAddress); //設定收件者Email
            MyMail.Subject = subject;
            MyMail.Body = content; //設定信件內容
            MyMail.IsBodyHtml = true; //是否使用html格式
            SmtpClient MySMTP = new SmtpClient("mail.evaair.com", 25); //SMTP設定

            if (attachmentPaths.IsAny())
            {
                //<-這是附件部分~先用附件的物件把路徑指定進去~
                foreach (string attchmentPath in attachmentPaths)
                {
                    string formatPath = attchmentPath.Replace("/", "\\\\");
                    //<-郵件訊息中加入附件
                    if (File.Exists(formatPath))
                    {
                        Attachment attachment = new Attachment(formatPath);
                        MyMail.Attachments.Add(attachment);
                    }
                }
            }

            try
            {
                MySMTP.Send(MyMail);
                MyMail.Dispose(); //釋放資源
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Program.LogError(ex.ToString());
            }
        }
        #endregion
    }
}
