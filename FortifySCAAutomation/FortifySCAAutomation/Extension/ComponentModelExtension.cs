using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FortifySCAAutomation
{
    [AttributeUsage(AttributeTargets.All)]
    public sealed class DisplayCode : Attribute
    {
        private string nameValue;
        public DisplayCode(string name)
        {
            nameValue = name;
        }

        public string Name
        {
            get
            {
                return nameValue;
            }
        }
    }
}
