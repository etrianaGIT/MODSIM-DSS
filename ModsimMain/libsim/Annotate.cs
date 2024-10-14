namespace Csu.Modsim.ModsimModel
{

    /// <summary>Class for annotations that appear on the interface canvas</summary>
    public class Annotate
    {
        /// <summary>Text that appears on the interface canvas</summary>
        public string Text;
        /// <summary>X coordinates at the lower left hand corner of the text.</summary>
        public int x;
        /// <summary>Y coordinates at the lower left hand corner of the text.</summary>
        public int y;
        /// <summmary> ID for this annotation for internal processing</summary>
        public int ID;
        /// <summary>Constructor</summary>
        public Annotate()
        {
            x = 0;
            y = 0;
            Text = "";
        }
    }
}