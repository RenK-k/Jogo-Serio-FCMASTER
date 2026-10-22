# SDD ledger — plan: docs/superpowers/plans/2026-10-21-career-hub-academy.md

Task 1: complete — added a same-day talk skip action that removes only talk moments and leaves other events intact.
Task 2: complete — each age group has a saved tactics view, lineup, substitutes/reserves, roster sheets, age-16+ promotion, and youth club dossier context; promoted players are not re-added to the academy.
Task 3: complete — 4-4-2 uses two central strikers and left/right midfielders; role names and card labels follow the selected language.
Task 4: complete — closing a club dossier opened from news reconstructs the same story popup; other dossier entry points close normally.

Verification: `git diff --check` passed. A source compile was attempted without opening Unity; the bundled legacy `mcs` compiler rejects existing local-function syntax in untouched `FootballMemory.cs` and reports an existing name-scope error in untouched `GameAudio.cs`, so this compiler cannot confirm the full project. No tests or editor UI were run.

Ruling: No tests will be added or run because the workspace instructions explicitly prohibit it; use source-level checks and compilation instead.
Ruling: Edit the active workspace on `main` because the user explicitly requested implementation in this project and this checkout contains unrelated uncommitted work that must be preserved.
