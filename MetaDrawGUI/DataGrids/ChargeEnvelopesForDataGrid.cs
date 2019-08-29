//
namespace MetaDrawGUI
{
    public class ChargeEnvelopesForDataGrid
    {
        public ChargeEnvelopesForDataGrid(int ind, double deconMass, double intensity, double mse)
        {
            Ind = ind;
            DeconMass = deconMass;
            Intensity = intensity;
            MSE = mse;
        }

        public int Ind { get; set; }

        public double DeconMass { get; set; }

        public double Intensity { get; set; }

        public double MSE { get; set; }

    }
}