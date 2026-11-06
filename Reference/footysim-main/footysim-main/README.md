# footysim — spatial agent-based football match engine (Phases 1-4, complete)

Headless, tick-driven 11v11 simulation. 22 player agents + ball on a real
105x68 m pitch, 100 ms ticks (54,000 per match). Every pass, interception,
tackle, shot, and xG value is resolved from player attributes + live geometry
at the point of contact — no top-level probability rolls. Pure library, no UI.

## Quick start
```bash
python3 example.py          # simulates a match, writes match_output.json
python3 -m pytest tests/ -q # acceptance tests (shortened matches)
```

```python
from footysim.sim import simulate_match, decimate_frames
import json

teams = json.load(open("example_teams.json"))
result = simulate_match(teams["team_a"], teams["team_b"], seed=42)
result.save("match.json")

# playback is a consumer concern: compress 90 min to a 60 s replay
frames_60s = decimate_frames(result.to_dict()["frames"], target_seconds=60)
```

Determinism: same inputs + same seed => **byte-identical JSON** (tested).

## Output contract (`result.to_dict()`)
| key | contents |
|---|---|
| `meta` | teams, formations, seed, config, final score |
| `events_general[]` | high-volume stream: every pass, control, carry sample, duel, shot, restart — with tick, actor, location, outcome |
| `events_key[]` | filtered highlights: goals, shots, saves, tackles won, turnovers, kickoffs |
| `frames[]` | every 0.5 s (config `frame_interval`): all 22 player (x,y) + ball (x,y) + current owner |
| `boxscore.teams` / `boxscore.players` | passes att/comp, accuracy, shots, on target, goals, xG, tackles, interceptions, saves, distance, possession % |
| `ratings` | per-player 0–10 match rating, accumulated live from contributions |

## Modules
```
pitch.py       geometry, coordinate helpers (105x68, origin at a corner)
modifiers.py   form / sharpness / role-fit / streak / fatigue modifiers
entities.py    Attributes / Player / Ball / Team dataclasses
tactics.py     formations (4-3-3, 4-4-2), per-tick target positions, defensive collapse
decisions.py   per-agent decision loop: shoot/pass/dribble utilities, pressing, marking
physics.py     player movement integration (pace/accel/stamina caps), ball flight + friction
resolution.py  contact-point resolution: pass error model, control/interception, shot + xG, duels
ratings.py     live rating adjustments
engine.py      tick loop, game states (owned/in-flight/loose), event + stat recording
output.py      JSON assembly, boxscore aggregation, frame decimation helper
sim.py         public API: simulate_match(team_a, team_b, config, seed)
```

## Config (all optional)
`tick_seconds` (0.1), `half_ticks` (27000), `frame_interval` (5),
`decide_interval` (10), `contest_interval` (12), `carry_log_interval` (10).

## Team input shape
See `example_teams.json`. Roles must exactly match the formation's role set.
Attributes are 0–100; unknown keys are ignored, missing keys default to 50.
Optional per-player: `form`, `sharpness`, `natural_positions`. Optional
per-team: `synergy`, `instructions` {pressing, line_height, width, tempo,
mentality} — all 0-100, 50 = neutral.

## Phase status
- **Phase 1: DONE.** Full 11v11 match end-to-end, spatial frames, two-tier
  events, per-team + per-player boxscore, xG per shot, live ratings, seeded
  determinism, tests.
- **Phase 2: DONE.** Full FM attribute set (26 attributes incl. crossing,
  heading, marking, strength, agility, work_rate, teamwork, aggression, and
  5 GK attributes). Fouls, yellow/red cards (incl. second yellows), sending
  offs that leave a team playing shorthanded, penalty kicks, crosses
  contested with heading, GK handling (catch vs parry with live rebounds),
  GK positioning shaping xG, command of area shaping claims, box caution
  (defenders challenge less recklessly in their own area), assists,
  key passes, and end-of-match xG over/under-performance rating adjustment.
  Typical distributions over seeds: 1-0 to 4-0 scorelines, 20-40 fouls,
  2-7 yellows, reds rare, penalties ~1 every 3 matches; team quality gaps
  visibly drive shot/xG dominance.
- **Phase 3: DONE.** Dynamic modifiers and tactical instructions:
  - Per-player inputs: `form` (0-100, +/-8% on effective attributes),
    `sharpness` (season fitness, up to -10% on technical/mental),
    `natural_positions` (role-fit penalty up to -18%, strongest on mental
    attributes, when played out of position).
  - Live modifiers: hot/cold streak from in-match rating (+/-6%), and
    fatigue eroding decision quality late on (wider decision noise, mental
    attribute decay) on top of the existing pace/acceleration decay.
  - Team inputs: `synergy` (0-100: pass-option support + reception bonus;
    measured ~+2.4% pass accuracy from synergy 5 -> 95) and `instructions`:
    `pressing` (pressers committed, engage radius, duel frequency, stamina
    cost; measured +7% possession and +24 tackles from 5 -> 95),
    `line_height` (+/-6 m block shift), `width` (0.85-1.15x lateral
    stretch), `tempo` (on-ball decision speed), `mentality` (positional
    push, progressive-pass weighting, shot eagerness).
  - All effective-attribute reads flow through `Player.eff()`
    (`modifiers.py`); inputs are recorded in `meta`.
- **Phase 4: DONE.** Calibration + micro-stats:
  - Duels are now episodic: one resolution per carrier engagement (with a
    protected window on gaining the ball), triggered by take-ons or a
    defender getting properly tight — not re-rolled every interval.
    Defenders jockey at arm's length; carriers shield between decisions.
  - Long-ball outlet: teams pinned in their own half with all short options
    covered go direct to the front man (low-completion, pressure-relieving).
  - New micro-stats: touches, progressive passes/carries, passes into the
    final third, dribbles completed, duels/duels won, big chances (xG>=0.3),
    shots inside/outside the box, GK xG faced and goals conceded.
  - Frame decimation helper (playback compression) shipped since Phase 1.

## Calibration (measured, symmetric neutral teams, full-match projection)
| stat (per team)   | engine | real-world typical |
|---|---|---|
| shots             | ~15    | 12–15  |
| xG                | ~1.5   | 1.2–1.6 |
| goals             | ~1.3   | ~1.4   |
| tackles won       | ~20    | 15–25  |
| fouls             | ~9     | 10–13  |
| interceptions     | ~9     | 8–12   |
| pass accuracy     | ~76%   | 75–88% |
| passes attempted  | ~670   | 350–700 |
| player distance   | ~12 km | 10–11.5 km |
| possession        | ~50/50 | —      |

## Known residual gaps
- **Quality gaps amplify.** Mismatched teams diverge more than in reality:
  a modest attribute + instruction edge can produce 30+ vs <6 shots, where
  real football would show ~20 vs ~8. Sequential completion probabilities
  compound through the progression funnel. Treat large input gaps as more
  lopsided than their real-world equivalents.
- No offside, corners, or throw-in geometry (restarts are simplified
  possession swaps at the boundary; free kicks and penalties do exist).
- Penalties resolve outcome + restart correctly but do not re-choreograph
  the other 20 players around the box first.
- Assist rate is low relative to real football because many sim goals come
  off carries/rebounds rather than a final pass.
- Passes attempted and player distance sit at the top of real ranges.
- Goal conversion runs slightly under recorded xG on some seeds.

Note on granularity: ticks are 100 ms, but stats are logged per *action*
(one pass = one event), not per tick — stat inflation above is gameplay
balance, except tackles, which genuinely re-log per contest interval.
