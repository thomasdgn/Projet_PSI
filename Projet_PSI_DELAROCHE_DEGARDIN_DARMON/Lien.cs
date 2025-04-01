using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projet_PSI_DELAROCHE_DEGARDIN_DARMON
{
    internal class Lien<T>
    {
        private Noeud<T> depart;
        private Noeud<T> arrivee;
        private int poids = 1;

        public Noeud<T> Depart { get { return depart; } }
        public Noeud<T> Arrivee { get { return arrivee; } }
        public int Poids { get { return poids; } }



        public Lien(Noeud<T> depart, Noeud<T> arrivee, int poids = 1)
        {
            this.depart = depart;
            this.arrivee = arrivee;
            this.poids = poids;
        }


    }
}
