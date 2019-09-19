//
using System.Collections.Generic;
using System.Linq;

namespace MetaDrawGUI
{
    public class ChargeEnvelopesForDataGrid
    {
        private new List<double> _deconMasses;

        public ChargeEnvelopesForDataGrid(int ind, double deconMz, double intensity, double mzRatio, int isoEnveNum, double score, List<double> deconMasses)
        {
            Ind = ind;
            DeconMz = deconMz;
            Intensity = intensity;
            MzRatio = mzRatio;
            IsoEnveNum = isoEnveNum;
            Score = score;
            _deconMasses = deconMasses;
        }

        public int Ind { get; set; }

        public double DeconMz { get; set; }

        public double Intensity { get; set; }

        public double MzRatio { get; set; }

        public double Score { get; set; }

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