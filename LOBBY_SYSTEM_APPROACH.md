# Approche pour la Finalisation du Système de Lobby (version simplifiée)

Ce document décrit l'approche de développement pour finaliser le système de lobby en utilisant une gestion d'états interne au `LobbyManager`, sans recours à l'architecture de machine à états basée sur les `ScriptableObjects`.

## 1. Modifications du Fichier `LobbyManager.cs`

Le script `LobbyManager.cs` a été refactorisé pour inclure toutes les fonctionnalités du lobby.

### A. Les États du Lobby (`enum LobbyState`)

Une énumération `LobbyState` a été définie directement dans le fichier `LobbyManager.cs` pour représenter les différentes phases du lobby :

```csharp
public enum LobbyState
{
    Offline,            // Hors ligne / non initialisé
    WaitingForPlayers,  // En attente de joueurs pour commencer
    Countdown,          // Compte à rebours avant le démarrage de la partie
    GameLoading         // Chargement de la scène de jeu
}
```

La variable réseau `NetworkVariable<LobbyState> CurrentLobbyState` synchronise l'état actuel du lobby entre le serveur et tous les clients.

### B. Données des Joueurs (`struct PlayerData` et `NetworkList<PlayerData>`)

Une structure `PlayerData` a été ajoutée pour encapsuler les informations de chaque joueur (ID client, nom, état "prêt") et est synchronisée via une `NetworkList<PlayerData>` nommée `ConnectedPlayers`.

```csharp
public struct PlayerData : INetworkSerializable, System.IEquatable<PlayerData>
{
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public bool IsReady;
    // ... (méthodes de sérialisation et d'égalité)
}

public NetworkList<PlayerData> ConnectedPlayers;
```

### C. Variables de Configuration et Réseau

Des champs `[SerializeField]` permettent de configurer le comportement du lobby depuis l'inspecteur Unity :
-   `_minPlayersToStart`: Nombre minimum de joueurs requis pour démarrer le compte à rebours.
-   `_countdownDuration`: Durée du compte à rebours.
-   `_gameSceneName`: Nom de la scène de jeu à charger.

Un `NetworkVariable<float> CountdownTimer` est utilisé pour synchroniser le temps restant du compte à rebours.

### D. Logique de la Gestion des États (Côté Serveur)

La méthode `Update()` du `LobbyManager` (exécutée uniquement sur le serveur) contient une instruction `switch` qui gère la logique de transition entre les états :

-   **`LobbyState.WaitingForPlayers`** :
    -   Vérifie si le nombre de joueurs connectés et "prêts" (`AllPlayersReady()`) est suffisant pour démarrer.
    -   Si oui, passe à l'état `Countdown` et initialise le timer.

-   **`LobbyState.Countdown`** :
    -   Décrémente `CountdownTimer`.
    -   Si le timer arrive à zéro, passe à l'état `GameLoading` et déclenche le chargement de la scène de jeu (`LoadGameScene()`).
    -   Si les conditions de démarrage ne sont plus remplies (joueurs déconnectés ou "non prêts"), retourne à `WaitingForPlayers`.

-   **`LobbyState.GameLoading`** :
    -   Cet état est transitoire et est géré par le `NetworkSceneManager` d'Unity après l'appel à `LoadGameScene()`.

### E. Gestion des Connexions et Déconnexions

Le serveur s'abonne aux événements `OnClientConnectedCallback` et `OnClientDisconnectedCallback` du `NetworkManager` pour ajouter ou supprimer les `PlayerData` de la `ConnectedPlayers` `NetworkList`.

### F. Communication Client-Serveur (RPC)

Une méthode `SetPlayerReadyServerRpc(ulong clientId, bool isReady)` a été ajoutée. Les clients pourront appeler cette `ServerRpc` pour informer le serveur de leur statut "prêt" ou "non prêt". Le serveur mettra à jour l'entrée correspondante dans la `ConnectedPlayers` `NetworkList`.

### G. Chargement de Scène

La méthode `LoadGameScene()` (exécutée sur le serveur) utilise `NetworkManager.Singleton.SceneManager.LoadScene()` pour initier le chargement de la scène de jeu sur tous les clients connectés.

### H. Mises à Jour de l'Interface Utilisateur (Côté Client)

Les clients s'abonnent aux événements `CurrentLobbyState.OnValueChanged` et `ConnectedPlayers.OnListChanged`. La méthode `UpdateLobbyUI()` est un point d'entrée pour implémenter la logique de mise à jour de l'interface utilisateur du lobby en fonction des changements d'état et de la liste des joueurs.

## 2. Prochaines Étapes Concrètes

1.  **Créer les Éléments UI du Lobby** : Mettre en place les éléments d'interface utilisateur pour afficher :
    *   L'état actuel du lobby (ex: "En attente...", "Compte à rebours...").
    *   La liste des joueurs connectés avec leur nom et leur statut "prêt".
    *   Un bouton "Prêt / Non Prêt" pour le joueur local.
    *   Le compte à rebours.
2.  **Implémenter `UpdateLobbyUI()`** : Écrire la logique dans cette méthode (ou une classe UI dédiée) pour que l'interface réagisse aux changements d'état et à la liste des joueurs.
3.  **Appeler `SetPlayerReadyServerRpc()`** : Connecter le bouton "Prêt" de l'UI pour qu'il appelle cette RPC lorsque le joueur change son statut.
4.  **Assurer le Bon Ordre de Chargement des Scènes** : Vérifier que la scène de jeu est correctement configurée dans les paramètres de build d'Unity.
5.  **Tests** : Tester minutieusement le flux du lobby avec plusieurs clients :
    *   Connexion / Déconnexion.
    *   Passage de l'état "non prêt" à "prêt".
    *   Lancement du compte à rebours.
    *   Annulation du compte à rebours si les conditions ne sont plus remplies.
    *   Chargement de la scène de jeu.