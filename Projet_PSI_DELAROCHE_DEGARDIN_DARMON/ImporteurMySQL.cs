using System.Data;
using DocumentFormat.OpenXml.Drawing;
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



        public static Graphe<Station> Charger()
        {
            Graphe<Station> graphe = new Graphe<Station>();
            Dictionary<int, Noeud<Station>> noeuds = new Dictionary<int, Noeud<Station>>();

            string connectionString = "Server=localhost;Database=metro;User ID=root;Password=root;";

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    Console.WriteLine("Connexion à MySQL réussie.");

                    // --- Chargement des stations ---
                    string queryStations = "SELECT id, nom, ligne, latitude, longitude FROM stations";
                    using (MySqlCommand cmdStations = new MySqlCommand(queryStations, conn))
                    using (MySqlDataReader readerStations = cmdStations.ExecuteReader())
                    {
                        while (readerStations.Read())
                        {
                            int id = readerStations.GetInt32("id");
                            string nom = readerStations.GetString("nom");
                            string ligne = readerStations.GetString("ligne");
                            double latitude = readerStations.GetDouble("latitude");
                            double longitude = readerStations.GetDouble("longitude");

                            Station station = new Station(id, nom, ligne, latitude, longitude);
                            Noeud<Station> noeud = new Noeud<Station>(station);

                            noeuds[id] = noeud;
                            graphe.AjouterNoeud(noeud);
                        }
                    }

                    Console.WriteLine($"✅ {noeuds.Count} stations chargées.");

                    // --- Chargement des liaisons ---
                    string queryLiens = "SELECT precedent, suivant, temps FROM liaisons";
                    using (MySqlCommand cmdLiens = new MySqlCommand(queryLiens, conn))
                    using (MySqlDataReader readerLiens = cmdLiens.ExecuteReader())
                    {
                        int nbLiensAjoutes = 0;

                        while (readerLiens.Read())
                        {
                            int precedentId = readerLiens.GetInt32("precedent");
                            int suivantId = readerLiens.GetInt32("suivant");
                            int temps = readerLiens.GetInt32("temps");

                            if (!noeuds.ContainsKey(precedentId))
                            {
                                Console.WriteLine($"⚠️ ID precedent introuvable : {precedentId}");
                                continue;
                            }

                            if (!noeuds.ContainsKey(suivantId))
                            {
                                Console.WriteLine($"⚠️ ID suivant introuvable : {suivantId}");
                                continue;
                            }

                            Noeud<Station> precedent = noeuds[precedentId];
                            Noeud<Station> suivant = noeuds[suivantId];

                            graphe.AjouterLien(precedent, suivant, temps);
                            nbLiensAjoutes++;
                        }

                        Console.WriteLine($"✅ {nbLiensAjoutes} liaisons ajoutées.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors du chargement des données MySQL : {ex.Message}");
            }

            Console.WriteLine($"🔎 Graphe final : {graphe.Noeuds.Count} stations, {graphe.Liens.Count} liens.");
            return graphe;
        }




        /// <summary>
        /// Récupère toutes les stations depuis la base de données
        /// </summary>
        /// <returns>Liste des stations</returns>
        public async Task<List<Station>> GetAllStationsAsync()
        {
            List<Station> stations = new List<Station>();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT id, nom, ligne, latitude, longitude FROM stations";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Station station = new Station(
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                reader.GetDouble(3),
                                reader.GetDouble(4)
                            );
                            stations.Add(station);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des stations: {ex.Message}");
                throw;
            }

            return stations;
        }

        /// <summary>
        /// Récupère toutes les liaisons depuis la base de données
        /// </summary>
        /// <returns>Dictionnaire des liaisons par station ID</returns>
        public async Task<Dictionary<int, List<(int Precedent, int Suivant, int Temps, int Changement)>>> GetAllLiaisonsAsync()
        {
            Dictionary<int, List<(int, int, int, int)>> liaisonsByStation = new Dictionary<int, List<(int, int, int, int)>>();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT station_id, precedent, suivant, temps, changement FROM liaisons";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int stationId = reader.GetInt32(0);

                            // Gérer les valeurs nulles pour precedent, suivant, temps et changement
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des liaisons: {ex.Message}");
                throw;
            }

            return liaisonsByStation;
        }

        /// <summary>
        /// Récupère toutes les correspondances depuis la base de données en évitant les duplicatas
        /// </summary>
        /// <returns>Liste des correspondances uniques</returns>
        public async Task<List<Correspondance>> GetAllCorrespondancesAsync()
        {
            List<Correspondance> correspondances = new List<Correspondance>();
            Dictionary<int, string> stationNames = new Dictionary<int, string>();

            // Utilisation d'un HashSet pour suivre les correspondances uniques
            HashSet<string> correspondanceKeys = new HashSet<string>();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // D'abord récupérer les noms des stations pour les associer aux IDs
                    string stationQuery = "SELECT id, nom FROM stations";
                    using (MySqlCommand command = new MySqlCommand(stationQuery, connection))
                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            stationNames[reader.GetInt32(0)] = reader.GetString(1);
                        }
                    }

                    // Ensuite récupérer les correspondances
                    string query = "SELECT station_id, ligne_origine, ligne_correspondance, temps_correspondance FROM correspondances";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            // Vérifier les valeurs nulles
                            int stationId = reader.GetInt32(0);
                            string ligneOrigine = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            string ligneCorrespondance = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            int tempsCorrespondance = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);

                            if (stationNames.ContainsKey(stationId))
                            {
                                string stationName = stationNames[stationId];

                                // Créer une clé unique pour détecter les duplicatas
                                // Format: "nomStation_ligneOrigine_ligneCorrespondance"
                                string key = $"{stationName}_{ligneOrigine}_{ligneCorrespondance}";

                                // Vérifier aussi la correspondance inverse pour éviter les duplicatas symétriques
                                string keyReverse = $"{stationName}_{ligneCorrespondance}_{ligneOrigine}";

                                // Ajouter seulement si la correspondance n'existe pas déjà
                                if (!correspondanceKeys.Contains(key) && !correspondanceKeys.Contains(keyReverse))
                                {
                                    Correspondance correspondance = new Correspondance(
                                        stationName,
                                        ligneOrigine,
                                        ligneCorrespondance,
                                        tempsCorrespondance
                                    );
                                    correspondances.Add(correspondance);
                                    correspondanceKeys.Add(key);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des correspondances: {ex.Message}");
                throw;
            }

            return correspondances;
        }

        /// <summary>
        /// Construit un graphe de stations avec les liaisons
        /// </summary>
        /// <returns>Liste des noeuds et liens du graphe</returns>
        public async Task<(List<Noeud<Station>> Noeuds, List<Lien<Station>> Liens)> BuildStationGraphAsync()
        {
            List<Station> stations = await GetAllStationsAsync();
            Dictionary<int, List<(int, int, int, int)>> liaisons = await GetAllLiaisonsAsync();

            List<Noeud<Station>> noeuds = new List<Noeud<Station>>();
            List<Lien<Station>> liens = new List<Lien<Station>>();

            // Créer les noeuds
            Dictionary<int, Noeud<Station>> stationNodes = new Dictionary<int, Noeud<Station>>();
            foreach (Station station in stations)
            {
                Noeud<Station> noeud = new Noeud<Station>(station);
                noeuds.Add(noeud);
                stationNodes[station.Id] = noeud;
            }

            // Créer les liens
            foreach (var stationId in liaisons.Keys)
            {
                if (stationNodes.ContainsKey(stationId))
                {
                    foreach (var (precedent, suivant, temps, changement) in liaisons[stationId])
                    {
                        if (stationNodes.ContainsKey(precedent) && stationNodes.ContainsKey(suivant))
                        {
                            // Lien du précédent vers la station
                            if (precedent > 0) // Vérifier que precedent est valide (pas 0 qui est notre valeur par défaut pour NULL)
                            {
                                Lien<Station> lienPrecedent = new Lien<Station>(
                                    stationNodes[precedent],
                                    stationNodes[stationId],
                                    temps
                                );
                                liens.Add(lienPrecedent);
                            }

                            if (suivant > 0) // Vérifier que suivant est valide (pas 0 qui est notre valeur par défaut pour NULL)
                            {
                                Lien<Station> lienSuivant = new Lien<Station>(
                                    stationNodes[stationId],
                                    stationNodes[suivant],
                                    temps
                                );
                                liens.Add(lienSuivant);
                            }
                        }
                    }
                }
            }

            return (noeuds, liens);
        }

        /// <summary>
        /// Permet de rechercher des stations par nom ou ligne
        /// </summary>
        /// <param name="keyword">Mot-clé à rechercher</param>
        /// <returns>Liste des stations correspondant au critère de recherche</returns>
        public async Task<List<Station>> SearchStationsAsync(string keyword)
        {
            List<Station> results = new List<Station>();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT id, nom, ligne, latitude, longitude FROM stations WHERE nom LIKE @keyword OR ligne LIKE @keyword";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Station station = new Station(
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la recherche de stations: {ex.Message}");
                throw;
            }

            return results;
        }

        /// <summary>
        /// Récupère les correspondances pour une station donnée
        /// </summary>
        /// <param name="stationId">ID de la station</param>
        /// <returns>Liste des correspondances pour la station</returns>
        public async Task<List<Correspondance>> GetCorrespondancesForStationAsync(int stationId)
        {
            List<Correspondance> correspondances = new List<Correspondance>();
            string stationName = "";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Récupérer d'abord le nom de la station
                    string stationQuery = "SELECT nom FROM stations WHERE id = @stationId";
                    using (MySqlCommand command = new MySqlCommand(stationQuery, connection))
                    {
                        command.Parameters.AddWithValue("@stationId", stationId);
                        object result = await command.ExecuteScalarAsync();
                        if (result != null)
                        {
                            stationName = result.ToString();
                        }
                        else
                        {
                            return correspondances; // Station non trouvée
                        }
                    }

                    // Récupérer les correspondances
                    string query = "SELECT ligne_origine, ligne_correspondance, temps_correspondance FROM correspondances WHERE station_id = @stationId";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@stationId", stationId);

                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string ligneOrigine = reader.GetString(0);
                                string ligneCorrespondance = reader.GetString(1);
                                int tempsCorrespondance = reader.GetInt32(2);

                                Correspondance correspondance = new Correspondance(
                                    stationName,
                                    ligneOrigine,
                                    ligneCorrespondance,
                                    tempsCorrespondance
                                );
                                correspondances.Add(correspondance);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des correspondances pour la station {stationId}: {ex.Message}");
                throw;
            }

            return correspondances;
        }



        /// <summary>
        /// Affiche toutes les correspondances du réseau de métro
        /// </summary>
        public async Task AfficherCorrespondancesAsync()
        {
            Console.WriteLine("===== CORRESPONDANCES DANS LE RÉSEAU DE MÉTRO =====");

            try
            {
                // Récupérer toutes les correspondances (déjà sans duplicatas)
                var correspondances = await GetAllCorrespondancesAsync();

                // Organiser les correspondances par station
                var correspondancesParStation = correspondances
                    .GroupBy(c => c.Station)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Afficher les correspondances par station
                foreach (var station in correspondancesParStation.Keys.OrderBy(s => s))
                {
                    bool premiereLigne = true;
                    foreach (var correspondance in correspondancesParStation[station])
                    {
                        if (premiereLigne)
                        {
                            Console.WriteLine($"Station: {correspondance.Station}");
                            premiereLigne = false;
                        }
                        Console.WriteLine($"  -> Correspondance de ligne {correspondance.Ligne1} vers ligne {correspondance.Ligne2} en {correspondance.TempsCorrespondance} minutes");
                    }
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'affichage des correspondances: {ex.Message}");
            }
        }

        /// <summary>
        /// Représente une arête orientée avec un poids pour l'algorithme de Bellman-Ford
        /// </summary>
        public readonly record struct Edge(int From, int To, int Weight);

        /// <summary>
        /// Charge les arêtes du graphe depuis la base de données pour l'algorithme de Bellman-Ford
        /// </summary>
        /// <returns>Tuple contenant la liste des arêtes et les dictionnaires de conversion</returns>
        public async Task<(List<Edge> edges, Dictionary<string, int> nameToId, Dictionary<int, string> idToName)> LoadEdgesFromDatabaseAsync()
        {
            var edges = new List<Edge>();
            var nameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var idToName = new Dictionary<int, string>();
            int nextNodeId = 0;

            try
            {
                // Récupérer toutes les stations
                var stations = await GetAllStationsAsync();

                // Créer un mapping de nom de station vers ID
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

                // Récupérer les liaisons
                var liaisons = await GetAllLiaisonsAsync();
                var stationsById = stations.ToDictionary(s => s.Id);

                // Créer les arêtes à partir des liaisons
                foreach (var stationId in liaisons.Keys)
                {
                    if (stationsById.TryGetValue(stationId, out var currentStation))
                    {
                        string currentStationKey = $"{currentStation.Nom} ({currentStation.Ligne})";

                        foreach (var (precedent, suivant, temps, _) in liaisons[stationId])
                        {
                            // Liaison avec la station précédente
                            if (precedent > 0 && stationsById.TryGetValue(precedent, out var prevStation))
                            {
                                string prevStationKey = $"{prevStation.Nom} ({prevStation.Ligne})";
                                if (nameToId.TryGetValue(prevStationKey, out int prevId) &&
                                    nameToId.TryGetValue(currentStationKey, out int currId))
                                {
                                    edges.Add(new Edge(prevId, currId, temps));
                                    // Ajouter également l'arête dans l'autre sens (trajet retour)
                                    edges.Add(new Edge(currId, prevId, temps));
                                }
                            }

                            // Liaison avec la station suivante
                            if (suivant > 0 && stationsById.TryGetValue(suivant, out var nextStation))
                            {
                                string nextStationKey = $"{nextStation.Nom} ({nextStation.Ligne})";
                                if (nameToId.TryGetValue(nextStationKey, out int nextId) &&
                                    nameToId.TryGetValue(currentStationKey, out int currId))
                                {
                                    edges.Add(new Edge(currId, nextId, temps));
                                    // Ajouter également l'arête dans l'autre sens (trajet retour)
                                    edges.Add(new Edge(nextId, currId, temps));
                                }
                            }
                        }
                    }
                }

                // Ajouter les correspondances comme des arêtes
                var correspondances = await GetAllCorrespondancesAsync();
                foreach (var correspondance in correspondances)
                {
                    string stationKey1 = $"{correspondance.Station} ({correspondance.Ligne1})";
                    string stationKey2 = $"{correspondance.Station} ({correspondance.Ligne2})";

                    if (nameToId.TryGetValue(stationKey1, out int id1) &&
                        nameToId.TryGetValue(stationKey2, out int id2))
                    {
                        edges.Add(new Edge(id1, id2, correspondance.TempsCorrespondance));
                        edges.Add(new Edge(id2, id1, correspondance.TempsCorrespondance));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement des arêtes: {ex.Message}");
                throw;
            }

            return (edges, nameToId, idToName);
        }

        /// <summary>
        /// Algorithme de Bellman-Ford adapté pour utiliser la base de données SQL
        /// </summary>
        /// <param name="sourceStation">Nom de la station de départ</param>
        /// <param name="destinationStation">Nom de la station d'arrivée</param>
        /// <returns>Le temps de trajet le plus court et le chemin</returns>
        public async Task<(int shortestTime, List<string> path)> BellmanFord(string sourceStation, string destinationStation)
        {
            try
            {
                // Charger les arêtes depuis la base de données
                var (edges, nameToId, idToName) = await LoadEdgesFromDatabaseAsync();

                // Vérifier si les stations existent
                if (!nameToId.Keys.Any(k => k.StartsWith(sourceStation + " (")))
                {
                    Console.WriteLine($"Station de départ '{sourceStation}' non trouvée");
                    return (-1, new List<string>());
                }

                if (!nameToId.Keys.Any(k => k.StartsWith(destinationStation + " (")))
                {
                    Console.WriteLine($"Station d'arrivée '{destinationStation}' non trouvée");
                    return (-1, new List<string>());
                }

                int nodeCount = nameToId.Count;

                // Préparer les structures pour mémoriser le chemin
                int[] distances = new int[nodeCount];
                int[] predecessors = new int[nodeCount];
                for (int i = 0; i < nodeCount; i++)
                {
                    distances[i] = int.MaxValue;
                    predecessors[i] = -1;
                }

                // Exécuter l'algorithme pour chaque variante de la station source (différentes lignes)
                bool foundPath = false;
                int shortestTime = int.MaxValue;
                List<string> bestPath = new List<string>();

                foreach (var sourceKey in nameToId.Keys.Where(k => k.StartsWith(sourceStation + " (")))
                {
                    int sourceId = nameToId[sourceKey];

                    // Réinitialiser les tableaux pour cette source
                    for (int i = 0; i < nodeCount; i++)
                    {
                        distances[i] = int.MaxValue;
                        predecessors[i] = -1;
                    }
                    distances[sourceId] = 0;

                    // Exécuter l'algorithme de Bellman-Ford
                    for (int i = 0; i < nodeCount - 1; i++)
                    {
                        bool updated = false;
                        foreach (var (from, to, weight) in edges)
                        {
                            if (distances[from] != int.MaxValue && distances[from] + weight < distances[to])
                            {
                                distances[to] = distances[from] + weight;
                                predecessors[to] = from;
                                updated = true;
                            }
                        }
                        if (!updated) break;
                    }

                    // Vérifier s'il y a un cycle négatif
                    foreach (var (from, to, weight) in edges)
                    {
                        if (distances[from] != int.MaxValue && distances[from] + weight < distances[to])
                        {
                            Console.WriteLine("Le graphe contient un cycle de poids négatif");
                            return (-1, new List<string>());
                        }
                    }

                    // Vérifier pour chaque variante de la destination (différentes lignes)
                    foreach (var destKey in nameToId.Keys.Where(k => k.StartsWith(destinationStation + " (")))
                    {
                        int destId = nameToId[destKey];

                        if (distances[destId] != int.MaxValue && distances[destId] < shortestTime)
                        {
                            shortestTime = distances[destId];

                            // Reconstruire le chemin
                            var path = new List<string>();
                            int currentNode = destId;

                            while (currentNode != -1)
                            {
                                path.Add(idToName[currentNode]);
                                currentNode = predecessors[currentNode];
                            }

                            path.Reverse();
                            bestPath = path;
                            foundPath = true;
                        }
                    }
                }

                if (!foundPath)
                {
                    Console.WriteLine($"Aucun chemin trouvé entre '{sourceStation}' et '{destinationStation}'");
                    return (-1, new List<string>());
                }

                return (shortestTime, bestPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'exécution de l'algorithme de Bellman-Ford: {ex.Message}");
                return (-1, new List<string>());
            }
        }

        /// <summary>
        /// Algorithme de Dijkstra adapté pour utiliser la base de données SQL
        /// </summary>
        /// <param name="sourceStation">Nom de la station de départ</param>
        /// <param name="destinationStation">Nom de la station d'arrivée</param>
        /// <returns>Le temps de trajet le plus court et le chemin</returns>
        public async Task<(int shortestTime, List<string> path)> Dijkstra(string sourceStation, string destinationStation)
        {
            try
            {
                // Charger les arêtes depuis la base de données (réutilise la même méthode que Bellman-Ford)
                var (edges, nameToId, idToName) = await LoadEdgesFromDatabaseAsync();

                // Vérifier si les stations existent
                if (!nameToId.Keys.Any(k => k.StartsWith(sourceStation + " (")))
                {
                    Console.WriteLine($"Station de départ '{sourceStation}' non trouvée");
                    return (-1, new List<string>());
                }

                if (!nameToId.Keys.Any(k => k.StartsWith(destinationStation + " (")))
                {
                    Console.WriteLine($"Station d'arrivée '{destinationStation}' non trouvée");
                    return (-1, new List<string>());
                }

                int nodeCount = nameToId.Count;

                // Construire la liste d'adjacence pour Dijkstra
                List<(int node, int weight)>[] adjacencyList = new List<(int, int)>[nodeCount];
                for (int i = 0; i < nodeCount; i++)
                {
                    adjacencyList[i] = new List<(int, int)>();
                }

                foreach (var (from, to, weight) in edges)
                {
                    adjacencyList[from].Add((to, weight));
                }

                // Exécuter l'algorithme pour chaque variante de la station source (différentes lignes)
                bool foundPath = false;
                int shortestTime = int.MaxValue;
                List<string> bestPath = new List<string>();

                foreach (var sourceKey in nameToId.Keys.Where(k => k.StartsWith(sourceStation + " (")))
                {
                    int sourceId = nameToId[sourceKey];

                    // Initialiser les distances et les prédécesseurs
                    int[] distances = new int[nodeCount];
                    int[] predecessors = new int[nodeCount];
                    bool[] visited = new bool[nodeCount];

                    for (int i = 0; i < nodeCount; i++)
                    {
                        distances[i] = int.MaxValue;
                        predecessors[i] = -1;
                        visited[i] = false;
                    }

                    distances[sourceId] = 0;

                    // File de priorité pour Dijkstra (simulation avec une liste)
                    for (int count = 0; count < nodeCount - 1; count++)
                    {
                        // Trouver le nœud non visité avec la distance minimale
                        int minDistance = int.MaxValue;
                        int u = -1;

                        for (int v = 0; v < nodeCount; v++)
                        {
                            if (!visited[v] && distances[v] <= minDistance)
                            {
                                minDistance = distances[v];
                                u = v;
                            }
                        }

                        // Si nous ne pouvons pas atteindre d'autres nœuds, sortir
                        if (u == -1 || distances[u] == int.MaxValue)
                            break;

                        visited[u] = true;

                        // Mettre à jour les distances des nœuds adjacents
                        foreach (var (v, weight) in adjacencyList[u])
                        {
                            if (!visited[v] && distances[u] != int.MaxValue && distances[u] + weight < distances[v])
                            {
                                distances[v] = distances[u] + weight;
                                predecessors[v] = u;
                            }
                        }
                    }

                    // Vérifier pour chaque variante de la destination (différentes lignes)
                    foreach (var destKey in nameToId.Keys.Where(k => k.StartsWith(destinationStation + " (")))
                    {
                        int destId = nameToId[destKey];

                        if (distances[destId] != int.MaxValue && distances[destId] < shortestTime)
                        {
                            shortestTime = distances[destId];

                            // Reconstruire le chemin
                            var path = new List<string>();
                            int currentNode = destId;

                            while (currentNode != -1)
                            {
                                path.Add(idToName[currentNode]);
                                currentNode = predecessors[currentNode];
                            }

                            path.Reverse();
                            bestPath = path;
                            foundPath = true;
                        }
                    }
                }

                if (!foundPath)
                {
                    Console.WriteLine($"Aucun chemin trouvé entre '{sourceStation}' et '{destinationStation}'");
                    return (-1, new List<string>());
                }

                return (shortestTime, bestPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'exécution de l'algorithme de Dijkstra: {ex.Message}");
                return (-1, new List<string>());
            }
        }



        /// <summary>
        /// Implémente l'algorithme de Floyd-Warshall pour trouver les plus courts chemins entre toutes les paires de stations
        /// </summary>
        /// <param name="stationDepart">Nom de la station de départ</param>
        /// <param name="stationArrivee">Nom de la station d'arrivée</param>
        /// <returns>Le temps de trajet le plus court et le chemin entre les stations spécifiées</returns>
        public async Task<(int shortestTime, List<string> path)> FloydWarshall(string stationDepart, string stationArrivee)
        {
            try
            {
                // Charger les arêtes depuis la base de données (réutilise la même méthode que Bellman-Ford)
                var (aretes, nomVersId, idVersNom) = await LoadEdgesFromDatabaseAsync();

                // Vérifier si les stations existent
                if (!nomVersId.Keys.Any(k => k.StartsWith(stationDepart + " (")))
                {
                    Console.WriteLine($"Station de départ '{stationDepart}' non trouvée");
                    return (-1, new List<string>());
                }

                if (!nomVersId.Keys.Any(k => k.StartsWith(stationArrivee + " (")))
                {
                    Console.WriteLine($"Station d'arrivée '{stationArrivee}' non trouvée");
                    return (-1, new List<string>());
                }

                int nombreNoeuds = nomVersId.Count;

                // Initialiser la matrice de distances et la matrice des prédécesseurs
                int[,] distances = new int[nombreNoeuds, nombreNoeuds];
                int[,] predecesseurs = new int[nombreNoeuds, nombreNoeuds];

                // Initialiser toutes les distances à l'infini et tous les prédécesseurs à -1
                for (int i = 0; i < nombreNoeuds; i++)
                {
                    for (int j = 0; j < nombreNoeuds; j++)
                    {
                        if (i == j)
                            distances[i, j] = 0; // La distance d'un nœud à lui-même est 0
                        else
                            distances[i, j] = int.MaxValue;

                        predecesseurs[i, j] = -1;
                    }
                }

                // Remplir la matrice avec les arêtes existantes
                foreach (var (origine, destination, poids) in aretes)
                {
                    if (poids < distances[origine, destination]) // En cas d'arêtes multiples, prendre la plus légère
                    {
                        distances[origine, destination] = poids;
                        predecesseurs[origine, destination] = origine;
                    }
                }

                // Algorithme de Floyd-Warshall
                for (int k = 0; k < nombreNoeuds; k++)
                {
                    for (int i = 0; i < nombreNoeuds; i++)
                    {
                        for (int j = 0; j < nombreNoeuds; j++)
                        {
                            // Vérifier que les distances ne sont pas infinies pour éviter les débordements
                            if (distances[i, k] != int.MaxValue && distances[k, j] != int.MaxValue)
                            {
                                int distancePotentielle = distances[i, k] + distances[k, j];

                                if (distancePotentielle < distances[i, j])
                                {
                                    distances[i, j] = distancePotentielle;
                                    predecesseurs[i, j] = predecesseurs[k, j];
                                }
                            }
                        }
                    }
                }

                // Trouver le chemin le plus court parmi toutes les variantes des stations source et destination
                bool cheminTrouve = false;
                int tempsPlusCourt = int.MaxValue;
                List<string> meilleurChemin = new List<string>();
                int meilleurIdDepart = -1;
                int meilleurIdArrivee = -1;

                foreach (var cleDepart in nomVersId.Keys.Where(k => k.StartsWith(stationDepart + " (")))
                {
                    int idDepart = nomVersId[cleDepart];

                    foreach (var cleArrivee in nomVersId.Keys.Where(k => k.StartsWith(stationArrivee + " (")))
                    {
                        int idArrivee = nomVersId[cleArrivee];

                        if (distances[idDepart, idArrivee] != int.MaxValue && distances[idDepart, idArrivee] < tempsPlusCourt)
                        {
                            tempsPlusCourt = distances[idDepart, idArrivee];
                            meilleurIdDepart = idDepart;
                            meilleurIdArrivee = idArrivee;
                            cheminTrouve = true;
                        }
                    }
                }

                if (!cheminTrouve)
                {
                    Console.WriteLine($"Aucun chemin trouvé entre '{stationDepart}' et '{stationArrivee}'");
                    return (-1, new List<string>());
                }

                // Reconstruire le chemin à partir de la matrice des prédécesseurs
                meilleurChemin = ReconstruireChemin(meilleurIdDepart, meilleurIdArrivee, predecesseurs, idVersNom);

                return (tempsPlusCourt, meilleurChemin);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'exécution de l'algorithme de Floyd-Warshall: {ex.Message}");
                return (-1, new List<string>());
            }
        }

        /// <summary>
        /// Reconstruit le chemin à partir de la matrice des prédécesseurs
        /// </summary>
        /// <param name="idDepart">ID du nœud de départ</param>
        /// <param name="idArrivee">ID du nœud d'arrivée</param>
        /// <param name="predecesseurs">Matrice des prédécesseurs</param>
        /// <param name="idVersNom">Dictionnaire de conversion des IDs vers les noms</param>
        /// <returns>Liste des stations constituant le chemin</returns>
        private List<string> ReconstruireChemin(int idDepart, int idArrivee, int[,] predecesseurs, Dictionary<int, string> idVersNom)
        {
            if (predecesseurs[idDepart, idArrivee] == -1)
            {
                // Aucun chemin
                return new List<string>();
            }

            List<string> chemin = new List<string>();

            // Commencer par la destination
            chemin.Add(idVersNom[idArrivee]);

            // Reconstruire le chemin en remontant les prédécesseurs
            int actuel = idArrivee;
            while (actuel != idDepart)
            {
                actuel = predecesseurs[idDepart, actuel];
                chemin.Add(idVersNom[actuel]);
            }

            // Inverser le chemin pour l'avoir dans l'ordre départ -> arrivée
            chemin.Reverse();

            return chemin;
        }
    }
}