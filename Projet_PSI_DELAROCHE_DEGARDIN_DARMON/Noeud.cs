using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projet_PSI_DELAROCHE_DEGARDIN_DARMON
{
    public class Noeud<T>
    {
        private T valeur;
        public T Valeur { get { return valeur; } }

        public Noeud(T valeur)
        {
            this.valeur = valeur;
        }

        public override string ToString()
        {
            return Valeur?.ToString() ?? "null";
        }

        public override bool Equals(object? obj)
        {
            return obj is Noeud<T> autre && EqualityComparer<T>.Default.Equals(Valeur, autre.Valeur);
        }

        public override int GetHashCode()
        {
            return Valeur != null ? EqualityComparer<T>.Default.GetHashCode(Valeur) : 0;
        }
    }
}
