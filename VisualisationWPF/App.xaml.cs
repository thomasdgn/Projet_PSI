using System.Configuration;
using System.Data;
using System.Windows;
using DocumentFormat.OpenXml.Bibliography;
using Projet_PSI_DELAROCHE_DEGARDIN_DARMON;
using MaStation = Projet_PSI_DELAROCHE_DEGARDIN_DARMON.Station;


namespace VisualisationWPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Création et chargement du graphe à partir de la base MySQL
            var graphe = new Graphe<MaStation>();
            string chaineConnexion = "server=localhost;user=root;password=root;database=metro;";
            ImporteurMySQL.Charger(chaineConnexion, graphe);

            // Lancement de la fenêtre principale avec le graphe
            var fenetre = new MainWindow(graphe);
            fenetre.Show();
        }
    }
}
