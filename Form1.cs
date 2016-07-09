using Halak;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace FlowStream
{
    public partial class Form1 : Form
    {
        private SoulStream soulStream = null;

        public Form1()
        {
            InitializeComponent();
            soulStream = new SoulStream();

            try
            {
                // Open the text file using a stream reader.
                using (StreamReader sr = new StreamReader("simple_data.json"))
                {
                    // Read the stream to a string, and try json parse.
                    var json = JValue.Parse(sr.ReadToEnd());

                    foreach (var page in json["pages"].Array())
                    {
                        soulStream.AddNode(page.AsString());
                    }

                    foreach (var link in json["links"].Array())
                    {
                        soulStream.AddLink(link["from"].AsString(), link["to"].AsString());
                    }
                }

                soulStream.MakeFlow();
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

            using (StreamWriter sw = new StreamWriter("output.html"))
            {
                sw.Write(@"<html>
  <head>
    <meta charset=""utf-8"">
    <title>FlowStream</title>
    <link href=""./vnv.css"" rel=""stylesheet"">
  </head>  <body>
  <svg width=""1000"" height=""1000"">");

                foreach (SoulStream.Node node in soulStream.GetNodes())
                {
                    if (node.Name == SoulStream.RootNodeName)
                        continue;

                    var nodeClass = new XElement(XName.Get("g"),
                        new XAttribute(XName.Get("class"), "node"),
                        new XAttribute(XName.Get("style"), "opacity: 1;"),
                        new XAttribute(XName.Get("transform"), "translate(" + (node.xPos * 100 + 50) + "," + node.depth * 50 + ")"));

                    var rect =
                        new XElement(XName.Get("rect"),
                        new XAttribute(XName.Get("class"), "node"),
                        new XAttribute(XName.Get("x"), -37.5),
                        new XAttribute(XName.Get("y"), -18),
                        new XAttribute(XName.Get("rx"), 5),
                        new XAttribute(XName.Get("ry"), 5),
                        new XAttribute(XName.Get("width"), 75),
                        new XAttribute(XName.Get("height"), 36));

                    //rect.Add(text);
                    nodeClass.Add(rect);

                    sw.Write(nodeClass.ToString());
                }

                sw.Write(@"  </body>
</html>");
            }

            Process.Start("chrome", "output.html");
        }
    }
}
