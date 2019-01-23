
namespace MetaDrawGUI
{
    public class AllScansForDataGrid
    {
        public AllScansForDataGrid(int scanNum, int? precursorScanNum, double rt, int msOrder, double? isolationMass)
        {
            ScanNum = scanNum;
            PrecursorScanNum = precursorScanNum;
            RT = rt;
            IsolationMass = isolationMass;
            MsOrder = msOrder;
        }

        public int ScanNum { get; set; }

        public int? PrecursorScanNum { get; set; }

        public double RT { get; set; }

        public int MsOrder { get; set; }

        public double? IsolationMass { get; set; }
    }
}
