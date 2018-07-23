namespace MetaDrawGUI
{
    public class EnvolopForDataGrid
    {
        public EnvolopForDataGrid(int ind, double deconMass, int charge, double intensity)
        {
            Ind = ind;
            DeconMass = deconMass;
            Charge = charge;
            Intensity = intensity;
        }

        public int Ind { get; set; }

        public double DeconMass { get; set; }

        public int Charge { get; set; }

        public double Intensity { get; set; }
    }
}
