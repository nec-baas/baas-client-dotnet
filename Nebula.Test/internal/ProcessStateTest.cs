using Nec.Nebula.Internal;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nec.Nebula.Test.Internal
{
    [TestFixture]
    public class ProcessStateTest
    {
        private ProcessState[] _states;
        private bool[] _rets;

        [SetUp]
        public void Setup()
        {
            _states = new ProcessState[2];
            _rets = new bool[2];
        }

        /**
        * GetInstance
        **/
        /// <summary>
        /// GetInstance（初期値）
        /// 同期状態、Crudの値がfalseであること
        /// </summary>
        [Test]
        public void TestGetInstanceNormalInit()
        {
            var p = ProcessState.GetInstance();

            Assert.False(p.Syncing);
            Assert.False(p.Crud);
        }

        /// <summary>
        /// GetInstance（正常）
        /// 同一のインスタンスが取得できること
        /// </summary>
        [Test]
        public void TestGetInstanceNormal()
        {
            var p1 = ProcessState.GetInstance();
            var p2 = ProcessState.GetInstance();

            Assert.AreSame(p1, p2);
        }

        /// <summary>
        /// GetInstance（正常）
        /// 同一のインスタンスが取得できること
        /// </summary>
        [Test]
        public void TestGetInstanceNormalOtherThread()
        {
            var p1 = ProcessState.GetInstance();

            Task.Run(() =>
            {
                var p2 = ProcessState.GetInstance();
                Assert.AreSame(p1, p2);
            }).Wait();
        }

        private void GetInstance(int index)
        {
            _states[index] = ProcessState.GetInstance();
        }

        /// <summary>
        /// GetInstance（正常）
        /// 同一のインスタンスが取得できること
        /// </summary>
        [Test]
        public void TestGetInstanceNormalParallel()
        {
            Parallel.Invoke(new Action[]
            {
                () => GetInstance(0),
                () => GetInstance(1)
            });

            Assert.AreSame(_states[0], _states[1]);
        }

        /**
        * TryStartSync
        **/
        /// <summary>
        /// TryStartSync（同期中・CRUD中でない）
        /// 同期状態がtrueになること
        /// trueが返ること
        /// </summary>
        [Test]
        public void TestTryStartSyncNormal()
        {
            var p = ProcessState.GetInstance();
            var result = p.TryStartSync();

            Assert.True(p.Syncing);
            Assert.True(result);

            // 後始末
            p.EndSync();
        }

        /// <summary>
        /// TryStartSync（同期中）
        /// falseが返ること
        /// </summary>
        [Test]
        public void TestTryStartSyncNormalWhileSyncing()
        {
            var p = ProcessState.GetInstance();
            p.TryStartSync();

            Assert.False(p.TryStartSync());

            // 後始末
            p.EndSync();
        }

        /// <summary>
        /// TryStartSync（同期中）
        /// falseが返ること
        /// </summary>
        [Test]
        public void TestTryStartSyncNormalWhileSyncingOtherThread()
        {
            var p = ProcessState.GetInstance();
            p.TryStartSync();

            Task.Run(() =>
            {
                Assert.False(p.TryStartSync());
            }).Wait();

            // 後始末
            p.EndSync();
        }

        private void ParallelTest(int index, int type, bool isDelayed)
        {
            ProcessState p = ProcessState.GetInstance();

            switch (type)
            {
                case 0:
                    if (isDelayed)
                    {
                        Thread.Sleep(500);
                    }
                    _rets[index] = p.TryStartSync();
                    break;
                case 1:
                    if (isDelayed)
                    {
                        Thread.Sleep(500);
                    }
                    _rets[index] = p.TryStartCrud();
                    break;
                case 2:
                    if (isDelayed)
                    {
                        Thread.Sleep(500);
                    }
                    p.EndSync();
                    break;
                case 3:
                    if (isDelayed)
                    {
                        Thread.Sleep(500);
                    }
                    p.EndCrud();
                    break;
                default: break;
            }
        }

        /// <summary>
        /// TryStartSync（同期中）
        /// falseが返ること
        /// </summary>
        [Test]
        public void TestTryStartSyncNormalWhileSyncingParallel()
        {
            Parallel.Invoke(new Action[]
            {
                () => ParallelTest(0, 0, false),
                () => ParallelTest(1, 0, false)
            });

            if (_rets[0])
            {
                Assert.False(_rets[1]);
            }
            else
            {
                Assert.True(_rets[1]);
            }

            // 後始末
            ProcessState.GetInstance().EndSync();
        }

        /// <summary>
        /// TryStartSync（CRUD中）
        /// 同期状態がtrueになること
        /// trueが返ること
        /// </summary>
        [Test]
        public void TestTryStartSyncNormalWhileCrud()
        {
            var p1 = ProcessState.GetInstance();
            p1.TryStartCrud();

            var p2 = ProcessState.GetInstance();
            Task.Run(() =>
            {
                Thread.Sleep(500);
                p1.EndCrud();
            });

            Assert.True(p2.TryStartSync());

            Assert.True(p2.Syncing);
            Assert.False(p2.Crud);

            // 後始末
            p2.EndSync();
        }

        /// <summary>
        /// TryStartSync（CRUD中）
        /// 同期状態がtrueになること
        /// trueが返ること
        /// </summary>
        [Test]
        public void TestTryStartSyncNormalWhileCrudParallel()
        {
            var p = ProcessState.GetInstance();

            Parallel.Invoke(new Action[]
            {
                () => ParallelTest(0, 1, false),
                () => ParallelTest(1, 0, false),
                () => ParallelTest(2, 3, true)
            });

            Assert.True(_rets[1]);

            Assert.True(p.Syncing);
            Assert.False(p.Crud);

            // 後始末
            p.EndSync();
        }

        /**
        * EndSync
        **/
        /// <summary>
        /// EndSync（同期中・CRUD中でない）
        /// 同期状態がfalseのままであること
        /// </summary>
        [Test]
        public void TestEndSyncNormal()
        {
            var p = ProcessState.GetInstance();
            p.EndSync();

            Assert.False(p.Syncing);
        }

        /// <summary>
        /// EndSync（同期中）
        /// 同期状態がfalseになること
        /// </summary>
        [Test]
        public void TestEndSyncNormalWhileSyncing()
        {
            var p = ProcessState.GetInstance();
            p.TryStartSync();
            p.EndSync();

            Assert.False(p.Syncing);
        }

        /**
        * TryStartCrud
        **/
        /// <summary>
        /// TryStartCrud（同期中・CRUD中でない）
        /// CRUD状態がtrueになること
        /// trueが返ること
        /// </summary>
        [Test]
        public void TestTryStartCrudNormal()
        {
            var p = ProcessState.GetInstance();
            var result = p.TryStartCrud();

            Assert.True(p.Crud);
            Assert.True(result);

            // 後始末
            p.EndCrud();
        }

        /// <summary>
        /// TryStartCrud（同期中）
        /// falseが返ること
        /// </summary>
        [Test]
        public void TestTryStartCrudNormalWhileSyncing()
        {
            var p = ProcessState.GetInstance();
            p.TryStartSync();

            Assert.False(p.TryStartCrud());
            Assert.False(p.Crud);

            // 後始末
            p.EndSync();
        }

        /// <summary>
        /// TryStartCrud（同期中）
        /// falseが返ること
        /// </summary>
        [Test]
        public void TestTryStartCrudNormalWhileSyncingOtherThread()
        {
            var p = ProcessState.GetInstance();
            p.TryStartSync();

            Task.Run(() =>
            {
                Assert.False(p.TryStartCrud());
            }).Wait();
            Assert.False(p.Crud);

            // 後始末
            p.EndSync();
        }

        /// <summary>
        /// TryStartCrud（同期中）
        /// falseが返ること
        /// </summary>
        [Test]
        public void TestTryStartCrudNormalWhileSyncingParallel()
        {
            Parallel.Invoke(new Action[]
            {
                () => ParallelTest(0, 0, false),
                () => ParallelTest(1, 1, true)
            });

            Assert.False(_rets[1]);

            // 後始末
            ProcessState.GetInstance().EndSync();
        }

        /// <summary>
        /// TryStartCrud（CRUD中）
        /// trueが返ること
        /// </summary>
        [Test]
        public void TestTryStartCrudNormalWhileCrud()
        {
            var p1 = ProcessState.GetInstance();
            p1.TryStartCrud();

            var p2 = ProcessState.GetInstance();
            Task.Run(() =>
            {
                Thread.Sleep(500);
                p1.EndCrud();
            });

            Assert.True(p2.TryStartCrud());
            Assert.True(p2.Crud);

            // 後始末
            p2.EndCrud();
        }

        /// <summary>
        /// TryStartCrud（CRUD中）
        /// trueが返ること
        /// </summary>
        [Test]
        public void TestTryStartCrudNormalWhileCrudParallel()
        {
            var p = ProcessState.GetInstance();

            Parallel.Invoke(new Action[]
            {
                () => ParallelTest(0, 1, false),
                () => ParallelTest(1, 1, false),
                () => ParallelTest(2, 3, true)
            });

            Assert.True(_rets[0]);
            Assert.True(_rets[1]);
            Assert.True(p.Crud);

            // 後始末
            p.EndCrud();
        }

        /**
        * EndCrud
        **/
        /// <summary>
        /// EndCrud（同期中・CRUD中でない）
        /// CRUD状態がfalseのままであること
        /// </summary>
        [Test]
        public void TestEndCrudNormal()
        {
            var p = ProcessState.GetInstance();
            p.EndCrud();

            Assert.False(p.Crud);
        }

        /// <summary>
        /// EndCrud（CRUD中）
        /// CRUD状態がfalseになること
        /// </summary>
        [Test]
        public void TestEndSyncNormalWhileCrud()
        {
            var p = ProcessState.GetInstance();
            p.TryStartCrud();
            p.EndCrud();

            Assert.False(p.Crud);
        }
    }
}
