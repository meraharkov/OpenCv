namespace WpfOpenCV.Entities
{
    public class WebCameraInfo
    {
        public int IndexWebCamera { get; set; }

        public string DeviceName { get; set; }

        public AdditionalCameraInfo AdditionalCameraInfo { get; set; }
      
    }

    public class AdditionalCameraInfo
    {
          public string CameraIndex { get; set; }

          public int NumberColumnInGrid { get; set; }

          public int NumberRowInGrid { get; set; }
    }
}
