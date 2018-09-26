using Nec.Nebula.Internal;
using System;
using System.Collections.Generic;

namespace Nec.Nebula
{
    /// <summary>
    /// ACL 基底クラス
    /// </summary>
    public abstract class NbAclBase
    {
        // コンストラクタで空が設定される
        private ISet<string> _r;
        private ISet<string> _w;
        private ISet<string> _u;
        private ISet<string> _c;
        private ISet<string> _d;

        /// <summary>
        /// Readable ユーザID/グループ名
        /// </summary>
        /// <exception cref="ArgumentNullException">Readable ユーザID/グループ名がnull</exception>
        public ISet<string> R
        {
            get { return _r; }
            set { _r = CheckValue(value, "R"); }
        }

        /// <summary>
        /// Writable ユーザID/グループ名
        /// </summary>
        /// <exception cref="ArgumentNullException">Writable ユーザID/グループ名がnull</exception>
        public ISet<string> W
        {
            get { return _w; }
            set { _w = CheckValue(value, "W"); }
        }

        /// <summary>
        /// Updatable ユーザID/グループ名
        /// </summary>
        /// <exception cref="ArgumentNullException">Updatable ユーザID/グループ名がnull</exception>
        public ISet<string> U
        {
            get { return _u; }
            set { _u = CheckValue(value, "U"); }
        }

        /// <summary>
        /// Creatable ユーザID/グループ名
        /// </summary>
        /// <exception cref="ArgumentNullException">Creatable ユーザID/グループ名がnull</exception>
        public ISet<string> C
        {
            get { return _c; }
            set { _c = CheckValue(value, "C"); }
        }

        /// <summary>
        /// Deletable ユーザID/グループ名
        /// </summary>
        /// <exception cref="ArgumentNullException">Deletable ユーザID/グループ名がnull</exception>
        public ISet<string> D
        {
            get { return _d; }
            set { _d = CheckValue(value, "D"); }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        protected NbAclBase()
        {
            R = new HashSet<string>();
            W = new HashSet<string>();
            U = new HashSet<string>();
            C = new HashSet<string>();
            D = new HashSet<string>();
        }

        /// <summary>
        /// JSON Object から ACL への変換
        /// </summary>
        /// <param name="json">JSON Object</param>
        /// <exception cref="ArgumentNullException">JSON Objectがnull</exception>
        protected NbAclBase(NbJsonObject json)
        {
            NbUtil.NotNullWithArgument(json, "json");

            R = ConvertJsonArray(json.GetEnumerable("r"));
            W = ConvertJsonArray(json.GetEnumerable("w"));
            C = ConvertJsonArray(json.GetEnumerable("c"));
            U = ConvertJsonArray(json.GetEnumerable("u"));
            D = ConvertJsonArray(json.GetEnumerable("d"));
        }

        internal ISet<string> ConvertJsonArray(IEnumerable<object> ary)
        {
            var set = new HashSet<string>();
            if (ary != null)
            {
                foreach (var x in ary)
                {
                    set.Add(x as string);
                }
            }
            return set;
        }

        /// <summary>
        /// JSON Object へ変換
        /// </summary>
        /// <returns>JSON Object</returns>
        public virtual NbJsonObject ToJson()
        {
            var json = new NbJsonObject();
            json["r"] = new NbJsonArray(R);
            json["w"] = new NbJsonArray(W);
            json["c"] = new NbJsonArray(C);
            json["u"] = new NbJsonArray(U);
            json["d"] = new NbJsonArray(D);
            return json;
        }

        /// <summary>
        /// Set型の各プロパティ(権限)に値を設定する。
        /// 設定値が null の場合は、ArgumentNullException がスローされる。
        /// </summary>
        /// <param name="value">設定値</param>
        /// <param name="message">プロパティ名</param>
        /// <returns>設定値</returns>
        /// <exception cref="ArgumentNullException">設定値がnull</exception>
        internal ISet<string> CheckValue(ISet<string> value, string message)
        {
            NbUtil.NotNullWithArgument(value, message);

            return value;
        }
    }
}
