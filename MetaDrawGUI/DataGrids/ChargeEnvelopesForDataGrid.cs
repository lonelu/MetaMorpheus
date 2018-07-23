//
namespace MetaDrawGUI
{
    class ChargeEnvelopesForDataGrid
    {
        public ChargeEnvelopesForDataGrid(int ind, double deconMass)
        {
            Ind = ind;
            DeconMass = deconMass;
        }

        public int Ind { get; set; }

        public double DeconMass { get; set; }

    }
}