namespace MetaDrawGUI
{
    public class EnvolopForDataGrid
    {
        public EnvolopForDataGrid(int i, double deconMass, int charge, double intensity)
        {
            I = i;
            DeconMass = deconMass;
            Charge = charge;
            Intensity = intensity;
        }

        public int I { get; set; }

        public double DeconMass { get; set; }

        public int Charge { get; set; }

        public double Intensity { get; set; }
    }
}
