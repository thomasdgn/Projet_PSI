using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projet_PSI_DELAROCHE_DEGARDIN_DARMON
{
    internal class Station
    {
        private int id;
        private string nom;
        private string ligne;
        private double latitude;
        private double longitude;


        public int Id { get { return id; } }
        public string Nom { get { return nom; } }
        public string Ligne { get { return ligne; } }
        public double Latitude { get { return latitude; } }
        public double Longitude { get { return longitude; } }


        public Station(int id, string nom, string ligne, double latitude, double longitude)
        {
            this.id = id;
            this.nom = nom;
            this.ligne = ligne;
            this.latitude = latitude;
            this.longitude = longitude;
        }

        public override string ToString()
        {
            return $"{Nom} ({Ligne})";
        }

        public override bool Equals(object obj)
        {
            return obj is Station other &&
                   Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

    }
}
