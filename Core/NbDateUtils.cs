using Nec.Nebula.Internal;
using System;
using System.Globalization;

namespace Nec.Nebula
{
    /// <summary>
    /// 日時変換
    /// </summary>
    public class NbDateUtils
    {
        // 日時文字列のフォーマット定義
        private static readonly string DateFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        /// <summary>
        /// 日時文字列をパースする。<br/>
        /// 日時は "yyyy/MM/ddTHH:mm:ss.fffZ" (UTC)であること。
        /// </summary>
        /// <param name="dateString">日時文字列</param>
        /// <returns>変換後の日時情報</returns>
        /// <exception cref="ArgumentNullException">日時情報がnull</exception>
        /// <exception cref="FormatException">日時情報のフォーマット不正</exception>
        public static DateTime ParseDateTime(string dateString)
        {
            NbUtil.NotNullWithArgument(dateString, "dateString");

            return
                DateTime.ParseExact(dateString, DateFormat, null, DateTimeStyles.AssumeUniversal)
                    .ToUniversalTime();
        }

        /// <summary>
        /// 日時情報を文字列に変換する<br/>
        /// フォーマットは yyyy/MM/ddTHH:mm:ss.fffZ (UTC)である。
        /// </summary>
        /// <param name="dt">日時情報</param>
        /// <returns>日時文字列</returns>
        public static string ToString(DateTime dt)
        {
            return dt.ToUniversalTime().ToString(DateFormat);
        }
    }
}
