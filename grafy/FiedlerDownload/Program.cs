using System;
using System.Collections.Generic;
using System.Text;

namespace Fiedler
{
    class Program
    {
        static void Main(string[] args)
        {
            Importer imp = new Importer();           
            imp.DownloadInterval = 2; //one day

            imp.ImportAll();
            imp.DrawGraphs();
        }
    }
}
