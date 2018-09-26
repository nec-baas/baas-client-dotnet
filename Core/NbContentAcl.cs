using System;

namespace Nec.Nebula
{
    /// <summary>
    /// Content ACL
    /// </summary>
    /// <remarks>
    /// <para>本クラスのインスタンスはスレッドセーフではない。</para>
    /// <para>Content ACL はバケット制御用なので、現行の .NET SDK では使用しない。
    /// このため、internal 扱いとしておく。</para>
    /// </remarks>
    internal class NbContentAcl : NbAclBase
    {
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public NbContentAcl()
        {
        }

        /// <summary>
        /// Content ACL の JSON Object 表現から変換
        /// </summary>
        /// <param name="json">JSON Object</param>
        /// <exception cref="ArgumentNullException">JSON Objectがnull</exception>
        public NbContentAcl(NbJsonObject json)
            : base(json)
        {
        }
    }
}
