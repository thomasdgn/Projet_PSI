using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Projet_PSI_DELAROCHE_DEGARDIN_DARMON;
using static System.Collections.Specialized.BitVector32;
using MaStation = Projet_PSI_DELAROCHE_DEGARDIN_DARMON.Station;


namespace VisualisationWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Graphe<MaStation> graphe;
        private Dictionary<Noeud<MaStation>, Point> positions = new();
        private Dictionary<Noeud<MaStation>, Ellipse> cerclesStations = new();
        private bool modeNuitActif = false;

        public MainWindow(Graphe<MaStation> g)
        {
            graphe = g;
            InitializeComponent();
            // DessinerCarte(graphe);
        }


        private void BtnReinitialiser_Click(object sender, RoutedEventArgs e)
        {
            DessinerCarte(graphe);
            txtTempsTotal.Text = "";
        }

        private void ChkModeNuit_Checked(object sender, RoutedEventArgs e)
        {
            modeNuitActif = true;
            DessinerCarte(graphe);
        }

        private void ChkModeNuit_Unchecked(object sender, RoutedEventArgs e)
        {
            modeNuitActif = false;
            DessinerCarte(graphe);
        }

        private Brush ObtenirFondCarte() => modeNuitActif ? Brushes.Black : Brushes.WhiteSmoke;
        private Brush ObtenirCouleurTexte() => modeNuitActif ? Brushes.White : Brushes.Black;
        private Brush CouleurStationNormale() => modeNuitActif ? Brushes.Cyan : Brushes.DarkBlue;



        private readonly Dictionary<string, Brush> couleursParLigne = new()
        {
            { "1", Brushes.Goldenrod },
            { "2", Brushes.Blue },
            { "3", Brushes.Green },
            { "4", Brushes.MediumPurple },
            { "5", Brushes.Orange },
            { "6", Brushes.Teal },
            { "7", Brushes.Pink },
            { "8", Brushes.MediumSlateBlue },
            { "9", Brushes.OrangeRed },
            { "10", Brushes.DarkGoldenrod },
            { "11", Brushes.LightGreen },
            { "12", Brushes.DarkGreen },
            { "13", Brushes.DarkCyan },
            { "14", Brushes.HotPink },

        };

        private void DessinerCarte(Graphe<Station> graphe)
        {
            canvasCarte.Children.Clear();
            positions.Clear();
            cerclesStations.Clear();

            canvasCarte.Background = ObtenirFondCarte();

            double minLat = graphe.Noeuds.Min(n => n.Valeur.Latitude);
            double maxLat = graphe.Noeuds.Max(n => n.Valeur.Latitude);
            double minLon = graphe.Noeuds.Min(n => n.Valeur.Longitude);
            double maxLon = graphe.Noeuds.Max(n => n.Valeur.Longitude);

            double deltaLat = maxLat - minLat;
            double deltaLon = maxLon - minLon;
            if (deltaLat == 0) deltaLat = 0.0001;
            if (deltaLon == 0) deltaLon = 0.0001;

            double largeur = canvasCarte.ActualWidth > 0 ? canvasCarte.ActualWidth : canvasCarte.Width;
            double hauteur = canvasCarte.ActualHeight > 0 ? canvasCarte.ActualHeight : canvasCarte.Height;


            // Positionnement des stations
            foreach (var noeud in graphe.Noeuds)
            {
                var s = noeud.Valeur;

                double x = (s.Longitude - minLon) / deltaLon * (largeur - 40) + 20;
                double y = (1 - (s.Latitude - minLat) / deltaLat) * (hauteur - 40) + 20;
                var point = new Point(x, y);
                positions[noeud] = point;

                Ellipse cercle = new()
                {
                    Width = 10,
                    Height = 10,
                    Fill = CouleurStationNormale(),
                    Stroke = Brushes.White,
                    StrokeThickness = 1,
                    ToolTip = $"{s.Nom} (Ligne {s.Ligne})"
                };

                Canvas.SetLeft(cercle, x - 5);
                Canvas.SetTop(cercle, y - 5);
                canvasCarte.Children.Add(cercle);
                cerclesStations[noeud] = cercle;

                TextBlock nom = new()
                {
                    Text = s.Nom,
                    FontSize = 10,
                    Foreground = ObtenirCouleurTexte()
                };
                Canvas.SetLeft(nom, x + 6);
                Canvas.SetTop(nom, y - 6);
                canvasCarte.Children.Add(nom);
            }

            // Lignes (liaisons)
            foreach (var lien in graphe.Liens)
            {
                if (!positions.TryGetValue(lien.Depart, out Point posDep) ||
                    !positions.TryGetValue(lien.Arrivee, out Point posArr))
                    continue;

                if (double.IsNaN(posDep.X) || double.IsNaN(posDep.Y) ||
                    double.IsNaN(posArr.X) || double.IsNaN(posArr.Y))
                    continue;

                var ligne = new Line
                {
                    X1 = posDep.X,
                    Y1 = posDep.Y,
                    X2 = posArr.X,
                    Y2 = posArr.Y,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1.5
                };

                canvasCarte.Children.Add(ligne);
            }
        }







        private async Task AfficherCheminPlusCourtAsync(Noeud<Station> depart, Noeud<Station> arrivee)
        {
            var (chemin, cout) = graphe.Dijkstra(depart, arrivee);

            if (chemin == null || chemin.Count < 2)
            {
                txtTempsTotal.Text = "";
                return;
            }

            txtTempsTotal.Text = $"🕒 Temps estimé : {cout} min";

            var glow = new DropShadowEffect
            {
                Color = Colors.Red,
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 0.8
            };

            foreach ((Noeud<Station> a, Noeud<Station> b) in chemin.Zip(chemin.Skip(1), (x, y) => (x, y)))
            {
                if (!positions.ContainsKey(a) || !positions.ContainsKey(b)) continue;

                var ligne = new Line
                {
                    X1 = positions[a].X,
                    Y1 = positions[a].Y,
                    X2 = positions[b].X,
                    Y2 = positions[b].Y,
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

            // Station de départ (vert foncé)
            if (cerclesStations.ContainsKey(depart))
            {
                var ellipseDepart = cerclesStations[depart];
                ellipseDepart.Fill = Brushes.DarkGreen;
                ellipseDepart.Width = 12;
                ellipseDepart.Height = 12;
            }

            // Station d’arrivée (rouge clignotant)
            if (cerclesStations.ContainsKey(arrivee))
            {
                var ellipseArrivee = cerclesStations[arrivee];
                ellipseArrivee.Fill = Brushes.Red;
                ellipseArrivee.Width = 12;
                ellipseArrivee.Height = 12;

                var clignote = new DoubleAnimation
                {
                    From = 1,
                    To = 0.2,
                    Duration = TimeSpan.FromMilliseconds(500),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };

                ellipseArrivee.BeginAnimation(UIElement.OpacityProperty, clignote);
            }
        }





        private async void BtnAfficherChemin_Click(object sender, RoutedEventArgs e)
        {
            string nomDepart = txtDepart.Text.Trim();
            string nomArrivee = txtArrivee.Text.Trim();

            // Recherche dans le graphe avec Noeud<Station>
            var noeudDepart = graphe.Noeuds.FirstOrDefault(n =>
                n.Valeur.Nom.Equals(nomDepart, StringComparison.OrdinalIgnoreCase));

            var noeudArrivee = graphe.Noeuds.FirstOrDefault(n =>
                n.Valeur.Nom.Equals(nomArrivee, StringComparison.OrdinalIgnoreCase));

            if (noeudDepart == null || noeudArrivee == null)
            {
                MessageBox.Show("❌ Station de départ ou d’arrivée introuvable.");
                return;
            }

            // Appel de ta méthode corrigée
            await AfficherCheminPlusCourtAsync(noeudDepart, noeudArrivee);
        }





    }
}