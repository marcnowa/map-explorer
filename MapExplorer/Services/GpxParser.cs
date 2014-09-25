using Microsoft.Phone.Maps.Controls;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Data.Xml.Dom;

namespace MapExplorer.Services
{
    public class GpxParser
    {
        public async Task<List<GeoCoordinateCollection>> GetCoordinates(string path)
        {
            var result = new List<GeoCoordinateCollection>();

            using (var s = new System.IO.FileStream(path, FileMode.Open))
            {
                var sr = new StreamReader(s);
                var contents = await sr.ReadToEndAsync();

                var document = new XmlDocument();
                document.LoadXml(contents, new XmlLoadSettings { ResolveExternals = true });


                var segments = document.GetElementsByTagName("trkseg");
                foreach (var segment in segments)
                {
                    var track = new GeoCoordinateCollection();
                    var nodes = segment.ChildNodes.Where(n => n.NodeName == "trkpt");
                    foreach (var node in nodes)
                    {
                        var latAttribute = node.Attributes.FirstOrDefault(a => a.NodeName == "lat");
                        var lonAttribute = node.Attributes.FirstOrDefault(a => a.NodeName == "lon");

                        if (latAttribute != null && lonAttribute != null)
                        {
                            double lat = XmlConvert.ToDouble(latAttribute.NodeValue.ToString());
                            double lon = XmlConvert.ToDouble(lonAttribute.NodeValue.ToString());

                            track.Add(new GeoCoordinate(lat, lon));
                        }
                    }
                    result.Add(track);
                }
            }

            return result;
        }
    }
}
