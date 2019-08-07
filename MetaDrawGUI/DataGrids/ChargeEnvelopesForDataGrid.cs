//
namespace MetaDrawGUI
{
    public class ChargeEnvelopesForDataGrid
    {
        public ChargeEnvelopesForDataGrid(int ind, double mz, int z, double intensity)
        {
            Ind = ind;
            DeconMass = mz*z - z*1.0072;
            Mz = mz;
            Charge = z;
            Intensity = intensity;
        }

        public int Ind { get; set; }

        public double DeconMass { get; set; }

        public double Mz { get; set; }

        public int Charge { get; set; }

        public double Intensity { get; set; }

        public double MSE { get; set; }

    }
}