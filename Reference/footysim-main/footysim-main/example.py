"""Simulate one match from example_teams.json and write match_output.json.

Also prints a human-readable summary and demonstrates frame decimation
(compress 90 minutes of frames to a 60-second playback).
"""
import json
import time

from footysim.sim import simulate_match, decimate_frames

with open("example_teams.json") as f:
    teams = json.load(f)

t0 = time.time()
result = simulate_match(teams["team_a"], teams["team_b"], seed=42)
elapsed = time.time() - t0

data = result.to_dict()
result.save("match_output.json")

meta = data["meta"]
box = data["boxscore"]["teams"]
print(f"simulated in {elapsed:.1f}s | final: {meta['final_score']}")
print(f"events_general={len(data['events_general'])} "
      f"events_key={len(data['events_key'])} frames={len(data['frames'])}")
for tid, s in box.items():
    print(f"\n[{tid}] poss {s['possession_pct']}% | passes {s['passes_completed']}/"
          f"{s['passes_attempted']} ({s['pass_accuracy']}) | shots {s['shots']} "
          f"(on {s['shots_on_target']}) | xG {s['xg']} | goals {s['goals']} | "
          f"tackles {s['tackles_won']} | interceptions {s['interceptions']}")

print("\nTop ratings:")
ratings = sorted(data["ratings"].items(), key=lambda kv: -kv[1])[:5]
for pid, r in ratings:
    print(f"  {pid}: {r}")

playback = decimate_frames(data["frames"], target_seconds=60)
print(f"\nframes decimated for 60s playback: {len(playback)}")

print("\nkey events:")
for ev in data["events_key"]:
    if ev["type"] in ("goal", "shot", "save"):
        print(f"  {ev['t']:7.1f}s {ev['type']:5s} {ev.get('actor','')} "
              f"xg={ev.get('xg','')} {ev.get('outcome','')}")
