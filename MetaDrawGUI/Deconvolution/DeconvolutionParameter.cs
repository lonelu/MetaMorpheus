using MzLibUtil;

namespace MetaDrawGUI
{
    public class DeconvolutionParameter
    {
        private double _deconvolutionMassTolerance = 0;

        public DeconvolutionParameter()
        {
            DeconvolutionMinAssumedChargeState = 2;
            DeconvolutionMaxAssumedChargeState = 6;
            DeconvolutionMassTolerance = 4;
            DeconvolutionIntensityRatio = 3;

            NeuCodeMassDefect = 18;
            MaxmiumNeuCodeNumber = 3;
            NeuCodePairRatio = 1;
        }

        public double DeconvolutionIntensityRatio { get; set; }
        public int DeconvolutionMinAssumedChargeState { get; set; }
        public int DeconvolutionMaxAssumedChargeState { get; set; }
        public double DeconvolutionMassTolerance
        {
            get
            {
                return _deconvolutionMassTolerance;
            }
            set
            {
                _deconvolutionMassTolerance = value;
            }
        }


        public double NeuCodeMassDefect { get; set; }
        public int MaxmiumNeuCodeNumber { get; set; }
        public double NeuCodePairRatio { get; set; }

        public Tolerance DeconvolutionAcceptor
        {
            get
            {
                return new PpmTolerance(_deconvolutionMassTolerance);
            }
        }
    }
}
