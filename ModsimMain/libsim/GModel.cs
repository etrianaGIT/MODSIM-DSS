namespace Csu.Modsim.ModsimModel
{
    /// <summary>The GModel class contains all graphic information for a network.</summary>
    public class GModel
    {
        public GModel()
        {
            nonstorageSize = 0;
            demandSize = 0;
            reservoirSize = 0;
            linkSize = 0;
            imageLocation = "";
            imageSize = 0.0;
        }
        public int nonstorageSize;
        public int demandSize;
        public int reservoirSize;
        public int linkSize;
        public string imageLocation;
        public double imageSize;
    }
}
