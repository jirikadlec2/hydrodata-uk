using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using Fiedler.Interfaces;
using System.Collections;

namespace Fiedler
{
    /// <summary>
    /// this is a special class for handling the
    /// database interaction functions
    /// </summary>
    public class DataManager
    {
        private DataUtils _dbHelper;

        private DataManager() { _dbHelper = null;  }

        private DataManager(string connectionString)
        {
            _dbHelper = new DataUtils(connectionString);
        }

        public static DataManager Create(string connectionString)
        {
            return new DataManager(connectionString);
        }

        public static DataManager CreateDefault()
        {
            return new DataManager();
        }

        public string LocalDataDir { get; set; }

        /// <summary>
        /// check if a station exists in database
        /// if it does not exist, add it to database
        /// </summary>
        /// <param name="st">the station object</param>
        /// <returns>true if a new station was added</returns>
        public bool CheckStation(Station st)
        {
            SqlCommand cmd = _dbHelper.CreateCommand("check_station");
            int added = 0;
            cmd.Parameters.Add("@id", SqlDbType.SmallInt);
            cmd.Parameters.Add("@name", SqlDbType.VarChar);
            cmd.Parameters.Add("@label", SqlDbType.VarChar);
            cmd.Parameters.Add("@result", SqlDbType.TinyInt);
            cmd.Parameters["@id"].Value = st.Id;
            cmd.Parameters["@name"].Value = st.Name;
            cmd.Parameters["@label"].Value = st.Label;
            cmd.Parameters["@result"].Direction = ParameterDirection.Output;
            try
            {
                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
                added = Convert.ToInt32(cmd.Parameters["@result"].Value);
            }
            finally
            {
                cmd.Connection.Close();
            }
            return (added > 0);
        }

        public bool CheckChannel(Channel chan)
        {
            SqlCommand cmd = _dbHelper.CreateCommand("check_channel");
            int added = 0;
            cmd.Parameters.Add("@ch_id", SqlDbType.TinyInt);
            cmd.Parameters.Add("@st_id", SqlDbType.SmallInt);
            cmd.Parameters.Add("@ch_type", SqlDbType.VarChar);
            cmd.Parameters.Add("@name", SqlDbType.VarChar);
            cmd.Parameters.Add("@label", SqlDbType.VarChar);
            cmd.Parameters.Add("@unit", SqlDbType.VarChar);
            cmd.Parameters.Add("@result", SqlDbType.TinyInt);

            cmd.Parameters["@ch_id"].Value = chan.ChId;
            cmd.Parameters["@st_id"].Value = chan.StId;
            cmd.Parameters["@ch_type"].Value = chan.ChType;
            cmd.Parameters["@name"].Value = chan.ChName;
            cmd.Parameters["@label"].Value = chan.ChLabel;
            cmd.Parameters["@unit"].Value = chan.Unit;
            cmd.Parameters["@result"].Direction = ParameterDirection.Output;
            try
            {
                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
                added = Convert.ToInt32(cmd.Parameters["@result"].Value);
            }
            finally
            {
                cmd.Connection.Close();
            }
            return (added > 0);
        }

        /// <summary>
        /// Check if all stations are in the DB and
        /// add any missing stations
        /// </summary>
        /// <param name="xmlFile">the xml file with station data</param>
        /// <returns>number of added stations</returns>
        public int CheckAllStations(string xmlFile)
        {
            List<Station> stations = Station.ReadListOfStations(xmlFile);
            int added = 0;
            foreach(Station st in stations)
            {
                if (CheckStation(st))
                {
                    ++added;
                }
            }
            return added;
        }

        /// <summary>
        /// checks all channels in the xmlFile
        /// and adds any missing channel entries to DB
        /// </summary>
        /// <param name="xmlFile"></param>
        /// <returns></returns>
        public int CheckAllChannels(string xmlFile)
        {
            List<Channel> channels = Channel.ReadFromXml(xmlFile);
            int added = 0;
            foreach (Channel ch in channels)
            {
                if (CheckChannel(ch) == true)
                {
                    ++added;
                }
            }
            return added;
        }

        /// <summary>
        /// checks the last time of observation for the station
        /// in case no time found, returns a time 48 hours before
        /// now
        /// </summary>
        /// <param name="stationId"></param>
        /// <returns></returns>
        public DateTime CheckLastDBTime(int chUid)
        {
            DateTime defaultLastTime = DateTime.Now.AddDays(-20);
            DateTime result = defaultLastTime;
            SqlCommand cmd = _dbHelper.CreateCommand("check_time");
            cmd.Parameters.Add("@ch_uid", SqlDbType.SmallInt);
            cmd.Parameters.Add("@result", SqlDbType.SmallDateTime);
            cmd.Parameters["@ch_uid"].Value = chUid;
            cmd.Parameters["@result"].Direction = ParameterDirection.Output;

            try
            {
                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
                if (!(Convert.IsDBNull(cmd.Parameters["@result"].Value)))
                {
                    result = Convert.ToDateTime(cmd.Parameters["@result"].Value);
                }
            }
            finally
            {
                cmd.Connection.Close();
            }
            return result;
        }

        /// <summary>
        /// adds all observations from the list to database
        /// </summary>
        /// <param name="obsList"></param>
        /// <returns></returns>
        public int AddObservations(List<Observation> obsList, List<Channel> chanList, StringBuilder log)
        {
            if (obsList == null) return 0;
            if (obsList.Count == 0) return 0;

            DateTime lastDBTime;
            int added = 0; //number of records added

            //hashtable with observ.times
            Hashtable ht = new Hashtable();
            foreach (Channel ch in chanList)
            {
                lastDBTime = CheckLastDBTime(ch.Uid);
                ht.Add(ch.Uid, lastDBTime);
            }

            //create a SQL command
            SqlCommand cmd = _dbHelper.CreateCommand("add_data");
            cmd.Parameters.Add(new SqlParameter("@ch_uid", SqlDbType.SmallInt));
            cmd.Parameters.Add(new SqlParameter("@time", SqlDbType.SmallDateTime));
            cmd.Parameters.Add(new SqlParameter("@value", SqlDbType.Real));
            
            foreach (Observation o in obsList)
            {
                int uid = o.ChannelUid;
                if (o.Time > ((DateTime)ht[uid]))
                {
                    try
                    {
                        cmd.Parameters["@ch_uid"].Value = uid;
                        cmd.Parameters["@time"].Value = o.Time;
                        cmd.Parameters["@value"].Value = o.Value;
                        cmd.Connection.Open();
                        cmd.ExecuteNonQuery();
                        ++added;
                    }
                    catch (Exception ex)
                    {
                        log.AppendLine("Error in DataManager.AddObservations ch " + uid
                            + " value " + o.Value + ex.Message + ex.StackTrace);
                    }

                    finally
                    {
                        cmd.Connection.Close();
                    }
                }
            }
            return added;
        }

        /// <summary>
        /// Loads the observations from the Text File
        /// </summary>
        /// <param name="stationId"></param>
        /// <param name="ch"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="observations"></param>
        /// <returns></returns>
        public int LoadObservationsFromFile(int stationId, Channel ch, DateTime start, DateTime end,
            IObservationList observations)
        {
            int channelId = ch.ChId;

            observations.Clear();

            List<Channel> oneChannel = new List<Channel> { ch };

            ChmiFile dta = new ChmiFile(stationId , oneChannel, LocalDataDir);
            List<Observation> rawData = dta.Read();

            foreach (Observation obs in rawData)
            {
                if (obs.Time >= start && obs.Time <= end)
                {
                    observations.AddObservation(obs.Time, obs.Value);
                }
            }

            return 0;
        }

        /// <summary>
        /// Loads observations from database and adds
        /// the observations to the list.
        /// </summary>
        /// <param name="stationId"></param>
        /// <param name="channelId"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="observations"></param>
        /// <returns></returns>
        public int LoadObservations(int stationId, Channel ch, DateTime start, DateTime end,
           IObservationList observations)
        {
            DateTime curTime;
            double curValue;
            int channelId = ch.ChId;

            observations.Clear();

            //create a SQL command
            SqlCommand cmd;
            if (ch.ChType == "sra")
            {
                cmd = _dbHelper.CreateCommand("qry_precip");
            }
            else
            {
                cmd = _dbHelper.CreateCommand("qry_data");
            }
            cmd.Parameters.Add(new SqlParameter("@st_id", SqlDbType.SmallInt));
            cmd.Parameters.Add(new SqlParameter("@ch_id", SqlDbType.TinyInt));
            cmd.Parameters.Add(new SqlParameter("@t1", SqlDbType.SmallDateTime));
            cmd.Parameters.Add(new SqlParameter("@t2", SqlDbType.SmallDateTime));

            cmd.Parameters["@st_id"].Value = stationId;
            cmd.Parameters["@ch_id"].Value = channelId;
            cmd.Parameters["@t1"].Value = start;
            cmd.Parameters["@t2"].Value = end;

            try
            {
                cmd.Connection.Open();
                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    curTime = Convert.ToDateTime(r["time"]);
                    curValue = Convert.ToDouble(r["value"]);
                    observations.AddObservation(curTime, curValue);
                }
            }
            finally
            {
                cmd.Connection.Close();
            }

            return 0;
        }
    }


    internal class DataUtils
    {
        private string _cnnStr =
        @"data source=.\SQLEXPRESS; Initial Catalog=hydrodataorg; User Id=hydrodataorg; password=iMnpf7fl";

        public DataUtils(string connectionString)
        {
            if (connectionString.Length > 0)
            {
                _cnnStr = connectionString;
            }
        }

        public string ConnectionString
        {
            get { return _cnnStr; }
        }

        /// <summary>
        /// creates the SQL command object
        /// cmdName is the procedure name or the SQL string
        /// </summary>
        /// <returns></returns>
        public SqlCommand CreateCommand(string cmdName)
        {
            string str = ConnectionString;
            SqlConnection cnn = new SqlConnection(str);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cnn;
            if (cmdName.ToLower().IndexOf("select") < 0)
            {
                cmd.CommandType = CommandType.StoredProcedure;
            }
            cmd.CommandText = cmdName;
            return cmd;
        }

        /// <summary>
        /// Creates a new SqlConnection object
        /// </summary>
        public SqlConnection getConnection()
        {
            string cnnStr = ConnectionString;
            return new SqlConnection(cnnStr);
        }
    }
}
