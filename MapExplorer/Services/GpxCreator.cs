using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MapExplorer.Services
{
    public class GpxCreator
    {
        public async Task<bool> CreateGpxFile(GeoCoordinateCollection coordinates)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(@"
<?xml version=""1.0"" encoding=""utf-8""?>
<gpx xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" version=""1.1"" xmlns=""http://www.topografix.com/GPX/1/1"">
  <trk>
    <trkseg>");

                foreach (var coordinate in coordinates)
                {
                    sb.AppendFormat(@"<trkpt lat=""{0}"" lon=""{1}"" />", coordinate.Latitude.ToString(new CultureInfo("en-US")), coordinate.Longitude.ToString(new CultureInfo("en-US")));
                }

                sb.AppendLine(@"</trkseg>
  </trk>
</gpx>");

                await WriteToFile(sb.ToString());
            }
            catch (Exception exception)
            {
                return false;
            }

            return true;
        }

        private async Task WriteToFile(string contents)
        {
            StorageFolder SDDevice = Windows.Storage.KnownFolders.RemovableDevices;
            //var folder = await SDDevice.GetFolderAsync("Tracks\\Autosave");
            StorageFolder sdCard = (await SDDevice.GetFoldersAsync()).FirstOrDefault();
            var name = String.Format("Autosave{0}.gpx", Guid.NewGuid().ToString());
            var file = await sdCard.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting);

            using (Stream textStream = await file.OpenStreamForWriteAsync())
            {
                byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(contents.ToCharArray());
                textStream.Write(fileBytes, 0, fileBytes.Length);
                textStream.Flush();
                //using (DataWriter textWriter = new DataWriter(textStream))
                //{
                //    textWriter.WriteString(contents);
                //    await textWriter.StoreAsync();
                //}
            }

            //byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(contents.ToCharArray());

            //using (var s = await file.OpenStreamForWriteAsync())
            //{
            //    s.Write(fileBytes, 0, fileBytes.Length);
            //    s.Flush();
            //}
        }
    }
}
