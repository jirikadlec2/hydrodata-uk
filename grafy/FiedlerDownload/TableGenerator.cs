using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace Fiedler
{
    /// <summary>
    /// Creates the HTML tables...
    /// </summary>
    public class TableGenerator
    {
        private string _stationXmlFile;
        private string _htmlDir;
        private List<Channel> _allChannels;
        private Hashtable _stations;

        public TableGenerator(string StationXml, string HtmlDir)
        {
            _stationXmlFile = StationXml;
            _htmlDir = HtmlDir;
            _allChannels = Channel.ReadFromXml(StationXml);
            List<Station> stlist = Station.ReadListOfStations(StationXml);
            _stations = new Hashtable();
            foreach(Station st in stlist)
            {
                _stations.Add(st.Id, st);
            }
        }

        public void CreateHtml(string ChanType)
        {
            List<Channel> channels = Channel.FindByChannelType(_allChannels, ChanType);

            Hashtable htStations = new Hashtable();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN"">");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine(@"<meta http-equiv=""content-type"" content=""text/html; charset=utf-8"">");
            sb.AppendLine("<title>");
            sb.Append("Mìøení KFGG PøF UK - ");
            sb.Append(channels[0].ChLabel);
            sb.Append("</title>");

            sb.Append("<style>");
            sb.Append(@"a img {border-width: 0px;}");
            sb.Append("</style>");
            sb.Append("</head>");

            sb.AppendLine("<body>");
            sb.AppendLine("<h1>" + "Seznam grafù - " + channels[0].ChLabel + "</h1>");

            sb.AppendLine("<table>");
            
            foreach(Channel ch in channels)
            {
                 string stName = ((Station)_stations[ch.StId]).Name;
                 string stLabel = ((Station)_stations[ch.StId]).Label;
                 string url = ChanType + "-" + stName + ".png";
                string url2 = ChanType + "-" + stName + "-m.png";

                if (!(htStations.ContainsKey(url)))
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine(@"<td><a href=""" + url + @"""><img src=""" + url2 + @"""></a></td>");
                    sb.AppendLine(@"<td><a href=""" + url + @""">" + stLabel + @"</a></td>");
                    sb.AppendLine(@"</tr>");

                    htStations.Add(url, url);
                }
            }
            sb.AppendLine("</table>");
            sb.AppendLine("<br><br>");
            sb.AppendLine("<p>Poslední aktualizace: " + DateTime.Now.ToString() + "</p>");
            sb.AppendLine(@"<a href =""" + ".." + @""">" + "Zpìt na celkový pøehled" + @"</a>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            string outFileName = System.IO.Path.Combine(_htmlDir, ChanType + ".html");

            using (System.IO.StreamWriter w = new System.IO.StreamWriter(outFileName))
            {
                w.Write(sb.ToString());
                w.Flush();
            }
        }
    }
}
