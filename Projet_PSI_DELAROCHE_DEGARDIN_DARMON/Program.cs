// See https://aka.ms/new-console-template for more information
using MySql.Data.MySqlClient;
using Projet_PSI_DELAROCHE_DEGARDIN_DARMON;
using System.Windows;
using static System.Net.Mime.MediaTypeNames;
using System;
using System.Text;
using DocumentFormat.OpenXml.Drawing;

public class Program
{
    [STAThread] // WPF !
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8; // Pour afficher correctement les caractères spéciaux

        try
        {
            // Configuration de la connexion à la base de données
            string server = "localhost";
            string database = "metro";
            string username = "root"; // À remplacer par votre nom d'utilisateur MySQL
            string password = "root"; // À remplacer par votre mot de passe MySQL

            Graphe<Station> graphe = await Task.Run(() => ImporteurMySQL.Charger());
            Console.WriteLine(graphe.Liens.Count);

            Console.WriteLine("=== TEST DU SERVICE DE DONNÉES MÉTRO ===");

            // Créer une instance du service de données
            ImporteurMySQL metroService = new ImporteurMySQL(server, database, username, password);

            // Test de récupération de toutes les stations
            Console.WriteLine("\n1. Récupération de toutes les stations:");
            List<Station> stations = await metroService.GetAllStationsAsync();
            Console.WriteLine($"Nombre de stations récupérées: {stations.Count}");

            // Afficher quelques stations
            int stationsToShow = Math.Min(5, stations.Count);
            for (int i = 0; i < stationsToShow; i++)
            {
                Console.WriteLine($"  - {stations[i]}");
            }

            // Test de l'affichage des correspondances
            Console.WriteLine("\n2. Test d'affichage des correspondances:");
            await metroService.AfficherCorrespondancesAsync();

            // Test de l'algorithme de Bellman-Ford
            Console.WriteLine("\n3. Test de l'algorithme de Bellman-Ford:");

            // Choisir deux stations de test (ajustez selon vos données)
            string stationDepart = "République";
            string stationArrivee = "Nation";

            Console.WriteLine($"Recherche du plus court chemin de {stationDepart} à {stationArrivee}...");
            var resultBellmanFord = await metroService.BellmanFord(stationDepart, stationArrivee);

            if (resultBellmanFord.shortestTime >= 0)
            {
                Console.WriteLine($"Temps de trajet le plus court: {resultBellmanFord.shortestTime} minutes");
                Console.WriteLine("Itinéraire:");
                foreach (var station in resultBellmanFord.path)
                {
                    Console.WriteLine($"  → {station}");
                }
            }

            // Test de l'algorithme de Dijkstra
            Console.WriteLine("\n4. Test de l'algorithme de Dijkstra:");
            Console.WriteLine($"Recherche du plus court chemin de {stationDepart} à {stationArrivee}...");
            var resultDijkstra = await metroService.Dijkstra(stationDepart, stationArrivee);

            if (resultDijkstra.shortestTime >= 0)
            {
                Console.WriteLine($"Temps de trajet le plus court: {resultDijkstra.shortestTime} minutes");
                Console.WriteLine("Itinéraire:");
                foreach (var station in resultDijkstra.path)
                {
                    Console.WriteLine($"  → {station}");
                }
            }

            // Test de l'algorithme de Floyd-Warshall
            Console.WriteLine("\n5. Test de l'algorithme de Floyd-Warshall:");
            Console.WriteLine($"Recherche du plus court chemin de {stationDepart} à {stationArrivee}...");
            var resultFloydWarshall = await metroService.FloydWarshall(stationDepart, stationArrivee);

            if (resultFloydWarshall.shortestTime >= 0)
            {
                Console.WriteLine($"Temps de trajet le plus court: {resultFloydWarshall.shortestTime} minutes");
                Console.WriteLine("Itinéraire:");
                foreach (var station in resultFloydWarshall.path)
                {
                    Console.WriteLine($"  → {station}");
                }
            }

            // Comparer avec les autres algorithmes
            if (resultBellmanFord.shortestTime >= 0 && resultDijkstra.shortestTime >= 0 && resultFloydWarshall.shortestTime >= 0)
            {
                if (resultBellmanFord.shortestTime == resultDijkstra.shortestTime && resultDijkstra.shortestTime == resultFloydWarshall.shortestTime)
                {
                    Console.WriteLine("\nLes trois algorithmes ont trouvé le même temps de trajet optimal!");
                }
                else
                {
                    Console.WriteLine("\nAttention: Les algorithmes ont trouvé des temps différents.");
                    Console.WriteLine($"Bellman-Ford: {resultBellmanFord.shortestTime} minutes");
                    Console.WriteLine($"Dijkstra: {resultDijkstra.shortestTime} minutes");
                    Console.WriteLine($"Floyd-Warshall: {resultFloydWarshall.shortestTime} minutes");
                }
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
