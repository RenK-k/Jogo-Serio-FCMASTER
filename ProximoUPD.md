# Coach Master: Audit Custom Physics and Evaluate Unity Rigidbody2D

Inspect the existing Coach Master project and determine whether its custom movement and physics systems should be replaced or partially integrated with Unity's built-in Rigidbody2D physics engine.

The game currently has several gameplay problems:
- The ball sometimes travels in unintended directions.
- The ball does not always remain correctly attached to the player's feet during possession.
- The ball can leave the pitch and remain there until the match ends.
- Players and goalkeepers sometimes teleport or move unnaturally.
- Collisions, ball movement and possession transitions may be inconsistent.

Do not immediately rewrite the engine or add Rigidbody2D components indiscriminately. First inspect the existing implementation and identify the root causes.

## 1. Audit the current architecture

Inspect the ball controller, player movement, AI, possession system, collisions, goalkeeper behaviour, match state machine, passing and shooting calculations, animation, coordinate systems, physics settings and time-step handling.

Determine which systems currently control object positions and velocities.

Identify conflicts between Transform updates, custom physics, Rigidbody2D, animation and tactical movement. Check whether the same object is being moved or updated by multiple systems.

Explain whether the current implementation is a custom physics engine, a wrapper around Unity physics, or a hybrid.

## 2. Evaluate Unity Rigidbody2D

Determine whether Unity's built-in 2D physics system would improve the current implementation.

Consider using:
- Rigidbody2D and Collider2D for the ball.
- Physics-based velocity, collisions, friction and rebounds.
- Controlled forces or impulses for passes and shots.
- Appropriate collision detection and interpolation.
- Kinematic Rigidbody2D bodies or controlled movement for players where suitable.
- Custom AI and tactical logic for player decisions and positioning.
- Custom football rules and match-state logic independent of the physics engine.

Choose the architecture that best fits the existing game rather than forcing every object to use the same physics model.

## 3. Fix ball control and possession

Ensure that:
- The ball remains correctly associated with a player's feet during controlled dribbling.
- Possession transfers reliably.
- Physics does not fight the possession system.
- The ball receives the correct velocity when released.
- Passes, shots, tackles and rebounds transition between states correctly.
- The ball does not retain stale velocity or receive forces multiple times.
- The ball cannot be controlled simultaneously by multiple players.

Use a deliberate possession state in which ball control is managed explicitly. When the ball is released, transition cleanly to free-ball physics.

Do not rely on collisions alone to simulate dribbling.

## 4. Fix passing and shooting

Passing is currently too slow.

Find the actual cause before modifying values. Check velocity calculations, friction, drag, time scaling, physics updates, animation interpolation and player attribute modifiers.

Make short passes responsive, medium passes believable, and long passes and through balls fast enough for realistic football.

Preserve appropriate differences between passes, crosses, shots and clearances. Ensure attributes affect execution appropriately without causing extreme or unpredictable outcomes.

Do not increase all forces globally or create arbitrary speed multipliers that hide the underlying problem.

## 5. Fix player movement and goalkeeper behaviour

Ensure players move smoothly and follow their tactical targets without teleporting.

Prevent conflicts between AI movement, physics, animation and direct Transform changes.

Keep goalkeepers under deliberate AI control while allowing appropriate collisions and movement. Their positioning, dives, saves and recovery must not fight the movement system.

Do not allow physics collisions to randomly launch players across the pitch.

## 6. Fix boundaries and match progression

Ensure the ball crossing a touchline or goal line triggers the correct match state and restart.

Implement or repair throw-ins, goal kicks, corners and kick-offs according to the existing rules system.

A failed restart must not leave the ball out of play while the match clock continues indefinitely.

Physics should detect or support the event; the match engine must decide which restart is required.

## 7. Preserve simulation stability

Ensure physics and gameplay logic update at appropriate time intervals.

Check fixed timestep handling, match speed multipliers, pause and resume, interpolation, collision detection and frame-rate independence.

Prevent ball tunnelling, unstable collisions, duplicated movement updates and physics state desynchronisation.

Avoid manually simulating physics twice or mixing incompatible movement methods on the same object.

## 8. Implementation and testing

First provide a concise diagnosis and recommend whether to retain the custom system, migrate selected components to Rigidbody2D, or adopt a hybrid architecture.

Then implement the smallest safe set of changes that fixes the identified issues.

Test:
- Short, medium and long passes.
- Shots, crosses and clearances.
- Dribbling and possession transitions.
- Tackles, collisions and rebounds.
- Goalkeeper saves and distribution.
- Throw-ins, goal kicks and corners.
- Different match speeds and frame rates.
- Pause, resume and match resets.

Verify that existing AI, tactics, attributes, traits, statistics and save compatibility remain intact.

Do not claim success based solely on compilation. Run the available tests and, if in-game testing is unavailable, explicitly state which behaviours remain unverified.

At the end, report the root causes, chosen architecture, files changed, tests performed and remaining issues.

The objective is a stable and believable football simulation, not a complete rewrite simply for the sake of using Rigidbody2D.

# PRIORITY 3: COMMUNITY MODDING AND CUSTOM DATABASE SYSTEM

Implement a complete community modding and custom database system for Coach Master.

Players must be able to install community-created content packs containing real or fictional football players, clubs, leagues, transfers, player attributes, contracts, nationalities, club crests, player faces and kits.

## 3.1 Inspect the existing architecture

Before implementing anything, inspect:
- Club and player data models.
- League and competition structures.
- Database loading.
- Career creation.
- Save files and save compatibility.
- Asset-loading pipelines.
- Existing import/export functionality.
- UI architecture and file storage.

Reuse existing systems wherever possible.

## 3.2 Define a documented pack format

Create a versioned, documented format for custom content packs.

Support structured data such as JSON for:
- Players.
- Clubs.
- Leagues and competitions.
- Transfers.
- Contracts.
- Player attributes and traits.
- Nationalities.
- Other supported football database information.

Support compatible image formats for:
- Club crests.
- Player faces.
- Kits.

Each pack must include metadata such as its name, author, version, description, supported game version, content types and dependencies where needed.

Use stable IDs and clear references between players, clubs and competitions.

## 3.3 In-game database importer

Create an Import Database interface that lets players select a compatible ZIP archive or supported pack file.

Players must not need to manually edit the installation directory or source code.

The importer should:
- Display import progress.
- Validate the package before activation.
- Show a summary of the included content.
- Report errors clearly.
- Avoid partially applying invalid imports.
- Roll back failed imports safely.
- Confirm successful imports.

## 3.4 Pack manager

Implement a Pack Manager where players can:
- View installed packs.
- Enable or disable packs.
- Remove packs safely.
- Inspect their contents.
- See version and compatibility information.
- Identify missing dependencies.
- Understand conflicts between packs.

Use deterministic conflict-resolution rules. Never silently overwrite conflicting records.

## 3.5 Database selection

Add a database selection step when starting a new career.

Players should be able to select:
- The default Coach Master database.
- An installed custom database.
- A compatible combination of content packs, if supported by the architecture.

Ensure the selected database genuinely changes the available clubs, players, leagues and other supported content.

Keep imported data separate from the default database. Never overwrite the original data when a pack is installed or enabled.

## 3.6 Graphics and asset loading

Support custom crests, player faces and kits through a safe asset-loading system.

Validate image formats and dimensions, handle missing assets gracefully, and provide fallback graphics.

Do not require the player to replace the game's original asset files manually.

## 3.7 Save compatibility

Career saves must preserve the identity and version of the database used to create them.

When a pack is updated, removed or disabled:
- Detect whether an existing career depends on that pack.
- Warn the player if the career cannot be loaded safely.
- Avoid silently deleting players, clubs or historical data.
- Provide sensible recovery or compatibility options where feasible.

Use versioned save metadata and migrations when necessary. Never silently corrupt existing saves.

## 3.8 Export and sharing

Provide an export function so players can package their own compatible databases and share them with other players.

The export must contain only the selected supported content and required metadata. Validate exported packs before reporting success.

Keep the architecture extensible so a future online community browser or Steam Workshop integration can be added without rebuilding the entire system.

## 3.9 Security and legal considerations

Treat all imported files as untrusted.

- Prevent path traversal and unsafe archive extraction.
- Reject executable code and unexpected file types.
- Enforce reasonable file-size and archive-size limits.
- Validate data types and references.
- Handle malformed files without crashing the game.
- Never execute scripts supplied by a content pack.
- Do not bundle unlicensed third-party assets or automatically download real-world player data.

Players must supply content they have the right to use.

# PRIORITY 4: TESTING AND PERFORMANCE

Create a systematic test plan covering the gameplay and modding systems.

## Gameplay tests

Test:
- Short, medium and long passes.
- Through balls, crosses and clearances.
- Shots from different distances.
- Dribbling and possession changes.
- Tackles, interceptions and rebounds.
- Goalkeeper saves and distribution.
- Ball going out on each boundary.
- Throw-ins, goal kicks and corners.
- Substitutions.
- Half-time, full-time, pause and resume.
- Different match speeds and frame rates.
- Players with different attributes and traits.
- Matches with different formations and tactical instructions.

## Modding tests

Test:
- Importing a valid pack.
- Rejecting a malformed pack.
- Duplicate player and club IDs.
- Missing player-to-club references.
- Invalid images.
- Conflicting packs.
- Enabling and disabling packs.
- Exporting and reimporting a pack.
- Starting a career with a custom database.
- Loading existing saves after installing or removing packs.
- Recovery after an interrupted or failed import.

## Performance and stability

Avoid unnecessary disk writes, excessive logging, repeated full database reloads and expensive per-frame calculations.

Ensure simulation performance remains acceptable with larger databases and many active players.

Do not introduce unnecessary dependencies or destabilise existing functionality.

# IMPLEMENTATION WORKFLOW

Work in stages:

1. Inspect the entire relevant project and map the architecture.
2. Produce a concise audit identifying actual defects, incomplete features and dependencies.
3. Fix the passing-speed problem and critical ball, movement and restart bugs.
4. Audit player attributes, traits, rules and other gameplay systems.
5. Fix the defects discovered and run regression tests.
6. Design and implement the custom pack format and validator.
7. Implement importing, database selection and asset loading.
8. Implement pack management and export.
9. Verify career save compatibility.
10. Run the available tests and build the project.

Prioritise correctness over the number of changes made. Work incrementally and verify each stage before moving on.

Do not stop after writing a plan or creating the UI. Implement the systems directly in the actual project files wherever access permits.

If the project is too large to audit completely in one pass, track progress explicitly and continue systematically. Do not claim that the entire project has been verified when only a few files have been inspected.

# FINAL REPORT

When finished, report:
- Root causes of the slow passes and other gameplay bugs.
- Gameplay features audited and their actual status.
- Bugs fixed and files changed.
- Player attributes and traits verified.
- Football rules implemented, missing or still unverified.
- Modding features implemented.
- Supported pack formats and schema documentation.
- Tests actually run and their results.
- Build status.
- Remaining bugs, limitations and manual tests still required.

**Final objective:** Coach Master must have responsive, believable football gameplay; correctly functioning player attributes, traits and football rules; stable match progression; and a complete, safe community modding system that allows players to install, manage, export and use custom databases without damaging existing careers.
