using System;
using System.Collections.Generic;
using System.Text;
using Fiedler.Graph;
using System.Collections;
using System.Xml;
using System.IO;

namespace Fiedler
{
    /// <summary>
    /// this is the main class which imports the data from FIEDLER and 
    /// saves them to database
    /// this class also manages logging and calls the methods from sub-classes
    /// </summary>
    public class Importer
    {
        public string logFile;
        public string StationXmlFile;
        public string LocalDataDir;
        public string GraphDir;
        public string TableDir;
        public int DownloadInterval = 1;
        public string ConnectionString;
        
        public void ImportAll()
        {
            //check stationXmlFile
            StationXmlFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stanice.xml");
            logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
            LocalDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(LocalDataDir)) 
                Directory.CreateDirectory(LocalDataDir);

            if (!File.Exists(StationXmlFile))
            {
                Logger.WriteMessage(logFile, "Couldn't find file " + StationXmlFile);
                return;
            }

            SetGraphDirFromXmlFile();

            
            StringBuilder log = new StringBuilder();
            DateTime start = DateTime.Now;  
            log.AppendLine(start.ToString() + @" Start import data from Fiedler..\n");
            try
            {
                Downloader d = new Downloader(LocalDataDir, DownloadInterval);
                //int numStations = 0;
                int numStations = d.DownloadAll(StationXmlFile, log);
                log.AppendLine("Files downloaded: " + numStations);
                DataManager m = DataManager.Create(ConnectionString);
                m.CheckAllStations(StationXmlFile);
                m.CheckAllChannels(StationXmlFile);
                List<Station> stations = Station.ReadListOfStations(StationXmlFile);
                List<Channel> channels = Channel.ReadFromXml(StationXmlFile);
                List<Channel> stChannels;
                List<Observation> observations;
                ChmiFile dta;
                int added = 0;
                foreach (Station st in stations)
                {
                    stChannels = Channel.FindByStation(channels, st.Id);
                    dta = new ChmiFile(st.Id, channels, LocalDataDir);
                    observations = dta.Read();
                    added += m.AddObservations(observations, stChannels, log);
                }
                log.AppendLine("observations added: " + added + "Time taken: " +
                    ((TimeSpan)(DateTime.Now.Subtract(start))).TotalSeconds + @" s.");
            }
            catch (Exception ex)
            {
                log.AppendLine("UNKNOWN ERROR at " + ex.Source);
                log.AppendLine(ex.Message);
            }
            finally
            {
                Logger logger = new Logger(logFile);
                logger.WriteMessage(log.ToString());
            }
        }

        private void SetGraphDirFromXmlFile()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(StationXmlFile);
            XmlElement root = doc.DocumentElement;
            foreach (XmlNode child in root.ChildNodes)
            {
                if (child.Name == "graphs")
                {
                    foreach (XmlNode child2 in child.ChildNodes)
                    {
                        if (child2.Name == "output_directory")
                        {
                            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                            GraphDir = Path.GetFullPath(child2.InnerText);
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draws the graphs and saves them to local files
        /// </summary>
        public void DrawGraphs()
        {
            StringBuilder log = new StringBuilder();
            log.AppendLine("starting DrawGraphs..");

            try
            {
                List<Station> stations = Station.ReadListOfStations(StationXmlFile);
                List<Channel> channels = Channel.ReadFromXml(StationXmlFile);

                DataManager m = DataManager.CreateDefault();
                DateTime end = DateTime.Now.Date.AddHours(DateTime.Now.Hour);
                int interval = ReadIntervalFromConfig();
                DateTime start = end.AddDays(interval * (-1));
                TimeSeries ts = new TimeSeries(start, end);
                TimeSeries curTs;
                List<TimeSeries> tslist = new List<TimeSeries>(4);
                ChartEngine eng = ChartEngine.FromConfigFile(StationXmlFile, GraphDir);
                ChartLabelInfo chi = new ChartLabelInfo();
                Channel curCh;
                chi.UnitsName = "mm";
                chi.Copyright = "Data: KFGG PřF UK Praha";
                chi.ChannelName = "hladina";
                string fileName;

                List<Channel> stch;
                List<Channel> chTypeList;
                List<string> labelList = new List<string>();
                Hashtable stHash = new Hashtable();

                foreach (Station st in stations)
                {
                    stch = Channel.FindByStation(channels, st.Id);
                    chi.StationName = st.Label;

                    //add channels to hashtable
                    //the hashtable now contains lists of multiple channels
                    stHash.Clear();
                    foreach (Channel ch in stch)
                    {
                        if (stHash.ContainsKey(ch.ChType))
                        {
                            ((List<Channel>)stHash[ch.ChType]).Add(ch);
                        }
                        else
                        {
                            stHash.Add(ch.ChType, new List<Channel>(4));
                            ((List<Channel>)stHash[ch.ChType]).Add(ch);
                        }
                    }

                    m.LocalDataDir = LocalDataDir;
                    foreach (DictionaryEntry de in stHash)
                    {
                        chTypeList = (List<Channel>)de.Value; //list of channels of same type
                        if (chTypeList.Count == 1)
                        {
                            curCh = chTypeList[0];
                            fileName = curCh.ChType + "-" + st.Name;
                            chi.ChannelName = curCh.ChLabel;
                            chi.UnitsName = curCh.Unit;

                            m.LoadObservationsFromFile(curCh.StId, curCh, start, end, ts);

                            //m.LoadObservations(curCh.StId, curCh, start, end, ts);
                            eng.CreateChart(ts, chi, curCh.ChType, fileName);
                        }
                        else if (chTypeList.Count > 1)
                        {
                            tslist.Clear();
                            labelList.Clear();
                            foreach (Channel ch in chTypeList)
                            {
                                curTs = new TimeSeries(start, end);
                                m.LoadObservationsFromFile(ch.StId, ch, start, end, curTs);
                                tslist.Add(curTs);
                                labelList.Add(ch.ChLabel);
                            }
                            chi.ChannelName = chTypeList[0].ChLabel;
                            if (chi.ChannelName.IndexOf(" ") >= 0)
                            {
                                chi.ChannelName = chi.ChannelName.Remove(chi.ChannelName.LastIndexOf(" "));
                            }
                            chi.UnitsName = chTypeList[0].Unit;

                            fileName = chTypeList[0].ChType + "-" + st.Name;
                            eng.CreateChart(tslist, chi, chTypeList[0].ChType, labelList, fileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.AppendLine("DrawGraphs ERROR - " + ex.Source + " " + ex.Message);
            }
            finally
            {
                log.AppendLine("finished DrawGraphs..");
                Logger logger = new Logger(logFile);
                logger.WriteMessage(log.ToString());
            }
        }

        public void WriteTables()
        {
            TableGenerator tabGen = new TableGenerator(StationXmlFile, TableDir);
            tabGen.CreateHtml("hla");
            tabGen.CreateHtml("tep");
            tabGen.CreateHtml("tpu");
            tabGen.CreateHtml("tvo");
            tabGen.CreateHtml("rad");
            tabGen.CreateHtml("ph");
            tabGen.CreateHtml("sra");
            tabGen.CreateHtml("vlh");
            tabGen.CreateHtml("ryv");
            tabGen.CreateHtml("tla");
            tabGen.CreateHtml("ro2");
        }

        private int ReadIntervalFromConfig()
        {
            int interval = 7;
            XmlDocument doc = new XmlDocument();
            doc.Load(StationXmlFile);
            XmlNodeList gnlist = doc.SelectNodes("//graphs");
            XmlNode graphNode = gnlist[0];
            gnlist = graphNode.ChildNodes;
            foreach (XmlNode node in gnlist)
            {
                if (node.LocalName == "interval") interval = int.Parse(node.InnerText);
            }
            return interval;
        }

    }
}
