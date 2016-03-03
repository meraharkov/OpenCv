using OpenCvSharp;

namespace WpfOpenCV.Entities
{
    public class VideoFrameContainer
    {
        public string Name { get; set; }
        public IplImage Frame { get; set; }
    }
}
