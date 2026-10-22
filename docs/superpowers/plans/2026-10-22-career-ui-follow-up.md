# Career UI Follow-up Implementation Plan

> **For agentic workers:** Execute inline; preserve existing work and save data compatibility.

**Goal:** Fix the academy squad interactions, make finance and club dossiers visual and compact, simplify global transfer sorting, and let the coach skip a complete non-match day.

**Architecture:** Keep the existing runtime-built uGUI and career-state flows. Add only the data needed to persist youth lineup/bench ordering and use small reusable uGUI graphics for donut charts and trophy marks. Keep match access explicit while club links open dossiers directly.

**Tech Stack:** Unity C#, uGUI, existing `CareerStore` text save format.

**Spec:** User request on 2026-10-22 in this conversation.

## Global Constraints

- Preserve existing career, fixture, finance ledger, and transfer behavior.
- Do not control Unity or the user's computer.
- Do not add or run tests; the user will test the game. Use source inspection and `git diff --check` only.
- Keep the club performance and history views compact; scroll only when content exceeds the available panel.

## Review Focus

- Academy first paint must contain all 11 players and cards for the selected bench group.
- Drag swaps and reserve/substitute selection must persist across page rebuilds and saves.
- A club name in the calendar opens its dossier; match details remain reachable separately.
- Finance charts must handle zero income/expenses without invalid or invisible geometry.
- Skipping a day must preserve unplayed matches and advance systems for the elapsed day.

---

### Task 1: Academy first paint and squad exchanges

**Files:** `CareerState.cs`, `CareerStore.cs`, `UI/Scenes/CareerHubController.cs`, `UI/SquadDragCard.cs`

- [x] Rebuild academy content after the initial layout pass so the first visit shows the XI and bench cards.
- [x] Place substitutes/reserves beneath the vertical lineup.
- [x] Persist academy XI and bench order; wire drag swaps and the requested double-click reserve-to-substitute selection flow.

### Task 2: Visual finance detail

**Files:** `UI/Scenes/CareerHubController.cs`, new `UI/FinanceDonutGraphic.cs` and Unity `.meta`

- [x] Replace the text-only modal with Overview, Transactions, and Budget views modeled on the supplied references.
- [x] Add a clear green/red donut and category breakdown for profit, earnings, and expenses.
- [x] Keep the existing detailed finance ledger visible in Transactions.

### Task 3: Direct club dossier and compact analytics/history

**Files:** `CareerState.cs`, `CareerStore.cs`, `UI/Scenes/CareerHubController.cs`, optional new trophy graphic and `.meta`

- [x] Make calendar card clicks open the club dossier and provide a separate match-details control.
- [x] Gate opponent performance charts behind an analysis action; remove the Facilities tab.
- [x] Fit performance graphs and tactical fit in one panel; show club events and drawn trophy cards in History.

### Task 4: Global transfer controls

**Files:** `UI/Scenes/CareerHubController.cs`

- [x] Replace club filtering with a season dropdown.
- [x] Add alternating fee and date sort controls, defaulting to high fees and most recent first.

### Task 5: Skip the complete day

**Files:** `Career/CareerMoments.cs`, `UI/Scenes/CareerHubController.cs`

- [x] Replace the conversation-only action with a localized Skip Day action that clears same-day non-match events and advances to the next day.
- [x] Keep the action unavailable when an unplayed match is scheduled that day; advance player systems and save normally.

### Verification

- [x] Inspect touched methods and ensure saved state still loads old saves.
- [x] Run `git diff --check`; do not launch Unity or run tests.
