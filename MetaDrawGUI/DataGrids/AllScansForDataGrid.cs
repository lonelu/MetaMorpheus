
namespace MetaDrawGUI
{
    public class AllScansForDataGrid
    {
        public AllScansForDataGrid(int scanNum, int? precursorScanNum, int msOrder, double? isolationMass)
        {
            ScanNum = scanNum;
            PrecursorScanNum = precursorScanNum;
            IsolationMass = isolationMass;
            MsOrder = msOrder;
        }

        public int ScanNum { get; set; }

        public int? PrecursorScanNum { get; set; }

        public int MsOrder { get; set; }

        public double? IsolationMass { get; set; }
    }
}
