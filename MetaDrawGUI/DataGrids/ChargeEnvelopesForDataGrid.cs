//
using System.Collections.Generic;
using System.Linq;

namespace MetaDrawGUI
{
    public class ChargeEnvelopesForDataGrid
    {
        private new List<double> _deconMasses;

        public ChargeEnvelopesForDataGrid(int ind, double monoMass, double mzRatio, int isoEnveNum, double score, List<double> deconMasses)
        {
            Ind = ind;
            MzRatio = mzRatio;
            IsoEnveNum = isoEnveNum;
            Score = score;
            _deconMasses = deconMasses;
        }

        public int Ind { get; set; }

        public double MonoMass { get; set; }


        public double MzRatio { get; set; }

        public double Score { get; set; }

        public double IsoEnveNum { get; set; }

        public string DeconMasses
        {
            get
            {
                return string.Join(",", _deconMasses.Select(p=>p.ToString("0.000")));
            }
        }

    }
}