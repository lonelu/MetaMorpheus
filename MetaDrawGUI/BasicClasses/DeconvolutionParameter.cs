using MzLibUtil;

namespace MetaDrawGUI
{
    public class DeconvolutionParameter
    {
        public DeconvolutionParameter()
        {
            DeconvolutionMinAssumedChargeState = 2;
            DeconvolutionMaxAssumedChargeState = 6;
            DeconvolutionMassTolerance = new PpmTolerance(5);
            DeconvolutionIntensityRatio = 3;
            IsNeuCode = false;
        }

        public double DeconvolutionIntensityRatio { get;  set; }
        public int DeconvolutionMinAssumedChargeState { get;  set; }
        public int DeconvolutionMaxAssumedChargeState { get;  set; }
        public Tolerance DeconvolutionMassTolerance { get;  set; }
        public bool IsNeuCode { get;  set; }

    }
}
