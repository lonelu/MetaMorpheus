namespace MetaDrawGUI
{
    public class EnvolopForDataGrid
    {
        public EnvolopForDataGrid(int ind, bool isNeuCode, double mz, int charge, double deconMass, double intensity)
        {
            Ind = ind;
            IsNeuCode = isNeuCode;
            Mz = mz;
            Charge = charge;
            DeconMass = deconMass;         
            Intensity = intensity;
            
        }

        public int Ind { get; set; }

        public bool IsNeuCode { get; set; }

        public double Mz { get; set; }

        public int Charge { get; set; }

        public double DeconMass { get; set; }

        public double Intensity { get; set; }

        
    }
}
