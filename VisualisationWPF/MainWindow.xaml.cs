using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using MySqlX.XDevAPI.Common;
using Projet_PSI_DELAROCHE_DEGARDIN_DARMON;

namespace VisualisationWPF
{
    public partial class MainWindow : Window
    {
        private ImporteurMySQL? importeur;
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
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
            Loaded += MainWindow_Loaded;
            canvasCarte.MouseWheel += CanvasCarte_MouseWheel;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            progressChemin.Visibility = Visibility.Visible;

            try
            {
                importeur = new ImporteurMySQL("localhost", "metro", "root", "root");

                var result = importeur.BuildStationGraph();

                graphe = new Graphe<Station>();
                foreach (var noeud in result.Noeuds)
                    graphe.AjouterNoeud(noeud);
                foreach (var lien in result.Liens)
                    graphe.AjouterLien(lien.Depart, lien.Arrivee, lien.Poids);

                Console.WriteLine($"[DEBUG] Graphe contient {graphe.Noeuds.Count} noeuds et {graphe.Liens.Count} liens.");

                Dispatcher.InvokeAsync(() =>
                {
                    DessinerCarte(graphe);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}");
            }
            finally
            {
                progressChemin.Visibility = Visibility.Collapsed;
            }
        }




        private void TesterCanvas()
        {
            // Crée un cercle rouge simple
            Ellipse cercle = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = Brushes.Red
            };

            // Place-le à x=100, y=100
            Canvas.SetLeft(cercle, 100);
            Canvas.SetTop(cercle, 100);

            // Ajoute-le au canvas
            canvasCarte.Children.Add(cercle);
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

            if (Math.Abs(maxLat - minLat) < 1e-6) maxLat += 0.0001;
            if (Math.Abs(maxLon - minLon) < 1e-6) maxLon += 0.0001;

            double largeur = canvasCarte.ActualWidth > 0 ? canvasCarte.ActualWidth : 1000;
            double hauteur = canvasCarte.ActualHeight > 0 ? canvasCarte.ActualHeight : 800;

            foreach (var noeud in graphe.Noeuds)
            {
                var s = noeud.Valeur;

                double x = (s.Longitude - minLon) / (maxLon - minLon) * largeur;
                double y = (maxLat - s.Latitude) / (maxLat - minLat) * hauteur;

                if (double.IsNaN(x) || double.IsNaN(y))
                    continue;

                Point p = new(x, y);
                positions[noeud] = p;

                var cercle = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = Brushes.White,
                    Stroke = Brushes.Black,
                    StrokeThickness = 0.5,
                    ToolTip = $"{s.Nom} (Ligne {s.Ligne})"
                };

                Canvas.SetLeft(cercle, x - 4);
                Canvas.SetTop(cercle, y - 4);
                canvasCarte.Children.Add(cercle);

                // Nom de la station affiché
                TextBlock label = new TextBlock
                {
                    Text = s.Nom,
                    FontSize = 10,
                    Foreground = Brushes.White
                };
                Canvas.SetLeft(label, x + 5); // Décalé à droite du point
                Canvas.SetTop(label, y - 5);  // Légèrement au-dessus
                canvasCarte.Children.Add(label);

                cerclesStations[noeud] = cercle;
            }

            foreach (var lien in graphe.Liens)
            {
                if (!positions.TryGetValue(lien.Depart, out Point posA) || !positions.TryGetValue(lien.Arrivee, out Point posB))
                    continue;

                string ligneNom = lien.Depart.Valeur.Ligne.Trim().ToLower();
                Brush couleur = Brushes.Gray;
                if (couleursLignes.TryGetValue(ligneNom, out var brush))
                    couleur = brush;

                var line = new Line
                {
                    X1 = posA.X,
                    Y1 = posA.Y,
                    X2 = posB.X,
                    Y2 = posB.Y,
                    Stroke = couleur,
                    StrokeThickness = 2,
                    Opacity = 0.6
                };

                canvasCarte.Children.Add(line);
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

        private void BtnAfficherChemin_Click(object sender, RoutedEventArgs e)
        {
            string departNom = txtDepart.Text.Trim();
            string arriveeNom = txtArrivee.Text.Trim();

            if (comboAlgorithme.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un algorithme.");
                return;
            }

            BtnReinitialiserCarte_Click(sender, null);

            var depart = graphe.Noeuds.FirstOrDefault(n => n.Valeur.Nom.Equals(departNom, StringComparison.OrdinalIgnoreCase));
            var arrivee = graphe.Noeuds.FirstOrDefault(n => n.Valeur.Nom.Equals(arriveeNom, StringComparison.OrdinalIgnoreCase));

            if (depart == null || arrivee == null)
            {
                MessageBox.Show("Stations introuvables.");
                return;
            }

            AfficherCheminPlusCourt(depart, arrivee);
        }

        private void AfficherCheminPlusCourt(Noeud<Station> depart, Noeud<Station> arrivee)
        {
            progressChemin.Visibility = Visibility.Visible;

            List<Noeud<Station>> chemin;
            List<string> pathNames = new();
            int cout = int.MaxValue;

            string algoChoisi = ((ComboBoxItem)comboAlgorithme.SelectedItem)?.Content?.ToString() ?? "";

            string nomDepart = depart.Valeur.Nom;
            string nomArrivee = arrivee.Valeur.Nom;

            try
            {
                var chrono = System.Diagnostics.Stopwatch.StartNew();

                // Mesurer Dijkstra
                var resultDijkstra = importeur!.Dijkstra(nomDepart, nomArrivee);
                chrono.Stop();
                long dijkstraTime = chrono.ElapsedMilliseconds;

                chrono.Restart();
                var resultBellman = importeur.BellmanFord(nomDepart, nomArrivee);
                chrono.Stop();
                long bellmanTime = chrono.ElapsedMilliseconds;

                chrono.Restart();
                var resultFloyd = importeur.FloydWarshall(nomDepart, nomArrivee);
                chrono.Stop();
                long floydTime = chrono.ElapsedMilliseconds;

                // 🎯 Comparer visuellement les algorithmes
                AfficherComparaisonAlgorithmes(dijkstraTime, bellmanTime, floydTime);

                // ➡️ Sélectionner le résultat selon l'algorithme choisi
                if (algoChoisi == "Dijkstra")
                {
                    cout = resultDijkstra.shortestTime;
                    pathNames = resultDijkstra.path;
                }
                else if (algoChoisi == "Bellman-Ford")
                {
                    cout = resultBellman.shortestTime;
                    pathNames = resultBellman.path;
                }
                else if (algoChoisi == "Floyd-Warshall")
                {
                    cout = resultFloyd.shortestTime;
                    pathNames = resultFloyd.path;
                }
                else
                {
                    MessageBox.Show("Algorithme inconnu sélectionné.");
                    progressChemin.Visibility = Visibility.Collapsed;
                    return;
                }

                chemin = pathNames
                    .Select(nom => graphe.Noeuds.FirstOrDefault(n => $"{n.Valeur.Nom} ({n.Valeur.Ligne})" == nom))
                    .Where(n => n != null)
                    .Select(n => n!)
                    .ToList();

                if (chemin.Count < 2)
                {
                    txtTempsTotal.Text = "";
                    txtListeStations.Text = "";
                    progressChemin.Visibility = Visibility.Collapsed;
                    return;
                }

                txtTempsTotal.Text = $"🕒 Temps estimé : {cout} min ({chemin.Count - 1} arrêt(s))\nTemps calcul Dijkstra: {dijkstraTime} ms | Bellman-Ford: {bellmanTime} ms | Floyd-Warshall: {floydTime} ms";
                txtListeStations.Text = "🧭 Itinéraire :\n" + FormatterCheminAvecCorrespondances(chemin);


                ZoomAutoSurChemin(chemin);
                AnimerChemin(chemin);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du calcul du chemin : {ex.Message}");
            }
            finally
            {
                progressChemin.Visibility = Visibility.Collapsed;
            }
        }


        private void AnimerChemin(List<Noeud<Station>> chemin)
        {
            int i = 0;
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };

            timer.Tick += (s, e) =>
            {
                if (i >= chemin.Count - 1)
                {
                    timer.Stop();
                    return;
                }

                var a = chemin[i];
                var b = chemin[i + 1];

                if (!positions.ContainsKey(a) || !positions.ContainsKey(b))
                {
                    i++;
                    return;
                }

                var ligne = canvasCarte.Children.OfType<Line>().FirstOrDefault(l =>
                    (Math.Abs(l.X1 - positions[a].X) < 1 && Math.Abs(l.Y1 - positions[a].Y) < 1 &&
                     Math.Abs(l.X2 - positions[b].X) < 1 && Math.Abs(l.Y2 - positions[b].Y) < 1) ||
                    (Math.Abs(l.X2 - positions[a].X) < 1 && Math.Abs(l.Y2 - positions[a].Y) < 1 &&
                     Math.Abs(l.X1 - positions[b].X) < 1 && Math.Abs(l.Y1 - positions[b].Y) < 1));

                if (ligne != null)
                {
                    var neonBrush = new SolidColorBrush(Colors.Red);
                    ligne.Stroke = neonBrush;

                    var colorAnim = new ColorAnimation
                    {
                        From = Colors.Red,
                        To = Colors.Orange,
                        Duration = TimeSpan.FromMilliseconds(500),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    };

                    neonBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);

                    ligne.StrokeThickness = 10;
                    ligne.Opacity = 1;

                    ligne.Effect = new DropShadowEffect
                    {
                        Color = Colors.Red,
                        BlurRadius = 60,
                        ShadowDepth = 0,
                        Opacity = 1
                    };
                }

                // ✨ Nouvelle partie : animer la station atteinte (b)
                if (cerclesStations.TryGetValue(b, out var ellipse))
                {
                    var growAnimation = new DoubleAnimation
                    {
                        From = 8,
                        To = 16,
                        Duration = TimeSpan.FromMilliseconds(250),
                        AutoReverse = true,
                        RepeatBehavior = new RepeatBehavior(2)
                    };

                    ellipse.BeginAnimation(Ellipse.WidthProperty, growAnimation);
                    ellipse.BeginAnimation(Ellipse.HeightProperty, growAnimation);
                }

                i++;
            };

            timer.Start();
        }





        private void BtnExporter_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtListeStations.Text))
            {
                MessageBox.Show("Aucun chemin à exporter.");
                return;
            }

            var cheminText = txtListeStations.Text.Replace("🧭 Itinéraire :\n", "");
            string cheminFichier = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "chemin_metro.txt");
            File.WriteAllText(cheminFichier, cheminText);

            MessageBox.Show($"Trajet exporté sur le bureau :\n{cheminFichier}");
        }


        private double zoomLevel = 1.0;

        private void CanvasCarte_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double zoomStep = 0.1;
            if (e.Delta > 0)
                zoomLevel += zoomStep;
            else if (e.Delta < 0)
                zoomLevel = Math.Max(zoomLevel - zoomStep, zoomStep);

            canvasCarte.LayoutTransform = new ScaleTransform(zoomLevel, zoomLevel);
        }


        private void ZoomAutoSurChemin(List<Noeud<Station>> chemin)
        {
            if (chemin.Count == 0)
                return;

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

            foreach (var noeud in chemin)
            {
                if (!positions.TryGetValue(noeud, out var pos))
                    continue;

                if (pos.X < minX) minX = pos.X;
                if (pos.X > maxX) maxX = pos.X;
                if (pos.Y < minY) minY = pos.Y;
                if (pos.Y > maxY) maxY = pos.Y;
            }

            double cheminWidth = maxX - minX;
            double cheminHeight = maxY - minY;

            if (cheminWidth == 0) cheminWidth = 1;
            if (cheminHeight == 0) cheminHeight = 1;

            double scaleX = scrollViewerCarte.ViewportWidth / cheminWidth;
            double scaleY = scrollViewerCarte.ViewportHeight / cheminHeight;
            double newZoom = Math.Min(scaleX, scaleY) * 0.8; // Petit facteur pour garder une marge visuelle

            // Appliquer le zoom
            zoomLevel = newZoom;
            canvasCarte.LayoutTransform = new ScaleTransform(zoomLevel, zoomLevel);

            // Centrer sur le chemin
            scrollViewerCarte.ScrollToHorizontalOffset(minX * zoomLevel - scrollViewerCarte.ViewportWidth / 2 + cheminWidth * zoomLevel / 2);
            scrollViewerCarte.ScrollToVerticalOffset(minY * zoomLevel - scrollViewerCarte.ViewportHeight / 2 + cheminHeight * zoomLevel / 2);
        }


        private void AfficherComparaisonAlgorithmes(long dijkstraTime, long bellmanTime, long floydTime)
        {
            canvasGraph.Children.Clear();

            double maxTime = Math.Max(dijkstraTime, Math.Max(bellmanTime, floydTime));
            if (maxTime == 0) maxTime = 1;

            double canvasWidth = canvasGraph.ActualWidth > 0 ? canvasGraph.ActualWidth : canvasGraph.Width;
            double canvasHeight = canvasGraph.Height;
            double baselineY = canvasHeight;

            int algoCount = 3;
            double totalBarWidth = canvasWidth * 0.8;
            double barWidth = totalBarWidth / (algoCount * 1.5);
            double espace = (canvasWidth - (barWidth * algoCount)) / (algoCount + 1);

            void AjouterBarre(int index, double hauteurFinale, Brush couleur, string label, long tempsMs)
            {
                double x = espace + index * (barWidth + espace);

                var rect = new Rectangle
                {
                    Width = barWidth,
                    Height = 0,
                    Fill = couleur
                };

                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, baselineY - hauteurFinale);
                canvasGraph.Children.Add(rect);

                var heightAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = hauteurFinale,
                    Duration = TimeSpan.FromSeconds(2),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };

                rect.BeginAnimation(Rectangle.HeightProperty, heightAnimation);

                // Label sous la barre
                var labelText = new TextBlock
                {
                    Text = label,
                    Foreground = Brushes.Black,
                    FontSize = 12,
                    TextAlignment = TextAlignment.Center,
                    Width = barWidth
                };
                Canvas.SetLeft(labelText, x);
                Canvas.SetTop(labelText, baselineY + 5);
                canvasGraph.Children.Add(labelText);

                // Valeur au-dessus de la barre
                var valueText = new TextBlock
                {
                    Text = $"{tempsMs} ms",
                    Foreground = Brushes.Black,
                    FontSize = 12,
                    TextAlignment = TextAlignment.Center,
                    Width = barWidth
                };
                Canvas.SetLeft(valueText, x);
                Canvas.SetTop(valueText, baselineY - hauteurFinale - 20); // 20 px au-dessus
                canvasGraph.Children.Add(valueText);
            }

            AjouterBarre(0, (dijkstraTime / maxTime) * canvasHeight, Brushes.Blue, "Dijkstra", dijkstraTime);
            AjouterBarre(1, (bellmanTime / maxTime) * canvasHeight, Brushes.Green, "BellmanF", bellmanTime);
            AjouterBarre(2, (floydTime / maxTime) * canvasHeight, Brushes.Red, "Floyd-W", floydTime);
        }


        private string FormatterCheminAvecCorrespondances(List<Noeud<Station>> chemin)
        {
            if (chemin.Count == 0) return "";

            var sb = new System.Text.StringBuilder();
            sb.Append(chemin[0].Valeur.Nom);

            for (int i = 1; i < chemin.Count; i++)
            {
                var precedente = chemin[i - 1].Valeur;
                var actuelle = chemin[i].Valeur;

                // S’il y a changement de ligne
                if (precedente.Ligne != actuelle.Ligne)
                {
                    sb.AppendLine();
                    sb.AppendLine($"    🔄 Correspondance : ligne {precedente.Ligne} ➜ ligne {actuelle.Ligne}");
                }

                sb.Append(" ➜ " + actuelle.Nom);
            }

            return sb.ToString();
        }


        private void BtnExporterStationsJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var chemin = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Stations.json");
                importeur!.ExporterStationsEnJson(chemin);
                MessageBox.Show($"✅ Stations exportées sur le bureau : {chemin}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur export JSON : {ex.Message}");
            }
        }

        private void BtnExporterCorrespondancesXml_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var chemin = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Correspondances.xml");
                importeur!.ExporterCorrespondancesEnXml(chemin);
                MessageBox.Show($"✅ Correspondances exportées sur le bureau : {chemin}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur export XML : {ex.Message}");
            }
        }


        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape)
            {
                // ✨ Revenir en mode fenêtre normale
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.SingleBorderWindow;
            }
        }


    }
}