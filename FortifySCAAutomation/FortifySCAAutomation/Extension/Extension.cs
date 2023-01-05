using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FortifySCAAutomation
{
    /// <summary>
    /// 擴充程式 - 一些方便開發的功能
    /// </summary>
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

        /// string轉DateTime 轉失敗會回傳DateTime.MinValue
        /// <summary>
        /// string轉DateTime 轉失敗會回傳DateTime.MinValue
        /// </summary>
        /// <returns></returns>
        public static DateTime ToDateTime<T>(this T sourceString)
        {
            string phreasingStr = sourceString == null ? "" : sourceString.ToString();
            DateTime retVal = DateTime.MinValue;
            DateTime.TryParse(phreasingStr, out retVal);
            return retVal;
        }

        /// string轉DateTime 轉失敗會回傳DateTime.MinValue
        /// <summary>
        /// string轉DateTime 轉失敗會回傳DateTime.MinValue
        /// </summary>
        /// <param name="sourceString"></param>
        /// <param name="format">格式</param>
        /// <returns></returns>
        public static DateTime ToDateTime(this string sourceString, string format)
        {
            DateTime retVal = DateTime.MinValue;
            DateTime.TryParseExact(sourceString, format, null, System.Globalization.DateTimeStyles.None, out retVal);
            return retVal;
        }

        /// <summary>
        /// 表示指定的型別 不是null 和 有包含項目
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static bool IsAny<T>(this IEnumerable<T> enumerable)
        {
            return enumerable != null && enumerable.Any();
        }

        /// <summary>
        /// 表示指定的型別 不是null 和 有包含項目
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static bool IsAny<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            return enumerable != null && enumerable.Any(predicate);
        }

        #region Enum

        #region 取得EnumAttribute的值
        public static string GetEnumDescription(this Enum value, string defaultValue = null)
        {
            return GetEnumAttribute<DescriptionAttribute>(value, a => a.Description, defaultValue).ToString();
        }
        public static string GetEnumDisplayCode(this Enum value, string defaultValue = null)
        {
            return GetEnumAttribute<DisplayCode>(value, a => a.Name, defaultValue).ToString();
        }
        private static object GetEnumAttribute<TAttr>(Enum value, Func<TAttr, object> expr, string defaultValue = null) where TAttr : Attribute
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            if (fi == null)
            {
                return "";
            }
            var attributes = fi.GetCustomAttributes<TAttr>(false).ToArray();
            return (attributes != null && attributes.Length > 0) ? expr(attributes.First()) : (defaultValue ?? value.ToString());
        }
        #endregion

        #region string 轉 Enum
        /// <summary>
        /// 取得指定Text的Enum
        /// </summary>
        /// <typeparam name=""></typeparam>
        /// <returns></returns>
        public static T GetAssignEnum<T>(string type)
            where T : struct
        {
            var result = Activator.CreateInstance<T>();
            if (!typeof(T).IsEnum || string.IsNullOrEmpty(type))
            {
                return result;
            }

            foreach (T Production in Enum.GetValues(typeof(T)))
            {
                if (Production.ToString() == type)
                {
                    result = Production;
                    break;
                }
            }
            return result;
        }


        /// <summary>
        /// 取得指定Attribute的Enum，如果沒指定參數則判斷Text
        /// </summary>
        /// <typeparam name=""></typeparam>
        /// <returns></returns>
        public static T GetAssignEnum<T, TAttr>(string type, Func<TAttr, string> expr)
            where T : struct
            where TAttr : Attribute
        {
            var result = Activator.CreateInstance<T>();
            if (!typeof(T).IsEnum || string.IsNullOrEmpty(type))
            {
                return result;
            }

            foreach (T Production in Enum.GetValues(typeof(T)))
            {
                FieldInfo fi = Production.GetType().GetField(Production.ToString());
                var attributes = fi.GetCustomAttributes<TAttr>(false).ToArray();
                string val = (attributes != null && attributes.Length > 0) ? expr(attributes.First()) : Production.ToString();
                if (val == type)
                {
                    result = Production;
                    break;
                }
            }
            return result;
        }
        #endregion

        public static string GetValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum) return "";
            foreach (var item in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(item, typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                    {
                        return item.GetValue(null).ToString();
                    }
                }
                else
                {
                    if (item.Name == description)
                    {
                        return item.GetValue(null).ToString();
                    }
                }
            }
            return "";
        }
        #endregion
    }
}
