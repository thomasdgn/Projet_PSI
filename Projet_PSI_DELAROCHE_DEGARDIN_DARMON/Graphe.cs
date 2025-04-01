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



        // Algorithme de Bellman-Ford

        // Représente une arête orientée avec un poids
        public readonly record struct Edge(int From, int To, int Weight);

        public static class ExcelGraphLoader
        {
            public static (List<Edge> edges, Dictionary<string, int> nameToId, Dictionary<int, string> idToName)
            LoadEdgesWithStationNames(string filePath, string sheetName = "Arcs")
            {
                var edges = new List<Edge>();
                var nameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var idToName = new Dictionary<int, string>();
                int currentId = 0;

                using var workbook = new XLWorkbook(filePath);
                var sheet = workbook.Worksheet(sheetName);
                var rows = sheet.RangeUsed().RowsUsed().Skip(1); // Ignore l’en-tête

                foreach (var row in rows)
                {
                    string fromStation = row.Cell(3).GetString().Trim();
                    string toStation = row.Cell(4).GetString().Trim();
                    int weight = (int)row.Cell(5).GetDouble();

                    if (!nameToId.ContainsKey(fromStation))
                    {
                        nameToId[fromStation] = currentId;
                        idToName[currentId] = fromStation;
                        currentId++;
                    }

                    if (!nameToId.ContainsKey(toStation))
                    {
                        nameToId[toStation] = currentId;
                        idToName[currentId] = toStation;
                        currentId++;
                    }

                    edges.Add(new Edge(nameToId[fromStation], nameToId[toStation], weight));
                }

                return (edges, nameToId, idToName);
            }
        }

        public static class BellmanFord
        {
            public static bool ComputeShortestPaths(int nodeCount, List<Edge> edges, int source, out int[] distances)
            {
                distances = Enumerable.Repeat(int.MaxValue, nodeCount).ToArray();
                distances[source] = 0;

                for (int i = 0; i < nodeCount - 1; i++)
                {
                    bool updated = false;

                    foreach (var (from, to, weight) in edges)
                    {
                        if (distances[from] != int.MaxValue && distances[from] + weight < distances[to])
                        {
                            distances[to] = distances[from] + weight;
                            updated = true;
                        }
                    }

                    if (!updated) break;
                }

                foreach (var (from, to, weight) in edges)
                {
                    if (distances[from] != int.MaxValue && distances[from] + weight < distances[to])
                        return false; // Cycle négatif détecté
                }

                return true;
            }


        }


        public List<Noeud<T>> ObtenirVoisins(Noeud<T> noeud)
        {
            if (ListeAdjacence.ContainsKey(noeud))
                return ListeAdjacence[noeud];
            else
                return new List<Noeud<T>>();
        }


        public (List<Noeud<T>> chemin, int cout) Dijkstra(Noeud<T> depart, Noeud<T> arrivee)
        {
            var distances = new Dictionary<Noeud<T>, int>();
            var precedent = new Dictionary<Noeud<T>, Noeud<T>?>();
            var file = new PriorityQueue<Noeud<T>, int>();

            foreach (var noeud in Noeuds)
            {
                distances[noeud] = int.MaxValue;
                precedent[noeud] = null;
            }

            distances[depart] = 0;
            file.Enqueue(depart, 0);

            while (file.Count > 0)
            {
                var courant = file.Dequeue();

                if (courant.Equals(arrivee))
                    break;

                foreach (var voisin in ObtenirVoisins(courant))
                {
                    var lien = Liens.FirstOrDefault(l => l.Depart.Equals(courant) && l.Arrivee.Equals(voisin));
                    if (lien == null) continue;

                    int nouveauCout = distances[courant] + lien.Poids;

                    if (nouveauCout < distances[voisin])
                    {
                        distances[voisin] = nouveauCout;
                        precedent[voisin] = courant;
                        file.Enqueue(voisin, nouveauCout);
                    }
                }
            }

            // Reconstruire le chemin
            var chemin = new List<Noeud<T>>();
            var noeudActuel = arrivee;

            if (precedent[noeudActuel] == null && !noeudActuel.Equals(depart))
                return (null, int.MaxValue); // pas de chemin trouvé

            while (noeudActuel != null)
            {
                chemin.Insert(0, noeudActuel);
                noeudActuel = precedent[noeudActuel];
            }

            return (chemin, distances[arrivee]);
        }

    }
}
