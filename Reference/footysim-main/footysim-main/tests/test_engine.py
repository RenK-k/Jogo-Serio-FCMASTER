"""Phase-1 acceptance tests. Run: python3 -m pytest tests/ -q
Uses a shortened match (5 sim-minutes per half) to keep tests fast."""
import json, copy, pathlib, sys
sys.path.insert(0, str(pathlib.Path(__file__).parent.parent))
from footysim.sim import simulate_match, decimate_frames

TEAMS = json.load(open(pathlib.Path(__file__).parent.parent / "example_teams.json"))
SHORT = {"half_ticks": 3000}  # 2 x 5 sim-minutes

def _run(seed=1):
    return simulate_match(TEAMS["team_a"], TEAMS["team_b"], config=SHORT, seed=seed)

def test_determinism_byte_identical():
    assert _run(5).to_json() == _run(5).to_json()

def test_seeds_differ():
    assert _run(1).to_json() != _run(2).to_json()

def test_output_contract():
    d = _run().to_dict()
    for key in ("meta", "events_general", "events_key", "frames", "boxscore", "ratings"):
        assert key in d
    assert len(d["frames"]) == 2 * 3000 // 5
    f = d["frames"][0]
    assert len(f["players"]) == 22 and len(f["ball"]) == 2
    assert set(d["meta"]["final_score"]) == {"RED", "BLU"}

def test_positions_in_bounds():
    d = _run().to_dict()
    for f in d["frames"][::50]:
        for x, y in f["players"].values():
            assert -1 <= x <= 106 and -1 <= y <= 69

def test_boxscore_consistency():
    d = _run().to_dict()
    players = d["boxscore"]["players"]
    teams = d["boxscore"]["teams"]
    for tid, agg in teams.items():
        rows = [r for r in players.values() if r["team"] == tid]
        assert agg["shots"] == sum(r["shots"] for r in rows)
        assert agg["goals"] == d["meta"]["final_score"][tid]
    assert abs(sum(t["possession_pct"] for t in teams.values()) - 100.0) < 0.2

def test_key_events_subset_of_general():
    d = _run().to_dict()
    gen = {(e["tick"], e["type"], e.get("actor")) for e in d["events_general"]}
    for e in d["events_key"]:
        assert (e["tick"], e["type"], e.get("actor")) in gen

def test_ratings_bounds():
    d = _run().to_dict()
    assert all(3.0 <= r <= 10.0 for r in d["ratings"].values())
    assert len(d["ratings"]) == 22

def test_decimation_pure():
    d = _run().to_dict()
    before = copy.deepcopy(d["frames"])
    out = decimate_frames(d["frames"], target_seconds=10)
    assert len(out) < len(before) and d["frames"] == before
    assert out[0] == before[0] and out[-1] == before[-1]

def test_bad_formation_rejected():
    bad = copy.deepcopy(TEAMS["team_a"]); bad["players"][3]["role"] = "ST"
    try:
        simulate_match(bad, TEAMS["team_b"], config=SHORT)
        assert False, "should have raised"
    except ValueError:
        pass

# ---------------------------------------------------------- Phase 2 tests

def test_phase2_stat_keys_present():
    d = _run().to_dict()
    row = next(iter(d["boxscore"]["players"].values()))
    for k in ("fouls", "yellow_cards", "red_cards", "key_passes",
              "assists", "crosses", "crosses_completed"):
        assert k in row

def test_fouls_produce_valid_events():
    # aggregate several short matches so fouls reliably occur
    fouls = yellows = 0
    for seed in range(6):
        d = _run(seed).to_dict()
        ev = d["events_general"]
        fouls += sum(1 for e in ev if e["type"] == "foul")
        yellows += sum(1 for e in ev if e["type"] == "yellow_card")
        for e in ev:
            if e["type"] == "foul":
                assert e.get("victim") and e.get("actor")
    assert fouls > 0, "no fouls across 6 short matches"

def test_sent_off_players_stay_off():
    for seed in range(12):
        d = _run(seed).to_dict()
        reds = [e for e in d["events_general"] if e["type"] == "red_card"]
        if not reds:
            continue
        pid = reds[0]["actor"]; tick = reds[0]["tick"]
        for f in d["frames"]:
            if f["tick"] > tick + 20:
                x, y = f["players"][pid]
                assert y < 1.5, "sent-off player moved back on pitch"
        return
    # no red in any short match is acceptable (they are rare)

def test_penalty_area_geometry():
    import numpy as np
    from footysim import pitch
    assert pitch.in_penalty_area(np.array([100.0, 34.0]), True)
    assert not pitch.in_penalty_area(np.array([80.0, 34.0]), True)
    assert pitch.in_penalty_area(np.array([5.0, 34.0]), False)
    assert not pitch.in_penalty_area(np.array([100.0, 5.0]), True)

def test_full_attribute_set_parsed():
    from footysim.entities import Attributes
    a = Attributes.from_dict({"crossing": 80, "heading": 70, "strength": 60,
                              "work_rate": 90, "command_of_area": 75,
                              "unknown_key": 999})
    assert a.crossing == 80 and a.command_of_area == 75
    assert a.marking == 50.0  # default

# ---------------------------------------------------------- Phase 3 tests

def test_role_fit_penalizes_out_of_position():
    from footysim import modifiers
    assert modifiers.role_fit("RB", ["RB"]) == 1.0
    assert modifiers.role_fit("RB", ["LB"]) == 0.96          # same group
    assert modifiers.role_fit("RB", ["RCM"]) == 0.90         # adjacent group
    assert modifiers.role_fit("RB", ["ST"]) == 0.82          # far group

def test_eff_reflects_form_and_fit():
    from footysim.entities import Attributes, Player
    hot = Player("h", "h", "T", "CM", Attributes(passing=70), form=90)
    cold = Player("c", "c", "T", "CM", Attributes(passing=70), form=10)
    hot.init_modifiers("CM"); cold.init_modifiers("CM")
    assert hot.eff("passing") > cold.eff("passing")
    oop = Player("o", "o", "T", "RB", Attributes(positioning=70),
                 natural_positions=["ST"])
    oop.init_modifiers("RB")
    assert oop.eff("positioning") < 70 * 0.9

def test_instructions_change_match_output():
    import copy
    hi = copy.deepcopy(TEAMS); lo = copy.deepcopy(TEAMS)
    hi["team_b"]["instructions"] = {"pressing": 95}
    lo["team_b"]["instructions"] = {"pressing": 5}
    a = simulate_match(hi["team_a"], hi["team_b"], config=SHORT, seed=3).to_json()
    b = simulate_match(lo["team_a"], lo["team_b"], config=SHORT, seed=3).to_json()
    assert a != b

def test_synergy_changes_match_output():
    import copy
    hi = copy.deepcopy(TEAMS); lo = copy.deepcopy(TEAMS)
    hi["team_a"]["synergy"] = 95; lo["team_a"]["synergy"] = 5
    a = simulate_match(hi["team_a"], hi["team_b"], config=SHORT, seed=3).to_json()
    b = simulate_match(lo["team_a"], lo["team_b"], config=SHORT, seed=3).to_json()
    assert a != b

def test_meta_records_phase3_inputs():
    d = _run().to_dict()
    tm = d["meta"]["teams"]["RED"]
    assert "instructions" in tm and "synergy" in tm
    assert all("form" in p and "sharpness" in p for p in tm["players"])

# ---------------------------------------------------------- Phase 4 tests

def test_micro_stats_present():
    d = _run().to_dict()
    row = next(iter(d["boxscore"]["players"].values()))
    for k in ("touches", "progressive_passes", "passes_final_third",
              "progressive_carries", "dribbles_completed", "duels",
              "duels_won", "big_chances", "shots_inside_box",
              "shots_outside_box", "xg_faced", "goals_conceded"):
        assert k in row, k

def test_duel_episode_semantics():
    # duels are episodic, not per-tick: bounded well below old inflation
    d = _run(seed=4).to_dict()
    teams = d["boxscore"]["teams"]
    total_duels = sum(t["duels"] for t in teams.values()) / 2  # counted per side
    minutes = 2 * SHORT["half_ticks"] * 0.1 / 60
    assert total_duels / minutes < 4.0, f"{total_duels} duels in {minutes} min"

def test_gk_xg_faced_consistency():
    d = _run(seed=2).to_dict()
    players = d["boxscore"]["players"]
    for pid, row in players.items():
        if row["role"] == "GK":
            # xg_faced only accumulates from on-target shots
            assert row["xg_faced"] >= 0.0
            assert row["goals_conceded"] <= row["xg_faced"] + 5  # sanity

def test_shot_box_split_sums():
    d = _run(seed=1).to_dict()
    for row in d["boxscore"]["players"].values():
        assert row["shots_inside_box"] + row["shots_outside_box"] == row["shots"]

def test_calibration_mirror_ranges():
    """Symmetric neutral teams must land in believable per-team ranges."""
    import copy
    a = copy.deepcopy(TEAMS["team_a"]); b = copy.deepcopy(TEAMS["team_b"])
    for t in (a, b):
        t["synergy"] = 50
        t["instructions"] = {k: 50 for k in
            ("pressing", "line_height", "width", "tempo", "mentality")}
        for p in t["players"]:
            p["form"] = 50; p["sharpness"] = 95
            p["natural_positions"] = [p["role"]]
    b["players"] = copy.deepcopy(a["players"])   # identical squads
    for p in b["players"]:
        p["pid"] = p["pid"].replace("RED", "BLU")
    # one third-length match; scale x3 and allow generous bounds
    d = simulate_match(a, b, config={"half_ticks": 9000}, seed=5).to_dict()
    for tid, t in d["boxscore"]["teams"].items():
        assert 3 * t["shots"] <= 40, (tid, t["shots"])
        assert 3 * t["tackles_won"] <= 60, (tid, t["tackles_won"])
        assert 3 * t["fouls"] <= 35, (tid, t["fouls"])
        assert (t["pass_accuracy"] or 0) > 0.65
