using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Nec.Nebula.Test
{
    [TestFixture]
    class NbObjectConflictResolverTest
    {
        private NbObject _server;
        private NbObject _client;

        [SetUp]
        public void SetUp()
        {
            _server = new NbObject("test1");
            _client = new NbObject("test1");

            _server.UpdatedAt = "2015-01-01T00:00:00.000Z";
            _client.UpdatedAt = "2015-01-01T00:00:01.000Z";
        }

        [Test]
        public void TestPreferServerResolver()
        {
            var resolved = NbObjectConflictResolver.PreferServerResolver(_server, _client);
            Assert.AreSame(_server, resolved);
        }

        [Test]
        public void TestPreferClientResolver()
        {
            var resolved = NbObjectConflictResolver.PreferClientResolver(_server, _client);
            Assert.AreSame(_client, resolved);
        }

        //[Test]
        //public void TestPreferRecentResolver()
        //{
        //    var resolved = NbObjectConflictResolver.PreferRecentResolver(_server, _client);
        //    Assert.AreSame(_client, resolved);

        //    _server.UpdatedAt = "2015-01-01T00:00:02.000Z";
        //    resolved = NbObjectConflictResolver.PreferRecentResolver(_server, _client);
        //    Assert.AreSame(_server, resolved);
        //}
    }
}
