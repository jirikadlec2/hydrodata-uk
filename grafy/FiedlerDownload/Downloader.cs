// exports the file from a 'fiedler' station
// the file is downloaded for a station when the station is specified
// in the 'stations.xml' file

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

namespace Fiedler
{    
    public class Downloader
    {
        #region Private Members
        private string _usr = "prfukexp";
        private string _pwd = "prfuk2008";
        private string _format = "CHMI_1";
        //private string _format = "DTA";
        private string _interval = "1"; //2 - one month, 1 one day
        private string _url = @"https://stanice.fiedler-magr.cz/ctistanici.php";
        private string _localPath = "";
        #endregion

        #region Properties
        //public string LocalPath;
        #endregion

        #region Constructor
        public Downloader(string localPath, int interval)
        {
            _localPath = localPath;
            _interval = interval.ToString();
        }
        #endregion

        #region Private Methods
        #endregion

        #region Properties

        #endregion

        #region Public Methods

        /// <summary>
        /// downloads the data file for a specific station
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public int DownloadFile(int id, string fileName)
        {
            // Function will return the number of bytes processed
            // to the caller. Initialize to 0 here.
            int bytesProcessed = 0;

            // Assign values to these objects here so that they can
            // be referenced in the finally block
            HttpWebRequest request = null;
            Stream remoteStream = null;
            Stream localStream = null;
            WebResponse response = null;

            string agent = @"Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 2.0.50727; InfoPath.2)";
            string requestMethod = "POST";
            string contentType = @"application/x-www-form-urlencoded";

            string postData = string.Format("usr={0}&pwd={1}&format={2}&interval={3}&idnum={4}",
                _usr, _pwd, _format, _interval, id);

            request = (HttpWebRequest)(HttpWebRequest.Create(_url));
            request.UserAgent = agent;
            request.Method = requestMethod;
            request.ContentType = contentType;

            //write parameters to request message
            StreamWriter requestWriter = new StreamWriter(request.GetRequestStream());
            requestWriter.Write(postData);
            requestWriter.Close();

            string localFileName = Path.Combine(_localPath, fileName);

            // Use a try/catch/finally block as both the WebRequest and Stream
            // classes throw exceptions upon error
            try
            {
                // Create a request for the specified remote file name
                if (request != null)
                {
                    // Send the request to the server and retrieve the
                    // WebResponse object 
                    response = request.GetResponse();
                    if (response != null)
                    {
                        // Once the WebResponse object has been retrieved,
                        // get the stream object associated with the response's data
                        remoteStream = response.GetResponseStream();

                        // Create the local file
                        localStream = File.Create(localFileName);

                        // Allocate a 1k buffer
                        byte[] buffer = new byte[1024];
                        int bytesRead;

                        // Simple do/while loop to read from stream until
                        // no bytes are returned
                        do
                        {
                            // Read data (up to 1k) from the stream
                            bytesRead = remoteStream.Read(buffer, 0, buffer.Length);

                            // Write the data to the local file
                            localStream.Write(buffer, 0, bytesRead);

                            // Increment total bytes processed
                            bytesProcessed += bytesRead;
                        } while (bytesRead > 0);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                // Close the response and streams objects here 
                // to make sure they're closed even if an exception
                // is thrown at some point
                if (response != null) response.Close();
                if (remoteStream != null) remoteStream.Close();
                if (localStream != null) localStream.Close();
            }

            // Return total bytes processed to caller.
            return bytesProcessed;
        }

        /// <summary>
        /// downloads the data files for all stations specified in the
        /// XML file and writes progress to the 'log' message string
        /// </summary>
        /// <param name="stationXmlFile"></param>
        public int DownloadAll(string stationXmlFile, System.Text.StringBuilder log, string format)
        {
            _format = format;
            
            int numStations = 0;
            
            //read xml file
            List<Station> stList = Station.ReadListOfStations(stationXmlFile);
            log.AppendLine(DateTime.Now.ToString() + " downloading stations from Fiedler");

            foreach (Station st in stList)
            {
                try
                {
                    if (_format == "CHMI_1")
                    {
                        DownloadFile(st.Id, st.Id + "_chm.txt");
                    }
                    else
                    {
                        DownloadFile(st.Id, st.Id + ".dta");
                    }
                    ++numStations;
                }
                catch (Exception ex)
                {
                    //log the exception's message
                    log.AppendLine("error - " + st.Name + " " + ex.Message);
                }
            }
            return numStations;
        }
        
        #endregion
    }
}
