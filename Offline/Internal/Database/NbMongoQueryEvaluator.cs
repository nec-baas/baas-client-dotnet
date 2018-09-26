using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nec.Nebula.Internal.Database
{
    /// <summary>
    /// MongoDB クエリの評価器。
    /// 対応している演算子は以下のとおり。
    /// - 論理演算子: $and, $or, $nor, $not
    /// - 比較演算子: $eq, $ne, $gt, $gte, $lte, $lt, $in, $nin, $all, $exists
    /// - 正規表現: $regex ($options は i, m, s, x のみ使用可, /re/ 形式は使用不可)
    /// 
    /// 以下には対応していない
    /// 
    /// - $size, $elemMatch, $type, $mod, /re/, $text, $where
    /// - 地理情報関係全部
    /// 
    /// Embedded Document 比較は対応している。
    /// Dot Notation も対応。ただし、配列インデックス省略形は未サポート。
    /// </summary>
    internal class NbMongoQueryEvaluator
    {
        // フィールドが存在しないことを表す特殊値。
        private static readonly Object NoField = new Object();

        /// <summary>
        /// JSON ドキュメントが MongoDB のクエリ（セレクタ）にマッチするか調べる
        /// </summary>
        /// <param name="doc">評価対象データ</param>
        /// <param name="expr">MongoDB のクエリ式</param>
        /// <returns>対象データが selector にマッチすれば true、しなければ false</returns>
        public bool Evaluate(NbJsonObject doc, NbJsonObject expr)
        {
            try
            {
                //expression単位でループ
                foreach (var pair in expr)
                {
                    var key = pair.Key;
                    var operand = pair.Value;

                    bool result;
                    if (key.StartsWith("$"))
                    {
                        //if (key.startsWith("$and") || key.startsWith("$or") || key.startsWith("$nor") || key.startsWith("$not")) {
                        result = EvaluateLogicalOperator(doc, key, operand);
                    }
                    else
                    {
                        result = EvaluateOperand(doc, key, operand);
                    }
                    if (!result)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (InvalidCastException)
            {
                //log.fine("evaluate() <end> return false [ClassCastException] ex=" + ex);
                return false;
            }
        }

        /// <summary>
        /// 論理演算子処理
        /// </summary>
        /// <param name="doc">JSON</param>
        /// <param name="oprtr">演算子</param>
        /// <param name="operand">オペランド</param>
        /// <returns>成功時は true</returns>
        private bool EvaluateLogicalOperator(NbJsonObject doc, string oprtr, object operand)
        {
            switch (oprtr)
            {
                case "$and":
                    return AndOperator(doc, (IEnumerable<object>)operand);
                case "$or":
                    return OrOperator(doc, (IEnumerable<object>)operand);
                case "$nor":
                    return !OrOperator(doc, (IEnumerable<object>)operand);
                case "$not":
                    return NotOperator(doc, operand as NbJsonObject);
                default:
                    return false; // unknown oprtr
            }
        }

        private bool AndOperator(NbJsonObject doc, IEnumerable<object> expressions)
        {
            return expressions.All(expr => Evaluate(doc, (NbJsonObject)expr));
        }

        private bool OrOperator(NbJsonObject doc, IEnumerable<object> expressions)
        {
            return expressions.Any(expr => Evaluate(doc, (NbJsonObject)expr));
        }

        private bool NotOperator(NbJsonObject doc, NbJsonObject expression)
        {
            return !Evaluate(doc, expression);
        }

        /// <summary>
        /// NbJsonObject から指定されたキーの位置の値を取得する。
        /// key が "." 区切りの場合は、その階層をたどる。
        /// </summary>
        /// <param name="doc">JSON</param>
        /// <param name="key">キー</param>
        /// <returns>値。存在しない場合は NoField。</returns>
        private object GetValue(object doc, string key)
        {
            try
            {
                if (!key.Contains("."))
                {
                    // "." 区切りなし
                    var json = (NbJsonObject)doc;
                    return json.ContainsKey(key) ? json.Opt<object>(key, null) : NoField;
                }

                // "." で区切って階層をたどる
                var keyArray = key.Split('.');
                foreach (var k in keyArray)
                {
                    try
                    {
                        // 配列インデックスの場合の処理
                        var index = int.Parse(k);
                        doc = ((IList<object>)doc)[index];
                    }
                    catch (FormatException)
                    {
                        var json = (NbJsonObject)doc;
                        if (!json.ContainsKey(k))
                        {
                            return NoField;
                        }
                        doc = json.Opt<object>(k, null);
                    }
                    if (doc == null)
                    {
                        return null;
                    }
                }
                return doc;
            }
            catch (ArgumentOutOfRangeException)
            {
                // 配列インデックス外の場合
                return null;
            }
            catch (InvalidCastException)
            {
                return null;
            }
        }

        /// <summary>
        /// オペランド評価
        /// </summary>
        /// <param name="doc">JSON</param>
        /// <param name="key">キー</param>
        /// <param name="operand">オペランド</param>
        /// <returns>評価値</returns>
        private bool EvaluateOperand(NbJsonObject doc, string key, object operand)
        {
            // 評価対象となる値を取得
            object value = GetValue(doc, key);

            // Operand null チェック
            // Operand = null は、以下の場合に条件が合致する。これは MongoDBの動作と同じ。
            // 1) 指定したキーが存在しない
            // 2) 指定したキーが存在するが、値が null
            if (operand == null)
            {
                return (value == NoField || value == null);
            }

            // 最初に直接比較を試みる
            // (スカラ値および Embedded Document 完全一致)
            if (NbTypeConverter.IsNumeric(operand) && (value != null && NbTypeConverter.IsNumeric(value)))
            {
                var comp =
                    NbTypeConverter.ConvertValue<double>(operand).CompareTo(NbTypeConverter.ConvertValue<double>(value));
                return comp == 0;
            }
            if (operand.Equals(value))
            {
                return true;
            }

            if (operand is NbJsonObject)
            {
                // 複合 operand 評価
                return EvaluateCompositeOperand(doc, key, value, (NbJsonObject)operand);
            }
            else if (operand is IEnumerable<object>)
            {
                // 配列同士の比較。valueが配列でない場合はfalseを返却
                var values = (IEnumerable<object>)value;
                return values != null ? EqualsOperator(value, operand) : false;
            }
            else if (value is IEnumerable<object>)
            {
                // 配列評価
                var values = (IEnumerable<object>)value;
                foreach (var v in values)
                {
                    if (v == null) continue;
                    if (NbTypeConverter.CompareObject(v, operand))
                        return true;
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// 複合 (NbJsonObject) オペランド評価。
        /// (中に比較演算子を含む NbJsonObject)
        /// </summary>
        /// <param name="doc">JSON</param>
        /// <param name="key">キー</param>
        /// <param name="value">値</param>
        /// <param name="operand">オペランド</param>
        /// <returns>評価値</returns>
        private bool EvaluateCompositeOperand(NbJsonObject doc, string key, object value, NbJsonObject operand)
        {
            return operand.All(entry => EvaluateOperator(doc, key, value, entry.Key, entry.Value, operand));
        }

        /// <summary>
        /// 比較演算子評価
        /// </summary>
        /// <param name="doc">JSON</param>
        /// <param name="key">キー</param>
        /// <param name="value">値</param>
        /// <param name="oprtr">演算子</param>
        /// <param name="operatorArg">演算子引数</param>
        /// <param name="parentOperand">親オペランド</param>
        /// <returns>評価値</returns>
        private bool EvaluateOperator(NbJsonObject doc, string key,
            object value, string oprtr, object operatorArg, NbJsonObject parentOperand)
        {
            switch (oprtr)
            {
                case "$in":
                    return InOperator(value, (IEnumerable<object>)operatorArg);

                case "$nin":
                    return !InOperator(value, (IEnumerable<object>)operatorArg);

                case "$all":
                    return AllOperator(value, (IEnumerable<object>)operatorArg);

                case "$exists":
                    if (operatorArg == null) return false;
                    return ExistsOperator(value, (bool)operatorArg);

                case "$eq":
                    if (operatorArg == null) return (value == null);
                    return EqualsOperator(value, operatorArg);

                case "$ne":
                    if (operatorArg == null) return !(value == null);
                    return !EqualsOperator(value, operatorArg);

                case "$gt":
                case "$gte":
                case "$lte":
                case "$lt":
                    return CompareOperator(oprtr, value, operatorArg);

                case "$regex":
                    return RegexOperator(value, operatorArg, parentOperand);

                case "$options":
                    return true; // ignore
                case "$not":
                    return !EvaluateOperand(doc, key, operatorArg);
                default:
                    return false; // unsupported operator
            }
        }

        /// <summary>
        /// 大小比較
        /// </summary>
        /// <param name="oprtr">演算子</param>
        /// <param name="op1">引数1</param>
        /// <param name="op2">引数2</param>
        /// <returns>評価値</returns>
        private bool CompareOperator(string oprtr, object op1, object op2)
        {
            if (op1 == null || op2 == null) return false;

            double comp;
            if (op1 is string && op2 is string)
            {
                comp = string.Compare((string)op1, (string)op2, StringComparison.Ordinal);
            }
            else if (NbTypeConverter.IsNumeric(op1) && NbTypeConverter.IsNumeric(op2))
            {
                comp = NbTypeConverter.ConvertValue<double>(op1) - (NbTypeConverter.ConvertValue<double>(op2));
            }
            else
            {
                return false;
            }

            switch (oprtr)
            {
                case "$gt":
                    return comp > 0.0;
                case "$gte":
                    return comp >= 0.0;
                case "$lte":
                    return comp <= 0.0;
                case "$lt":
                    return comp < 0.0;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 一致比較
        /// </summary>
        /// <param name="value">値</param>
        /// <param name="arg">引数</param>
        /// <returns>評価値</returns>
        private bool EqualsOperator(object value, object arg)
        {
            if (value == null || arg == null) return false;

            var values = value as IEnumerable<object>;
            var args = arg as IEnumerable<object>;
            // 両方配列でない場合
            if (values == null && args == null)
            {
                return NbTypeConverter.CompareObject(value, arg);
            }
            // どちらかが配列の場合は不一致
            else if (values == null || args == null)
            {
                return false;
            }
            // 両方配列の場合は要素比較
            else
            {
                // 要素数が異なる場合は不一致
                if (args.Count() != values.Count()) return false;
                var match = true;
                for (int i = 0; i < args.Count(); i++)
                {
                    var a = args.ToArray()[i];
                    var v = values.ToArray()[i];
                    if (!NbTypeConverter.CompareObject(v, a))
                    {
                        match = false;
                        break;
                    }
                }
                return match;
            }
        }

        /// <summary>
        /// $exists演算子
        /// </summary>
        /// <param name="value">値</param>
        /// <param name="exists">trueなら存在、falseなら非存在チェック</param>
        /// <returns>評価値</returns>
        private bool ExistsOperator(object value, bool exists)
        {
            return ((value != NoField && exists) || (value == NoField && !exists));
        }

        /// <summary>
        /// $in 演算子
        /// </summary>
        /// <param name="value">値</param>
        /// <param name="args">引数</param>
        /// <returns>評価値</returns>
        private bool InOperator(object value, IEnumerable<object> args)
        {
            if (args == null) return false;
            var values = value as IEnumerable<object>;
            if (values != null)
            {
                return values.Any(v => InOperator(v, args));
            }
            else
            {
                foreach (var arg in args)
                {
                    if (NbTypeConverter.CompareObject(value, arg))
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// $all演算子
        /// </summary>
        /// <param name="value">値</param>
        /// <param name="args">引数</param>
        /// <returns>評価値</returns>
        private bool AllOperator(object value, IEnumerable<object> args)
        {
            var values = value as IEnumerable<object>;
            if (values == null || args == null) return false;
            foreach (var arg in args)
            {
                var match = false;
                foreach (var v in values)
                {
                    if (NbTypeConverter.CompareObject(v, arg))
                    {
                        match = true;
                        break;
                    }
                }
                if (!match)
                    return false;
            }
            return true;
        }


        /// <summary>
        /// $regex演算子
        /// </summary>
        /// <param name="value">検査対象値</param>
        /// <param name="regexValue">正規表現</param>
        /// <param name="parentOperand">上位オペランド ($options 条件を取得するために必要)</param>
        /// <returns>評価値</returns>
        private bool RegexOperator(object value, object regexValue, NbJsonObject parentOperand)
        {
            if (!(value is string))
            {
                return false;
            }

            if (regexValue == null) return false;

            var regex = regexValue as Regex;
            if (regex == null)
            {
                // $options チェック
                var options = SetOptionsFlg(parentOperand);
                regex = new Regex((string)regexValue, options);
            }

            return regex.IsMatch((string)value);
        }

        private RegexOptions SetOptionsFlg(NbJsonObject parentOperand)
        {
            var ropts = RegexOptions.None;
            if (!parentOperand.ContainsKey("$options")) return ropts;

            var options = parentOperand.Get<string>("$options");
            if (options.Contains("i"))
            {
                ropts |= RegexOptions.IgnoreCase;
            }
            if (options.Contains("m"))
            {
                ropts |= RegexOptions.Multiline;
            }
            if (options.Contains("s"))
            {
                ropts |= RegexOptions.Singleline;
            }
            if (options.Contains("x"))
            {
                ropts |= RegexOptions.IgnorePatternWhitespace;
            }
            return ropts;
        }
    }
}
