using System.Collections.Generic;

namespace MetaDrawGUI
{
    public class ChromatographicPeak
    {
        public List<Timepoint> Timepoints;
        public string File;
        public string Sequence;
        public double Ms2Rt;
        public int PrecursorZ;
        public double Mass;

        public ChromatographicPeak(string str)
        {
            Timepoints = new List<Timepoint>();
            var split = str.Split('\t');
            File = split[0];
            Sequence = split[2];

            if (double.TryParse(split[5], out Ms2Rt))
            {

            }
            else
            {
                Ms2Rt = double.NaN;
            }
            
            PrecursorZ = int.Parse(split[6]);
            Mass = double.Parse(split[4]);
            
            string timepointsString = split[21].Trim('"');

            if (string.IsNullOrEmpty(timepointsString))
            {
                return;
            }

            var splitTimepoints = timepointsString.Split(',');
            foreach (string timepointString in splitTimepoints)
            {
                var timepointSplit = timepointString.Split('|');
                var charge = int.Parse(timepointSplit[0].Trim('+'));
                double intensity = double.Parse(timepointSplit[1]);
                double rt = double.Parse(timepointSplit[2]);
                int scanIndex = int.Parse(timepointSplit[3]);

                Timepoints.Add(new Timepoint(scanIndex, rt, intensity, charge));
            }
        }

        public override string ToString()
        {
            return Timepoints.Count + "; " + File + "; " + Sequence;
        }
    }
}
