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

namespace VisualisationWPF
{
    public partial class MainWindow : Window
    {
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

            await Task.Run(() =>
            {
                string conn = "server=localhost;user=root;password=root;database=metro;";
                ImporteurMySQL.Charger(conn, graphe);
            });

            DessinerCarte(graphe);
            progressChemin.Visibility = Visibility.Collapsed;
        }

        private void DessinerCarte(Graphe<Station> graphe)
        {
            canvasCarte.Children.Clear();
            positions.Clear();
            cerclesStations.Clear();

            double minLat = graphe.Noeuds.Min(n => n.Valeur.Latitude);
            double maxLat = graphe.Noeuds.Max(n => n.Valeur.Latitude);
            double minLon = graphe.Noeuds.Min(n => n.Valeur.Longitude);
            double maxLon = graphe.Noeuds.Max(n => n.Valeur.Longitude);

            double largeur = canvasCarte.ActualWidth > 0 ? canvasCarte.ActualWidth : canvasCarte.Width;
            double hauteur = canvasCarte.ActualHeight > 0 ? canvasCarte.ActualHeight : canvasCarte.Height;

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

            foreach (var lien in graphe.Liens)
            {
                if (!positions.TryGetValue(lien.Depart, out Point posA) || !positions.TryGetValue(lien.Arrivee, out Point posB))
                    continue;

                string ligneNom = lien.Depart.Valeur.Ligne.Trim().ToLower();

                Brush couleur = Brushes.Gray;
                if (couleursLignes.TryGetValue(ligneNom, out var brush))
                    couleur = brush;

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

            string algo = ((ComboBoxItem)comboAlgorithme.SelectedItem).Content.ToString();

            if (algo == "Dijkstra")
            {
                (chemin, cout) = graphe.Dijkstra(depart, arrivee);
            }
            else if (algo == "Bellman-Ford")
            {
                // 🧠 Appel indirect à Bellman-Ford (système basé sur ID)
                var edges = Graphe<Station>.ExcelGraphLoader.LoadEdgesWithStationNames("MetroParis.xlsx");
                if (!edges.nameToId.TryGetValue(depart.Valeur.Nom, out int idDep) ||
                    !edges.nameToId.TryGetValue(arrivee.Valeur.Nom, out int idArr)) return;

                if (Graphe<Station>.BellmanFord.ComputeShortestPaths(edges.nameToId.Count, edges.edges, idDep, out int[] distances))
                {
                    cout = distances[idArr];
                    chemin = graphe.Noeuds
                        .Where(n => edges.nameToId.ContainsKey(n.Valeur.Nom)) // approx simplifiée
                        .ToList();
                }
            }


            if (chemin == null || chemin.Count < 2)
            {
                txtTempsTotal.Text = "";
                txtListeStations.Text = "";
                progressChemin.Visibility = Visibility.Collapsed;
                return;
            }

            txtTempsTotal.Text = $"🕒 Temps estimé : {cout} min";
            txtListeStations.Text = "🧭 Itinéraire :\n" + string.Join(" ➜ ", chemin.Select(n => n.Valeur.Nom));

            var glow = new DropShadowEffect
            {
                Color = Colors.Red,
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 0.8
            };

            foreach (var (a, b) in chemin.Zip(chemin.Skip(1), (x, y) => (x, y)))
            {
                if (!positions.TryGetValue(a, out Point posA) || !positions.TryGetValue(b, out Point posB))
                    continue;

                var ligne = new Line
                {
                    X1 = posA.X,
                    Y1 = posA.Y,
                    X2 = posB.X,
                    Y2 = posB.Y,
                    Stroke = Brushes.Red,
                    StrokeThickness = 4,
                    Effect = glow,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Opacity = 0
                };

                canvasCarte.Children.Add(ligne);

                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(200)
                };

                ligne.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                await Task.Delay(300);
            }

            if (cerclesStations.TryGetValue(depart, out Ellipse cercleDepart))
            {
                cercleDepart.Fill = Brushes.Green;
                cercleDepart.Width = 12;
                cercleDepart.Height = 12;
            }

            if (cerclesStations.TryGetValue(arrivee, out Ellipse cercleArrivee))
            {
                cercleArrivee.Fill = Brushes.Red;
                cercleArrivee.Width = 12;
                cercleArrivee.Height = 12;

                var clignote = new DoubleAnimation
                {
                    From = 1,
                    To = 0.2,
                    Duration = TimeSpan.FromMilliseconds(500),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };

                cercleArrivee.BeginAnimation(UIElement.OpacityProperty, clignote);
            }

            progressChemin.Visibility = Visibility.Collapsed;


            await Task.Delay(1000); // pause avant retour

            foreach (var (a, b) in chemin.Reverse<Noeud<Station>>().Zip(chemin.Reverse<Noeud<Station>>().Skip(1), (x, y) => (x, y)))
            {
                if (!positions.TryGetValue(a, out Point posA) || !positions.TryGetValue(b, out Point posB))
                    continue;

                var ligneRetour = new Line
                {
                    X1 = posA.X,
                    Y1 = posA.Y,
                    X2 = posB.X,
                    Y2 = posB.Y,
                    Stroke = Brushes.LightBlue,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 },
                    Opacity = 0
                };

                canvasCarte.Children.Add(ligneRetour);

                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(100)
                };

                ligneRetour.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                await Task.Delay(150);
            }

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
