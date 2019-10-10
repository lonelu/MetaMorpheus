//
using System.Collections.Generic;
using System.Linq;

namespace MetaDrawGUI
{
    public class ChargeEnvelopesForDataGrid
    {
        private new List<double> _deconMzs;

        public ChargeEnvelopesForDataGrid(int ind, double monoMass, double mzRatio, int isoEnveNum, double score, double itensityRatio, List<double> deconMzs)
        {
            Ind = ind;
            MzRatio = mzRatio;
            IsoEnveNum = isoEnveNum;
            Score = score;
            IntensityRatio = itensityRatio;
            _deconMzs = deconMzs;
        }

        public int Ind { get; set; }

        public double MonoMass { get; set; }

        public double MzRatio { get; set; }

        public double Score { get; set; }

        public double IsoEnveNum { get; set; }

        public double IntensityRatio { get; set; }

        public string DeconMzs
        {
            get
            {
                return string.Join(",", _deconMzs.Select(p=>p.ToString("0.000")));
            }
        }

    }
}