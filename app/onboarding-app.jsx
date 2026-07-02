// onboarding-app.jsx
// Five-step welcome flow: Welcome → Goals → Self-assessment → Generating → Reveal.
// Persists preferences to localStorage, marks the user onboarded, and hands off
// to the dashboard with the freshly-generated plan.

const { useState: oS, useEffect: oE, useMemo: oM, useRef: oR } = React;

const ONB_TWEAKS = /*EDITMODE-BEGIN*/{
  "accent": "emerald",
  "skipGen": false,
  "apiBase": "http://localhost:5000",
  "demoMode": false
}/*EDITMODE-END*/;

const ONB_PALETTES = {
  emerald: { accent: 'oklch(0.62 0.14 155)', accentInk: 'oklch(0.32 0.10 155)', accentTint: 'oklch(0.95 0.04 155)' },
  indigo:  { accent: 'oklch(0.55 0.18 270)', accentInk: 'oklch(0.30 0.14 270)', accentTint: 'oklch(0.95 0.04 270)' },
  amber:   { accent: 'oklch(0.66 0.16 60)',  accentInk: 'oklch(0.36 0.12 60)',  accentTint: 'oklch(0.96 0.05 60)' },
};

/* ─────────────── Icons ─────────────── */
const OIcon = {
  Target: (p) => (<svg viewBox="0 0 18 18" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...p}><circle cx="9" cy="9" r="7"/><circle cx="9" cy="9" r="3.5"/><circle cx="9" cy="9" r="1" fill="currentColor"/></svg>),
  Stack:  (p) => (<svg viewBox="0 0 18 18" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M2.5 6L9 2.5l6.5 3.5L9 9.5 2.5 6z"/><path d="M2.5 9.5L9 13l6.5-3.5M2.5 13L9 16.5 15.5 13"/></svg>),
  Lock:   (p) => (<svg viewBox="0 0 18 18" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...p}><rect x="4" y="8.5" width="10" height="7.5" rx="2"/><path d="M6 8.5V6a3 3 0 0 1 6 0v2.5"/></svg>),
  Zap:    (p) => (<svg viewBox="0 0 18 18" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M10 2L4 11h4l-1 5 6-9h-4l1-5z"/></svg>),
  DB:     (p) => (<svg viewBox="0 0 18 18" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...p}><ellipse cx="9" cy="4" rx="6" ry="2"/><path d="M3 4v10c0 1.1 2.7 2 6 2s6-.9 6-2V4M3 9c0 1.1 2.7 2 6 2s6-.9 6-2"/></svg>),
  Calendar: (p) => (<svg viewBox="0 0 18 18" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...p}><rect x="2.5" y="3.5" width="13" height="12" rx="2"/><path d="M2.5 7h13M6 2v3M12 2v3"/></svg>),
  Check:  (p) => (<svg viewBox="0 0 14 14" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M2.5 7.5l3 3L11.5 4"/></svg>),
  Back:   (p) => (<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M13 8H3M7 4L3 8l4 4"/></svg>),
  Arrow:  (p) => (<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M3 8h10M9 4l4 4-4 4"/></svg>),
};

const GOALS = [
  { id: 'interview', label: 'Sharpen for a system design interview',  hint: 'Architecture, scaling, trade-offs',          icon: OIcon.Target },
  { id: 'ef',        label: 'Get production-ready in EF Core 8',       hint: 'Querying, migrations, performance',          icon: OIcon.Stack  },
  { id: 'cqrs',      label: 'Master CQRS + Clean Architecture',        hint: 'Pipelines, boundaries, testability',         icon: OIcon.Stack  },
  { id: 'auth',      label: 'Strengthen security & JWT fundamentals',  hint: 'Tokens, rotation, identity',                 icon: OIcon.Lock   },
  { id: 'perf',      label: 'Speed up slow APIs and noisy DB queries', hint: 'Caching, indexing, profiling',               icon: OIcon.Zap    },
  { id: 'pg',        label: 'Level up on PostgreSQL internals',         hint: 'EXPLAIN, locking, partitioning',             icon: OIcon.DB     },
  { id: 'daily',     label: 'Just practice daily, indefinitely',        hint: "We'll keep you sharp on a rolling plan",     icon: OIcon.Calendar },
];

const SKILL_LEVELS = [
  { id: 'novice',     label: 'Novice'     },
  { id: 'familiar',   label: 'Familiar'   },
  { id: 'strong',     label: 'Strong'     },
];

/* ═══════════════════════════════════════════════════════════════════════
   Step shell
   ═════════════════════════════════════════════════════════════════════ */

function Topbar({ step, totalSteps, onSkip }) {
  return (
    <header className="topbar-thin">
      <div className="flex items-center gap-2.5">
        <div className="tp-mark">T</div>
        <div className="font-semibold tracking-tight text-[14.5px]"
             style={{ color: 'var(--ink)', letterSpacing: '-0.015em' }}>
          Training Platform
        </div>
      </div>
      <div className="flex items-center gap-2.5">
        {Array(totalSteps).fill(0).map((_, i) => (
          <span key={i} className="step-dot"
                data-state={i < step ? 'done' : i === step ? 'active' : 'pending'} />
        ))}
      </div>
      <div>
        {onSkip ? (
          <button className="btn-text font-medium" onClick={onSkip}>
            Skip for now
          </button>
        ) : <span className="w-[100px]" />}
      </div>
    </header>
  );
}

function StageFooter({ left, right, hint }) {
  return (
    <div className="mt-8 flex items-center justify-between gap-4">
      <div className="flex items-center gap-3">{left}</div>
      <div className="flex items-center gap-3">
        {hint && <span className="text-[12.5px]" style={{ color: 'var(--ink-mute)' }}>{hint}</span>}
        {right}
      </div>
    </div>
  );
}

/* ═══════════════════════════════════════════════════════════════════════
   Step 1 — Welcome
   ═════════════════════════════════════════════════════════════════════ */

function StepWelcome({ user, onNext }) {
  // Drifting shapes — purely decorative
  const shapes = oM(() => Array(7).fill(0).map((_, i) => ({
    size: 12 + (i % 3) * 8,
    left: `${10 + i * 12}%`,
    top:  `${15 + (i % 4) * 18}%`,
    rotate: i * 23,
    dx: `${(i % 2 ? -1 : 1) * (30 + i * 10)}px`,
    dy: `${(i % 3 ? -1 : 1) * (40 + i * 12)}px`,
    color: i % 3 === 0 ? 'var(--accent)' : i % 3 === 1 ? 'var(--ink)' : 'oklch(0.75 0.06 80)',
    duration: 24 + i * 4,
  })), []);

  return (
    <div className="relative flex-1 flex flex-col items-center justify-center stage-enter"
         style={{ minHeight: 'calc(100vh - 80px)' }}>
      {/* Decorative background */}
      <div className="absolute inset-0 grid-bg" />
      {shapes.map((s, i) => (
        <div key={i} className="drift"
             style={{
               width: s.size, height: s.size, left: s.left, top: s.top,
               background: s.color,
               transform: `rotate(${s.rotate}deg)`,
               '--dx': s.dx, '--dy': s.dy, '--d': `${s.duration}s`,
             }} />
      ))}

      <div className="relative z-10 text-center container-narrow">
        <div className="eyebrow mb-5">// step 01 · welcome</div>
        <h1 className="font-semibold tracking-tight mx-auto"
            style={{ fontSize: 52, letterSpacing: '-0.035em', lineHeight: 1.05, color: 'var(--ink)', maxWidth: 560, textWrap: 'balance' }}>
          Welcome aboard, <span style={{ color: 'var(--accent-ink)' }}>{(user?.displayName || 'learner').split(' ')[0]}</span>.
        </h1>
        <p className="mt-5 text-[16px] mx-auto"
           style={{ color: 'var(--ink-soft)', lineHeight: 1.55, textWrap: 'pretty', maxWidth: 540 }}>
          We'll calibrate to where you actually struggle — not where you breeze through. Three quick steps, then your first daily plan is ready.
        </p>

        <div className="mt-10 flex items-center justify-center gap-3">
          <button className="btn btn-primary btn-lg" onClick={onNext}>
            Let's go <OIcon.Arrow />
          </button>
          <span className="font-mono text-[11px]" style={{ color: 'var(--ink-mute)' }}>
            takes ~2 minutes
          </span>
        </div>

        {/* What we ask */}
        <div className="mt-14 grid grid-cols-3 gap-3 rise-stagger" style={{ maxWidth: 560, margin: '56px auto 0' }}>
          <MiniCard num="01" label="Your goal" hint="What you're solving for" />
          <MiniCard num="02" label="Your level" hint="Self-assessment per topic" />
          <MiniCard num="03" label="Time budget" hint="Minutes per day" />
        </div>
      </div>
    </div>
  );
}

function MiniCard({ num, label, hint }) {
  return (
    <div className="card text-left" style={{ padding: '14px 16px' }}>
      <div className="font-mono text-[11px]" style={{ color: 'var(--ink-mute)' }}>{num}</div>
      <div className="mt-1.5 text-[13.5px] font-medium" style={{ color: 'var(--ink)', letterSpacing: '-0.005em' }}>
        {label}
      </div>
      <div className="mt-0.5 text-[11.5px]" style={{ color: 'var(--ink-soft)' }}>{hint}</div>
    </div>
  );
}

/* ═══════════════════════════════════════════════════════════════════════
   Step 2 — Goals + Minutes / day
   ═════════════════════════════════════════════════════════════════════ */

function StepGoals({ goals, setGoals, minutes, setMinutes, onBack, onNext }) {
  const toggle = (id) => {
    setGoals((g) => {
      const next = new Set(g);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });
  };

  const canNext = goals.size > 0;

  return (
    <div className="container-wide pb-20 stage-enter">
      <div className="eyebrow">// step 02 · what's the goal?</div>
      <h1 className="mt-2 font-semibold tracking-tight"
          style={{ fontSize: 32, letterSpacing: '-0.028em', color: 'var(--ink)', lineHeight: 1.15 }}>
        What are you here for?
      </h1>
      <p className="mt-2 text-[14.5px]" style={{ color: 'var(--ink-soft)' }}>
        Pick all that apply. We'll bias your daily plan toward these and let the rest grow at maintenance pace.
      </p>

      <div className="mt-7 flex flex-col gap-2.5 rise-stagger">
        {GOALS.map((g) => {
          const Ic = g.icon;
          const selected = goals.has(g.id);
          return (
            <button key={g.id} className="goal-card" data-selected={selected}
                    type="button" onClick={() => toggle(g.id)}>
              <span className="goal-icon"><Ic /></span>
              <span className="min-w-0">
                <span className="block text-[14.5px] font-medium"
                      style={{ color: 'var(--ink)', letterSpacing: '-0.005em' }}>
                  {g.label}
                </span>
                <span className="block text-[12.5px] mt-0.5" style={{ color: 'var(--ink-soft)' }}>
                  {g.hint}
                </span>
              </span>
              <span className="goal-check">
                {selected && <OIcon.Check />}
              </span>
            </button>
          );
        })}
      </div>

      {/* Minutes per day */}
      <div className="mt-8 card p-6">
        <div className="flex items-baseline justify-between mb-4">
          <div>
            <div className="eyebrow">time budget</div>
            <div className="mt-1.5 text-[14.5px] font-medium" style={{ color: 'var(--ink)' }}>
              Roughly how many minutes a day can you spend?
            </div>
          </div>
          <div className="font-semibold tabular-nums"
               style={{ fontSize: 28, letterSpacing: '-0.03em', color: 'var(--ink)' }}>
            {minutes}
            <span className="text-[14px]" style={{ color: 'var(--ink-mute)', marginLeft: 4, fontWeight: 400 }}>min</span>
          </div>
        </div>
        <input type="range" min={5} max={60} step={5} value={minutes}
               className="slider"
               onChange={(e) => setMinutes(parseInt(e.target.value, 10))} />
        <div className="flex justify-between mt-2 font-mono text-[10.5px]" style={{ color: 'var(--ink-mute)' }}>
          <span>5</span><span>15</span><span>30</span><span>45</span><span>60</span>
        </div>
        <div className="mt-3 text-[12.5px]" style={{ color: 'var(--ink-soft)' }}>
          {minutes <= 10
            ? "Tight budget. We'll keep daily plans to 2–3 quick questions."
            : minutes <= 25
              ? 'Comfortable cadence — 4–6 items per day.'
              : minutes <= 45
                ? "Solid runway. We'll mix in coding challenges 2–3× per week."
                : "Big runway. Expect a deep coding challenge most days."}
        </div>
      </div>

      <StageFooter
        left={<button className="btn btn-ghost" onClick={onBack}><OIcon.Back /> Back</button>}
        right={
          <button className="btn btn-primary btn-lg" disabled={!canNext} onClick={onNext}>
            Continue <OIcon.Arrow />
          </button>
        }
        hint={canNext ? null : 'Pick at least one goal'}
      />
    </div>
  );
}

/* ═══════════════════════════════════════════════════════════════════════
   Step 3 — Self-assessment
   ═════════════════════════════════════════════════════════════════════ */

function StepAssessment({ assessments, setAssessments, topics, onBack, onNext }) {
  const setLevel = (topicId, lvl) =>
    setAssessments((prev) => ({ ...prev, [topicId]: lvl }));

  // Count filled
  const filled = Object.keys(assessments).length;
  const total = topics.length;
  const canNext = filled === total;

  return (
    <div className="container-wide pb-20 stage-enter">
      <div className="eyebrow">// step 03 · self-assessment</div>
      <h1 className="mt-2 font-semibold tracking-tight"
          style={{ fontSize: 32, letterSpacing: '-0.028em', color: 'var(--ink)', lineHeight: 1.15 }}>
        Where do you stand today?
      </h1>
      <p className="mt-2 text-[14.5px]" style={{ color: 'var(--ink-soft)' }}>
        Best guess is fine — we'll refine it from your first few answers. Be honest about "Strong"; we won't waste your time on basics.
      </p>

      {/* Progress */}
      <div className="mt-5 flex items-center gap-3">
        <div className="flex-1 h-[5px] rounded-full" style={{ background: 'oklch(0.94 0.006 85)' }}>
          <div className="h-full rounded-full" style={{
            width: `${(filled / total) * 100}%`,
            background: 'var(--accent)',
            transition: 'width .35s ease',
          }} />
        </div>
        <span className="font-mono text-[11.5px] tabular-nums" style={{ color: 'var(--ink-mute)' }}>
          {filled} / {total}
        </span>
      </div>

      <div className="mt-6 flex flex-col gap-2.5 rise-stagger">
        {topics.map((t) => (
          <div key={t.id} className="sa-row">
            <div className="min-w-0">
              <div className="text-[14px] font-medium"
                   style={{ color: 'var(--ink)', letterSpacing: '-0.005em' }}>
                {t.name}
              </div>
              <div className="mt-1 text-[12px]" style={{ color: 'var(--ink-soft)' }}>
                {t.description.split('—')[0].trim()}
              </div>
            </div>
            <div className="sa-seg">
              {SKILL_LEVELS.map((lvl) => (
                <button key={lvl.id} type="button"
                        data-active={assessments[t.id] === lvl.id}
                        onClick={() => setLevel(t.id, lvl.id)}>
                  {lvl.label}
                </button>
              ))}
            </div>
          </div>
        ))}
      </div>

      <StageFooter
        left={<button className="btn btn-ghost" onClick={onBack}><OIcon.Back /> Back</button>}
        right={
          <button className="btn btn-primary btn-lg" disabled={!canNext} onClick={onNext}>
            Generate my plan <OIcon.Arrow />
          </button>
        }
        hint={canNext ? null : `${total - filled} to go`}
      />
    </div>
  );
}

/* ═══════════════════════════════════════════════════════════════════════
   Step 4 — Generating animation
   ═════════════════════════════════════════════════════════════════════ */

function StepGenerating({ onDone, demoMode, apiBase, skipGen }) {
  const phases = [
    { id: 'assess', label: 'Mapping your self-assessment to topic dependency graph', duration: 1100 },
    { id: 'risk',   label: 'Scoring forgetting risk + readiness for each topic',     duration: 1100 },
    { id: 'pick',   label: 'Picking today\'s items — weak areas, revision, stretch', duration: 1300 },
    { id: 'order',  label: 'Sequencing for spaced repetition + your time budget',     duration: 900 },
  ];

  const [phaseIdx, setPhaseIdx] = oS(0);
  const [done, setDone] = oS(false);

  oE(() => {
    if (skipGen) {
      setTimeout(() => { setPhaseIdx(phases.length); setDone(true); onDone(); }, 200);
      return;
    }
    if (phaseIdx >= phases.length) {
      // Trigger the plan generate API (or simulated)
      generatePlan({ apiBase, demoMode })
        .catch(() => null)
        .finally(() => {
          setDone(true);
          setTimeout(() => onDone(), 700);
        });
      return;
    }
    const id = setTimeout(() => setPhaseIdx((i) => i + 1), phases[phaseIdx].duration);
    return () => clearTimeout(id);
  }, [phaseIdx, skipGen]);

  return (
    <div className="container-narrow pb-20 stage-enter flex flex-col items-center"
         style={{ minHeight: 'calc(100vh - 80px)', justifyContent: 'center' }}>
      <div className="eyebrow">// step 04 · generating</div>
      <h1 className="mt-2 font-semibold tracking-tight text-center"
          style={{ fontSize: 30, letterSpacing: '-0.028em', color: 'var(--ink)', lineHeight: 1.15 }}>
        Tailoring your first plan.
      </h1>
      <p className="mt-2 text-center text-[14.5px]" style={{ color: 'var(--ink-soft)' }}>
        This usually takes a few seconds.
      </p>

      {/* Pulsing ring */}
      <div className="my-10 relative" style={{ width: 140, height: 140 }}>
        <svg width="140" height="140" viewBox="0 0 140 140">
          <circle cx="70" cy="70" r="60" fill="none" stroke="oklch(0.93 0.006 85)" strokeWidth="2" />
          <circle cx="70" cy="70" r="60" fill="none" stroke="var(--accent)" strokeWidth="3"
                  strokeLinecap="round"
                  strokeDasharray={`${(60*2*Math.PI) * (phaseIdx / phases.length)} ${(60*2*Math.PI)}`}
                  transform="rotate(-90 70 70)"
                  style={{ transition: 'stroke-dasharray .4s ease' }} />
          <circle cx="70" cy="70" r="48" fill="none" stroke="var(--accent)" strokeWidth="1"
                  strokeDasharray="2 6" opacity="0.4">
            <animateTransform attributeName="transform" type="rotate" from="0 70 70" to="360 70 70" dur="20s" repeatCount="indefinite" />
          </circle>
        </svg>
        <div className="absolute inset-0 flex items-center justify-center">
          <span className="font-mono text-[13px] tabular-nums" style={{ color: 'var(--ink)' }}>
            {Math.round((phaseIdx / phases.length) * 100)}%
          </span>
        </div>
      </div>

      {/* Phase lines */}
      <div className="w-full flex flex-col gap-2.5">
        {phases.map((p, i) => {
          const state = i < phaseIdx ? 'done' : i === phaseIdx ? 'active' : 'pending';
          return (
            <div key={p.id} className="gen-line" data-state={state}>
              <span>
                {state === 'done'
                  ? <span className="w-[18px] h-[18px] rounded-full inline-flex items-center justify-center"
                          style={{ background: 'var(--accent)', color: '#fff' }}>
                      <OIcon.Check />
                    </span>
                  : state === 'active'
                    ? <span className="gen-spinner-dot" />
                    : <span className="w-[18px] h-[18px] rounded-full border" style={{ borderColor: 'var(--line)' }} />
                }
              </span>
              <span className="text-[13.5px]"
                    style={{ color: state === 'pending' ? 'var(--ink-mute)' : 'var(--ink)' }}>
                {p.label}
              </span>
              <span className="font-mono text-[10.5px]" style={{ color: 'var(--ink-mute)' }}>
                {state === 'done' ? '✓' : state === 'active' ? 'running' : 'queued'}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

/* ═══════════════════════════════════════════════════════════════════════
   Step 5 — Reveal
   ═════════════════════════════════════════════════════════════════════ */

function StepReveal({ goals, minutes, assessments, plan, onFinish }) {
  const itemCount = plan?.items?.length ?? 0;
  const estimatedTotal = itemCount > 0
    ? plan.items.reduce((sum, it) => sum + (it.meta?.estimatedMinutes ?? 5), 0)
    : minutes;

  const topGoals = Array.from(goals).slice(0, 3).map(id => GOALS.find(g => g.id === id)?.label).filter(Boolean);

  // Burst pills (decorative)
  const burstPills = oM(() => {
    return Object.entries(TOPIC_NAMES)
      .map(([id, name], i) => ({
        name,
        delay: i * 0.15,
        dur: 2.4 + (i % 3) * 0.6,
        fromX: `${(Math.random() * 60 - 30)}px`,
        toX: `${(Math.random() * 240 - 120)}px`,
        rot: `${(Math.random() * 50 - 25)}deg`,
      }));
  }, []);

  return (
    <div className="container-narrow stage-enter flex flex-col items-center justify-center"
         style={{ minHeight: 'calc(100vh - 80px)', position: 'relative' }}>
      {/* Pill burst (one-shot) */}
      <div style={{ position: 'absolute', inset: 0, overflow: 'hidden', pointerEvents: 'none' }}>
        {burstPills.map((p, i) => (
          <span key={i} className="pill-burst"
                style={{
                  '--from-x': p.fromX,
                  '--to-x': p.toX,
                  '--rot': p.rot,
                  '--dur': `${p.dur}s`,
                  '--delay': `${p.delay}s`,
                }}>
            {p.name}
          </span>
        ))}
      </div>

      <div className="relative z-10 text-center">
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full"
             style={{ background: 'var(--accent-tint)', color: 'var(--accent-ink)' }}>
          <OIcon.Check />
          <span className="font-mono text-[11.5px] tracking-wide">PLAN READY</span>
        </div>

        <h1 className="mt-5 font-semibold tracking-tight"
            style={{ fontSize: 44, letterSpacing: '-0.032em', color: 'var(--ink)', lineHeight: 1.1, textWrap: 'balance', maxWidth: 540, margin: '20px auto 0' }}>
          Your first day is ready.
        </h1>
        <p className="mt-4 text-[15.5px] mx-auto"
           style={{ color: 'var(--ink-soft)', lineHeight: 1.55, maxWidth: 520, textWrap: 'pretty' }}>
          <span style={{ color: 'var(--ink)', fontWeight: 500 }}>{itemCount} items</span>,
          {' '}roughly <span style={{ color: 'var(--ink)', fontWeight: 500 }}>{estimatedTotal} minutes</span>.
          Calibrated to your goals — we'll adjust as you go.
        </p>

        {/* Recap card */}
        <div className="mt-8 card text-left" style={{ padding: '20px 22px', maxWidth: 460, margin: '32px auto 0' }}>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <div className="eyebrow" style={{ fontSize: 10.5 }}>Goals</div>
              <ul className="mt-2 flex flex-col gap-1.5">
                {topGoals.length ? topGoals.map((g, i) => (
                  <li key={i} className="text-[13px] flex items-start gap-1.5">
                    <span style={{ color: 'var(--accent-ink)', marginTop: 4, fontSize: 8 }}>●</span>
                    <span style={{ color: 'var(--ink)' }}>{g}</span>
                  </li>
                )) : <li className="text-[13px]" style={{ color: 'var(--ink-mute)' }}>—</li>}
              </ul>
            </div>
            <div>
              <div className="eyebrow" style={{ fontSize: 10.5 }}>Time / day</div>
              <div className="mt-1.5 font-semibold tabular-nums" style={{ fontSize: 24, letterSpacing: '-0.025em' }}>
                {minutes}<span className="text-[13px]" style={{ color: 'var(--ink-mute)', marginLeft: 4, fontWeight: 400 }}>min</span>
              </div>
              <div className="text-[12px] mt-1" style={{ color: 'var(--ink-soft)' }}>
                You can change this anytime in settings.
              </div>
            </div>
          </div>
        </div>

        <div className="mt-10">
          <button className="btn btn-primary btn-lg" onClick={onFinish}>
            Open today's plan <OIcon.Arrow />
          </button>
        </div>
      </div>
    </div>
  );
}

/* ═══════════════════════════════════════════════════════════════════════
   Main App
   ═════════════════════════════════════════════════════════════════════ */

function App() {
  const [t, setTweak] = useTweaks(ONB_TWEAKS);
  const [step, setStep] = oS(0);
  const [user, setUser] = oS(getCurrentUser);

  const [goals, setGoals] = oS(new Set());
  const [minutes, setMinutes] = oS(20);
  const [assessments, setAssessments] = oS({}); // topicId → 'novice'|'familiar'|'strong'
  const [topics, setTopics] = oS(MOCK_TOPICS);
  const [plan, setPlan] = oS(null);

  const totalSteps = 5;

  // Load topics — demo mode returns MOCK_TOPICS, real mode hits /api/topics so
  // the self-assessment screen submits actual topic UUIDs the backend knows.
  oE(() => {
    fetchTopics({ apiBase: t.apiBase, demoMode: t.demoMode })
      .then((list) => {
        if (Array.isArray(list) && list.length > 0) {
          setTopics(list);
          setAssessments((prev) => {
            const allowed = new Set(list.map((topic) => topic.id));
            const next = {};
            for (const key of Object.keys(prev)) {
              if (allowed.has(key)) next[key] = prev[key];
            }
            return next;
          });
        }
      })
      .catch(() => { /* fall back to MOCK_TOPICS */ });
  }, [t.demoMode]);

  // Apply accent
  oE(() => {
    const p = ONB_PALETTES[t.accent] || ONB_PALETTES.emerald;
    document.documentElement.style.setProperty('--accent', p.accent);
    document.documentElement.style.setProperty('--accent-ink', p.accentInk);
    document.documentElement.style.setProperty('--accent-tint', p.accentTint);
  }, [t.accent]);

  // No valid user means we landed here without auth — go back to sign in.
  oE(() => {
    if (!user) {
      window.location.replace('Auth.html');
    }
  }, []);

  const handleSkip = () => {
    localStorage.setItem('training_onboarded', 'true');
    window.location.href = 'Dashboard.html';
  };

  const handleFinish = async () => {
    const prefs = {
      goals: Array.from(goals),
      minutesPerDay: minutes,
      assessments,
      completedAt: new Date().toISOString(),
    };
    localStorage.setItem('training_prefs', JSON.stringify(prefs));
    localStorage.setItem('training_onboarded', 'true');

    const levelToBackend = { novice: 'Novice', familiar: 'Familiar', strong: 'Strong' };
    const validTopicIds = new Set(topics.map((topic) => topic.id));
    const assessmentPayload = Object.entries(assessments)
      .filter(([topicId, level]) => validTopicIds.has(topicId) && levelToBackend[level])
      .map(([topicId, level]) => ({ topicId, level: levelToBackend[level] }));

    try {
      await completeOnboarding({ apiBase: t.apiBase, demoMode: t.demoMode }, {
        dailyQuestionTarget: Math.max(1, Math.round(minutes / 2.5)),
        dailyStudyMinutes: minutes,
        dailyCodingChallengeTarget: 1,
        dailyScenarioChallengeTarget: 1,
        includeWeekends: true,
        goals: Array.from(goals),
        assessments: assessmentPayload,
      });
    } catch (err) {
      // Don't block the flow if the sync fails — user can adjust in Settings.
      console.warn('Onboarding sync failed:', err);
    }

    window.location.href = 'Dashboard.html';
  };

  // After generation, fetch the new plan to power the reveal stats
  oE(() => {
    if (step === 4 && !plan) {
      fetchTodayPlan({ apiBase: t.apiBase, demoMode: t.demoMode })
        .then(setPlan).catch(() => setPlan(null));
    }
  }, [step]);

  return (
    <>
      <Topbar step={step} totalSteps={totalSteps} onSkip={step < 3 ? handleSkip : null} />

      <main style={{ minHeight: 'calc(100vh - 60px)', paddingBottom: 24 }}>
        {step === 0 && <StepWelcome user={user} onNext={() => setStep(1)} />}
        {step === 1 && (
          <StepGoals
            goals={goals} setGoals={setGoals}
            minutes={minutes} setMinutes={setMinutes}
            onBack={() => setStep(0)} onNext={() => setStep(2)}
          />
        )}
        {step === 2 && (
          <StepAssessment
            assessments={assessments} setAssessments={setAssessments}
            topics={topics}
            onBack={() => setStep(1)} onNext={() => setStep(3)}
          />
        )}
        {step === 3 && (
          <StepGenerating
            onDone={() => setStep(4)}
            demoMode={t.demoMode}
            apiBase={t.apiBase}
            skipGen={t.skipGen}
          />
        )}
        {step === 4 && (
          <StepReveal
            goals={goals} minutes={minutes} assessments={assessments}
            plan={plan}
            onFinish={handleFinish}
          />
        )}
      </main>

      <TweaksPanel>
        <TweakSection label="Visual" />
        <TweakRadio label="Accent" value={t.accent}
                    options={['emerald', 'indigo', 'amber']}
                    onChange={(v) => setTweak('accent', v)} />

        <TweakSection label="Dev shortcuts" />
        <TweakToggle label="Skip generation animation" value={t.skipGen}
                     onChange={(v) => setTweak('skipGen', v)} />
        <TweakToggle label="Demo mode" value={t.demoMode}
                     onChange={(v) => setTweak('demoMode', v)} />

        <TweakSection label="Jump to step" />
        <div className="grid grid-cols-5 gap-1.5">
          {['Welcome', 'Goals', 'Levels', 'Generating', 'Reveal'].map((label, i) => (
            <button key={i}
                    className="text-[10.5px] py-1.5 rounded-md"
                    style={{
                      background: step === i ? 'var(--ink)' : 'oklch(0.96 0.006 88)',
                      color: step === i ? '#fff' : 'var(--ink-soft)',
                      fontFamily: 'Geist Mono, monospace',
                    }}
                    onClick={() => setStep(i)}>
              {String(i + 1).padStart(2, '0')}
            </button>
          ))}
        </div>

        <TweakButton label="Reset onboarding flag" onClick={() => {
          localStorage.removeItem('training_onboarded');
          alert('Done — next login will route through onboarding.');
        }} />
      </TweaksPanel>
    </>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<App />);
