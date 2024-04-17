using System;

namespace XmlToProto
{
    class MyLog
    {
        public static void Log(object obj)
        {
            Console.WriteLine(obj);
        }

        public static void TestLog(object obj)
        {
            if (false)
            {
                Console.WriteLine(obj);
            }
        }
    }
}
