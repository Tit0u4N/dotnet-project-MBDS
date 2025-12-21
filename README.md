# Plateforme de distribution de contenu + Editeur

- [Titouan LACOMBE--FABRE](mailto:titouan.lacombre--fabre.etu.unice.fr)
  ([Tit0u4N](https://github.com/Tit0u4N))
- [Tamas PALOTAS](mailto:tamas.palotas@etu.unice.fr)
  ([Shiyamii](https://github.com/Shiyamii))

Sujet choisie : **Client Lourd**

## But

Construire un web service avec son client Windows pour gérer une plateforme de distribution de contenu limitée aux jeux
vidéo.

Ajouter à celui-ci un jeu multijoueur comprenant le serveur ainsi que le jeu correspondant.

## Setup requis

- Ajouter le fihier `default.zip` dans `Gauniv.WebServer/GameUploads/` pour le bon fonctionnement de l'application. Le
  fichier peut être un binaire zipé ou non mais doit exister. (si il n'est pas zipé il ne pourra pas etre lancé depuis
  le client)

# Plateforme de distribution de contenu (ASP.NET)

L'objectif est de créer une plateforme de distribution de contenu (jeux vidéo) similaire à Steam. Ainsi que
d'implementer des foncitonnalités d'administration pour gérer les jeux et les catégories.

## Todo liste

- Un administrateur doit pouvoir:
  - [x]  Ajouter des jeux
  - [x]  Upload
  - [x]  Supprimer des jeux
  - [x]  Modifier un jeu
  - [x]  Ajouter de nouvelles catégories
  - [x]  Modifier une catégorie
  - [x]  Supprimer une catégorie
- Un utilisateur doit pouvoir:
  - [x]  Consulter la liste des jeux possédés
  - [x]  Acheter un nouveau jeu
  - [x]  Voir les jeux possédés
  - [x]  Consulter la liste des autres joueurs inscrits et leurs statuts en temps réel
- Tout le monde peut:
  - [x]  Consulter la liste de toutes les catégories
  - Consulter la liste de tous les jeux avec ou sans filtre
      - [x]  nom
      - [x]  prix
      - [x]  catégorie
      - [x]  possédé
      - [x]  taille

### **Options**

- Afficher des filtres dans la liste des jeux pour filtrer par
  - [x]  catégorie
  - [x]  prix
  - [x]  possédé.
- Une page affichant les statistiques sur :
  - [x]  Le nombre total de jeux disponibles
  - [x]  Le nombre de jeux par catégorie
  - [x]  Le nombre moyen de jeux joués par compte
  - [x]  Le temps moyen joué par jeu
  - [x]  Le maximum de joueurs en simultané sur la plateforme et par jeu
- [x]  Un jeu pouvant faire plusieurs Gio, il est nécessaire de pouvoir les stocker sur autre chose qu’une base de
  données classique. Trouver et mettre en place un mécanisme pour stocker les jeux hors de la BDD.
- [x]  Stocker sur la machine
- [x]  En suivant le même principe, il est nécessaire de ne pas stocker l’ensemble du fichier en mémoire avant de
  l’envoyer. Streamer le binaire en direct pour réduire l’empreinte mémoire de votre serveur.

## Partocularités techniques et metiers

Lors de la premier lancement sur un BDD postgreSQL vide, en plus de créer les tables plus de 5000 jeux venant d'un
dataset de gog.com seront insérés dans la base de données. Mais les jeux n'on pas de fichier binaire associé, le zip
`default.zip` sera utilisé à la place.

Il n'existe pas de filtre "jeux possédés" car il y a une page `Shop`avec tout les jeux et une page `MyGames` avec
seulement les jeux possédés par l'utilisateur.

Il est imposible de supprimer une categorie si au moins un jeu y est rattaché.

# Application (WPF, MAUI, WINUI)

## Todo liste

- Lister les jeux
  - [x]  Lister les jeux (vous pouvez définir la limite comme bon vous semble)
  - [x]  Incluant la pagination (scroll infini, bouton ou autres)
  - [x]  Filtrer par jeux possédés / catégorie / prix / …
- Lister les jeux possédés
  - [ ]  Lister les jeux possédés par le joueur (vous pouvez définir la limite comme bon vous semble)
  - [ ]  Incluant la pagination (scroll infini, bouton ou autres)
  - [ ]  Filtrer par jeux possédés / catégorie / prix / …
- Afficher les détails d’un jeu 
  - [ ] nom, description, statuts, catégories)
  - [ ]  Description
  - [ ]  Statuts (acheté / non acheté, téléchargé / non téléchargé, en cours de jeu, …)
  - [ ]  Categories
-  Télécharger, supprimer et lancer un jeu
  - [x]  L’utilisateur ne devra pas voir les boutons "jouer" et "supprimer" si le jeu n’a pas été téléchargé
  - [ ]  De même, le bouton "télécharger" ne sera pas visible si le jeu est déjà disponible
  - [ ]  Jouer à un jeu
  - [ ]  Visualiser l’état du jeu (non téléchargé, prêt, en jeu, …)
  - [ ]  Contrôler le jeu (lancement, arrêt forcé, …)
- [x]  Voir et mettre à jour son profil d’application (dossier d’installation, identifiants, …)

### **Options**

- [ ]  Afficher la description avec un formatage : style de police, couleur, taille du texte, ...
- [ ]  Penser au RTF, HTML, PDF, ...
- [ ]  Dans un premier temps, gérez uniquement un format. Si vous avez fini, vous pouvez gérer plusieurs formats en même
  temps
- [ ]  Lire la description grâce à
  l'API[System.Speech.SpeechSynthesizer](https://learn.microsoft.com/en-us/dotnet/api/system.speech.synthesis.speechsynthesizer?view=net-9.0)
- [ ]  Gérer la lecture / l'arrêt / la pause / la reprise
- [ ]  Changer les boutons de contrôle en fonction de l'état de la lecture (comme un lecteur vidéo, ex : YouTube)
- [ ]  Commencer à lire à partir de la sélection de l'utilisateur. L'utilisateur doit pouvoir faire un clic droit sur un
  mot et lancer la lecture à partir de ce mot