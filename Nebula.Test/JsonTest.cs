using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NUnit.Framework;

namespace Nec.Nebula.Test
{
    [TestFixture]
    public class JsonTest
    {
        [SetUp]
        public void Setup()
        {
        }

        public static IEnumerable JsonTestNormalArg()
        {
            yield return new TestCaseData("{d:1, n0:{c:2, a:3}}", 1).Returns(3);
            yield return new TestCaseData("{d:1, n0:{c:2, n1:{c:2, a:3}}}", 2).Returns(3);
            yield return new TestCaseData("{d:1, n0:{c:2, n1:{c:2, n2:{c:2, a:3}}}}", 3).Returns(3);
        }

        [Test, TestCaseSource("JsonTestNormalArg")]
        public int JsonTestNormal(string jsonString, int nestCount)
        {
            var json = JObject.Parse(jsonString);

            for (var i = 0; i < nestCount; i++)
            {
                json = (JObject) json["n" + i];
            }

            return (int) json["a"];
        }
    }
}
