namespace MetaDrawGUI
{
    public class Timepoint
    {
        public int Index;
        public double RT;
        public double Intensity;
        public int Charge;

        public Timepoint(int index, double retentionTime, double intensity, int charge)
        {
            this.Index = index;
            this.RT = retentionTime;
            this.Intensity = intensity;
            this.Charge = charge;
        }

        public override string ToString()
        {
            return "+" + Charge + "; " + Index + "; " + Intensity.ToString("F0");
        }
    }
}
