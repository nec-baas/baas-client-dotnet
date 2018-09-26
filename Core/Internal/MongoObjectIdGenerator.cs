using System;

namespace Nec.Nebula.Internal
{
    /// <summary>
    /// MongoDB 方式の ObjectID を生成する。
    /// Unix epoch(sec) 4byte, Machine ID 3byte, Process ID 2byte, Counter 3byte から構成される。
    /// </summary>
    internal class MongoObjectIdGenerator
    {
        private static readonly string MachineProcessPart;

        private static int _counter;

        private static readonly object Lock = new object();

        static MongoObjectIdGenerator()
        {
            var random = new Random();

            int machineId = random.Next(0x1000000); // 乱数で生成

            int processId = random.Next(0x10000); // 乱数で生成
            //int processId = Process.GetCurrentProcess().Id & 0xffff;

            MachineProcessPart = ToHex(ToBytes(machineId)).Substring(2);
            MachineProcessPart += ToHex(ToBytes(processId)).Substring(4);

            _counter = random.Next(0x1000000); // 3bytes
        }

        /// <summary>
        /// オブジェクトIDを生成する
        /// </summary>
        /// <returns></returns>
        public static string CreateObjectId()
        {
            var t = (int)NbUtil.CurrentUnixTime();
            var s = ToHex(ToBytes(t));

            s += MachineProcessPart;

            int counter;
            lock (Lock)
            {
                counter = _counter++;
            }
            s += ToHex(ToBytes(counter)).Substring(2);

            return s.ToLower();
        }

        private static byte[] ToBytes(int value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        private static string ToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }
    }
}
