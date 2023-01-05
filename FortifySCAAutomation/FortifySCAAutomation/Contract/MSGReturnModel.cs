using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FortifySCAAutomation
{
    /// <summary>
    /// Message Return Model (回傳Message使用)
    /// </summary>
    public class MSGReturnModel
    {
        /// <summary>
        /// Message Return Flag
        /// </summary>
        public bool RETURN_FLAG { get; set; }

        /// <summary>
        /// Message Reason Code
        /// </summary>
        public string REASON_CODE { get; set; }

        /// <summary>
        /// Message Description
        /// </summary>
        public string DESCRIPTION { get; set; }
    }
}
