using Nec.Nebula.Internal;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Nec.Nebula.Test.Internal
{
    [TestFixture]
    public class MongoObjectIdGeneratorTest
    {
        /**
        * CreateObjectId
        **/
        /// <summary>
        /// CreateObjectId（正常）
        /// 複数生成したObjectIdが一致しないこと
        /// </summary>
        [Test]
        public void TestCreateObjectIdNormal()
        {
            string[] ids = new string[10];

            for (var i = 0; i < 10; i++)
            {
                ids[i] = MongoObjectIdGenerator.CreateObjectId();
            }

            for (var i = 0; i < 10; i++)
            {
                for (var j = 0; j < 10; j++)
                {
                    if (i == j) continue;

                    if (ids[i].Equals(ids[j]))
                    {
                        Assert.Fail("same objectId");
                    }
                }
                Assert.AreEqual(24, ids[i].Length);
            }
        }

        string[] objectIds = new string[2];
        private void GetObjectId(int index)
        {
            objectIds[index] = MongoObjectIdGenerator.CreateObjectId();
        }

        /// <summary>
        /// CreateObjectId（Task）
        /// 生成したObjectIdが一致しないこと
        /// </summary>
        [Test]
        public void TestCreateObjectIdNormalTask()
        {
            var s1 = MongoObjectIdGenerator.CreateObjectId();

            Task.Run(() =>
            {
                var s2 = MongoObjectIdGenerator.CreateObjectId();
                Assert.AreNotSame(s1, s2);
            }).Wait();
        }

        /// <summary>
        /// CreateObjectId（Parallel）
        /// 生成したObjectIdが一致しないこと
        /// </summary>
        [Test]
        public void TestCreateObjectIdNormalParallel()
        {
            Parallel.Invoke(new Action[]
            {
                () => GetObjectId(0),
                () => GetObjectId(1)
            });

            Assert.AreNotSame(objectIds[0], objectIds[1]);
        }
    }
}
