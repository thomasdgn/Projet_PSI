using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Projet_PSI_DELAROCHE_DEGARDIN_DARMON
{
    [TestClass]
    public class CheminPlusCourtTests
    {
        private ImporteurMySQL importeur;

        [TestInitialize]
        public void Initialize()
        {
            // Initialiser l'importeur avec les paramètres de connexion
            importeur = new ImporteurMySQL("localhost", "metro", "root", "root");
        }

        [TestMethod]
        public async Task TestDijkstra_CheminExistant()
        {
            // Arrange
            string depart = "Châtelet";
            string arrivee = "Nation";

            // Act
            var (temps, chemin) = await importeur.Dijkstra(depart, arrivee);

            // Assert
            Assert.IsTrue(temps > 0, "Le temps devrait être positif pour un chemin existant");
            Assert.IsTrue(chemin.Count > 0, "Le chemin devrait contenir au moins une station");
            Assert.IsTrue(chemin[0].StartsWith(depart), $"Le chemin devrait commencer par {depart}");
            Assert.IsTrue(chemin[chemin.Count - 1].StartsWith(arrivee), $"Le chemin devrait se terminer par {arrivee}");

            Console.WriteLine($"Temps total: {temps} minutes");
            Console.WriteLine("Chemin: " + string.Join(" -> ", chemin));
        }

        [TestMethod]
        public async Task TestBellmanFord_CheminExistant()
        {
            // Arrange
            string depart = "Châtelet";
            string arrivee = "Nation";

            // Act
            var (temps, chemin) = await importeur.BellmanFord(depart, arrivee);

            // Assert
            Assert.IsTrue(temps > 0, "Le temps devrait être positif pour un chemin existant");
            Assert.IsTrue(chemin.Count > 0, "Le chemin devrait contenir au moins une station");
            Assert.IsTrue(chemin[0].StartsWith(depart), $"Le chemin devrait commencer par {depart}");
            Assert.IsTrue(chemin[chemin.Count - 1].StartsWith(arrivee), $"Le chemin devrait se terminer par {arrivee}");

            Console.WriteLine($"Temps total: {temps} minutes");
            Console.WriteLine("Chemin: " + string.Join(" -> ", chemin));
        }

        [TestMethod]
        public async Task TestFloydWarshall_CheminExistant()
        {
            // Arrange
            string depart = "Châtelet";
            string arrivee = "Nation";

            // Act
            var (temps, chemin) = await importeur.FloydWarshall(depart, arrivee);

            // Assert
            Assert.IsTrue(temps > 0, "Le temps devrait être positif pour un chemin existant");
            Assert.IsTrue(chemin.Count > 0, "Le chemin devrait contenir au moins une station");
            Assert.IsTrue(chemin[0].StartsWith(depart), $"Le chemin devrait commencer par {depart}");
            Assert.IsTrue(chemin[chemin.Count - 1].StartsWith(arrivee), $"Le chemin devrait se terminer par {arrivee}");

            Console.WriteLine($"Temps total: {temps} minutes");
            Console.WriteLine("Chemin: " + string.Join(" -> ", chemin));
        }

        [TestMethod]
        public async Task TestDijkstra_CheminInexistant()
        {
            // Arrange
            string depart = "StationInexistante";
            string arrivee = "Nation";

            // Act
            var (temps, chemin) = await importeur.Dijkstra(depart, arrivee);

            // Assert
            Assert.AreEqual(-1, temps, "Le temps devrait être -1 pour un chemin inexistant");
            Assert.AreEqual(0, chemin.Count, "Le chemin devrait être vide");
        }

        [TestMethod]
        public async Task TestBellmanFord_CheminInexistant()
        {
            // Arrange
            string depart = "StationInexistante";
            string arrivee = "Nation";

            // Act
            var (temps, chemin) = await importeur.BellmanFord(depart, arrivee);

            // Assert
            Assert.AreEqual(-1, temps, "Le temps devrait être -1 pour un chemin inexistant");
            Assert.AreEqual(0, chemin.Count, "Le chemin devrait être vide");
        }

        [TestMethod]
        public async Task TestFloydWarshall_CheminInexistant()
        {
            // Arrange
            string depart = "StationInexistante";
            string arrivee = "Nation";

            // Act
            var (temps, chemin) = await importeur.FloydWarshall(depart, arrivee);

            // Assert
            Assert.AreEqual(-1, temps, "Le temps devrait être -1 pour un chemin inexistant");
            Assert.AreEqual(0, chemin.Count, "Le chemin devrait être vide");
        }

        [TestMethod]
        public async Task CompareAlgorithmes_MemeResultat()
        {
            // Arrange
            string depart = "Châtelet";
            string arrivee = "Nation";

            // Act
            var resultDijkstra = await importeur.Dijkstra(depart, arrivee);
            var resultBellmanFord = await importeur.BellmanFord(depart, arrivee);
            var resultFloydWarshall = await importeur.FloydWarshall(depart, arrivee);

            // Assert
            Assert.AreEqual(resultDijkstra.shortestTime, resultBellmanFord.shortestTime,
                "Dijkstra et Bellman-Ford devraient donner le même temps");
            Assert.AreEqual(resultDijkstra.shortestTime, resultFloydWarshall.shortestTime,
                "Dijkstra et Floyd-Warshall devraient donner le même temps");

            Console.WriteLine($"Temps Dijkstra: {resultDijkstra.shortestTime} minutes");
            Console.WriteLine($"Temps Bellman-Ford: {resultBellmanFord.shortestTime} minutes");
            Console.WriteLine($"Temps Floyd-Warshall: {resultFloydWarshall.shortestTime} minutes");
        }

   
    }
}