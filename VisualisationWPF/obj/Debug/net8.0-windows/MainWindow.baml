   M S B A M L   `   `   `  ����  VisualisationWPF!    VisualisationWPF.MainWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  Carte du métro��5   	   $
��1000��6    $
��1600��6.   ���#222.��5      ]� .+�5	   
   �� $	��Top=�6   ���#333 �� Padding$ 10q�68   ._�5
      ��  �� Orientation$ 
Horizontal=�6   $��Center=�62   .+�5      �� $��	Départ :��6   
�������$��Center=�6?   $��10q�6Z   �� 5      -   5         ��Name$ 	txtDepart��5      $	��150��6-   $
��0,10q�69   �� 5      $��
Arrivée :��6   
�������$��Center=�6@   $��10q�6[   �� 5      -   5        $ 
txtArrivee��5      $	��150��6.   $
��0,10q�6:   �� 5      -   5        $��Afficher chemin��5      $��10q�6S   $
��10,5q�6_   �� 5      -   5        $��Réinitialiser��5      $��10q�6U   $
��10,5q�6a   �� 5      -   5         ��Name$ comboAlgorithme��5      $	��150��64    �� SelectedIndex$ 0��6@   $��10,10q�6R   .@�5      �� .��6#   
Dijkstra6-   �� 5      .��6#   Bellman-Ford61   �� 5      .��6#   Floyd-Warshall63   5      �� 5      -   5        $��Exporter Trajet��5      $��10q�6M   $
��10,5q�6Y   �� 5      -   5        $��Exporter Stations JSON��5      $��10q�6`   $
��10,5q�6l   �� 5      -   5        $"��Exporter Correspondances XML��5      $��10q�6l   $
��10,5q�6x   � 5      -	   5         �Name$ progressChemin��5      $	��150��66   $��12��6B    � IsIndeterminate$
 True��6N   $y�	Collapsed=�6e   $��20q�6|   5      5      �� 5   
   $��Right=�6   $	��400��6,   $��10q�68   .+�5      �� -
   5         ��Name$ txtTempsTotal��5      $��14(�6/   
�������$
��Wrap=�6P   	 �� TextAlignment$
	 Left=�6d   $��10q�6y   �� 5       $	��200��6   $
��Auto=�6(   .��5!      �� -   5        $ txtListeStations��5!      $��12(�66   
�������$
	 Left=�6W   $
��Wrap=�6l   $��10q�6�   5"      �� 5$      $,��&Comparaison des algorithmes (temps ms)��6   $��14(�6F   
�������$��10q�6g   �� 5%      -   5        
 ��Name$
 canvasGraph��5%      $	��280��5&   	   $	��180��5'   	   
�������$
��0,10q�5)   	   $��Center=�5*   	   5,      �� 5/   
   -   5         ��Name$ scrollViewerCarte��5/      $
��Auto=�50      $
��Auto=�51      
���   �.��53      �� -   5        $
 canvasCarte��53      $
��3000��6*   $
��2000��67   
���   �6E   54      55      56      