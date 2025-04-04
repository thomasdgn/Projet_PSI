using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;


namespace Projet_PSI_DELAROCHE_DEGARDIN_DARMON
{
    public class Graphe<T>
    {
        private List<Noeud<T>> noeuds = new List<Noeud<T>>();
        private List<Lien<T>> liens = new List<Lien<T>>();
        private Dictionary<Noeud<T>, List<Noeud<T>>> listeAdjacence = new();
        private Dictionary<(Noeud<T>, Noeud<T>), int> matriceAdjacence = new();

        public List<Noeud<T>> Noeuds { get { return noeuds; } }
        public List<Lien<T>> Liens { get { return liens; } }
        public Dictionary<Noeud<T>, List<Noeud<T>>> ListeAdjacence { get { return listeAdjacence; } }

        public Dictionary<(Noeud<T>, Noeud<T>), int> MatriceAdjacence { get { return matriceAdjacence; } }



        public void AjouterNoeud(Noeud<T> n)
        {
            if (!Noeuds.Contains(n))
            {
                Noeuds.Add(n);
                ListeAdjacence[n] = new List<Noeud<T>>();
            }
        }


        public void AjouterLien(Noeud<T> depart, Noeud<T> arrivee, int poids = 1)
        {
            Liens.Add(new Lien<T>(depart, arrivee, poids));
            ListeAdjacence[depart].Add(arrivee);
            ListeAdjacence[arrivee].Add(depart); //utile si graphe non orienté
            MatriceAdjacence[(depart, arrivee)] = poids;
            MatriceAdjacence[(arrivee, depart)] = poids;
        }


        public void AfficherListeAdjacence()
        {
            foreach (var pair in ListeAdjacence)
            {
                Console.Write(pair.Key + ": ");
                foreach (var voisin in pair.Value)
                {
                    Console.Write(voisin + " ");
                }
                Console.WriteLine();
            }
        }



        public void AfficherMatriceAdjacence()
        {
            Console.WriteLine("Matrice d'adjacence :");

            // En-tête
            Console.Write("     ");
            foreach (var col in Noeuds)
            {
                Console.Write($"{col,6} ");
            }
            Console.WriteLine();

            foreach (var ligne in Noeuds)
            {
                Console.Write($"{ligne,5} ");
                foreach (var col in Noeuds)
                {
                    int poids = 0;
                    if (MatriceAdjacence.ContainsKey((ligne, col)))
                    {
                        poids = MatriceAdjacence[(ligne, col)];
                    }
                    Console.Write($"{poids,6} ");
                }
                Console.WriteLine();
            }
        }



        // Algorithme BFS :

        public void BFS(Noeud<T> depart)
        {
            HashSet<Noeud<T>> visites = new HashSet<Noeud<T>>();

            Queue<Noeud<T>> file = new Queue<Noeud<T>>();

            visites.Add(depart);
            file.Enqueue(depart);

            Console.WriteLine("Algorithme BFS :");

            while (file.Count > 0)
            {
                Noeud<T> a = file.Dequeue();
                Console.WriteLine("Visite de : " + a);


                foreach (var voisin in ListeAdjacence[a])
                {
                    if (!visites.Contains(voisin))
                    {
                        visites.Add(voisin);
                        file.Enqueue(voisin);
                    }
                }
            }
        }



        // Algorithme DFS :

        public void DFS(Noeud<T> depart)
        {
            HashSet<Noeud<T>> visites = new HashSet<Noeud<T>>();

            Console.WriteLine("Algorithme DFS :");
            DFSRecursive(depart, visites);
        }



        private void DFSRecursive(Noeud<T> a, HashSet<Noeud<T>> visites)
        {
            visites.Add(a);
            Console.WriteLine("Visite de : " + a);

            foreach (var voisin in ListeAdjacence[a])
            {
                if (!visites.Contains(voisin))
                {
                    DFSRecursive(voisin, visites);
                }
            }
        }



        // Test de connexité :

        public bool EstConnexe()
        {
            bool res = false;
            if (Noeuds.Count == 0)
            {
                res = true;
            }

            var visites = new HashSet<Noeud<T>>();
            var depart = Noeuds[0];

            ExplorerDFS(depart, visites);

            if (visites.Count == Noeuds.Count)
            {
                res = true;
            }

            return res;
        }


        private void ExplorerDFS(Noeud<T> a, HashSet<Noeud<T>> visites)
        {
            visites.Add(a);

            foreach (var voisin in ListeAdjacence[a])
            {
                if (visites.Contains(voisin) == false)
                {
                    ExplorerDFS(voisin, visites);
                }
            }
        }



        // Test détection de cycles :

        public bool ContientCycle()
        {
            bool res = false;
            var visites = new HashSet<Noeud<T>>();

            foreach (var noeud in Noeuds)
            {
                if (visites.Contains(noeud) == false)
                {
                    if (DetecterCycleDFS(noeud, visites, null) == true)
                    {
                        res = true;
                    }
                }
            }

            return res;
        }


        private bool DetecterCycleDFS(Noeud<T> a, HashSet<Noeud<T>> visites, Noeud<T> pred)
        {
            bool res = false;
            visites.Add(a);

            foreach (var voisin in ListeAdjacence[a])
            {
                if (visites.Contains(voisin) == false)
                {
                    if (DetecterCycleDFS(voisin, visites, a) == true)
                    {
                        res = true;
                    }
                }
                else if (voisin.Equals(pred) == false)
                {
                    res = true;
                }
            }

            return res;
        }


        public void AfficherCorrespondances()
        {
            Console.WriteLine("===== CORRESPONDANCES DANS LE RÉSEAU DE MÉTRO =====");

            // Utiliser un ensemble pour éviter les duplicatas
            var correspondancesAffichees = new HashSet<(string, string, string, int)>();

            foreach (var noeud in Noeuds)
            {
                var station = noeud.Valeur as Station;
                if (station != null)
                {
                    var correspondances = ListeAdjacence[noeud]
                        .Where(adjacent => (adjacent.Valeur as Station).Ligne != station.Ligne)
                        .Select(adjacent => new
                        {
                            Station = (adjacent.Valeur as Station).Nom,
                            Ligne = (adjacent.Valeur as Station).Ligne,
                            Temps = MatriceAdjacence[(noeud, adjacent)]
                        })
                        .ToList();

                    foreach (var correspondance in correspondances)
                    {
                        var clef = (station.Nom, correspondance.Station, station.Ligne, correspondance.Temps);
                        var clefInverse = (correspondance.Station, station.Nom, correspondance.Ligne, correspondance.Temps);

                        // Vérifier si la correspondance ou son inverse a déjà été affichée
                        if (!correspondancesAffichees.Contains(clef) && !correspondancesAffichees.Contains(clefInverse))
                        {
                            Console.WriteLine($"Station: {station.Nom} ({station.Ligne})");
                            Console.WriteLine($"  -> Correspondance avec {correspondance.Station} ({correspondance.Ligne}) en {correspondance.Temps} minutes");
                            correspondancesAffichees.Add(clef);
                        }
                    }
                }
            }
        }  
    }
}
