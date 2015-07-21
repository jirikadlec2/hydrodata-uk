using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Fiedler
{
    public class Channel : IComparable<Channel>
    {
        public int ChId;
        public int StId;
        public string ChType;
        public string ChName;
        public string ChLabel;
        public string Unit;

        public int Uid
        {
            get
            {
                return (StId - 28500) * 100 + ChId;
            }
        }

        public static List<Channel> ReadFromXml(string xmlFile)
        {
            int chId, stId;
            string chType, chName, chLabel, chUnit;
            List<Channel> chList = new List<Channel>();

            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFile);
            XmlNodeList chNodes = doc.SelectNodes("//channel");
            
            //string chanXml;

            foreach (XmlNode node in chNodes)
            {
                chId = Convert.ToInt32(node.Attributes["id"].InnerText);
                stId = Convert.ToInt32(node.ParentNode.Attributes["id"].InnerText);
                chType = node.Attributes["type"].InnerText;
                chName = "";
                chLabel = node.Attributes["label"].InnerText;
                chUnit = node.Attributes["units"].InnerText;
                
                Channel chan = new Channel();
                chan.ChId = chId;
                chan.StId = stId;
                chan.ChType = chType;
                chan.ChName = chName;
                chan.ChLabel = chLabel;
                chan.Unit = chUnit;
                chList.Add(chan);                
            }
            doc = null;
            return chList;
        }

        public static List<Channel> FindByStation(List<Channel> bigList, int stationId)
        {
            List<Channel> found = new List<Channel>(8);
            foreach (Channel ch in bigList)
            {
                if (ch.StId == stationId)
                {
                    found.Add(ch);
                }
            }
            found.Sort();
            return found;
        }

        public static List<Channel> FindByChannelType(List<Channel> bigList, string chType)
        {
            List<Channel> found = new List<Channel>(24);
            foreach (Channel ch in bigList)
            {
                if (ch.ChType == chType)
                {
                    found.Add(ch);
                }
            }
            found.Sort();
            return found;
        }

        private static bool BelongsToStation(Channel ch, int stId)
        {
            return (ch.StId == stId);
        }

        #region IComparable<Channel> Members

        public int CompareTo(Channel other)
        {
            return (this.Uid).CompareTo(other.Uid);
        }

        #endregion
    }

}
