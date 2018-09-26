using Nec.Nebula.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nec.Nebula
{
    /// <summary>
    /// JSON オブジェクト
    /// </summary>
    public class NbJsonObject : Dictionary<string, object>
    {
        /// <summary>
        /// JSON 文字列をパースする。
        /// </summary>
        /// <param name="jsonString">JSON 文字列</param>
        /// <returns>NbJsonObject</returns>
        /// <exception cref="ArgumentNullException">JSON 文字列がnull</exception>
        /// <exception cref="ArgumentException">パース失敗</exception>
        public static NbJsonObject Parse(string jsonString)
        {
            return NbJsonParser.Parse(jsonString);
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
        public NbJsonObject()
        {
        }

        /// <summary>
        /// キーに対応する値を取得する。
        /// キーが存在しない場合は KeyNotFoundException がスローされる。
        /// </summary>
        /// <typeparam name="T">型</typeparam>
        /// <param name="key">キー</param>
        /// <returns>キーに対応する値</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        /// <exception cref="KeyNotFoundException">キーが存在しない</exception>
        /// <exception cref="InvalidCastException">型が一致しない</exception>
        public T Get<T>(string key)
        {
            NbUtil.NotNullWithArgument(key, "key");

            var obj = this[key];
            return NbTypeConverter.ConvertValue<T>(obj);
        }

        /// <summary>
        /// キーに対応する値を取得する。
        /// 値が存在しない場合はデフォルト値を返す。
        /// </summary>
        /// <typeparam name="T">型</typeparam>
        /// <param name="key">キー</param>
        /// <param name="defValue">デフォルト値</param>
        /// <returns>キーに対応する値</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        /// <exception cref="InvalidCastException">型が一致しない</exception>
        public T Opt<T>(string key, T defValue)
        {
            NbUtil.NotNullWithArgument(key, "key");

            if (!ContainsKey(key))
            {
                return defValue;
            }
            return Get<T>(key);
        }

        /// <summary>
        /// キーに対応する Json Object を取得する。
        /// 存在しない場合は null が返る。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>キーに対応する Json Object</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        /// <exception cref="InvalidCastException">型が一致しない</exception>
        public NbJsonObject GetJsonObject(string key)
        {
            return Opt<NbJsonObject>(key, null);
        }

        /// <summary>
        /// キーに対応する IEnumerable を取得する。
        /// 存在しない場合は null が返る。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>キーに対応する IEnumerable</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        /// <exception cref="InvalidCastException">型が一致しない</exception>
        public IEnumerable<object> GetEnumerable(string key)
        {
            return Opt<IEnumerable<object>>(key, null);
        }

        /// <summary>
        /// キーに対応する Json Array を取得する。
        /// 存在しない場合は null が返る。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>キーに対応する Json Array</returns>
        /// <exception cref="ArgumentNullException">キーがnull</exception>
        /// <exception cref="InvalidCastException">型が一致しない</exception>
        public NbJsonArray GetArray(string key)
        {
            return Opt<NbJsonArray>(key, null);
        }

        /// <summary>
        /// オブジェクトをマージする
        /// 同じキーが含まれる場合は上書きされる
        /// </summary>
        /// <param name="jsonObject">マージするオブジェクト</param>
        internal void Merge(NbJsonObject jsonObject)
        {
            NbUtil.NotNullWithArgument(jsonObject, "jsonObject");
            foreach (var kv in jsonObject)
            {
                this[kv.Key] = kv.Value;
            }
        }

        /// <summary>
        /// 指定のオブジェクトが現在のオブジェクトと等しいか判断する
        /// </summary>
        /// <param name="obj">比較するオブジェクト</param>
        /// <returns>等しい場合は true。それ以外の場合は false</returns>
        public override bool Equals(object obj)
        {
            var that = obj as NbJsonObject;
            if (that == null) return false;

            if (Count != that.Count) return false;
            return Keys.All(key => that.ContainsKey(key) && NbTypeConverter.CompareObject(this[key], that[key]));
        }

        /// <summary>
        /// 特定の型のハッシュコードを取得する
        /// </summary>
        /// <returns>現在のオブジェクトのハッシュコード</returns>
        public override int GetHashCode()
        {
            int hash = 0;
            foreach (var pair in this)
            {
                hash ^= pair.Key.GetHashCode();

                var value = pair.Value;
                if (value != null)
                {
                    hash ^= NbTypeConverter.GetHashCode(value);
                }
            }
            return hash;
        }
    }
}
