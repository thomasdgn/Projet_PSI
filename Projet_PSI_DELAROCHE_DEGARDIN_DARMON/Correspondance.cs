using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace Projet_PSI_DELAROCHE_DEGARDIN_DARMON
{
    public class Correspondance
    {
        private string station;
        private string ligne1;
        private string ligne2;
        private int tempsCorrespondance;

        public string Station { get { return station; } }
        public string Ligne1 { get { return ligne1; } }
        public string Ligne2 { get { return ligne2; } }
        public int TempsCorrespondance { get { return tempsCorrespondance; } }


        public Correspondance() { }
        public Correspondance(string station, string ligne1, string ligne2, int temps)
        {
            this.station = station;
            this.ligne1 = ligne1;
            this.ligne2 = ligne2;
            this.tempsCorrespondance = temps;
        }

    }
}
