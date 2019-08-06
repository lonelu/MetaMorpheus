﻿using MzLibUtil;

namespace MetaDrawGUI
{
    public class DeconvolutionParameter
    {
        public Tolerance DeconvolutionAcceptor = new PpmTolerance(5);

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
        public double DeconvolutionMassTolerance { get; set; }


        public double NeuCodeMassDefect { get; set; }
        public int MaxmiumNeuCodeNumber { get; set; }
        public double NeuCodePairRatio { get; set; }
    }
}
