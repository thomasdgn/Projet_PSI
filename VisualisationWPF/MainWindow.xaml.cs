using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Projet_PSI_DELAROCHE_DEGARDIN_DARMON;
using System.IO;
using static Google.Protobuf.Reflection.UninterpretedOption.Types;

namespace VisualisationWPF
{
    public partial class MainWindow : Window
    {
        private ImporteurMySQL importeur;
        private Graphe<Station> graphe = new();
        private Dictionary<Noeud<Station>, Point> positions = new();
        private Dictionary<Noeud<Station>, Ellipse> cerclesStations = new();
        private readonly Dictionary<string, Brush> couleursLignes = new()
        {
            { "1", Brushes.Gold },
            { "2", Brushes.BlueViolet },
            { "3", Brushes.Olive },
            { "3bis", Brushes.LightGreen },
            { "4", Brushes.Purple },
            { "5", Brushes.Orange },
            { "6", Brushes.LightGreen },
            { "7", Brushes.Pink },
            { "7bis", Brushes.SkyBlue },
            { "8", Brushes.Violet },
            { "9", Brushes.YellowGreen },
            { "10", Brushes.Goldenrod },
            { "11", Brushes.Brown },
            { "12", Brushes.DarkGreen },
            { "13", Brushes.Teal },
            { "14", Brushes.MediumVioletRed }
        };


        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            progressChemin.Visibility = Visibility.Visible;

            try
            {
                importeur = new ImporteurMySQL("localhost", "metro", "root", "root");
                var result = await importeur.BuildStationGraphAsync();
                graphe = new Graphe<Station>();
                foreach (var noeud in result.Noeuds)
                    graphe.AjouterNoeud(noeud);
                foreach (var lien in result.Liens)
                    graphe.AjouterLien(lien.Depart, lien.Arrivee, lien.Poids);
                DessinerCarte(graphe);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des données : {ex.Message}");
            }
            finally
            {
                progressChemin.Visibility = Visibility.Collapsed;
            }
        }

        private void DessinerCarte(Graphe<Station> graphe)
        {
            // Vider le Canvas avant de redessiner la carte
            canvasCarte.Children.Clear();
            positions.Clear();
            cerclesStations.Clear();

            // Calculer les limites géographiques du graphique
            double minLat = graphe.Noeuds.Min(n => n.Valeur.Latitude);
            double maxLat = graphe.Noeuds.Max(n => n.Valeur.Latitude);
            double minLon = graphe.Noeuds.Min(n => n.Valeur.Longitude);
            double maxLon = graphe.Noeuds.Max(n => n.Valeur.Longitude);

            // Calculer la taille du Canvas
            double largeur = canvasCarte.ActualWidth > 0 ? canvasCarte.ActualWidth : canvasCarte.Width;
            double hauteur = canvasCarte.ActualHeight > 0 ? canvasCarte.ActualHeight : canvasCarte.Height;

            // Dessiner les stations (cercles)
            foreach (var noeud in graphe.Noeuds)
            {
                var s = noeud.Valeur;
                double x = (s.Longitude - minLon) / (maxLon - minLon) * (largeur - 40) + 20;
                double y = (1 - (s.Latitude - minLat) / (maxLat - minLat)) * (hauteur - 40) + 20;

                Point p = new(x, y);
                positions[noeud] = p;

                Ellipse cercle = new()
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.DarkBlue,
                    Stroke = Brushes.White,
                    StrokeThickness = 1,
                    ToolTip = $"{s.Nom} (Ligne {s.Ligne})"
                };

                Canvas.SetLeft(cercle, x - 5);
                Canvas.SetTop(cercle, y - 5);
                canvasCarte.Children.Add(cercle);
                cerclesStations[noeud] = cercle;

                TextBlock label = new()
                {
                    Text = s.Nom,
                    FontSize = 10,
                    Foreground = Brushes.White
                };
                Canvas.SetLeft(label, x + 6);
                Canvas.SetTop(label, y - 6);
                canvasCarte.Children.Add(label);
            }

            // Dessiner les liens entre les stations
            foreach (var lien in graphe.Liens)
            {
                if (!positions.TryGetValue(lien.Depart, out Point posA) || !positions.TryGetValue(lien.Arrivee, out Point posB))
                    continue; // Si les positions des stations ne sont pas trouvées, on ignore cette arête

                string ligneNom = lien.Depart.Valeur.Ligne.Trim().ToLower();

                // Déterminer la couleur de l'arête en fonction de la ligne
                Brush couleur = Brushes.Gray;
                if (couleursLignes.TryGetValue(ligneNom, out var brush))
                    couleur = brush;

                // Créer la ligne (l'arête) entre les deux stations
                var ligne = new Line
                {
                    X1 = posA.X,
                    Y1 = posA.Y,
                    X2 = posB.X,
                    Y2 = posB.Y,
                    Stroke = couleur,
                    StrokeThickness = 2.5,
                    ToolTip = $"Ligne {ligneNom.ToUpper()} : {lien.Depart.Valeur.Nom} ⇄ {lien.Arrivee.Valeur.Nom}"
                };

                // Ajouter la ligne (arête) au canvas
                canvasCarte.Children.Add(ligne);
            }
        }





        private void BtnReinitialiserCarte_Click(object sender, RoutedEventArgs e)
        {
            DessinerCarte(graphe);
            txtDepart.Text = "";
            txtArrivee.Text = "";
            txtTempsTotal.Text = "";
            txtListeStations.Text = "";
        }

        private async void BtnAfficherChemin_Click(object sender, RoutedEventArgs e)
        {
            string departNom = txtDepart.Text.Trim();
            string arriveeNom = txtArrivee.Text.Trim();

            if (comboAlgorithme.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un algorithme.");
                return;
            }

            BtnReinitialiserCarte_Click(null, null);

            var depart = graphe.Noeuds.FirstOrDefault(n => n.Valeur.Nom.Equals(departNom, StringComparison.OrdinalIgnoreCase));
            var arrivee = graphe.Noeuds.FirstOrDefault(n => n.Valeur.Nom.Equals(arriveeNom, StringComparison.OrdinalIgnoreCase));

            if (depart == null || arrivee == null)
            {
                MessageBox.Show("Stations introuvables.");
                return;
            }

            await AfficherCheminPlusCourtAsync(depart, arrivee);
        }

        private async Task AfficherCheminPlusCourtAsync(Noeud<Station> depart, Noeud<Station> arrivee)
        {
            progressChemin.Visibility = Visibility.Visible;

            List<Noeud<Station>> chemin = null;
            int cout = int.MaxValue;
            List<string> pathNames = new();

            string algo = ((ComboBoxItem)comboAlgorithme.SelectedItem).Content.ToString();

            string nomDepart = depart.Valeur.Nom;
            string nomArrivee = arrivee.Valeur.Nom;

            var chrono = System.Diagnostics.Stopwatch.StartNew();

            if (algo == "Dijkstra")
                (cout, pathNames) = await importeur.Dijkstra(nomDepart, nomArrivee);
            else if (algo == "Bellman-Ford")
                (cout, pathNames) = await importeur.BellmanFord(nomDepart, nomArrivee);
            else if (algo == "Floyd-Warshall")
                (cout, pathNames) = await importeur.FloydWarshall(nomDepart, nomArrivee);

            chrono.Stop();

            chemin = pathNames
                .Select(nom => graphe.Noeuds.FirstOrDefault(n => $"{n.Valeur.Nom} ({n.Valeur.Ligne})" == nom))
                .Where(n => n != null)
                .ToList();

            if (chemin == null || chemin.Count < 2)
            {
                txtTempsTotal.Text = "";
                txtListeStations.Text = "";
                progressChemin.Visibility = Visibility.Collapsed;
                return;
            }

            txtTempsTotal.Text = $"🕒 Temps estimé : {cout} min ({chemin.Count - 1} arrêt(s))\nTemps calcul : {chrono.ElapsedMilliseconds} ms";
            txtListeStations.Text = "🧭 Itinéraire :\n" + string.Join(" ➜ ", chemin.Select(n => n.Valeur.Nom));

            await AnimerChemin(chemin);

            progressChemin.Visibility = Visibility.Collapsed;

            AfficherGraphiqueComparatif(nomDepart, nomArrivee);
        }




        private async Task AnimerChemin(List<Noeud<Station>> chemin)
        {
            for (int i = 0; i < chemin.Count - 1; i++)
            {
                var a = chemin[i];
                var b = chemin[i + 1];

                var ligne = canvasCarte.Children.OfType<Line>().FirstOrDefault(l =>
                    (Math.Abs(l.X1 - positions[a].X) < 0.1 && Math.Abs(l.Y1 - positions[a].Y) < 0.1 &&
                     Math.Abs(l.X2 - positions[b].X) < 0.1 && Math.Abs(l.Y2 - positions[b].Y) < 0.1) ||
                    (Math.Abs(l.X2 - positions[a].X) < 0.1 && Math.Abs(l.Y2 - positions[a].Y) < 0.1 &&
                     Math.Abs(l.X1 - positions[b].X) < 0.1 && Math.Abs(l.Y1 - positions[b].Y) < 0.1));

                if (ligne != null)
                {
                    var originalStroke = ligne.Stroke;
                    var brush = new SolidColorBrush(Colors.White);
                    ligne.Stroke = brush;

                    var colorAnim = new ColorAnimation
                    {
                        From = Colors.White,
                        To = Colors.Red,
                        AutoReverse = true,
                        Duration = TimeSpan.FromMilliseconds(200),
                        RepeatBehavior = new RepeatBehavior(3)
                    };

                    brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);

                    ligne.StrokeThickness = 5;

                    ligne.Effect = new DropShadowEffect
                    {
                        Color = Colors.Red,
                        BlurRadius = 25,
                        ShadowDepth = 0,
                        Opacity = 1
                    };
                }

                await Task.Delay(200);
            }

            // Ensuite, stations en mode glow
            foreach (var noeud in chemin)
            {
                if (cerclesStations.TryGetValue(noeud, out Ellipse cercle))
                {
                    var brush = new SolidColorBrush(Colors.White);
                    cercle.Fill = brush;

                    var colorAnim = new ColorAnimation
                    {
                        From = Colors.White,
                        To = Colors.Red,
                        Duration = TimeSpan.FromMilliseconds(300),
                        AutoReverse = true,
                        RepeatBehavior = new RepeatBehavior(2)
                    };

                    brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);

                    cercle.Effect = new DropShadowEffect
                    {
                        Color = Colors.Red,
                        BlurRadius = 20,
                        ShadowDepth = 0,
                        Opacity = 1
                    };
                }
                await Task.Delay(80);
            }
        }





        private async void AfficherGraphiqueComparatif(string depart, string arrivee)
        {
            var chrono = new System.Diagnostics.Stopwatch();
            var resultats = new Dictionary<string, long>();

            foreach (var algo in new[] { "Dijkstra", "Bellman-Ford", "Floyd-Warshall" })
            {
                chrono.Restart();
                if (algo == "Dijkstra")
                    await importeur.Dijkstra(depart, arrivee);
                else if (algo == "Bellman-Ford")
                    await importeur.BellmanFord(depart, arrivee);
                else if (algo == "Floyd-Warshall")
                    await importeur.FloydWarshall(depart, arrivee);
                chrono.Stop();

                resultats[algo] = chrono.ElapsedMilliseconds;
            }

            string message = "⏱ Temps de calcul par algorithme (ms) :\n" + string.Join("\n", resultats.Select(kvp => $"- {kvp.Key} : {kvp.Value} ms"));
            MessageBox.Show(message, "Comparaison des algorithmes");
        }




        private void BtnExporter_Click(object sender, RoutedEventArgs e)
        {
            if (txtListeStations.Text == "")
            {
                MessageBox.Show("Aucun chemin à exporter.");
                return;
            }

            var cheminText = txtListeStations.Text.Replace("🧭 Itinéraire :\n", "");

            string cheminFichier = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "chemin_metro.txt");
            File.WriteAllText(cheminFichier, cheminText);

            MessageBox.Show($"Trajet exporté sur le bureau :\n{cheminFichier}");
        }
    }
}
