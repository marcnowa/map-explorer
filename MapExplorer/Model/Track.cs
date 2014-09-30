using Microsoft.Phone.Maps.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapExplorer.Model
{
    class Track
    {
        public string Name { get; set; }
        public List<MapPolyline> Segments { get; set; }

        public Track()
        {
            Segments = new List<MapPolyline>();
        }
    }
}
