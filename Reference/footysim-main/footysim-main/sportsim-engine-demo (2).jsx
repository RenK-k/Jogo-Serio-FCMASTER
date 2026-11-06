import { useState, useRef, useEffect } from "react";
import { Play, Pause, FastForward, RotateCcw, Zap, Trophy, ChevronRight } from "lucide-react";

/* ================= Engine (pure, seeded) ================= */

function mulberry32(seed) {
  let a = seed >>> 0;
  return () => {
    a |= 0; a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
const clamp = (v, lo, hi) => Math.min(hi, Math.max(lo, v));
const logistic = (x) => 1 / (1 + Math.exp(-x));
const pick = (rng, arr) => arr[Math.floor(rng() * arr.length)];

const FIRST = ["Kai","Mateo","Luca","Emeka","Yusuf","Dario","Tunde","Rafael","Jonas","Idris","Marco","Femi","Sacha","Bruno","Kofi"];
const LAST = ["Okafor","Silva","Mensah","Vidal","Traoré","Kovac","Adeyemi","Rossi","Hansen","Diallo","Costa","Bakker","Moreau","Eze","Zubair"];

function makeSquad(rng, base) {
  const positions = ["GK","DEF","DEF","DEF","DEF","MID","MID","MID","MID","ATT","ATT"];
  return positions.map((pos, i) => {
    const rating = Math.round(base + (rng() * 10 - 5));
    return {
      name: `${FIRST[Math.floor(rng() * FIRST.length)]} ${LAST[Math.floor(rng() * LAST.length)]}`,
      pos, rating,
      shooting: clamp(rating + (pos === "ATT" ? 6 : pos === "MID" ? 0 : -10) + rng() * 6 - 3, 40, 99),
      passing: clamp(rating + (pos === "MID" ? 5 : 0) + rng() * 6 - 3, 40, 99),
      defending: clamp(rating + (pos === "DEF" ? 6 : pos === "GK" ? 4 : -8) + rng() * 6 - 3, 40, 99),
      stamina: 100,
      goals: 0, assists: 0, saves: 0, yellows: 0, key: `${pos}-${i}`,
    };
  });
}

const MENTALITIES = ["Tiki-Taka", "Gegenpressing", "Counter-Attack", "Park the Bus"];
const MENTALITY_CTRL = { "Tiki-Taka": 6, "Gegenpressing": 3, "Counter-Attack": -2, "Park the Bus": -11 };

function makeClub(name, color, base, mentality, seed) {
  const rng = mulberry32(seed);
  return { name, color, base, mentality, squad: makeSquad(rng, base) };
}

function teamStrength(club) {
  const s = club.squad;
  const avgStam = s.reduce((a, p) => a + p.stamina, 0) / s.length / 100;
  const control = s.reduce((a, p) => a + p.passing, 0) / s.length;
  const attack = s.filter(p => p.pos !== "GK").reduce((a, p) => a + p.shooting, 0) / 10;
  const defence = s.reduce((a, p) => a + p.defending, 0) / s.length;
  const gk = s.find(p => p.pos === "GK").defending;
  return {
    control: (control + MENTALITY_CTRL[club.mentality]) * (0.7 + 0.3 * avgStam),
    attack: attack * (0.7 + 0.3 * avgStam),
    defence, gk,
  };
}

function simTick(state, rng) {
  const ev = [];
  const minute = state.tick * 3;
  const home = teamStrength(state.home);
  const away = teamStrength(state.away);
  const homeCtrl = home.control * 1.14;
  const pHome = rng() < homeCtrl / (homeCtrl + away.control);
  const attacking = pHome ? "home" : "away";
  const att = attacking === "home" ? state.home : state.away;
  const defC = attacking === "home" ? state.away : state.home;
  const atk = attacking === "home" ? home : away;
  const dfn = attacking === "home" ? away : home;

  state.possession[attacking] += 1;

  const passers = att.squad.filter(p => p.pos === "MID" || p.pos === "ATT");
  const passer = pick(rng, passers);
  const progressP = logistic((atk.control - dfn.defence) / 18 + 0.35);
  if (rng() > progressP) {
    const interceptor = pick(rng, defC.squad.filter(p => p.pos === "DEF" || p.pos === "MID"));
    ev.push({ minute, type: "play", text: pick(rng, [
      `${interceptor.name} steps in and breaks up the move.`,
      `${passer.name}'s pass is cut out by ${interceptor.name}.`,
      `Loose touch from ${passer.name} — ${defC.name} clear their lines.`,
    ])});
  } else {
    const shooters = att.squad.filter(p => p.pos === "ATT" || (p.pos === "MID" && rng() < 0.3));
    const shooter = pick(rng, shooters);
    const z = (shooter.shooting + atk.attack) / 2 / 20 - dfn.defence / 22 - dfn.gk / 60
      + (att.mentality === "Counter-Attack" && state.tick % 4 === 0 ? 0.35 : 0);
    const xg = clamp(logistic(z - 2.05), 0.01, 0.85);
    state.stats[attacking].shots += 1;
    state.stats[attacking].xg += xg;
    const roll = rng();
    if (roll < xg) {
      state.score[attacking] += 1;
      shooter.goals += 1;
      const assister = pick(rng, passers.filter(p => p.key !== shooter.key));
      if (assister && rng() < 0.7) { assister.assists += 1; }
      state.stats[attacking].sot += 1;
      ev.push({ minute, type: "goal", text: `GOAL! ${shooter.name} finishes for ${att.name}!${assister && assister.assists ? ` Assist: ${assister.name}.` : ""} (xG ${xg.toFixed(2)})` });
    } else if (roll < xg + 0.28) {
      state.stats[attacking].sot += 1;
      const keeper = defC.squad.find(p => p.pos === "GK");
      keeper.saves += 1;
      ev.push({ minute, type: "save", text: `${shooter.name} forces a save from ${keeper.name}. (xG ${xg.toFixed(2)})` });
    } else {
      if (rng() < 0.35) state.stats[attacking].corners += 1;
      ev.push({ minute, type: "play", text: pick(rng, [
        `${shooter.name} drags it wide of the far post.`,
        `${shooter.name} blazes over from distance.`,
        `Effort from ${shooter.name} deflected behind — corner.`,
      ])});
    }
  }

  if (rng() < 0.28) {
    const fouler = pick(rng, defC.squad);
    const side = attacking === "home" ? "away" : "home";
    state.stats[side].fouls += 1;
    if (rng() < 0.19) {
      state.stats[side].yellows += 1;
      fouler.yellows += 1;
      ev.push({ minute, type: "card", text: `Yellow card — ${fouler.name} (${defC.name}) for a late challenge.` });
    }
  }

  for (const club of [state.home, state.away]) {
    const drain = club.mentality === "Gegenpressing" ? 2.6 : 1.8;
    club.squad.forEach(p => { p.stamina = clamp(p.stamina - drain - rng(), 20, 100); });
  }

  state.tick += 1;
  if (state.tick === 15) ev.push({ minute: 45, type: "whistle", text: `Half-time: ${state.home.name} ${state.score.home}–${state.score.away} ${state.away.name}` });
  if (state.tick === 30) ev.push({ minute: 90, type: "whistle", text: `Full-time: ${state.home.name} ${state.score.home}–${state.score.away} ${state.away.name}` });
  return ev;
}

/* Post-match ratings: base + goal-diff swing + goals×1.6 + assists×1.1 + saves, clamped 3.0–10.0 */
function computeRatings(state, rng) {
  const gd = state.score.home - state.score.away;
  const rate = (club, sideGd) =>
    club.squad.map(p => ({
      ...p,
      matchRating: clamp(
        6.4 + sideGd * 0.35 + p.goals * 1.6 + p.assists * 1.1 + p.saves * 0.35
        - p.yellows * 0.4 + (rng() * 0.8 - 0.4),
        3.0, 10.0),
      club: club.name, color: club.color,
    }));
  const all = [...rate(state.home, gd), ...rate(state.away, -gd)];
  const motm = all.reduce((a, b) => (b.matchRating > a.matchRating ? b : a));
  return { all, motm };
}

function winProb(state) {
  const home = teamStrength(state.home), away = teamStrength(state.away);
  const diff = (state.score.home - state.score.away) * 1.1
    + (home.control * 1.14 - away.control) / 25
    + (30 - state.tick) * 0.005;
  return clamp(logistic(diff), 0.03, 0.97);
}

/* ================= World ================= */

const CLUB_POOL = [
  ["Northgate Titans", "#38bdf8", 86], ["Emerald City FC", "#34d399", 85],
  ["Riverside Athletic", "#f472b6", 80], ["Iron Valley United", "#fbbf24", 79],
  ["Harbour Rovers", "#a78bfa", 74], ["Oakfield Wanderers", "#fb923c", 72],
  ["Lagos Lions", "#4ade80", 78], ["Crestwood Albion", "#f87171", 68],
];

function freshState(homeIdx, awayIdx, seed) {
  const [hn, hc, hb] = CLUB_POOL[homeIdx];
  const [an, ac, ab] = CLUB_POOL[awayIdx];
  return {
    home: makeClub(hn, hc, hb, MENTALITIES[homeIdx % 4], seed * 7 + 1),
    away: makeClub(an, ac, ab, MENTALITIES[awayIdx % 4], seed * 13 + 2),
    tick: 0,
    score: { home: 0, away: 0 },
    possession: { home: 0, away: 0 },
    stats: {
      home: { shots: 0, sot: 0, xg: 0, corners: 0, fouls: 0, yellows: 0 },
      away: { shots: 0, sot: 0, xg: 0, corners: 0, fouls: 0, yellows: 0 },
    },
  };
}

/* ================= UI ================= */

function StatRow({ label, h, a, fmt = (v) => v }) {
  const total = h + a || 1;
  return (
    <div className="py-1.5">
      <div className="flex justify-between text-sm text-slate-200 font-medium">
        <span className="w-12 text-left tabular-nums">{fmt(h)}</span>
        <span className="text-[11px] uppercase tracking-widest text-slate-500">{label}</span>
        <span className="w-12 text-right tabular-nums">{fmt(a)}</span>
      </div>
      <div className="flex gap-1 mt-1 h-1">
        <div className="rounded-full bg-sky-400" style={{ width: `${(h / total) * 50}%` }} />
        <div className="flex-1" />
        <div className="rounded-full bg-emerald-400" style={{ width: `${(a / total) * 50}%` }} />
      </div>
    </div>
  );
}

const ratingColor = (r) =>
  r >= 8 ? "text-emerald-300" : r >= 7 ? "text-sky-300" : r >= 6 ? "text-slate-200" : "text-amber-300";

export default function SportSimDemo() {
  const [homeIdx, setHomeIdx] = useState(0);
  const [awayIdx, setAwayIdx] = useState(6);
  const [seed, setSeed] = useState(42);
  const [speed, setSpeed] = useState(900);
  const [running, setRunning] = useState(false);
  const [, force] = useState(0);
  const [events, setEvents] = useState([]);
  const [postMatch, setPostMatch] = useState(null);
  const [week, setWeek] = useState(1);
  const [record, setRecord] = useState({ p: 0, w: 0, d: 0, l: 0, gf: 0, ga: 0, pts: 0, form: [] });
  const [history, setHistory] = useState([]);
  const stateRef = useRef(freshState(0, 6, 42));
  const rngRef = useRef(mulberry32(42));
  const feedRef = useRef(null);

  const reset = (h = homeIdx, a = awayIdx, s = seed) => {
    setRunning(false);
    stateRef.current = freshState(h, a, s);
    rngRef.current = mulberry32(s * 31 + 7);
    setEvents([]);
    setPostMatch(null);
  };

  const finalize = () => {
    const st = stateRef.current;
    if (st.ratingsApplied) return; // guard the double-rating bug (§11.5)
    st.ratingsApplied = true;
    setPostMatch(computeRatings(st, rngRef.current));
    const gf = st.score.home, ga = st.score.away;
    const res = gf > ga ? "W" : gf === ga ? "D" : "L";
    setRecord(r => ({
      p: r.p + 1, w: r.w + (res === "W"), d: r.d + (res === "D"), l: r.l + (res === "L"),
      gf: r.gf + gf, ga: r.ga + ga,
      pts: r.pts + (res === "W" ? 3 : res === "D" ? 1 : 0),
      form: [...r.form, res].slice(-5),
    }));
    setHistory(h => [...h, { week, opp: st.away.name, score: `${gf}–${ga}`, res }]);
  };

  const step = () => {
    const st = stateRef.current;
    if (st.tick >= 30) { setRunning(false); return; }
    const ev = simTick(st, rngRef.current);
    if (ev.length) setEvents(prev => [...prev, ...ev]);
    force(n => n + 1);
    if (st.tick >= 30) { setRunning(false); finalize(); }
  };

  useEffect(() => {
    if (!running) return;
    const id = setInterval(step, speed);
    return () => clearInterval(id);
  }, [running, speed]);

  useEffect(() => {
    if (feedRef.current) feedRef.current.scrollTop = feedRef.current.scrollHeight;
  }, [events]);

  const quickSim = () => {
    setRunning(false);
    const st = stateRef.current;
    const all = [];
    while (st.tick < 30) all.push(...simTick(st, rngRef.current));
    setEvents(prev => [...prev, ...all]);
    finalize();
    force(n => n + 1);
  };

  const nextMatch = () => {
    // rotate to next opponent, advance the week, new seed offset
    let next = (awayIdx + 1) % CLUB_POOL.length;
    if (next === homeIdx) next = (next + 1) % CLUB_POOL.length;
    setAwayIdx(next);
    setWeek(w => w + 1);
    reset(homeIdx, next, seed + week);
  };

  const st = stateRef.current;
  const minute = Math.min(st.tick * 3, 90);
  const wp = winProb(st);
  const posTotal = st.possession.home + st.possession.away || 1;
  const posH = Math.round((st.possession.home / posTotal) * 100);
  const done = st.tick >= 30;
  const scorers = (side) => st[side].squad.filter(p => p.goals > 0);
  const topRatings = (clubName) =>
    postMatch ? postMatch.all.filter(p => p.club === clubName).sort((a, b) => b.matchRating - a.matchRating) : [];

  return (
    <div className="min-h-screen text-slate-100 font-sans" style={{ background: "radial-gradient(1200px 600px at 20% -10%, #10243a 0%, #0c0f16 55%)" }}>
      <div className="max-w-5xl mx-auto px-4 py-6">
        {/* Header + progression strip */}
        <div className="flex flex-wrap items-center justify-between gap-3 mb-5">
          <div>
            <div className="text-[11px] uppercase tracking-[0.3em] text-sky-400/80">SportSim Pro · Engine demo</div>
            <h1 className="text-xl font-bold">Live match simulator</h1>
          </div>
          <div className="flex items-center gap-3 text-xs">
            <span className="px-2.5 py-1 rounded-lg bg-slate-800/80 border border-slate-700 tabular-nums">Week {week}</span>
            <span className="px-2.5 py-1 rounded-lg bg-slate-800/80 border border-slate-700 tabular-nums">
              P{record.p} W{record.w} D{record.d} L{record.l} · {record.pts} pts
            </span>
            <span className="flex gap-1">
              {record.form.map((f, i) => (
                <span key={i} className={`w-5 h-5 rounded grid place-items-center font-bold text-[10px] ${
                  f === "W" ? "bg-emerald-500/80 text-slate-950" : f === "D" ? "bg-slate-500/80 text-slate-950" : "bg-red-500/80 text-slate-950"}`}>{f}</span>
              ))}
            </span>
            {running && <span className="px-2 py-1 rounded-full bg-red-500/20 text-red-300 border border-red-500/40 animate-pulse font-semibold">● LIVE</span>}
          </div>
        </div>

        {/* Fixture setup */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 mb-4">
          <select value={homeIdx} onChange={(e) => { setHomeIdx(+e.target.value); reset(+e.target.value, awayIdx); }}
            className="bg-slate-900/80 border border-slate-700 rounded-lg px-3 py-2 text-sm">
            {CLUB_POOL.map((c, i) => <option key={c[0]} value={i} disabled={i === awayIdx}>{c[0]} (base {c[2]})</option>)}
          </select>
          <select value={awayIdx} onChange={(e) => { setAwayIdx(+e.target.value); reset(homeIdx, +e.target.value); }}
            className="bg-slate-900/80 border border-slate-700 rounded-lg px-3 py-2 text-sm">
            {CLUB_POOL.map((c, i) => <option key={c[0]} value={i} disabled={i === homeIdx}>{c[0]} (base {c[2]})</option>)}
          </select>
          <div className="flex items-center gap-2 bg-slate-900/80 border border-slate-700 rounded-lg px-3 py-2 text-sm">
            <span className="text-slate-400 text-xs">Seed</span>
            <input type="number" value={seed} onChange={(e) => { setSeed(+e.target.value); reset(homeIdx, awayIdx, +e.target.value); }}
              className="bg-transparent w-full outline-none tabular-nums" />
          </div>
        </div>

        {/* Scoreboard */}
        <div className="rounded-2xl border border-slate-700/60 bg-slate-900/60 backdrop-blur p-5 mb-4">
          <div className="grid grid-cols-3 items-center">
            <div className="text-right">
              <div className="font-bold text-lg" style={{ color: st.home.color }}>{st.home.name}</div>
              <div className="text-[11px] text-slate-400">{st.home.mentality} · home</div>
              <div className="text-xs text-slate-300 mt-1">{scorers("home").map(p => `${p.name} ×${p.goals}`).join(", ")}</div>
            </div>
            <div className="text-center">
              <div className="text-5xl font-black tabular-nums tracking-tight">{st.score.home}<span className="text-slate-500 mx-2">–</span>{st.score.away}</div>
              <div className="text-xs text-slate-400 mt-1 tabular-nums">{done ? "Full-time" : `${minute}'`} · tick {st.tick}/30</div>
            </div>
            <div className="text-left">
              <div className="font-bold text-lg" style={{ color: st.away.color }}>{st.away.name}</div>
              <div className="text-[11px] text-slate-400">{st.away.mentality} · away</div>
              <div className="text-xs text-slate-300 mt-1">{scorers("away").map(p => `${p.name} ×${p.goals}`).join(", ")}</div>
            </div>
          </div>

          <div className="mt-4">
            <div className="flex justify-between text-[11px] text-slate-400 mb-1">
              <span>Win probability {(wp * 100).toFixed(0)}%</span>
              <span>{((1 - wp) * 100).toFixed(0)}%</span>
            </div>
            <div className="h-2 rounded-full overflow-hidden flex bg-slate-800">
              <div className="bg-sky-400 transition-all duration-500" style={{ width: `${wp * 100}%` }} />
              <div className="bg-emerald-400 transition-all duration-500" style={{ width: `${(1 - wp) * 100}%` }} />
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-2 mt-4">
            <button onClick={() => setRunning(r => !r)} disabled={done}
              className="flex items-center gap-1.5 px-4 py-2 rounded-lg bg-sky-500 hover:bg-sky-400 disabled:opacity-40 text-slate-950 text-sm font-semibold transition-colors">
              {running ? <Pause size={14} /> : <Play size={14} />} {running ? "Pause" : "Watch live"}
            </button>
            <button onClick={step} disabled={running || done}
              className="flex items-center gap-1.5 px-3 py-2 rounded-lg border border-slate-600 hover:bg-slate-800 disabled:opacity-40 text-sm transition-colors">
              <FastForward size={14} /> Tick
            </button>
            <button onClick={quickSim} disabled={done}
              className="flex items-center gap-1.5 px-3 py-2 rounded-lg border border-emerald-500/50 text-emerald-300 hover:bg-emerald-500/10 disabled:opacity-40 text-sm transition-colors">
              <Zap size={14} /> Quick-sim
            </button>
            {done ? (
              <button onClick={nextMatch}
                className="flex items-center gap-1.5 px-4 py-2 rounded-lg bg-emerald-500 hover:bg-emerald-400 text-slate-950 text-sm font-semibold transition-colors">
                Next match <ChevronRight size={14} />
              </button>
            ) : (
              <button onClick={() => reset()}
                className="flex items-center gap-1.5 px-3 py-2 rounded-lg border border-slate-600 hover:bg-slate-800 text-sm transition-colors">
                <RotateCcw size={14} /> Reset
              </button>
            )}
            <label className="ml-auto flex items-center gap-2 text-xs text-slate-400">
              Speed
              <input type="range" min={150} max={1500} step={50} value={1650 - speed}
                onChange={(e) => setSpeed(1650 - +e.target.value)} className="accent-sky-400" />
            </label>
          </div>
        </div>

        {/* Post-match: MOTM + ratings */}
        {postMatch && (
          <div className="rounded-2xl border border-emerald-500/30 bg-slate-900/70 p-4 mb-4">
            <div className="flex items-center gap-2 mb-3">
              <Trophy size={16} className="text-amber-300" />
              <span className="text-sm font-semibold">Man of the Match:</span>
              <span className="text-sm font-bold" style={{ color: postMatch.motm.color }}>{postMatch.motm.name}</span>
              <span className={`text-sm font-black tabular-nums ${ratingColor(postMatch.motm.matchRating)}`}>{postMatch.motm.matchRating.toFixed(1)}</span>
              <span className="text-xs text-slate-500">({postMatch.motm.club})</span>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {[st.home, st.away].map(club => (
                <div key={club.name}>
                  <div className="text-[11px] uppercase tracking-[0.25em] text-slate-500 mb-1.5" style={{ color: club.color }}>{club.name} — player ratings</div>
                  <div className="space-y-0.5">
                    {topRatings(club.name).map(p => (
                      <div key={p.key} className="flex items-center text-sm py-0.5 border-b border-slate-800/60">
                        <span className="w-9 text-[10px] text-slate-500 font-semibold">{p.pos}</span>
                        <span className="flex-1 truncate">{p.name}</span>
                        <span className="text-[10px] text-slate-500 mr-2">
                          {p.goals ? `⚽${p.goals} ` : ""}{p.assists ? `🅰${p.assists} ` : ""}{p.saves ? `🧤${p.saves}` : ""}
                        </span>
                        <span className={`tabular-nums font-bold ${ratingColor(p.matchRating)}`}>{p.matchRating.toFixed(1)}</span>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Stats + Feed */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="rounded-2xl border border-slate-700/60 bg-slate-900/60 p-4">
            <div className="text-[11px] uppercase tracking-[0.25em] text-slate-500 mb-2">Match stats</div>
            <StatRow label="Possession" h={posH} a={100 - posH} fmt={(v) => `${v}%`} />
            <StatRow label="Shots" h={st.stats.home.shots} a={st.stats.away.shots} />
            <StatRow label="On target" h={st.stats.home.sot} a={st.stats.away.sot} />
            <StatRow label="xG" h={+st.stats.home.xg.toFixed(2)} a={+st.stats.away.xg.toFixed(2)} fmt={(v) => v.toFixed(2)} />
            <StatRow label="Corners" h={st.stats.home.corners} a={st.stats.away.corners} />
            <StatRow label="Fouls" h={st.stats.home.fouls} a={st.stats.away.fouls} />
            <StatRow label="Yellows" h={st.stats.home.yellows} a={st.stats.away.yellows} />

            {history.length > 0 && (
              <div className="mt-4">
                <div className="text-[11px] uppercase tracking-[0.25em] text-slate-500 mb-1.5">Season so far</div>
                {history.map((m, i) => (
                  <div key={i} className="flex items-center text-xs py-1 border-b border-slate-800/60">
                    <span className="w-12 text-slate-500 tabular-nums">Wk {m.week}</span>
                    <span className="flex-1 truncate text-slate-300">vs {m.opp}</span>
                    <span className="tabular-nums mr-2">{m.score}</span>
                    <span className={`w-5 h-5 rounded grid place-items-center font-bold text-[10px] ${
                      m.res === "W" ? "bg-emerald-500/80 text-slate-950" : m.res === "D" ? "bg-slate-500/80 text-slate-950" : "bg-red-500/80 text-slate-950"}`}>{m.res}</span>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div className="rounded-2xl border border-slate-700/60 bg-slate-900/60 p-4 flex flex-col">
            <div className="text-[11px] uppercase tracking-[0.25em] text-slate-500 mb-2">Live commentary</div>
            <div ref={feedRef} className="overflow-y-auto space-y-1.5 pr-1" style={{ maxHeight: 420 }}>
              {events.length === 0 && <div className="text-sm text-slate-500 italic">Kick off to see the engine narrate each tick.</div>}
              {events.map((e, i) => (
                <div key={i} className={`text-sm rounded-lg px-3 py-1.5 border ${
                  e.type === "goal" ? "bg-emerald-500/15 border-emerald-500/40 text-emerald-200 font-semibold" :
                  e.type === "card" ? "bg-amber-500/10 border-amber-500/30 text-amber-200" :
                  e.type === "whistle" ? "bg-sky-500/10 border-sky-500/30 text-sky-200 font-medium" :
                  e.type === "save" ? "bg-slate-800/80 border-slate-700 text-slate-200" :
                  "bg-slate-900/40 border-slate-800 text-slate-400"
                }`}>
                  <span className="tabular-nums text-slate-500 mr-2">{e.minute}'</span>{e.text}
                </div>
              ))}
            </div>
          </div>
        </div>

        <div className="mt-4 text-[11px] text-slate-600">
          Seeded RNG — same seed + fixture replays identically. Ratings use base + goal-diff + goals×1.6 + assists×1.1, gated by ratingsApplied to prevent the double-rating bug.
        </div>
      </div>
    </div>
  );
}
