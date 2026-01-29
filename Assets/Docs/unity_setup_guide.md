# 🎮 Guide de Configuration Final - Fall Guys

Ce document récapitule toutes les étapes nécessaires pour finaliser la boucle de jeu, de l'édition VR du niveau à la compétition multijoueur.

---

## 🛠️ 1. Synchronisation des Projets
Le package `com.eraflo.common` est partagé entre l'**Editeur** et le **Jeu**.
> [!IMPORTANT]
> Après les dernières modifications sur les overrides et le Blackboard, vous **devez** copier le contenu de :
> `FallGuys/Packages/com.eraflo.common` -> `FallGuysEditor/Packages/com.eraflo.common`

Cela garantit que l'Editeur reconnaît les attributs `LevelEditable` et que la sauvegarde inclut bien toutes les données nécessaires.

---

## 🏗️ 2. Configuration de l'Editeur (VR)

### A. Logic Registry
Assurez-vous que le fichier `LogicRegistry` (dans `Assets/Resources`) de l'Editeur contient les entrées suivantes pour que les objets soient fonctionnels au spawn :
- `Jumper` : Pointe vers le `SimpleBehaviourSO` du Jumper.
- `MovingPlatform` : Pointe vers le `StateConfigSO` de la plateforme mobile.
- `Bumper` : Pointe vers le `SimpleBehaviourSO` du Bumper.
- `Blower` : Pointe vers le `SimpleBehaviourSO` du Blower.
- `Launcher` : Pointe vers le `StateConfigSO` du Launcher.
- `StartArea`, `Checkpoint`, `FinishArea` : Pointent vers leurs comportements respectifs.

### B. Objets de Niveau (`ObjectSO`)
Pour chaque objet que vous voulez rendre éditable, vérifiez que son scriptable object (`ObjectSO`) utilise les champs `LevelEditable`.
- **Note** : L'`index` du checkpoint est masqué dans l'inspecteur VR mais sera calculé automatiquement lors du clic sur **Save**.

---

## 🏁 3. Configuration du Jeu (Multi)

### A. Scène Lobby
La scène `Lobby` doit contenir :
1.  **NetworkManager** : Configuré avec `UnityTransport`.
2.  **LobbyManager** :
    *   `Min Players To Start` : Nombre minimum de joueurs prêts pour lancer le décompte.
    *   `Game Scene Name` : Doit correspondre exactement au nom de votre scène de jeu (ex: `GameScene`).
3.  **UI Lobby** :
    *   Un `LobbyLevelSelector` pour permettre à l'Host de choisir le fichier `.json` du niveau à charger.

### B. Scène de Jeu
La scène de jeu (ex: `GameScene`) doit être **vide d'objets de niveau**. Ils seront spawnés dynamiquement par le `LevelLoader`.
Elle doit cependant contenir :
1.  **GameManager** :
    *   `Level Loader` : Référence vers le composant `LevelLoader` de la scène.
    *   `Game Scene Name` & `Lobby Scene Name` : Pour les transitions.
2.  **LevelLoader** :
    *   `Base Object Prefab` : Le prefab contenant `NetworkObject`, `BaseObject` et `ObjectBehaviourDriver`.
3.  **UI Fin de Course** :
    *   Un canvas avec le script `EndRaceUI` pour afficher le classement.

---

## 🔄 4. Flux de Travail Nominal

1.  **Build de l'Editeur** : Créez votre niveau en VR, placez les pièges, ajustez les forces.
2.  **Sauvegarde** : Cliquez sur Save. Le fichier `.json` est généré dans `persistentDataPath/Saves`.
3.  **Lancement du Jeu** :
    *   L'Host lance le serveur.
    *   L'Host sélectionne le niveau via le `LobbyLevelSelector`.
    *   Tous les joueurs se mettent "Prêt".
4.  **Course** :
    *   Le `GameManager` charge la scène, demande au `LevelLoader` de spawner les objets.
    *   Les joueurs apparaissent.
    *   Le franchissement de la `StartArea` lance le chrono.
    *   Le franchissement de la `FinishArea` enregistre le score et affiche le tableau de bord final après un délai.

---

## 💡 Rappel technique sur le Blackboard
Chaque objet spawn avec un **Blackboard local**. 
- Si vous avez défini un override dans l'Editeur (ex: `_jumpStrength = 15`), cette valeur est injectée dans le Blackboard au spawn.
- Les scripts de comportement (`JumperBehaviourSO`, etc.) font toujours : 
  `blackboard.Get<float>("_jumpStrength", config.JumpStrength)`.
- Si l'override existe, il gagne. Sinon, la valeur par défaut du scriptable object est utilisée.
