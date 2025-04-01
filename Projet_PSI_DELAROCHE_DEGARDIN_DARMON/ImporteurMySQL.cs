using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Projet_PSI_DELAROCHE_DEGARDIN_DARMON
{
    public class ImporteurMySQL
    {

        public static void Charger(string cheminSQL, Graphe<Station> graphe)
        {
            var noeuds = new Dictionary<int, Noeud<Station>>();

            using var connection = new MySqlConnection(cheminSQL);
            connection.Open();

            var cmdStations = new MySqlCommand("SELECT * FROM stations", connection);
            using var readerStations = cmdStations.ExecuteReader();

            
            while (readerStations.Read())
            {
                int id = readerStations.GetInt32("id");
                string nom = readerStations.GetString("nom");
                string ligne = readerStations.GetString("ligne");
                double lat = readerStations.GetDouble("latitude");
                double lon = readerStations.GetDouble("longitude");

                var station = new Station(id, nom, ligne, lat, lon);
                var noeud = new Noeud<Station>(station);

                graphe.AjouterNoeud(noeud);
                noeuds[id] = noeud;
            }


            readerStations.Close();


            var cmdLiaisons = new MySqlCommand("SELECT * FROM liaisons", connection);
            using var readerLiaisons = cmdLiaisons.ExecuteReader();

            while (readerLiaisons.Read())
            {
                int stationId = readerLiaisons.GetInt32("station_id");

                if (readerLiaisons["suivant"] != DBNull.Value)
                {
                    int suivant = readerLiaisons.GetInt32("suivant");
                    int temps = readerLiaisons.GetInt32("temps");
                    graphe.AjouterLien(noeuds[stationId], noeuds[suivant], temps);
                }

                if (readerLiaisons["precedent"] != DBNull.Value)
                {
                    int precedent = readerLiaisons.GetInt32("precedent");
                    int temps = readerLiaisons.GetInt32("temps");
                    graphe.AjouterLien(noeuds[precedent], noeuds[stationId], temps);
                }

                if (readerLiaisons["changement"] != DBNull.Value)
                {
                    int changement = readerLiaisons.GetInt32("changement");
                    graphe.AjouterLien(noeuds[stationId], noeuds[stationId], changement);
                }
            }

            connection.Close();
        }
    }
}
