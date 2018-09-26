using Nec.Nebula.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nec.Nebula
{
    /// <summary>
    /// JSON配列
    /// </summary>
    public class NbJsonArray : List<object>
    {
        /// <summary>
        /// JSON配列文字列をパースする
        /// </summary>
        /// <param name="jsonString">JSON配列文字列</param>
        /// <returns>NbJsonArray</returns>
        /// <exception cref="ArgumentNullException">JSON配列文字列がnull</exception>
        /// <exception cref="ArgumentException">パース失敗</exception>
        public static NbJsonArray Parse(string jsonString)
        {
            return NbJsonParser.ParseArray(jsonString);
        }

        /// <summary>
        /// JSON文字列に変換する
        /// </summary>
        /// <returns>JSON文字列</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NbJsonArray()
        {
        }

        /// <summary>
        /// <para>コンストラクタ</para>
        /// <para>コレクションの内容をコピーする。</para>
        /// </summary>
        /// <param name="collection">コレクション</param>
        /// <exception cref="ArgumentNullException">コレクションがnull</exception>
        public NbJsonArray(IEnumerable<object> collection)
            : base(collection)
        {
        }

        /// <summary>
        /// 指定した位置の値を取得する
        /// </summary>
        /// <typeparam name="T">型</typeparam>
        /// <param name="index">インデックス</param>
        /// <returns>指定した位置の値</returns>
        /// <exception cref="ArgumentOutOfRangeException">インデックスが範囲外</exception>
        /// <exception cref="InvalidCastException">型が一致しない</exception>
        public T Get<T>(int index)
        {
            var obj = this[index];
            return NbTypeConverter.ConvertValue<T>(obj);
        }

        /// <summary>
        /// 指定した位置の値(NbJsonObject)を返す
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns>指定した位置の NbJsonObject</returns>
        /// <exception cref="ArgumentOutOfRangeException">インデックスが範囲外</exception>
        /// <exception cref="InvalidCastException">型が一致しない</exception>
        public NbJsonObject GetJsonObject(int index)
        {
            return Get<NbJsonObject>(index);
        }

        /// <summary>
        /// 指定した位置の値(NbJsonArray)を返す
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns>指定した位置の NbJsonArray</returns>
        /// <exception cref="ArgumentOutOfRangeException">インデックスが範囲外</exception>
        /// <exception cref="InvalidCastException">型が一致しない</exception>
        public NbJsonArray GetArray(int index)
        {
            return Get<NbJsonArray>(index);
        }

        /// <summary>
        /// リスト型の型変換を行う
        /// </summary>
        /// <typeparam name="T">型</typeparam>
        /// <returns>リスト</returns>
        /// <exception cref="InvalidCastException">型が一致しない</exception> 
        public IList<T> ToList<T>()
        {
            return (from x in this select (T)x).ToList();
        }

        /// <summary>
        /// 指定のオブジェクトが現在のオブジェクトと等しいか判断する
        /// </summary>
        /// <param name="obj">比較するオブジェクト</param>
        /// <returns>等しい場合は true。それ以外の場合は false</returns>
        public override bool Equals(object obj)
        {
            var that = obj as NbJsonArray;
            if (that == null) return false;

            if (Count != that.Count) return false;
            for (int i = 0; i < Count; i++)
            {
                if (!NbTypeConverter.CompareObject(this[i], that[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// 特定の型のハッシュコードを取得する
        /// </summary>
        /// <returns>現在のオブジェクトのハッシュコード</returns>
        public override int GetHashCode()
        {
            int hash = 0;
            foreach (var value in this)
            {
                if (value != null)
                {
                    hash ^= NbTypeConverter.GetHashCode(value);
                }
            }
            return hash;
        }
    }
}
