
using System.Collections.Generic;
using System.Linq;
using DirectShowLib;

namespace WpfOpenCV.Entities
{
    public static class VideoHelper
    {
        public static List<WebCamDevice> GetWebCamDevices()
        {
            var items = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice)
                .Select(
                    item => new WebCamDevice { Name = item.Name })
                .ToList();

            var items2 = DsDevice
                .GetDevicesOfCat(FilterCategory.VideoInputDevice).ToList();

            var list = items2;

            return items;
        }
    }
}
