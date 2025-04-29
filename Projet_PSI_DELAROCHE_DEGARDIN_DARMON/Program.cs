// See https://aka.ms/new-console-template for more information
using MySql.Data.MySqlClient;
using Projet_PSI_DELAROCHE_DEGARDIN_DARMON;
using System.Text;

public class Program
{
    [STAThread] // WPF !
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8; // Pour afficher correctement les caractères spéciaux

        try
        {
            // Configuration de la connexion à la base de données
            string server = "localhost";
            string database = "metro";
            string username = "root";
            string password = "root";

            string connectionString = $"Server={server};Database={database};User ID={username};Password={password};";

            // Charger le graphe directement en synchrone
            Graphe<Station> graphe = ImporteurMySQL.Charger();
            Console.WriteLine($"Nombre de liens dans le graphe : {graphe.Liens.Count}");

            Console.WriteLine("=== TEST DU SERVICE DE DONNÉES MÉTRO ===");

            // Créer une instance du service
            ImporteurMySQL metroService = new ImporteurMySQL(server, database, username, password);

            // Test de récupération de toutes les stations
            Console.WriteLine("\n1. Récupération de toutes les stations:");
            List<Station> stations = metroService.GetAllStations();
            Console.WriteLine($"Nombre de stations récupérées: {stations.Count}");
            foreach (var station in stations)
            {
                Console.WriteLine($"Station importée : {station.Nom} ({station.Ligne}) - {station.Latitude},{station.Longitude}");
            }


            // Afficher quelques stations
            int stationsToShow = Math.Min(5, stations.Count);
            for (int i = 0; i < stationsToShow; i++)
            {
                Console.WriteLine($"  - {stations[i]}");
            }

            // Test de l'affichage des correspondances
            Console.WriteLine("\n2. Test d'affichage des correspondances:");
            metroService.AfficherCorrespondances();

            // Test des algorithmes de trajets
            Console.WriteLine("\n3. Test des algorithmes de trajets:");

            string stationDepart = "République";
            string stationArrivee = "Nation";

            Console.WriteLine($"\nBellman-Ford de {stationDepart} à {stationArrivee}:");
            var resultBellmanFord = metroService.BellmanFord(stationDepart, stationArrivee);
            if (resultBellmanFord.shortestTime >= 0)
            {
                Console.WriteLine($"Temps: {resultBellmanFord.shortestTime} min");
                foreach (var station in resultBellmanFord.path)
                    Console.WriteLine($"  → {station}");
            }

            Console.WriteLine($"\nDijkstra de {stationDepart} à {stationArrivee}:");
            var resultDijkstra = metroService.Dijkstra(stationDepart, stationArrivee);
            if (resultDijkstra.shortestTime >= 0)
            {
                Console.WriteLine($"Temps: {resultDijkstra.shortestTime} min");
                foreach (var station in resultDijkstra.path)
                    Console.WriteLine($"  → {station}");
            }

            Console.WriteLine($"\nFloyd-Warshall de {stationDepart} à {stationArrivee}:");
            var resultFloydWarshall = metroService.FloydWarshall(stationDepart, stationArrivee);
            if (resultFloydWarshall.shortestTime >= 0)
            {
                Console.WriteLine($"Temps: {resultFloydWarshall.shortestTime} min");
                foreach (var station in resultFloydWarshall.path)
                    Console.WriteLine($"  → {station}");
            }

            // Comparaison finale
            if (resultBellmanFord.shortestTime == resultDijkstra.shortestTime &&
                resultDijkstra.shortestTime == resultFloydWarshall.shortestTime)
            {
                Console.WriteLine("\nTous les algorithmes donnent le même résultat !");
            }
            else
            {
                Console.WriteLine("\nAttention, résultats différents selon l'algorithme !");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERREUR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine("\nAppuyez sur une touche pour quitter...");
        Console.ReadKey();
    }
}
