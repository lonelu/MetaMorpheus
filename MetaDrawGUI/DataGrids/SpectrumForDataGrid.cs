
namespace MetaDrawGUI
{
    class SpectrumForDataGrid
    {
        public SpectrumForDataGrid(int scanNum, int precursorScanNum, double precursorMz,string organism)
        {
            ScanNum = scanNum;
            PrecursorScanNum = precursorScanNum;
            PrecursorMz = precursorMz;
            Organism = organism;
        }

        public int ScanNum { get; set; }
        public int PrecursorScanNum { get; set; }
        public double PrecursorMz { get; set; }
        public string Organism { get; set; }

    }
}
