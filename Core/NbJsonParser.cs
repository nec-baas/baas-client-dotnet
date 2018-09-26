using Nec.Nebula.Internal;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Nec.Nebula
{
    /// <summary>
    /// JSONパーサ
    /// </summary>
    internal static class NbJsonParser
    {
        /// <summary>
        /// JSON Object をパースする
        /// </summary>
        /// <param name="jsonString">JSON 文字列</param>
        /// <returns>NbJsonObject</returns>
        /// <exception cref="ArgumentNullException">JSON 文字列がnull</exception>
        /// <exception cref="ArgumentException">パース失敗</exception>
        public static NbJsonObject Parse(string jsonString)
        {
            NbUtil.NotNullWithArgument(jsonString, "jsonString");

            try
            {
                var reader = CreateReader(jsonString);
                if (!reader.Read())
                {
                    // ""や" "がこのルートに入る
                    // JObject.Parse("")でも、JsonReaderExceptionが発生
                    throw new ArgumentException("Not JSON Object");
                }

                return ReadJsonObject(reader);
            }
            catch (JsonReaderException)
            {
                // JSONの形式がおかしければ、ArgmentExceptionに変換
                throw new ArgumentException("Not JSON Object");
            }
        }

        /// <summary>
        /// JSON Array をパースする
        /// </summary>
        /// <param name="jsonString">JSON配列文字列</param>
        /// <returns>NbJsonArray</returns>
        /// <exception cref="ArgumentNullException">JSON配列文字列がnull</exception>
        /// <exception cref="ArgumentException">パース失敗</exception>
        public static NbJsonArray ParseArray(string jsonString)
        {
            NbUtil.NotNullWithArgument(jsonString, "jsonString");

            try
            {
                var reader = CreateReader(jsonString);
                if (!reader.Read())
                {
                    // ""や" "がこのルートに入る
                    throw new ArgumentException("Not JSON Array");
                }

                return ReadJsonArray(reader);
            }
            catch (JsonReaderException)
            {
                // JSONの形式がおかしければ、ArgmentExceptionに変換
                throw new ArgumentException("Not JSON Array");
            }
        }

        private static JsonTextReader CreateReader(string jsonString)
        {
            var reader = new JsonTextReader(new StringReader(jsonString))
            {
                // 日時文字列をコンバートさせない。
                // (Json.NET はデフォルトで日時文字列を自動的に DateTime に変換してしまう)
                DateParseHandling = DateParseHandling.None
            };
            return reader;
        }

        private static NbJsonObject ReadJsonObject(JsonTextReader reader)
        {
            var json = new NbJsonObject();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                {
                    return json;
                }

                // "[1,2,3]"の場合は、int→stringのため、InvalidCastExceptionが発生
                // 回避のため、stringかどうかをチェックする
                if (!(reader.Value is string)) throw new ArgumentException("Invalid json");
                
                var key = (string)reader.Value;

                // "[["や"[{"の場合は、keyがnullになり、jsonに設定する時に、ArgumentNullException発生
                // 回避のため、nullチェックを実施する
                if (key == null) throw new ArgumentException("Invalid json");

                reader.Read();
                json[key] = ReadValue(reader);
            }

            throw new ArgumentException("Invalid json");
        }

        private static NbJsonArray ReadJsonArray(JsonTextReader reader)
        {
            var array = new NbJsonArray();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                {
                    return array;
                }
                array.Add(ReadValue(reader));
            }

            throw new ArgumentException("Invalid json");
        }

        private static object ReadValue(JsonTextReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    return ReadJsonObject(reader);

                case JsonToken.StartArray:
                    return ReadJsonArray(reader);

                default:
                    return reader.Value;
            }
        }
    }
}
