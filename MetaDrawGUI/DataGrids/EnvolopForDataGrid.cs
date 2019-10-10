namespace MetaDrawGUI
{
    public class EnvolopForDataGrid
    {
        public EnvolopForDataGrid(int ind, bool hasPartner, double mz, int charge, double deconMass, double intensity, double intensityRatio, double score, double significance)
        {
            Ind = ind;
            HasPartner = hasPartner;
            Mz = mz;
            Charge = charge;
            DeconMass = deconMass;         
            Intensity = intensity;
            IntensityRatio = intensityRatio;
            Score = score;
            Significance = significance;
        }

        public int Ind { get; set; }

        public bool HasPartner { get; set; }

        public double Mz { get; set; }

        public int Charge { get; set; }

        public double DeconMass { get; set; }

        public double Intensity { get; set; }

        public double IntensityRatio { get; set; }

        public double Score { get; set; }

        public double Significance { get; set; }
    }
}
