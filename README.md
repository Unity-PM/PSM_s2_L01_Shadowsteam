# Branching Strategies and Commit Naming
## Branching Strategy

EVERYTHING MADE SHOULD CREATE A PULL REQUEST

To keep the project organized and collaboration smooth, we follow a simple branching workflow:

### 1. Main Branches

- **main**  
  Contains stable, production-ready code. Only tested and reviewed features get merged here.

- **develop**  
  Integration branch for new features and bug fixes. Always kept in a deployable state but may contain work in progress.

### 2. Feature Branches

- Each new feature or task should be developed in its own branch created from `develop`.  
- Naming convention:  
  `feature/<short-description>`  
  Example: `feature/player-controller` or `feature/quest-system`

### 3. Bugfix Branches

- For fixes to bugs found in `develop` or `main`.  
- Naming convention:  
  `bugfix/<short-description>`  
  Example: `bugfix/ui-flicker` or `bugfix/enemy-spawner`

### 4. Hotfix Branches

- For urgent fixes on the `main` branch.  
- Naming convention:  
  `hotfix/<short-description>`  
  Example: `hotfix/crash-on-start`

### 5. Pull Requests

- Always create pull requests from feature, bugfix, or hotfix branches into `develop` or `main` accordingly.  
- Require code review before merging.

## Commit Naming Conventions

Consistent commit messages help with history tracking and collaboration. Use the following format: 

type(scope): short summary

- **type:** the kind of change  
- **scope:** the area affected (optional)  
- **short summary:** brief description (max 50 characters)

### Common Types

| Type       | Description                              | Example                           |
|------------|----------------------------------------|---------------------------------|
| feat       | New feature                            | feat(player): add sprint action  |
| fix        | Bug fix                               | fix(ui): correct health bar size |
| docs       | Documentation updates                 | docs: update README              |
| style      | Code style changes (formatting, etc.) | style: fix indentation           |
| refactor   | Code restructuring without behavior change | refactor(enemy): simplify AI    |
| chore      | Maintenance tasks (build, deps)        | chore: update dependencies       |
| test       | Add or fix tests                      | test(quest): add completion test |

---

### Examples

- `feat(audio): add background music system`  
- `fix(player): resolve movement jitter bug`  
- `docs: add branching strategy section`  
- `refactor(ui): modularize health bar component`  

## Summary

- Branches keep work isolated and organized  
- Commit messages follow a clear, consistent style  
- Use pull requests for reviews before merging  
