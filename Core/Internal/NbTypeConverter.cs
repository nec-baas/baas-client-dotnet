using System;

namespace Nec.Nebula.Internal
{
    /// <summary>
    /// オブジェクト型変換
    /// </summary>
    internal static class NbTypeConverter
    {
        /// <summary>
        ///  型変換を行う。int -> long 型変換など。
        /// </summary>
        /// <typeparam name="T">型</typeparam>
        /// <param name="value">値</param>
        /// <returns>変換後の値</returns>
        /// <exception cref="InvalidCastException">キャスト失敗</exception>
        internal static T ConvertValue<T>(object value)
        {
            // null の場合
            if (value == null)
            {
                return default(T);
            }

            // 型が同一の場合はそのままキャスト。
            if (value is T)
            {
                return (T)value;
            }

            // 数値型でなければそのままキャスト
            if (!IsNumeric(value) || !IsNumeric(typeof(T)))
            {
                return (T)value;
            }

            // 自動変換
            return (T)Convert.ChangeType(value, typeof(T));
        }

        internal static bool IsNumeric(object value)
        {
            return IsNumeric(value.GetType());
        }

        internal static bool IsNumeric(Type type)
        {
            if (type == typeof(Byte) ||
                type == typeof(SByte) ||
                type == typeof(UInt16) ||
                type == typeof(Int16) ||
                type == typeof(UInt32) ||
                type == typeof(Int32) ||
                type == typeof(UInt64) ||
                type == typeof(Int64) ||
                type == typeof(Decimal) ||
                type == typeof(Single) ||
                type == typeof(Double))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 値比較。数値の場合は、型が違っていても値が同一なら同一と判定。
        /// </summary>
        /// <param name="arg1">オブジェクト１</param>
        /// <param name="arg2">オブジェクト２</param>
        /// <returns>同一の場合はtrue、それ以外の場合はfalse</returns>
        internal static bool CompareObject(object arg1, object arg2)
        {
            // null check
            if (arg1 == null && arg2 == null) return true;
            if (arg1 == null || arg2 == null) return false;

            if (IsNumeric(arg1) && IsNumeric(arg2))
            {
                var comp =
                    ConvertValue<double>(arg1).CompareTo(ConvertValue<double>(arg2));
                if (comp == 0)
                    return true;
            }
            else
            {
                if (arg1 == arg2 || arg1.Equals(arg2))
                    return true;
            }
            return false;
        }

        internal static int GetHashCode(object arg)
        {
            if (arg == null) return 0;
            if (IsNumeric(arg))
            {
                return ConvertValue<double>(arg).GetHashCode();
            }
            else
            {
                return arg.GetHashCode();
            }
        }
    }
}
