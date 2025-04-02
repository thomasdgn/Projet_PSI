// See https://aka.ms/new-console-template for more information
using MySql.Data.MySqlClient;
using Projet_PSI_DELAROCHE_DEGARDIN_DARMON;
using System.Windows;
using static System.Net.Mime.MediaTypeNames;
using System;

public class Program
{
    [STAThread] // WPF !
    static void Main(string[] args)
    {
        // Import données via MySQL :

        string cheminSQL = "server=localhost;user=root;password=root;database=metro;";

        Graphe<Station> graphe = new Graphe<Station>();
        ImporteurMySQL.Charger(cheminSQL, graphe);

        // Choisis deux stations sur des lignes différentes mais avec correspondance
        var depart = graphe.Noeuds.First(n => n.Valeur.Nom == "Châtelet" && n.Valeur.Ligne == "1");
        var arrivee = graphe.Noeuds.First(n => n.Valeur.Nom == "Gare de Lyon" && n.Valeur.Ligne == "14");

        var (chemin, cout) = graphe.Dijkstra(depart, arrivee);

        Console.WriteLine("Itinéraire trouvé :");
        if (chemin == null || chemin.Count == 0)
        {
            Console.WriteLine("Aucun chemin trouvé entre les deux stations.");
            Console.WriteLine("Coût total : N/A");
        }
        else
        {
            Console.WriteLine("Itinéraire trouvé :");
            foreach (var station in chemin)
            {
                Console.WriteLine(station);
            }
            Console.WriteLine($"Coût total : {cout} minutes");
        }

        /*
        Console.WriteLine("===== PLAN DU MÉTRO DE PARIS =====");

        while (true)
        {
            Console.WriteLine("\nMenu :");
            Console.WriteLine("1. Lister toutes les stations");
            Console.WriteLine("2. Rechercher une station");
            Console.WriteLine("3. Calculer un itinéraire (chemin le plus court)");
            Console.WriteLine("4. Quitter");
            Console.Write("Votre choix : ");
            var choix = Console.ReadLine();

            switch (choix)
            {
                case "1":
                    ListerStations(graphe);
                    break;
                case "2":
                    RechercherStation(graphe);
                    break;
                case "3":
                    // CalculerItineraire(grapheMetro); (Clément doit s'en occuper)
                    break;
                case "4":
                    Console.WriteLine("À bientôt !");
                    return;
                default:
                    Console.WriteLine("Choix invalide.");
                    break;
            }
        }

    }


    // Début de l'interface graphique :


    static void ListerStations(Graphe<Station> graphe)
    {
        Console.WriteLine("\n--- Liste des stations ---");
        foreach (var noeud in graphe.Noeuds)
        {
            Console.WriteLine($"- {noeud.Valeur}");
        }
    }


    static void RechercherStation(Graphe<Station> graphe)
    {
        Console.Write("\nEntrez un mot-clé pour rechercher une station : ");
        string saisie = Console.ReadLine()?.ToLower();

        var resultats = graphe.Noeuds
            .Where(n => n.Valeur.Nom.ToLower().Contains(saisie))
            .Select(n => n.Valeur)
            .ToList();

        if (resultats.Count == 0)
        {
            Console.WriteLine("Aucune station trouvée.");
        }
        else
        {
            Console.WriteLine("Résultats :");
            foreach (var station in resultats)
            {
                Console.WriteLine($"- {station}");
            }
        }

        */
    }

        
}
