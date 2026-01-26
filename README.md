# Architecture modulaire

## 🧠 Laby.Core (lib)
**Modèle métier pur et règles stables**

- `Maze`, `Cell`, `Door`, `Key`, `Position`
- `CrawlerState`
- Règles d’ouverture, inventaire, logique métier
- **Zéro dépendance externe**

## 📦 Laby.Contracts (lib)
**Contrats partagés client / serveur**

- DTOs
- Messages API
- Types de commandes et résultats
- Évite le couplage du client à l’implémentation serveur

## 🤖 Laby.Algorithms (lib)
**Algorithmes et stratégies**

- BFS / A*
- Exploration
- Gestion des objectifs
- Heuristiques
- Dépend uniquement de **Laby.Core**

## 🗺️ Laby.Mapping (lib)
**Vision partagée du labyrinthe**

- Fusion d’observations
- Carte partielle / complète
- Gestion des inconnus
- Thread-safe  
  _(ex : `ConcurrentDictionary` + règles de fusion)_

## 🧩 Laby.Application (lib)
**Orchestration des agents**

- `ExplorerCoordinator`
- Gestion multi-crawlers
- Plan commun
- Choix des actions
- Dépend de **Core**, **Algorithms**, **Mapping**, **Contracts**

## 🔌 Laby.Infrastructure (lib)
**Implémentations techniques**

- Clients HTTP
- Sérialisation
- Persistance éventuelle
- Horloge abstraite
- Dépend de **Contracts** + bibliothèques externes

## 💻 Laby.Client.Console (app)
**Client console**

- Création des crawlers
- Lancement de l’exploration locale ou distante
- Dépend de **Application** + **Infrastructure**

## 🌐 Laby.Server.Training (app)
**API minimale ASP.NET**

- Endpoints conformes à Swagger
- Labyrinthes pré-définis
- Dépend de **Core** + **Contracts**  
  _(optionnellement Infrastructure)_

## 🧪 Tests

### Laby.Tests
- Tests unitaires
- Priorité sur **Core**, **Algorithms**, **Mapping**

### Laby.IntegrationTests (optionnel)
- Tests de l’API serveur
- Scénarios complets d’exploration

## 🔒 Règles de dépendances (règle d’or)

- **Core** ne dépend de rien
- **Algorithms**, **Mapping** → dépendent uniquement de **Core**
- **Application** → dépend de **Core**, **Algorithms**, **Mapping**, **Contracts**
- **Apps** → dépendent de **Application** ou de **Core** + **Contracts**
- **Infrastructure** → fournit les implémentations techniques et dépend de **Contracts**