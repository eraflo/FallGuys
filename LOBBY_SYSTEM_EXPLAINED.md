# Architecture du Système de Lobby & Découverte LAN
*Document technique pour la soutenance*

Ce document détaille le fonctionnement technique du système multijoueur (Lobby) implémenté dans le projet **Fall Guys Clone**. Il explique "Comment ça marche" sous le capot, en reliant les briques de code (Netcode) à l'Interface Utilisateur.

---

## 1. Vue d'Ensemble (Big Picture)

Le système repose sur **Unity Netcode for GameObjects (NGO)**. Il est divisé en trois couches distinctes :

1.  **La Couche Réseau (Back-End)** : Gère la connexion, l'état de la partie, et la synchronisation des données.
    *   Scripts : `LobbyManager.cs`, `LanDiscoveryManager.cs`
2.  **La Couche Données (Data)** : Structures simples pour transporter l'information.
    *   Scripts : `PlayerData` (struct), `LobbyEntry` (class).
3.  **La Couche Présentation (Front-End UI)** : Affiche les données au joueur et capture ses actions.
    *   Scripts : `LobbyUI.cs`, `LobbyListUI.cs`, `LobbyEntryUI.cs`.

---

## 2. Le Cœur du Réseau : `LobbyManager.cs`

C'est le chef d'orchestre. C'est un `NetworkBehaviour` présent sur la scène.

### Rôle 1 : Gestion de l'État (State Machine)
Il utilise une `NetworkVariable<LobbyState>` pour synchroniser l'état du jeu entre tous les joueurs.
*   **WaitingForPlayers** : On attend dans le salon.
*   **Countdown** : Tous les joueurs sont prêts, le compte à rebours se lance via une `NetworkVariable<float>`.
*   **GameLoading** : Le serveur charge la scène de jeu avec `NetworkSceneManager`.

### Rôle 2 : Gestion des Joueurs (`ConnectedPlayers`)
Il utilise une `NetworkList<PlayerData>`. C'est une liste magique : quand le Serveur ajoute/modifie un élément dedans, **tous les Clients** reçoivent automatiquement la mise à jour.
*   *Pourquoi une Struct ?* : `PlayerData` contient l'ID, le Nom et le statut "Prêt" (`IsReady`). Elle doit être sérialisable (`INetworkSerializable`) pour voyager sur le réseau.

### Rôle 3 : Connexion ("Smart Localhost")
Lors d'une connexion, le script détecte si le Client essaie de se connecter à sa propre adresse publique (ce qui échoue souvent à cause du routeur/NAT). Si c'est le cas, il redirige silencieusement vers `127.0.0.1` (Localhost) pour garantir la connexion.

---

## 3. Le Système de Découverte : `LanDiscoveryManager.cs`

C'est ce qui permet de voir les parties sans taper d'IP. Il utilise le protocole **UDP** (rapide, sans connexion) pour faire du "Broadcasting".

### Côté Serveur (Host)
*   **Action** : Toutes les secondes, il "crie" sur le réseau local (Port 47777) : *"Je suis le serveur X, voici mon IP !"*.
*   **Code** : `UdpClient.Send(bytes, broadcastEndPoint)`.

### Côté Client
*   **Action** : Il écoute en permanence le Port 47777.
*   **Réception** : Dès qu'il reçoit un message, il décode le JSON (`LobbyEntry`) et déclenche l'événement `OnLobbyFound`.

---

## 4. L'Interface Utilisateur (UI)

L'UI est purement **réactive** (Event-Driven). Elle ne fait jamais de calculs, elle ne fait qu'afficher ce que le `LobbyManager` lui dit.

### `LobbyUI.cs` (Le Chef d'Orchestre UI)
*   S'abonne aux événements du Manager : `OnPlayerListChanged`, `OnLobbyStateChanged`.
*   **Auto-Sync** : Si un joueur rejoint ou clique sur "Prêt", le `LobbyManager` met à jour la `NetworkList`. L'UI reçoit l'événement et redessine la liste instantanément.
*   **Navigation** : Gère l'affichage des panneaux (Connexion vs Lobby vs Navigateur) grâce à l'événement `OnConnectionStarted`.

### `LobbyListUI.cs` (Le Navigateur)
*   Écoute le `LanDiscoveryManager`.
*   Tient un Dictionnaire des serveurs trouvés (Clé = IP).
*   Pour chaque serveur trouvé, il instancie un bouton prefab (`LobbyEntryPrefab`).

### `LobbyEntryUI.cs` (La Ligne de Serveur)
*   Contient les données d'un serveur spécifique (IP, Port).
*   Bouton "Rejoindre" : Appelle `LobbyManager.StartClient(ip)` pour lancer la connexion précise vers cette machine.

---

## 5. Flux de Données (Data Flow) - Exemple Complet

Voici ce qui se passe quand **Joueur A** héberge et **Joueur B** rejoint :

1.  **A (Host)** clique sur "Créer".
    *   `LobbyManager` démarre le Host Netcode.
    *   `LanDiscoveryManager` commence à diffuser l'IP de A en UDP.
2.  **B (Client)** ouvre le "Navigateur".
    *   `LanDiscoveryManager` de B reçoit le message UDP de A.
    *   `LobbyListUI` crée un bouton pour A.
3.  **B** clique sur "Rejoindre A".
    *   `LobbyManager` de B se connecte à l'IP de A.
4.  **Connexion Établie**.
    *   Le Serveur A détecte B (`OnClientConnected`). Il ajoute B à la `NetworkList`.
5.  **Mise à jour UI**.
    *   La `NetworkList` change. L'événement part vers A et B.
    *   `LobbyUI` (chez A et B) redessine la liste -> B apparaît sur les deux écrans en même temps !

---

## Questions Possibles du Jury

**Q: Pourquoi utiliser une `NetworkList` et pas une liste normale ?**
R: Une liste normale (List<T>) reste locale. Une `NetworkList` gère toute la complexité de la synchronisation (Dirty bits, sérialisation delta) pour que tous les clients voient exactement la même chose au même moment.

**Q: Comment gérez-vous le "NAT Loopback" pour les tests locaux ?**
R: J'ai implémenté une logique qui compare l'IP cible avec l'IP locale. Si elles sont identiques, je force la connexion sur `127.0.0.1` pour éviter que le routeur ne bloque le paquet.

**Q: Votre système de découverte marche-t-il sur Internet ?**
R: Non, c'est du **Broadcast LAN** (réseau local). Les routeurs d'Internet bloquent le Broadcast. Pour jouer en ligne, il faudrait utiliser un "Relay Service" (comme Unity Relay) pour traverser les NAT.
