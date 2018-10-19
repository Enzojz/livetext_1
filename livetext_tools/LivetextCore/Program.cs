using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Squish;

namespace Livetext
{
    class Program
    {
        static void Main(string[] args)
        {
            Directory.Delete("res", true);
            new FontExtractor("Lato").generate();
            //new FontExtractor("Lato", FontStyle.Bold).generate();

        }
    }
}
