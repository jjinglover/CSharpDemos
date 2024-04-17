using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlToProto
{
    class Program
    {
        static void Main(string[] args)
        {
            ProtoXml xml = new ProtoXml();
            xml.loadAllXml((int)Load.ConfReadFlag.Cli);

            var pp = xml.findFile(Load.XmlInfo.XmlSchool);
            if (pp != null)
            {
                var sch = (Load.SchoolConf)(pp);
                MyLog.Log(sch.Attr.ToString());
                for (int i = 0; i < sch.Student.Count; ++i)
                {
                    MyLog.Log(sch.Student[i].ToString());
                }
            }
            Console.ReadLine();
        }
    }
}
