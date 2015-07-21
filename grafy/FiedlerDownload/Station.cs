using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Fiedler
{
    public class Station
    {
        public int Id;
        public string Name;
        public string Label;

        public Station(int id, string name, string label)
        {
            Id = id;
            Name = name;
            Label = label;
        }

        public static List<Station> ReadListOfStations(string xmlFile)
        {
            List<Station> stList = new List<Station>();
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFile);
            XmlNodeList stationNodes = doc.SelectNodes("//station");
            //XmlNodeList channelNodes;
            string stId, stName, stLbl;
            //string chanXml;
            
            foreach (XmlNode node in stationNodes)
            {
                stId = node.Attributes["id"].InnerText;
                stName = node.Attributes["name"].InnerText;
                stLbl = node.Attributes["label"].InnerText;
                stList.Add(new Station(Convert.ToInt32(stId), stName, stLbl));
            }
            doc = null;
            return stList;
        }
    }
}
