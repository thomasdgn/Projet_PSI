using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using MySql.Data.MySqlClient;

namespace Projet_PSI_DELAROCHE_DEGARDIN_DARMON
{
    public class ImporteurMySQL
    {
        private readonly string connectionString;

        public ImporteurMySQL(string server, string database, string username, string password)
        {
            connectionString = $"Server={server};Database={database};User ID={username};Password={password};";
        }

        // Chargement du graphe complet
        public static Graphe<Station> Charger()
        {
            string server = "localhost";
            string database = "metro";
            string username = "root";
            string password = "root";

            string connectionString = $"Server={server};Database={database};User ID={username};Password={password};";

            var graphe = new Graphe<Station>();
            var noeuds = new Dictionary<int, Noeud<Station>>();

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // --- Chargement des stations ---
                var queryStations = "SELECT id, nom, ligne, latitude, longitude FROM stations";
                using (var cmdStations = new MySqlCommand(queryStations, conn))
                using (var readerStations = cmdStations.ExecuteReader())
                {
                    while (readerStations.Read())
                    {
                        int idIndex = readerStations.GetOrdinal("id");
                        int nomIndex = readerStations.GetOrdinal("nom");
                        int ligneIndex = readerStations.GetOrdinal("ligne");
                        int latitudeIndex = readerStations.GetOrdinal("latitude");
                        int longitudeIndex = readerStations.GetOrdinal("longitude");

                        int id = readerStations.IsDBNull(idIndex) ? -1 : readerStations.GetInt32(idIndex);
                        string nom = readerStations.IsDBNull(nomIndex) ? "Inconnu" : readerStations.GetString(nomIndex);
                        string ligne = readerStations.IsDBNull(ligneIndex) ? "?" : readerStations.GetString(ligneIndex);
                        double latitude = readerStations.IsDBNull(latitudeIndex) ? 0.0 : readerStations.GetDouble(latitudeIndex);
                        double longitude = readerStations.IsDBNull(longitudeIndex) ? 0.0 : readerStations.GetDouble(longitudeIndex);

                        if (id != -1)
                        {
                            var station = new Station(id, nom, ligne, latitude, longitude);
                            var noeud = new Noeud<Station>(station);
                            noeuds[station.Id] = noeud;
                            graphe.AjouterNoeud(noeud);
                        }
                    }

                }

                // --- Chargement des liaisons ---
                var queryLiens = "SELECT precedent, suivant, temps FROM liaisons";
                using (var cmdLiens = new MySqlCommand(queryLiens, conn))
                using (var readerLiens = cmdLiens.ExecuteReader())
                {
                    while (readerLiens.Read())
                    {
                        int precedentId = readerLiens.IsDBNull(readerLiens.GetOrdinal("precedent")) ? -1 : readerLiens.GetInt32(readerLiens.GetOrdinal("precedent"));
                        int suivantId = readerLiens.IsDBNull(readerLiens.GetOrdinal("suivant")) ? -1 : readerLiens.GetInt32(readerLiens.GetOrdinal("suivant"));
                        int temps = readerLiens.IsDBNull(readerLiens.GetOrdinal("temps")) ? 0 : readerLiens.GetInt32(readerLiens.GetOrdinal("temps"));

                        if (precedentId != -1 && suivantId != -1)
                        {
                            if (noeuds.ContainsKey(precedentId) && noeuds.ContainsKey(suivantId))
                            {
                                graphe.AjouterLien(noeuds[precedentId], noeuds[suivantId], temps);
                            }
                        }
                    }
                }
            }

            return graphe;
        }


        // Récupère toutes les stations
        public List<Station> GetAllStations()
        {
            var stations = new List<Station>();

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT id, nom, ligne, latitude, longitude FROM stations";

                using (var command = new MySqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = Convert.ToInt32(reader["id"]);
                        string nom = reader["nom"]?.ToString() ?? "";
                        string ligne = reader["ligne"]?.ToString() ?? "";
                        double latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? 0.0 : reader.GetDouble(reader.GetOrdinal("latitude"));
                        double longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? 0.0 : reader.GetDouble(reader.GetOrdinal("longitude"));

                        var station = new Station(id, nom, ligne, latitude, longitude);
                        stations.Add(station);
                    }
                }
            }

            return stations;
        }



        // Récupère toutes les liaisons
        public Dictionary<int, List<(int Precedent, int Suivant, int Temps, int Changement)>> GetAllLiaisons()
        {
            var liaisonsByStation = new Dictionary<int, List<(int, int, int, int)>>();

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT station_id, precedent, suivant, temps, changement FROM liaisons";

                using (var command = new MySqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int stationId = reader.GetInt32(0);
                        int precedent = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        int suivant = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                        int temps = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                        int changement = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);

                        if (!liaisonsByStation.ContainsKey(stationId))
                        {
                            liaisonsByStation[stationId] = new List<(int, int, int, int)>();
                        }

                        liaisonsByStation[stationId].Add((precedent, suivant, temps, changement));
                    }
                }
            }

            return liaisonsByStation;
        }

        // Récupère toutes les correspondances
        public List<Correspondance> GetAllCorrespondances()
        {
            var correspondances = new List<Correspondance>();
            var stationNames = new Dictionary<int, string>();
            var correspondanceKeys = new HashSet<string>();

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // D'abord récupérer les noms des stations
                string stationQuery = "SELECT id, nom FROM stations";
                using (var command = new MySqlCommand(stationQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stationNames[reader.GetInt32(0)] = reader.GetString(1);
                    }
                }

                // Ensuite récupérer les correspondances
                string query = "SELECT station_id, ligne_origine, ligne_correspondance, temps_correspondance FROM correspondances";
                using (var command = new MySqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int stationId = reader.GetInt32(0);
                        string ligneOrigine = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        string ligneCorrespondance = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        int tempsCorrespondance = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);

                        if (stationNames.ContainsKey(stationId))
                        {
                            string stationName = stationNames[stationId];

                            string key = $"{stationName}_{ligneOrigine}_{ligneCorrespondance}";
                            string keyReverse = $"{stationName}_{ligneCorrespondance}_{ligneOrigine}";

                            if (!correspondanceKeys.Contains(key) && !correspondanceKeys.Contains(keyReverse))
                            {
                                if (!string.IsNullOrEmpty(stationName))
                                {
                                    correspondances.Add(new Correspondance(
                                        stationName,
                                        ligneOrigine,
                                        ligneCorrespondance,
                                        tempsCorrespondance
                                    ));
                                }
                                correspondanceKeys.Add(key);
                            }
                        }
                    }
                }
            }

            return correspondances;
        }


        // Construit le graphe des stations avec liaisons
        public (List<Noeud<Station>> Noeuds, List<Lien<Station>> Liens) BuildStationGraph()
        {
            var stations = GetAllStations();
            var liaisons = GetAllLiaisons();

            var noeuds = new List<Noeud<Station>>();
            var liens = new List<Lien<Station>>();
            var stationNodes = new Dictionary<int, Noeud<Station>>();

            foreach (var station in stations)
            {
                var noeud = new Noeud<Station>(station);
                noeuds.Add(noeud);
                stationNodes[station.Id] = noeud;
            }

            foreach (var stationId in liaisons.Keys)
            {
                if (stationNodes.ContainsKey(stationId))
                {
                    foreach (var (precedent, suivant, temps, changement) in liaisons[stationId])
                    {
                        if (precedent > 0 && stationNodes.ContainsKey(precedent))
                        {
                            liens.Add(new Lien<Station>(
                                stationNodes[precedent],
                                stationNodes[stationId],
                                temps
                            ));
                        }
                        if (suivant > 0 && stationNodes.ContainsKey(suivant))
                        {
                            liens.Add(new Lien<Station>(
                                stationNodes[stationId],
                                stationNodes[suivant],
                                temps
                            ));
                        }
                    }
                }
            }

            return (noeuds, liens);
        }

        // Recherche des stations par nom ou ligne
        public List<Station> SearchStations(string keyword)
        {
            var results = new List<Station>();

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT id, nom, ligne, latitude, longitude FROM stations WHERE nom LIKE @keyword OR ligne LIKE @keyword";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var station = new Station(
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                reader.GetDouble(3),
                                reader.GetDouble(4)
                            );
                            results.Add(station);
                        }
                    }
                }
            }

            return results;
        }

        // Récupère les correspondances pour une station donnée
        public List<Correspondance> GetCorrespondancesForStation(int stationId)
        {
            var correspondances = new List<Correspondance>();
            string stationName = "";

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // Nom de la station
                string stationQuery = "SELECT nom FROM stations WHERE id = @stationId";
                using (var command = new MySqlCommand(stationQuery, connection))
                {
                    command.Parameters.AddWithValue("@stationId", stationId);
                    var result = command.ExecuteScalar();
                    if (result != null)
                        stationName = result?.ToString() ?? "";
                    else
                        return correspondances;
                }

                // Récupérer les correspondances
                string query = "SELECT ligne_origine, ligne_correspondance, temps_correspondance FROM correspondances WHERE station_id = @stationId";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@stationId", stationId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!string.IsNullOrEmpty(stationName))
                            {
                                correspondances.Add(new Correspondance(
                                stationName,
                                reader.GetString(0),
                                reader.GetString(1),
                                reader.GetInt32(2)
                                ));
                            }
                        }
                    }
                }
            }

            return correspondances;
        }

        // Affiche toutes les correspondances
        public void AfficherCorrespondances()
        {
            Console.WriteLine("===== CORRESPONDANCES DANS LE RÉSEAU DE MÉTRO =====");

            var correspondances = GetAllCorrespondances();

            var correspondancesParStation = new Dictionary<string, List<Correspondance>>();

            foreach (var c in correspondances)
            {
                if (!correspondancesParStation.ContainsKey(c.Station))
                    correspondancesParStation[c.Station] = new List<Correspondance>();

                correspondancesParStation[c.Station].Add(c);
            }

            foreach (var station in correspondancesParStation.Keys)
            {
                Console.WriteLine($"\nStation : {station}");
                foreach (var correspondance in correspondancesParStation[station])
                {
                    Console.WriteLine($"  -> Correspondance de ligne {correspondance.Ligne1} vers ligne {correspondance.Ligne2} en {correspondance.TempsCorrespondance} minutes");
                }
            }
        }


        public (List<Edge> edges, Dictionary<string, int> nameToId, Dictionary<int, string> idToName) LoadEdgesFromDatabase()
        {
            var edges = new List<Edge>();
            var nameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var idToName = new Dictionary<int, string>();
            int nextNodeId = 0;

            var stations = GetAllStations();

            foreach (var station in stations)
            {
                string stationKey = $"{station.Nom} ({station.Ligne})";
                if (!nameToId.ContainsKey(stationKey))
                {
                    nameToId[stationKey] = nextNodeId;
                    idToName[nextNodeId] = stationKey;
                    nextNodeId++;
                }
            }

            var liaisons = GetAllLiaisons();
            var stationsById = new Dictionary<int, Station>();
            foreach (var station in stations)
                stationsById[station.Id] = station;

            foreach (var stationId in liaisons.Keys)
            {
                if (stationsById.TryGetValue(stationId, out var currentStation))
                {
                    string currentStationKey = $"{currentStation.Nom} ({currentStation.Ligne})";

                    foreach (var (precedent, suivant, temps, _) in liaisons[stationId])
                    {
                        if (precedent > 0 && stationsById.TryGetValue(precedent, out var prevStation))
                        {
                            string prevKey = $"{prevStation.Nom} ({prevStation.Ligne})";
                            edges.Add(new Edge(nameToId[prevKey], nameToId[currentStationKey], temps));
                            edges.Add(new Edge(nameToId[currentStationKey], nameToId[prevKey], temps));
                        }
                        if (suivant > 0 && stationsById.TryGetValue(suivant, out var nextStation))
                        {
                            string nextKey = $"{nextStation.Nom} ({nextStation.Ligne})";
                            edges.Add(new Edge(nameToId[currentStationKey], nameToId[nextKey], temps));
                            edges.Add(new Edge(nameToId[nextKey], nameToId[currentStationKey], temps));
                        }
                    }
                }
            }

            var correspondances = GetAllCorrespondances();
            foreach (var c in correspondances)
            {
                string key1 = $"{c.Station} ({c.Ligne1})";
                string key2 = $"{c.Station} ({c.Ligne2})";

                if (nameToId.ContainsKey(key1) && nameToId.ContainsKey(key2))
                {
                    edges.Add(new Edge(nameToId[key1], nameToId[key2], c.TempsCorrespondance));
                    edges.Add(new Edge(nameToId[key2], nameToId[key1], c.TempsCorrespondance));
                }
            }

            return (edges, nameToId, idToName);
        }

        public (int shortestTime, List<string> path) BellmanFord(string sourceStation, string destinationStation)
        {
            var (edges, nameToId, idToName) = LoadEdgesFromDatabase();

            if (!nameToId.Keys.Any(k => k.StartsWith(sourceStation + " (")))
                return (-1, new List<string>());

            if (!nameToId.Keys.Any(k => k.StartsWith(destinationStation + " (")))
                return (-1, new List<string>());

            int nodeCount = nameToId.Count;
            int[] distances = new int[nodeCount];
            int[] predecessors = new int[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                distances[i] = int.MaxValue;
                predecessors[i] = -1;
            }

            int shortestTime = int.MaxValue;
            List<string> bestPath = new List<string>();

            foreach (var sourceKey in nameToId.Keys.Where(k => k.StartsWith(sourceStation + " (")))
            {
                int sourceId = nameToId[sourceKey];

                for (int i = 0; i < nodeCount; i++)
                {
                    distances[i] = int.MaxValue;
                    predecessors[i] = -1;
                }
                distances[sourceId] = 0;

                for (int i = 0; i < nodeCount - 1; i++)
                {
                    foreach (var (from, to, weight) in edges)
                    {
                        if (distances[from] != int.MaxValue && distances[from] + weight < distances[to])
                        {
                            distances[to] = distances[from] + weight;
                            predecessors[to] = from;
                        }
                    }
                }

                foreach (var destKey in nameToId.Keys.Where(k => k.StartsWith(destinationStation + " (")))
                {
                    int destId = nameToId[destKey];

                    if (distances[destId] != int.MaxValue && distances[destId] < shortestTime)
                    {
                        shortestTime = distances[destId];
                        bestPath = ReconstructPath(predecessors, idToName, sourceId, destId);
                    }
                }
            }

            return (shortestTime, bestPath);
        }

        public (int shortestTime, List<string> path) Dijkstra(string sourceStation, string destinationStation)
        {
            var (edges, nameToId, idToName) = LoadEdgesFromDatabase();

            var sourceCandidates = nameToId.Keys.Where(k => k.StartsWith(sourceStation + " (")).ToList();
            var destCandidates = nameToId.Keys.Where(k => k.StartsWith(destinationStation + " (")).ToList();

            if (sourceCandidates.Count == 0 || destCandidates.Count == 0)
                return (-1, new List<string>());

            int nodeCount = nameToId.Count;
            int shortestTime = int.MaxValue;
            List<string> bestPath = new();

            foreach (var sourceKey in sourceCandidates)
            {
                int[] distances = new int[nodeCount];
                int[] predecessors = new int[nodeCount];
                bool[] visited = new bool[nodeCount];

                for (int i = 0; i < nodeCount; i++)
                {
                    distances[i] = int.MaxValue;
                    predecessors[i] = -1;
                    visited[i] = false;
                }

                int sourceId = nameToId[sourceKey];
                distances[sourceId] = 0;

                for (int count = 0; count < nodeCount - 1; count++)
                {
                    int minDistance = int.MaxValue, u = -1;

                    for (int v = 0; v < nodeCount; v++)
                    {
                        if (!visited[v] && distances[v] <= minDistance)
                        {
                            minDistance = distances[v];
                            u = v;
                        }
                    }

                    if (u == -1) break;
                    visited[u] = true;

                    foreach (var (from, to, weight) in edges)
                    {
                        if (from == u && !visited[to] && distances[u] + weight < distances[to])
                        {
                            distances[to] = distances[u] + weight;
                            predecessors[to] = u;
                        }
                    }
                }

                foreach (var destKey in destCandidates)
                {
                    int destId = nameToId[destKey];
                    if (distances[destId] < shortestTime)
                    {
                        shortestTime = distances[destId];
                        bestPath = ReconstructPath(predecessors, idToName, sourceId, destId);
                    }
                }
            }

            return (shortestTime, bestPath);
        }


        public (int shortestTime, List<string> path) FloydWarshall(string stationDepart, string stationArrivee)
        {
            var (edges, nameToId, idToName) = LoadEdgesFromDatabase();
            int n = nameToId.Count;
            var distances = new int[n, n];
            var predecessors = new int[n, n];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    distances[i, j] = (i == j) ? 0 : int.MaxValue;
                    predecessors[i, j] = -1;
                }

            foreach (var (from, to, weight) in edges)
            {
                distances[from, to] = weight;
                predecessors[from, to] = from;
            }

            for (int k = 0; k < n; k++)
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        if (distances[i, k] != int.MaxValue && distances[k, j] != int.MaxValue && distances[i, k] + distances[k, j] < distances[i, j])
                        {
                            distances[i, j] = distances[i, k] + distances[k, j];
                            predecessors[i, j] = predecessors[k, j];
                        }

            var sourceCandidates = nameToId.Where(kvp => kvp.Key.StartsWith(stationDepart)).Select(kvp => kvp.Value).ToList();
            var destCandidates = nameToId.Where(kvp => kvp.Key.StartsWith(stationArrivee)).Select(kvp => kvp.Value).ToList();

            int bestTime = int.MaxValue;
            List<string> bestPath = new List<string>();

            foreach (var s in sourceCandidates)
                foreach (var d in destCandidates)
                    if (distances[s, d] != int.MaxValue && distances[s, d] < bestTime)
                    {
                        bestTime = distances[s, d];
                        bestPath = ReconstructPath(predecessors, idToName, s, d);
                    }

            return (bestTime, bestPath);
        }

        private List<string> ReconstructPath(int[] predecessors, Dictionary<int, string> idToName, int sourceId, int destId)
        {
            var path = new List<string>();
            int current = destId;
            while (current != -1)
            {
                path.Add(idToName[current]);
                current = predecessors[current];
            }
            path.Reverse();
            return path;
        }

        private List<string> ReconstructPath(int[,] predecessors, Dictionary<int, string> idToName, int sourceId, int destId)
        {
            var path = new List<string>();
            if (predecessors[sourceId, destId] == -1)
                return path;

            int current = destId;
            while (current != sourceId)
            {
                path.Add(idToName[current]);
                current = predecessors[sourceId, current];
            }
            path.Add(idToName[sourceId]);
            path.Reverse();
            return path;
        }

        // Export JSON des stations
        public void ExporterStationsEnJson(string chemin)
        {
            var stations = GetAllStations();
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonString = JsonSerializer.Serialize(stations, options);
            File.WriteAllText(chemin, jsonString);
        }

        // Export XML des correspondances
        public void ExporterCorrespondancesEnXml(string chemin)
        {
            var correspondances = GetAllCorrespondances();
            var serializer = new XmlSerializer(typeof(List<Correspondance>));
            using var writer = new StreamWriter(chemin);
            serializer.Serialize(writer, correspondances);
        }
    }

    public readonly record struct Edge(int From, int To, int Weight);
}



