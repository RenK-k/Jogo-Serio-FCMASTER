# Career Hub Academy and Navigation Fixes

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make daily conversations skippable, make each academy age group a usable squad, correct 4-4-2 wide-midfield roles, and return from a club dossier to its originating news story.

**Architecture:** Keep the changes in the existing career and presentation flows. Persist academy tactical identities per age group, share the senior team’s tactics UI behavior where practical, and retain an explicit callback for dossier navigation back to a news story.

**Tech Stack:** Unity C#, uGUI, existing CareerStore save format.

**Spec:** User request from 2026-10-21 in this conversation.

## Global Constraints

- Preserve existing career, senior-squad, match, and news behavior.
- Do not control the Unity Editor or the user’s computer.
- Do not add or run tests; verify edits by source inspection, diff checks, and script compilation only.
- Promotion is available from age 16.

## Review Focus

- Skipping conversations must preserve fixtures and other day events.
- Academy tactics and lineups must remain separate per age group and survive saves.
- Returning from a club dossier opened in a news story must reconstruct that same story popup.
- A 4-4-2 must display and select wide midfielders as midfield roles, while other formations retain their winger roles.

---

### Task 1: Skip unresolved daily conversations

**Files:** `Career/CareerMoments.cs`, `UI/Scenes/CareerHubController.cs`

- [x] Add a domain operation that marks the current talk handled and removes only remaining same-day talk moments, preserving notes and fixtures.
- [x] Add a localized home-page button visible only while a talk is waiting; route it through the normal Continue flow and save.
- [x] Wire both generated and designed Home pages.

### Task 2: Full academy squads and promotion

**Files:** `Career/CareerState.cs`, `Career/CareerStore.cs`, `UI/Scenes/CareerHubController.cs`

- [x] Make the default academy view the 11-player lineup, with separate substitutes and reserves below it; enlarge cards to fit the field.
- [x] Add academy tactics/formation controls per age group, persist them, and use them for youth lineups and fixtures.
- [x] Keep a full squad view with player sheets and a clear promotion action for eligible players aged 16+.
- [x] Use the modern club dossier for youth-club reports, retaining the selected age group and youth roster context.

### Task 3: 4-4-2 wide midfield roles

**Files:** `Domain/Enums.cs`, `Career/StartingShape.cs`, `Domain/PlayerPositionProfile.cs`, `Presentation/PresentationLabels.cs`, related tactical role scoring.

- [x] Add left/right midfielder roles, use them only in 4-4-2 wide slots, and give midfielders/wingers sensible fit scores and localized labels.
- [x] Keep the two strikers in the central forward slots and slightly enlarge squad cards without pushing players outside the pitch.

### Task 4: News dossier return navigation

**Files:** `UI/Scenes/CareerHubController.cs`

- [x] Add an explicit return action when opening a club dossier from a news story.
- [x] Preserve that action while switching dossier tabs and invoke it from the dossier close button.
- [x] Keep normal dossier close behavior for entry points without a parent popup.

### Verification

- [x] Inspect changed paths and run `git diff --check`.
- [x] Attempt script compilation without launching Unity; report the environment limitation when the compiler is unavailable.
