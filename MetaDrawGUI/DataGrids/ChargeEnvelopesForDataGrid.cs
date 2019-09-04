//
using System.Collections.Generic;
using System.Linq;

namespace MetaDrawGUI
{
    public class ChargeEnvelopesForDataGrid
    {
        private new List<double> _deconMasses;

        public ChargeEnvelopesForDataGrid(int ind, double deconMz, double intensity, double intensityRatio, double mzRatio, int isoEnveNum, List<double> deconMasses)
        {
            Ind = ind;
            DeconMz = deconMz;
            Intensity = intensity;
            IntensityRation = intensityRatio;
            MzRatio = mzRatio;
            IsoEnveNum = isoEnveNum;
            _deconMasses = deconMasses;
        }

        public int Ind { get; set; }

        public double DeconMz { get; set; }

        public double Intensity { get; set; }

        public double IntensityRation { get; set; }

        public double MzRatio { get; set; }

        public double IsoEnveNum { get; set; }

        public string DeconMasses
        {
            get
            {
                return string.Join(",", _deconMasses.Take(3));
            }
        }

    }
}