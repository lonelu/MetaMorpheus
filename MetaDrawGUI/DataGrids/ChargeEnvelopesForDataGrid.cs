//
namespace MetaDrawGUI
{
    class ChargeEnvelopesForDataGrid
    {
        public ChargeEnvelopesForDataGrid(int ind, double deconMass, double mse)
        {
            Ind = ind;
            DeconMass = deconMass;
            MSE = mse;
        }

        public int Ind { get; set; }

        public double DeconMass { get; set; }

        public double MSE { get; set; }

    }
}