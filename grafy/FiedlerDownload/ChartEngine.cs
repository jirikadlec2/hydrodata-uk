using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Drawing;
using ZedGraph;
using Fiedler.Interfaces;
using System.Xml;

namespace Fiedler.Graph
{

/// <summary>
/// This is the class for creating chart output.
/// The code was accustomed for Fiedler graphs.
/// </summary>
    public class ChartEngine
    {
        #region Declarations

        // private control variables
        int _width = 656;
        int _height = 240;
        int _thumbWidth = 40;
        int _thumbHeight = 30;
        string _dir = @"d:\jirka\temp\";
        string _thumbDir;
        bool _showText = true;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new ChartEngine, charts can be saved to disk
        /// </summary>
        public ChartEngine(string ChartDir, string ThumbDir)
        {
            _dir = ChartDir;
            _thumbDir = ThumbDir;
            _width = DefaultWidth;
            _height = DefaultHeight;
        }

        /// <summary>
        /// Creates a new ChartEngine, with resulting image width and height specified
        /// </summary>
        public ChartEngine(string chartDir, string thumbDir,
            int width, int height)
        {
            _dir = chartDir;
            _thumbDir = thumbDir;
            _width = width; 
            _height = height;
        }

        /// <summary>
        /// initializes chart engine from the configuration xml file.
        /// </summary>
        /// <param name="configFile"></param>
        public static ChartEngine FromConfigFile(string configFile, string graphDir)
        {
            ChartEngine eng = new ChartEngine(graphDir, graphDir);
            
            XmlDocument doc = new XmlDocument();
            doc.Load(configFile);
            XmlNodeList gnlist = doc.SelectNodes("//graphs");
            XmlNode graphNode = gnlist[0];
            gnlist = graphNode.ChildNodes;
            foreach (XmlNode node in gnlist)
            {
                if (node.LocalName == "height") eng._height = int.Parse(node.InnerText);
                if (node.LocalName == "width") eng._width = int.Parse(node.InnerText);
                if (node.LocalName == "thumb_height") eng._thumbHeight = int.Parse(node.InnerText);
                if (node.LocalName == "thumb_width") eng._thumbWidth = int.Parse(node.InnerText);
                if (node.LocalName == "output_directory")
                {
                    Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                    eng._dir = Path.GetFullPath(node.InnerText);
                    if (!Directory.Exists(eng._dir))
                    {
                        Directory.CreateDirectory(eng._dir);
                    }
                }
            }
            return eng;
        }

        #endregion

        #region Properties

        public bool ShowText
        {
            get { return ShowText; }
            set { _showText = value; }
        }

        public int Width
        {
            get { return _width; }
            set { _width = value; }
        }

        public int Height
        {
            get { return _height; }
            set { _height = value; }
        }

        public int ThumbWidth
        {
            get { return _thumbWidth; }
            set { _thumbWidth = value; }
        }

        public int ThumbHeight
        {
            get { return _thumbHeight; }
            set { _thumbHeight = value; }
        }

        public static int DefaultWidth
        {
            get { return 656; }
        }

        public static int DefaultHeight
        {
            get { return 290; }
        }

        #endregion Properties

        #region Create Chart
        
        /// <summary>
        /// Creates a chart of given type from the time series
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="labels"></param>
        /// <param name="chartType"></param>
        /// <returns></returns>
        public void CreateChart(ITimeSeries ts, ChartLabelInfo chartText, string chartType, string file)
        {
            Bitmap bmp;
            
            GraphPane pane = SetupGraphPane();
            SetupChart(ts, chartText, chartType, pane);
            PlotChartForSensor(ts, pane, chartType);
            

            if (ts.Count == 0 || Math.Abs(ts.MaxValue - ts.MinValue) < 0.001)
            {
                AddText(pane,"čidlo mimo provoz");
            }

            bmp = ExportGraph(pane);

            string filePath = Path.Combine(_dir , file + ".png");
            bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            //DrawThumb(bmp, file);
            bmp.Dispose();

            //draws the 'thumb' image
            //DrawThumb2(ts, chartType, filePath.Replace(".png", "-m.png"));

            DrawThumb(pane, filePath.Replace(".png", "-m.png"));
        }

        /// <summary>
        /// Overloaded version --- draws the chart for multiple time series (max.3)
        /// </summary>
        /// <param name="tsList"></param>
        /// <param name="chList"></param>
        /// <param name="file"></param>
        public void CreateChart(List<TimeSeries> tsList, ChartLabelInfo chli, string chartType,
            List<string> chNames, string file)
        {
            Bitmap bmp;

            GraphPane pane = SetupGraphPane();
            
            SetupChart(tsList[0], chli, chartType, pane);

            PlotMultiChart(tsList, chNames, pane);

            bmp = ExportGraph(pane);

            string filePath = Path.Combine(_dir, file + ".png");
            bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            bmp.Dispose();

            //draws the 'thumb' image
            DrawThumb(pane, filePath.Replace(".png", "-m.png"));
        }

        private void DrawThumb(GraphPane pane, string file)
        {
            pane.Title.IsVisible = false;
            pane.XAxis.Title.IsVisible = false;
            pane.YAxis.Title.IsVisible = false;

            pane.AxisChange();

            Bitmap bmp = ExportGraph(pane);

            Bitmap bmp2 = (Bitmap)bmp.GetThumbnailImage(_thumbWidth, _thumbHeight, null, System.IntPtr.Zero);
            bmp.Dispose();
            bmp2.Save(file, System.Drawing.Imaging.ImageFormat.Png);
            bmp2.Dispose();
        }

        /// <summary>
        /// Draws the 'thumb' version of the chart
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="chartType"></param>
        /// <param name="file"></param>
        private void DrawThumb2(ITimeSeries ts, string chartType, string file)
        {
            GraphPane myPane = new GraphPane(new RectangleF(0, 0, this.ThumbWidth, this.ThumbHeight),
                "", "", "");
            if (ts.Count > 0)
            {
                myPane.XAxis.Scale.Min = XDate.DateTimeToXLDate(ts.Start);
                myPane.XAxis.Scale.Max = XDate.DateTimeToXLDate(ts.End);
                LineItem myCurve;
                StickItem myCurve2;
                switch (chartType)
                {
                    case "hla":
                        myCurve = myPane.AddCurve("", ts, Color.Blue, SymbolType.None);
                        myCurve.Line.Width = 0;
                        myCurve.Line.Fill = new Fill(Color.Blue);
                        break;
                    case "rad":
                        myCurve = myPane.AddCurve("", ts, Color.Orange, SymbolType.None);
                        myCurve.Line.Width = 0;
                        myCurve.Line.Fill = new Fill(Color.Orange);
                        break;
                    case "sra":
                        myCurve2 = myPane.AddStick("", ts, Color.Blue);
                        //cumulative precipitation..
                        ITimeSeries ts2 = ts.ShowCumulative();
                        myCurve = myPane.AddCurve("", ts2, Color.Red, SymbolType.None);
                        myCurve.IsY2Axis = true;
                        break;
                    case "tep":
                        myCurve = myPane.AddCurve("", ts, Color.Red, SymbolType.None);
                        double[] xdata = new double[]{XDate.DateTimeToXLDate(ts.Start), 
                            XDate.DateTimeToXLDate(ts.End)};
                        double[] ydata = new double[]{0.0, 0.0};
                        LineItem myCurve3 = myPane.AddCurve("", xdata, ydata, Color.Blue);
                        break;
                    default:
                        myCurve = myPane.AddCurve("", ts, Color.Red, SymbolType.None);
                        break;
                }
                myPane.XAxis.IsVisible = false;
                myPane.YAxis.IsVisible = false;
                myPane.Y2Axis.IsVisible = false;
                myPane.XAxis.MajorGrid.IsVisible = false;
                myPane.Border.Color = Color.White;
                myPane.Legend.IsVisible = false;
                
                myPane.AxisChange();
                Bitmap bmp = ExportGraph(myPane);
                //string filePath = _dir + "m-" + file;
                bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                bmp.Dispose();
            }
        }

        private GraphPane SetupGraphPane()
        {
            GraphPane myPane = new GraphPane(new RectangleF(0, 0, this.Width, this.Height), 
                "no data", "x", "y");
            myPane.Border.IsVisible = false;
            myPane.Legend.IsVisible = true;
            return myPane;
        }

        //plot chart titles and other text description, setup the axes etc.
        private void SetupChart(ITimeSeries ts, ChartLabelInfo chartText, string chType, GraphPane pane)
        {
            //Station st = ts.Station;
            //Variable var = ts.Variable;
            DateTime start = ts.Start;
            DateTime end = ts.End;
            DrawTitle(pane, chartText.StationName, chartText.ChannelName, start, end);

            SetupAxis(pane, chType, chartText, start, end);
            SetupGrid(pane, chType);
            SetupLegend(pane);
        }

        private void DrawTitle(GraphPane myPane, string stName, string chName, DateTime minDate, DateTime maxDate)
        {
            string title = String.Format("{0} - {1} {2} - {3}",
                chName, stName, minDate.ToShortDateString(),
                maxDate.ToShortDateString());
            myPane.Title.Text = title;
            myPane.Title.FontSpec.Size = 18;
            myPane.Title.FontSpec.IsBold = false;
        }

        private void SetupAxis(GraphPane myPane, string chartType, ChartLabelInfo chText, 
            DateTime minDate, DateTime maxDate)
        {

            //setup font for axis name and tick marks text
            int scaleFontSize = 16;
            int titleFontSize = 18;

            // X axis
            ZedGraph.Axis myXAxis = myPane.XAxis;
            myXAxis.Type = AxisType.Date;
            myXAxis.Scale.Min = XDate.DateTimeToXLDate(minDate);
            myXAxis.Scale.Max = XDate.DateTimeToXLDate(maxDate);
            myXAxis.Scale.FontSpec.Size = scaleFontSize;
            myXAxis.Scale.Format = "dd.MM";
            myPane.XAxis.Title.FontSpec.Size = titleFontSize;

            // Y axis
            ZedGraph.Axis myYAxis = myPane.YAxis;
            
            myYAxis.Scale.FontSpec.Size = scaleFontSize;
            myYAxis.Title.Text = chText.ChannelName + " (" + chText.UnitsName + ")";
            myYAxis.Title.FontSpec.Size = titleFontSize;

            // data Copyright
            myPane.XAxis.Title.Text = chText.Copyright;

            // Y axis - setup for water stage
            if (chartType == "hla" || chartType == "rad" || chartType == "sra")
            {
                myYAxis.Scale.Min = 0;
            }

            if (chartType == "vlh")
            {
                myYAxis.Scale.Max = 100;
            }

            // Y2 axis - setup for cumulative precipitation
            if (chartType == "sra")
            {
                myYAxis.Title.Text = "srážky (mm/h)";
                
                ZedGraph.Axis myY2Axis = myPane.Y2Axis;
                myY2Axis.Scale.Min = 0;
                myY2Axis.Scale.FontSpec.Size = scaleFontSize;

                myY2Axis.Title.Text = "suma srážek (mm)";
                myY2Axis.Title.FontSpec.Size = titleFontSize;
                myY2Axis.IsVisible = true;
            }
        }

        private void SetupGrid(GraphPane myPane, string chType)
        {
            //first do common grid settings
            myPane.XAxis.MajorGrid.DashOn = 6.0f;
            myPane.XAxis.MajorGrid.DashOff = 2.0f;
            myPane.XAxis.MajorGrid.Color = Color.LightGray;
            myPane.XAxis.MajorGrid.IsVisible = true;

            myPane.YAxis.MajorGrid.Color = Color.LightGray;
            myPane.YAxis.MajorGrid.DashOn = 6.0f;
            myPane.YAxis.MajorGrid.DashOff = 2.0f;
            myPane.YAxis.MajorGrid.IsVisible = true;

            //special variable-specific settings
            switch (chType)
            {
                case "hla":
                    myPane.XAxis.MajorGrid.IsVisible = true;
                    myPane.YAxis.MajorGrid.IsVisible = true;
                    break;
                case "tep":
                    //myPane.XAxis.MajorGrid.IsVisible = false;
                    break;
                case "sra":
                    //myPane.Y2Axis.MajorGrid.IsVisible = true;
                    //myPane.Y2Axis.MajorGrid.Color = Color.LightGray;
                    //myPane.Y2Axis.MajorGrid.DashOn = 3.0f;
                    //myPane.Y2Axis.MajorGrid.DashOff = 2.0f;
                    break;
                default:
                    break;
            }
        }

        private void SetupLegend(GraphPane myPane)
        {
            int legendFontSize = 18;
            ZedGraph.Legend leg = myPane.Legend;
            leg.FontSpec.Size = legendFontSize;
        }

        private Bitmap ExportGraph(GraphPane pane)
        {
            Bitmap bm = new Bitmap(1, 1);
            using (Graphics g = Graphics.FromImage(bm))
            {
                pane.AxisChange(g);
            }

            return pane.GetImage();
        }

        private void PlotChartForSensor(ITimeSeries ts, GraphPane pane, string chartType)
        {
            switch ( chartType )
            {
                case "hla":
                    PlotStage(ts, pane);
                    break;
                case "sra":
                    PlotPrecipHour(ts, pane);
                    break;
                case "tep":
                case "tvo":
                case "tpu":
                case "tp1":
                case "tp2":
                    PlotTemperature(ts, pane);
                    break;
                case "ph":
                    PlotPh(ts, pane);
                    break;
                case "rad":
                    PlotRad(ts, pane);
                    break;
                case "vlh":
                    PlotVlh(ts, pane);
                    break;
                case "ryv":
                    PlotRyv(ts, pane);
                    break;
                default:
                    PlotTemperature(ts, pane);
                    break;
            }
            //also plot 'no data' if necessary
            //if ( ts.Count > ts.NumValid )
            //{
            //    PlotMissingData(ts, pane);
            //}
        }

        

        /// <summary>
        /// Do actual plotting - first try - STAGE !!!
        /// </summary>
        private void PlotStage(ITimeSeries ts, GraphPane myPane)
        {
            DateTime MinDate, MaxDate;

            if ( ts.Count > 0 )
            {
                MinDate = ts.Start;
                MaxDate = ts.End;

                //Main creation of curve
                LineItem myCurve = myPane.AddCurve("", ts, Color.Blue, SymbolType.None);
                if ( ( (TimeSpan) ( MaxDate - MinDate ) ).TotalDays < 1 )
                {
                    myCurve.Line.Width = 2F;
                }
                else
                {
                    myCurve.Line.Width = 1F;
                    myCurve.Line.Fill = new Fill(Color.FromArgb(128, Color.Blue));
                }
            }
        }

        /// <summary>
        /// Plot the discharge !!!!
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="myPane"></param>
        private void PlotDischarge(ITimeSeries ts, GraphPane myPane)
        {
            DateTime MinDate, MaxDate;

            if ( ts.Count > 0 )
            {
                MinDate = ts.Start;
                MaxDate = ts.End;

                //Main creation of curve
                LineItem myCurve = myPane.AddCurve("", ts, Color.Blue, SymbolType.None);
                if ( ( (TimeSpan) ( MaxDate - MinDate ) ).TotalDays < 31 )
                {
                    myCurve.Line.Width = 2F;
                }
                else
                {
                    myCurve.Line.Width = 1F;
                    myCurve.Line.Fill = new Fill(Color.FromArgb(128, Color.Blue));
                }
            }
        }

        /// <summary>
        /// Plot the temperature !!!!
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="myPane"></param>
        private void PlotTemperature(ITimeSeries ts, GraphPane myPane)
        {
            if (ts.Count > 0)
            {
                Color lineColor = Color.Red;
                
                //Main creation of curve
                LineItem myCurve = myPane.AddCurve("", ts, lineColor, SymbolType.None); 
                myCurve.Line.Width = 2F;
            }
        }

        private void PlotPh(ITimeSeries ts, GraphPane myPane)
        {
            if (ts.Count > 0)
            {
                Color lineColor = Color.Purple;

                //Main creation of curve
                LineItem myCurve = myPane.AddCurve("", ts, lineColor, SymbolType.None);
                myCurve.Line.Width = 2F;
            }
        }

        private void PlotRad(ITimeSeries ts, GraphPane myPane)
        {
            if (ts.Count > 0)
            {
                Color lineColor = Color.Orange;

                //Main creation of curve
                LineItem myCurve = myPane.AddCurve("", ts, lineColor, SymbolType.None);
                myCurve.Line.Fill = new Fill(Color.FromArgb(128, Color.Orange));
            }
        }

        private void PlotVlh(ITimeSeries ts, GraphPane myPane)
        {
            if (ts.Count > 0)
            {
                Color lineColor = Color.Blue;

                //Main creation of curve
                LineItem myCurve = myPane.AddCurve("", ts, lineColor, SymbolType.None);
                myCurve.Line.Width = 2F;
                myCurve.Line.Fill = new Fill(Color.FromArgb(128, Color.AliceBlue));
            }
        }

        private void PlotRyv(ITimeSeries ts, GraphPane myPane)
        {
            if (ts.Count > 0)
            {
                Color lineColor = Color.Blue;

                //Main creation of curve
                LineItem myCurve = myPane.AddCurve("", ts, lineColor, SymbolType.None);
                myCurve.Line.Width = 2F;
            }
        }

        /// <summary>
        /// Creates a chart for multiple sensors!!!
        /// </summary>
        /// <param name="tsList"></param>
        /// <param name="labelList"></param>
        /// <param name="myPane"></param>
        private void PlotMultiChart(List<TimeSeries> tsList, List<string> labelList, GraphPane myPane)
        {
            Random rand = new Random();
            Color color;
            for(int i=0; i < tsList.Count; ++i)
            {
                //get a random color
                switch (i)
                {
                    case 0:
                        color = Color.Red;
                        break;
                    case 1:
                        color = Color.Green;
                        break;
                    case 2:
                        color = Color.Blue;
                        break;
                    default:
                        color = Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));
                        break;
                }
                LineItem myCurve = myPane.AddCurve(labelList[i], tsList[i], color, SymbolType.None);
                myCurve.Line.Width = 2F;
            }
        }

        /// <summary>
        /// Plot the hourly precipitation !!!!
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="myPane"></param>
        private void PlotPrecipHour(ITimeSeries ts, GraphPane myPane)
        {
            DateTime MinDate, MaxDate;

            if (ts.Count > 0)
            {
                TimeSeries ts1 = (TimeSeries)ts.AggregateHourly();
                
                MinDate = ts1.Start;
                MaxDate = ts1.End;

                //Main creation of curve
                double totalDays = (MaxDate.Subtract(MinDate)).TotalDays;
                if (totalDays < 2)
                {
                    BarItem myCurve = myPane.AddBar("srážky", ts1, Color.Blue);
                    myCurve.Bar.Border.Color = Color.Blue;
                    myCurve.Bar.Border.IsVisible = true;
                    myCurve.Bar.Fill.Type = FillType.Solid;
                    myCurve.Bar.Fill.IsScaled = false;
                }
                else
                {
                    StickItem myCurve = myPane.AddStick("srážky", ts1, Color.Blue);
                }

                //cumulative precipitation..
                if (ts != null)
                {
                    
                    TimeSeries ts2 = (TimeSeries)ts.ShowCumulative();
                    LineItem myCurve2 = myPane.AddCurve("suma srážek", ts2, Color.Red, SymbolType.None);
                    myCurve2.IsY2Axis = true;

                    myPane.AxisChange();
                }
            }
        }

        /// <summary>
        /// puts little "crosses" on missing data points
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="myPane"></param>
        //private void PlotMissingData(TimeSeries ts, GraphPane myPane)
        //{
        //    string noDataText = GlobalResource.no_data;

        //    MeasuredTimeSeries mts = ts as MeasuredTimeSeries;
        //    if ( mts != null )
        //    {
        //        MissingTimeSeries ts2 = mts.ShowMissingData();

        //        if ( ts2.Count > 0 )
        //        {
        //            LineItem missingCurve = myPane.AddCurve(noDataText, ts2, Color.Red, SymbolType.XCross);
        //            missingCurve.Line.IsVisible = false;
        //            missingCurve.Symbol.Size = 10F;
        //            missingCurve.Symbol.Fill = new Fill(Color.Green);
        //            missingCurve.Symbol.IsVisible = true;

        //            myPane.AxisChange();
        //        }
        //    }

        //    //show a 'no data' box
        //    if ( ts.PercentAvailableData < 0.1 )
        //    {
        //        TextObj noDataObj = new TextObj(noDataText.ToUpper(), 0.5, 0.5, CoordType.PaneFraction);
        //        noDataObj.FontSpec.Size = 40f;
        //        noDataObj.IsVisible = true;
        //        noDataObj.IsClippedToChartRect = true;
        //        myPane.GraphObjList.Add(noDataObj);
        //        myPane.AxisChange();
        //    }
        //}

        #endregion


        //generate chart with error message
        // and write it directly to output stream
        public void AddText(GraphPane pane, string ErrorMessage)
        {
            TextObj text = new TextObj(ErrorMessage,
               0.40, 0.40, CoordType.PaneFraction);
            text.Location.AlignH = AlignH.Center;
            text.Location.AlignV = AlignV.Bottom;
            text.FontSpec.Border.IsVisible = true;
            text.FontSpec.Family = "Arial";
            text.FontSpec.Size = 40;
            pane.GraphObjList.Add(text);
//text.FontSpec.Fill = Fill(Color.White, Color.FromArgb(255, 100, 100), 45.0)
//text.FontSpec.StringAlignment = StringAlignment.Center
//pane.GraphObjList.Add(text)
            
            
            
        }
    }
}
