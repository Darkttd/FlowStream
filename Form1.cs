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
                using (StreamReader sr = new StreamReader("sample_data.json"))
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
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

            soulStream.MakeFlow();

            using (StreamWriter sw = new StreamWriter("output.html"))
            {
                sw.Write(@"<html>
  <head>
    <meta charset=""utf-8"">
    <title>FlowStream</title>
    <link href=""./vnv.css"" rel=""stylesheet"">
  </head>
  <body>
  <svg width=""10000"" height=""10000"">");

                Func<SoulStream.Node, int> GetXPos = node => node.xPos * 200 + 50;
                Func<SoulStream.Node, int> GetYPos = node => node.depth * 140;

                var arrowHeadDefs = new XElement(XName.Get("defs"));

                var arrowHeadMarker = new XElement(XName.Get("marker"),
                    new XAttribute(XName.Get("id"), "commonArrowHead"),
                    new XAttribute(XName.Get("viewBox"), "0 0 10 10"),
                    new XAttribute(XName.Get("refX"), "9"),
                    new XAttribute(XName.Get("refY"), "5"),
                    new XAttribute(XName.Get("markerUnits"), "strokeWidth"),
                    new XAttribute(XName.Get("markerWidth"), "8"),
                    new XAttribute(XName.Get("markerHeight"), "6"),
                    new XAttribute(XName.Get("orient"), "auto"));

                var arrowHeadPath = new XElement(XName.Get("path"),
                    new XAttribute(XName.Get("d"), "M 0 0 L 10 5 L 0 10 z"),
                    new XAttribute(XName.Get("style"), "stroke-width: 1; stroke-dasharray: 1, 0;"));

                arrowHeadMarker.Add(arrowHeadPath);
                arrowHeadDefs.Add(arrowHeadMarker);
                sw.Write(arrowHeadDefs.ToString());

                foreach (SoulStream.Node node in soulStream.GetNodes())
                {
                    // node 그리기

                    if (node.Name == SoulStream.RootNodeName)
                        // 단, 루트는 숨깁니다.
                        continue;

                    if (!node.Name.StartsWith(SoulStream.HiddenNodePrefix))
                    {
                        // 히든노드는 그리지 않습니다.

                        var nodeClass = new XElement(XName.Get("g"),
                            new XAttribute(XName.Get("class"), "node"),
                            new XAttribute(XName.Get("style"), "opacity: 1;"),
                            new XAttribute(XName.Get("transform"), "translate(" + GetXPos(node) + "," + GetYPos(node) + ")"));

                        var rect =
                            new XElement(XName.Get("rect"),
                            new XAttribute(XName.Get("class"), "node"),
                            new XAttribute(XName.Get("x"), -37.5),
                            new XAttribute(XName.Get("y"), -18),
                            new XAttribute(XName.Get("rx"), 5),
                            new XAttribute(XName.Get("ry"), 5),
                            new XAttribute(XName.Get("width"), 75),
                            new XAttribute(XName.Get("height"), 36));

                        var label =
                            new XElement(XName.Get("g"),
                            new XAttribute(XName.Get("class"), "label"),
                            new XAttribute(XName.Get("transform"), "translate(0,0)"));

                        var text =
                            new XElement(XName.Get("text"));

                        var tspan =
                            new XElement(XName.Get("tspan"),
                            new XAttribute(XName.Get("space"), "preserve"),
                            new XAttribute(XName.Get("dy"), "lem"),
                            new XAttribute(XName.Get("x"), "1"));

                        tspan.Add(node.Name);
                        text.Add(tspan);
                        label.Add(text);
                        nodeClass.Add(rect);
                        nodeClass.Add(label);

                        sw.Write(nodeClass.ToString());
                    }

                    foreach (SoulStream.Node toNode in node.to)
                    {
                        // node 에서 toNode 로 가는 화살표 그리기

                        var edgePathClass = new XElement(XName.Get("g"),
                            new XAttribute(XName.Get("class"), "edgePath"),
                            new XAttribute(XName.Get("style"), "opacity: 1;"));

                        int yPosFix = -18;

                        bool isHidden = toNode.Name.StartsWith(SoulStream.HiddenNodePrefix);
                        if (isHidden)
                            yPosFix = -yPosFix;

                        var path = new XElement(XName.Get("path"),
                            new XAttribute(XName.Get("class"), "path"),
                            new XAttribute(XName.Get("d"), "M" + GetXPos(node) + "," + (GetYPos(node) + 18) + "L" + GetXPos(toNode) + "," + (GetYPos(toNode) + yPosFix)),
                            new XAttribute(XName.Get("marker-end"), isHidden ? "" : "url(#commonArrowHead)"),
                            new XAttribute(XName.Get("style"), "fill: none;"));

                        edgePathClass.Add(path);

                        sw.Write(edgePathClass.ToString());
                    }
                }

                sw.Write(@"  </body>
</html>");
            }

            Process.Start("chrome", Directory.GetCurrentDirectory() + "/output.html");
        }
    }
}
