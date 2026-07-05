// dashboard-widgets.jsx
// Reusable widgets for the Dashboard: icons, progress ring, plan items,
// mastery grid/radial, weak-areas, trend chart, stats trio.

const { useState: useS, useEffect: useE, useMemo: useM, useRef: useR } = React;

/* ─────────────── Icons (stroke, ~16px) ─────────────── */
const Icon = {
  Question: (p) => (<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...p}><circle cx="8" cy="8" r="6.5" /><path d="M6 6a2 2 0 1 1 2.6 1.9c-.5.2-.6.5-.6.9V9.5" /><circle cx="8" cy="12" r=".5" fill="currentColor"/></svg>),
  Code:     (p) => (<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M5 5L2 8l3 3M11 5l3 3-3 3M9.5 3.5l-3 9" /></svg>),
  Scenario: (p) => (<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...p}><rect x="2.5" y="3" width="11" height="10" rx="2" /><path d="M5 6.5h6M5 9h6M5 11.5h4" /></svg>),
  Check:    (p) => (<svg viewBox="0 0 16 16" width="13" height="13" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M3 8.5l3.2 3.2L13 5" /></svg>),
  Arrow:    (p) => (<svg viewBox="0 0 16 16" width="13" height="13" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M3 8h10M9 4l4 4-4 4" /></svg>),
  Refresh:  (p) => (<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M13 3v3.5h-3.5" /><path d="M3 13v-3.5h3.5" /><path d="M13 6.5A5 5 0 0 0 4 5.5M3 9.5A5 5 0 0 0 12 10.5" /></svg>),
  Flame:    (p) => (<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M8 14c-2.8 0-5-1.7-5-4.4 0-1.4.6-2.4 1.4-3.1.2 1 .6 1.4 1.2 1.5C5 6.6 5.6 4.7 7.4 3 7.4 5 8 5.5 9.6 6.8c.8.7 1.4 1.7 1.4 2.8 0 2.7-2.2 4.4-5 4.4z" /></svg>),
  Clock:    (p) => (<svg viewBox="0 0 16 16" width="13" height="13" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...p}><circle cx="8" cy="8" r="6" /><path d="M8 4.5V8l2 1.5" /></svg>),
  Trophy:   (p) => (<svg viewBox="0 0 16 16" width="13" height="13" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M5 3h6v3.5a3 3 0 0 1-6 0V3z" /><path d="M3.5 4h1.5M11 4h1.5M5 12h6M6.5 12v-2.2M9.5 12v-2.2" /></svg>),
  Spark:    (p) => (<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M8 2v3M8 11v3M2 8h3M11 8h3M4 4l2 2M10 10l2 2M12 4l-2 2M4 12l2-2" /></svg>),
  Calendar: (p) => (<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...p}><rect x="2.5" y="3.5" width="11" height="10" rx="2" /><path d="M2.5 6.5h11M5.5 2v2M10.5 2v2" /></svg>),
  Caret:    (p) => (<svg viewBox="0 0 16 16" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M5 6l3 3 3-3" /></svg>),
  Spinner:  (p) => (<svg viewBox="0 0 16 16" width="13" height="13" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" {...p}><circle cx="8" cy="8" r="6" opacity=".25"/><path d="M14 8a6 6 0 0 0-6-6"><animateTransform attributeName="transform" type="rotate" from="0 8 8" to="360 8 8" dur=".9s" repeatCount="indefinite"/></path></svg>),
  AlertCircle: (p) => (<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...p}><circle cx="8" cy="8" r="6.5"/><path d="M8 5v3.5M8 11h.01"/></svg>),
};

/* ─────────────── Type tile for plan items ─────────────── */
function TypeTile({ type }) {
  if (type === StudyPlanItemType.CodingChallenge) {
    return <span className="type-tile type-coding"><Icon.Code /></span>;
  }
  if (type === StudyPlanItemType.ScenarioChallenge) {
    return <span className="type-tile type-scenario"><Icon.Scenario /></span>;
  }
  return <span className="type-tile type-question"><Icon.Question /></span>;
}
function typeLabel(type) {
  if (type === StudyPlanItemType.CodingChallenge) return 'Kod';
  if (type === StudyPlanItemType.ScenarioChallenge) return 'Senaryo';
  return 'Soru';
}
function categoryLabel(c) {
  // SourceCategory is freeform string in backend; keep it readable
  const map = { WeakArea: 'Zayıf alan', Revision: 'Tekrar', NewMaterial: 'Yeni', Stretch: 'Zorlayıcı', weak: 'zayıf', recent: 'yakın', strong: 'güçlü', new: 'yeni', challenge: 'görev' };
  return map[c] || c;
}
function difficultyLabel(d) {
  // The API serializes enums as strings ("Intermediate"); mocks use numbers.
  if (typeof d === 'string' && d) return d;
  const en = { Fundamental: 'Temel', Intermediate: 'Orta', Advanced: 'İleri', Expert: 'Uzman' };
  if (typeof d === 'string' && en[d]) return en[d];
  const map = { 1: 'Temel', 2: 'Orta', 3: 'İleri', 4: 'Uzman' };
  return map[d] || '—';
}

// Numeric rank (1-4) for a difficulty that may arrive as string or number.
function difficultyRank(d) {
  if (typeof d === 'number') return Math.max(1, Math.min(4, d || 1));
  const order = { Fundamental: 1, Intermediate: 2, Advanced: 3, Expert: 4 };
  return order[d] || 1;
}

/* ─────────────── Priority dots (4-level) ─────────────── */
function PrioDots({ value }) {
  // value ~0-1; show 1-4 filled
  const filled = value >= 0.85 ? 4 : value >= 0.7 ? 3 : value >= 0.5 ? 2 : 1;
  return (
    <span className="prio-dots" title={`Priority ${(value*100).toFixed(0)}`}>
      {[0,1,2,3].map(i => <i key={i} data-on={i < filled ? 'true' : 'false'} />)}
    </span>
  );
}

/* ─────────────── Progress Ring ─────────────── */
function ProgressRing({ value = 0, size = 64, stroke = 6, children }) {
  const r = (size - stroke) / 2;
  const c = 2 * Math.PI * r;
  const [shown, setShown] = useS(0);
  useE(() => {
    let raf, start = performance.now();
    const dur = 900;
    const fr = (now) => {
      const t = Math.min(1, (now - start) / dur);
      const ease = 1 - Math.pow(1 - t, 3);
      setShown(value * ease);
      if (t < 1) raf = requestAnimationFrame(fr);
    };
    raf = requestAnimationFrame(fr);
    return () => cancelAnimationFrame(raf);
  }, [value]);
  return (
    <div className="relative inline-flex items-center justify-center" style={{ width: size, height: size }}>
      <svg width={size} height={size}>
        <circle cx={size/2} cy={size/2} r={r} fill="none" stroke="oklch(0.93 0.006 85)" strokeWidth={stroke} />
        <circle cx={size/2} cy={size/2} r={r} fill="none" stroke="var(--accent)" strokeWidth={stroke}
          strokeLinecap="round" strokeDasharray={c} strokeDashoffset={c * (1 - shown)}
          transform={`rotate(-90 ${size/2} ${size/2})`} />
      </svg>
      <div className="absolute inset-0 flex items-center justify-center">{children}</div>
    </div>
  );
}

/* ─────────────── Plan item row ─────────────── */
function PlanRow({ item, isActive, onClick }) {
  const meta = item.meta || {};
  return (
    <div className="plan-row" data-done={item.isCompleted} data-active={isActive} onClick={onClick}>
      <TypeTile type={item.itemType} />
      <div className="min-w-0">
        <div className="plan-title text-[14px] font-medium leading-snug truncate"
             style={{ color: 'var(--ink)', letterSpacing: '-0.005em' }}>
          {item.title}
        </div>
        <div className="flex items-center gap-2 mt-1.5">
          <span className="topic-chip">{TOPIC_NAMES[item.topicId] || item.topicId}</span>
          <span className="text-[11.5px] font-mono" style={{ color: 'var(--ink-mute)' }}>
            {typeLabel(item.itemType)} · {categoryLabel(item.sourceCategory)}
          </span>
        </div>
      </div>
      <div className="flex flex-col items-end gap-1">
        <div className="flex items-center gap-1 text-[11.5px] font-mono" style={{ color: 'var(--ink-mute)' }}>
          <Icon.Clock /> {meta.estimatedMinutes ?? 5}m
        </div>
        <PrioDots value={item.priority} />
      </div>
      <div className="w-7 flex justify-end">
        {item.isCompleted ? (
          <span className="w-6 h-6 rounded-full inline-flex items-center justify-center"
                style={{ background: 'var(--accent)', color: '#fff' }}>
            <Icon.Check />
          </span>
        ) : isActive ? (
          <span className="w-6 h-6 rounded-full inline-flex items-center justify-center"
                style={{ background: 'var(--ink)', color: '#fff' }}>
            <Icon.Arrow />
          </span>
        ) : (
          <span className="w-6 h-6 rounded-full border inline-flex items-center justify-center"
                style={{ borderColor: 'var(--line-strong)', color: 'var(--ink-mute)' }} />
        )}
      </div>
    </div>
  );
}

/* ─────────────── Today's plan card ─────────────── */
function TodayCard({ plan, loading, onGenerate, generating, onItemClick }) {
  const dateLabel = useM(() => {
    if (!plan) return null;
    const d = new Date(plan.studyDateUtc);
    return d.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' });
  }, [plan]);

  const completed = plan ? plan.items.filter(i => i.isCompleted).length : 0;
  const total = plan ? plan.items.length : 0;
  const pct = total ? completed / total : 0;
  const firstActive = plan?.items.find(i => !i.isCompleted);
  const statusLabel = pct === 1 ? 'Tamamlandı' : pct > 0 ? 'Devam ediyor' : 'Başlanmadı';

  return (
    <section className="card overflow-hidden">
      <div className="card-pad flex items-start justify-between gap-4 pb-3">
        <div>
          <div className="eyebrow flex items-center gap-2">
            <Icon.Calendar /> Bugünün planı
          </div>
          <h2 className="mt-2 font-semibold tracking-tight"
              style={{ fontSize: 22, letterSpacing: '-0.025em', color: 'var(--ink)' }}>
            {loading ? <span className="skel inline-block h-[24px] w-[260px]" /> : dateLabel}
          </h2>
          <div className="flex items-center gap-2 mt-2">
            <span className="pill">
              <span className="dot" style={{ background: pct === 1 ? 'var(--accent)' : pct > 0 ? 'var(--warn)' : 'var(--ink-mute)' }} />
              {statusLabel}
            </span>
            {plan && (
              <span className="text-[11.5px] font-mono" style={{ color: 'var(--ink-mute)' }}>
                {fmtRelative(plan.generatedAtUtc)} oluşturuldu
              </span>
            )}
          </div>
        </div>

        <div className="flex items-center gap-3">
          {plan && (
            <div className="text-right hidden sm:block">
              <div className="text-[11px] font-mono uppercase tracking-wider" style={{ color: 'var(--ink-mute)' }}>
                Progress
              </div>
              <div className="font-semibold tabular-nums" style={{ fontSize: 18, letterSpacing: '-0.02em' }}>
                {completed}<span style={{ color: 'var(--ink-mute)', fontWeight: 400 }}> / {total}</span>
              </div>
            </div>
          )}
          <ProgressRing value={pct} size={56} stroke={5}>
            <span className="font-mono text-[11.5px] tabular-nums font-medium">
              {Math.round(pct * 100)}%
            </span>
          </ProgressRing>
        </div>
      </div>

      {/* Items */}
      <div className="px-6 pb-5">
        {loading ? (
          <div className="flex flex-col gap-2.5">
            {[1,2,3,4].map(i => <div key={i} className="skel h-[72px]" />)}
          </div>
        ) : !plan || plan.items.length === 0 ? (
          <EmptyPlan onGenerate={onGenerate} generating={generating} />
        ) : (
          <div className="flex flex-col gap-2.5 rise-stagger">
            {plan.items.map((it) => (
              <PlanRow
                key={it.id}
                item={it}
                isActive={firstActive && firstActive.id === it.id}
                onClick={() => onItemClick && onItemClick(it)}
              />
            ))}
          </div>
        )}
      </div>

      {/* Footer CTAs */}
      <div className="hr" />
      <div className="px-6 py-4 flex items-center justify-between gap-3">
        <div className="text-[12.5px]" style={{ color: 'var(--ink-soft)' }}>
          {firstActive
            ? <>Sırada: <span style={{ color: 'var(--ink)', fontWeight: 500 }}>{firstActive.title}</span></>
            : 'You\'re all caught up for today.'}
        </div>
        <div className="flex items-center gap-2">
          <button className="btn btn-ghost" onClick={onGenerate} disabled={generating}>
            {generating ? <><Icon.Spinner /> Regenerating…</> : <><Icon.Refresh /> Regenerate</>}
          </button>
          <button className="btn btn-primary" disabled={!firstActive}
                  onClick={() => firstActive && onItemClick && onItemClick(firstActive)}>
            {firstActive ? <>Antrenmana devam et <Icon.Arrow /></> : 'Hepsi tamam'}
          </button>
        </div>
      </div>
    </section>
  );
}

function EmptyPlan({ onGenerate, generating }) {
  return (
    <div className="rounded-xl border border-dashed flex flex-col items-center justify-center text-center py-10 px-6"
         style={{ borderColor: 'var(--line-strong)', background: 'oklch(0.985 0.006 88)' }}>
      <Icon.Spark />
      <div className="font-medium mt-3" style={{ fontSize: 15, color: 'var(--ink)' }}>
        Bugün için henüz plan yok.
      </div>
      <p className="mt-1 text-[13px]" style={{ color: 'var(--ink-soft)' }}>
        Bir plan oluştur — en zayıf konularına ve tekrar takvimine göre ayarlayalım.
      </p>
      <button className="btn btn-primary mt-4" onClick={onGenerate} disabled={generating}>
        {generating ? <><Icon.Spinner /> Oluşturuluyor…</> : <>Bugünün planını oluştur <Icon.Arrow /></>}
      </button>
    </div>
  );
}

function fmtRelative(iso) {
  const t = new Date(iso).getTime();
  const diff = Date.now() - t;
  const m = Math.round(diff / 60000);
  if (m < 1) return 'az önce';
  if (m < 60) return `${m} dk önce`;
  const h = Math.round(m / 60);
  if (h < 24) return `${h} sa önce`;
  const d = Math.round(h / 24);
  return `${d}d ago`;
}

/* ─────────────── Stats trio ─────────────── */
function StatCard({ icon, label, value, suffix, accent = false, hint, trend }) {
  return (
    <div className="card card-pad" style={{ padding: '18px 20px' }}>
      <div className="flex items-center gap-2">
        <span className="w-6 h-6 rounded-md inline-flex items-center justify-center"
              style={{
                background: accent ? 'var(--accent-tint)' : 'oklch(0.96 0.006 88)',
                color: accent ? 'var(--accent-ink)' : 'var(--ink-soft)',
              }}>
          {icon}
        </span>
        <span className="eyebrow" style={{ letterSpacing: '0.1em', fontSize: 11 }}>{label}</span>
      </div>
      <div className="mt-3 flex items-baseline gap-1.5">
        <span className="font-semibold tabular-nums" style={{ fontSize: 30, letterSpacing: '-0.03em', color: 'var(--ink)' }}>
          {value}
        </span>
        {suffix && <span className="text-[13.5px]" style={{ color: 'var(--ink-mute)' }}>{suffix}</span>}
      </div>
      <div className="flex items-center justify-between mt-1.5">
        <div className="text-[12px]" style={{ color: 'var(--ink-soft)' }}>{hint}</div>
        {trend && <Sparkline values={trend} />}
      </div>
    </div>
  );
}

function Sparkline({ values, width = 60, height = 22 }) {
  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = max - min || 1;
  const pts = values.map((v, i) => {
    const x = (i / (values.length - 1)) * width;
    const y = height - ((v - min) / range) * (height - 4) - 2;
    return `${x},${y}`;
  }).join(' ');
  return (
    <svg className="spark" viewBox={`0 0 ${width} ${height}`}>
      <polyline points={pts} fill="none" stroke="var(--accent)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function StatsTrio({ dash, loading }) {
  if (loading || !dash) {
    return (
      <div className="grid grid-cols-3 gap-3">
        {[1,2,3].map(i => <div key={i} className="card skel" style={{ height: 110 }} />)}
      </div>
    );
  }
  const accuracyTrend = dash.learningTrend.slice(-7).map(p => p.accuracy);
  const attemptsTrend = dash.learningTrend.slice(-7).map(p => p.attempts);
  return (
    <div className="grid grid-cols-3 gap-3">
      <StatCard
        icon={<Icon.Flame />}
        label="Study streak"
        value={dash.consistencyDays}
        suffix="days"
        accent
        hint="Keep it going today."
        trend={attemptsTrend}
      />
      <StatCard
        icon={<Icon.Clock />}
        label="Avg response"
        value={dash.averageResponseTimeSeconds.toFixed(0)}
        suffix="s"
        hint="Down 8s vs. last week."
        trend={[...accuracyTrend].reverse()}
      />
      <StatCard
        icon={<Icon.Trophy />}
        label="Challenge success"
        value={Math.round(dash.challengeSuccessRate * 100)}
        suffix="%"
        hint="Of submissions scored ≥ 70."
        trend={accuracyTrend}
      />
    </div>
  );
}

/* ─────────────── Mastery — Grid view ─────────────── */
function MasteryGrid({ topics, loading }) {
  if (loading) {
    return (
      <div className="grid grid-cols-2 gap-2.5">
        {[1,2,3,4,5,6].map(i => <div key={i} className="skel" style={{ height: 56 }} />)}
      </div>
    );
  }
  return (
    <div className="grid grid-cols-2 gap-2.5">
      {topics.map((t) => (
        <div key={t.topicId} className="rounded-xl border bg-white p-3" style={{ borderColor: 'var(--line)' }}>
          <div className="flex items-center justify-between gap-2">
            <div className="text-[13.5px] font-medium truncate" style={{ color: 'var(--ink)', letterSpacing: '-0.005em' }}>
              {t.topicName}
            </div>
            <div className="font-mono text-[12px] tabular-nums" style={{ color: 'var(--ink-soft)' }}>
              {t.masteryScore}
            </div>
          </div>
          <div className="mbar mt-2.5">
            <i style={{ width: `${t.masteryScore}%` }} />
          </div>
          <div className="mt-2 flex items-center justify-between">
            <span className="text-[11px] font-mono" style={{ color: 'var(--ink-mute)' }}>
              {(t.accuracy * 100).toFixed(0)}% acc
            </span>
            <RiskBadge value={t.forgettingRisk} />
          </div>
        </div>
      ))}
    </div>
  );
}

function RiskBadge({ value }) {
  const level = value > 0.5 ? 'high' : value > 0.25 ? 'med' : 'low';
  const label = level === 'high' ? 'High decay' : level === 'med' ? 'Decay rising' : 'Stable';
  const cls = level === 'high' ? 'risk-high' : level === 'med' ? 'risk-med' : 'risk-low';
  return (
    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10.5px] font-mono ${cls}`}>
      <span className="w-1.5 h-1.5 rounded-full" style={{ background: 'currentColor' }} />
      {label}
    </span>
  );
}

/* ─────────────── Mastery — Radial galaxy view ─────────────── */
function MasteryRadial({ topics, loading }) {
  if (loading || !topics) return <div className="skel" style={{ height: 320 }} />;
  const size = 320;
  const cx = size / 2, cy = size / 2;
  // Sort biggest mastery → outer
  const ordered = [...topics].sort((a, b) => a.masteryScore - b.masteryScore);
  return (
    <div className="relative flex items-center justify-center" style={{ minHeight: size + 20 }}>
      <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
        {/* Rings */}
        {[0.25, 0.5, 0.75, 1].map(f => (
          <circle key={f} cx={cx} cy={cy} r={f * (size/2 - 20)} fill="none"
                  stroke="oklch(0.95 0.006 85)" strokeWidth="1" strokeDasharray="2 4" />
        ))}
        {/* Topics as dots */}
        {ordered.map((t, i) => {
          const angle = (i / ordered.length) * Math.PI * 2 - Math.PI/2;
          const radius = (1 - t.masteryScore / 100) * (size/2 - 24);
          const x = cx + Math.cos(angle) * radius;
          const y = cy + Math.sin(angle) * radius;
          const r = 6 + (t.accuracy * 7);
          const hue = 155 - t.forgettingRisk * 130; // green → red
          return (
            <g key={t.topicId}>
              <circle cx={x} cy={y} r={r} fill={`oklch(0.66 0.13 ${hue})`} opacity="0.85" />
              <text x={x} y={y - r - 6} textAnchor="middle"
                    style={{
                      fontFamily: 'Geist Mono, monospace',
                      fontSize: 10.5,
                      fill: 'var(--ink)',
                      letterSpacing: '-0.01em',
                    }}>
                {t.topicName}
              </text>
              <text x={x} y={y + 3} textAnchor="middle"
                    style={{
                      fontFamily: 'Geist Mono, monospace',
                      fontSize: 9.5,
                      fontWeight: 600,
                      fill: '#fff',
                    }}>
                {t.masteryScore}
              </text>
            </g>
          );
        })}
        {/* Center label */}
        <circle cx={cx} cy={cy} r="3" fill="var(--ink)" />
        <text x={cx} y={cy + 14} textAnchor="middle"
              style={{ fontFamily: 'Geist Mono, monospace', fontSize: 9.5, fill: 'var(--ink-mute)', letterSpacing: '0.1em', textTransform: 'uppercase' }}>
          mastered
        </text>
      </svg>
      <div className="absolute right-0 top-0 text-[11px] font-mono" style={{ color: 'var(--ink-mute)' }}>
        ← outer = needs work
      </div>
    </div>
  );
}

/* ─────────────── Weak areas focus card ─────────────── */
function WeakAreasCard({ areas, loading, onPractice }) {
  return (
    <section className="card card-pad">
      <div className="flex items-start justify-between">
        <div>
          <div className="eyebrow flex items-center gap-2"><Icon.AlertCircle /> Zayıf alanlar</div>
          <h3 className="mt-2 font-semibold tracking-tight"
              style={{ fontSize: 17, letterSpacing: '-0.02em', color: 'var(--ink)' }}>
            Sırada odaklanacaklarımız
          </h3>
        </div>
        <button className="btn btn-soft" onClick={onPractice}>
          Bunları çalış <Icon.Arrow />
        </button>
      </div>
      {loading ? (
        <div className="mt-4 flex flex-col gap-2">
          {[1,2,3].map(i => <div key={i} className="skel" style={{ height: 44 }} />)}
        </div>
      ) : (
        <div className="mt-4 flex flex-col gap-2">
          {areas.slice(0, 4).map((t, idx) => (
            <div key={t.topicId} className="flex items-center gap-3 py-1.5 px-1">
              <span className="w-5 font-mono text-[11px] tabular-nums" style={{ color: 'var(--ink-mute)' }}>
                {String(idx + 1).padStart(2, '0')}
              </span>
              <div className="flex-1 min-w-0">
                <div className="text-[13.5px] font-medium truncate" style={{ color: 'var(--ink)' }}>
                  {t.topicName}
                </div>
                <div className="mbar mt-1.5">
                  <i style={{ width: `${t.masteryScore}%`, background: `oklch(0.66 0.13 ${155 - t.forgettingRisk * 130})` }} />
                </div>
              </div>
              <RiskBadge value={t.forgettingRisk} />
              <span className="font-mono text-[12px] tabular-nums w-7 text-right" style={{ color: 'var(--ink-soft)' }}>
                {t.masteryScore}
              </span>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

/* ─────────────── Trend chart ─────────────── */
function TrendChart({ points, loading }) {
  const wrap = useR(null);
  const [w, setW] = useS(800);
  useE(() => {
    if (!wrap.current) return;
    const ro = new ResizeObserver(() => {
      setW(wrap.current.clientWidth);
    });
    ro.observe(wrap.current);
    return () => ro.disconnect();
  }, []);

  const h = 200;
  const padL = 40, padR = 16, padT = 18, padB = 28;
  const innerW = Math.max(100, w - padL - padR);
  const innerH = h - padT - padB;

  const [hover, setHover] = useS(null);

  if (loading || !points) {
    return <div className="card skel" style={{ height: h + 40 }} />;
  }

  const n = points.length;
  const barW = innerW / n * 0.55;
  const maxAttempts = Math.max(...points.map(p => p.attempts), 1);

  const xs = points.map((_, i) => padL + (i + 0.5) * (innerW / n));
  const ys = points.map(p => padT + (1 - p.accuracy) * innerH);

  // smooth line
  const linePath = (() => {
    if (n === 0) return '';
    let d = `M ${xs[0]} ${ys[0]}`;
    for (let i = 1; i < n; i++) {
      const xm = (xs[i-1] + xs[i]) / 2;
      d += ` Q ${xm} ${ys[i-1]} ${xs[i]} ${ys[i]}`;
    }
    return d;
  })();

  // area fill under the line
  const areaPath = (() => {
    if (n === 0) return '';
    let d = `M ${xs[0]} ${padT + innerH}`;
    d += ` L ${xs[0]} ${ys[0]}`;
    for (let i = 1; i < n; i++) {
      const xm = (xs[i-1] + xs[i]) / 2;
      d += ` Q ${xm} ${ys[i-1]} ${xs[i]} ${ys[i]}`;
    }
    d += ` L ${xs[n-1]} ${padT + innerH} Z`;
    return d;
  })();

  return (
    <section className="card card-pad">
      <div className="flex items-start justify-between mb-2">
        <div>
          <div className="eyebrow flex items-center gap-2"><Icon.Spark /> Öğrenme eğilimi</div>
          <h3 className="mt-2 font-semibold tracking-tight"
              style={{ fontSize: 17, letterSpacing: '-0.02em', color: 'var(--ink)' }}>
            Son 14 gün · doğruluk ve hacim
          </h3>
        </div>
        <div className="flex items-center gap-4 text-[11.5px] font-mono" style={{ color: 'var(--ink-mute)' }}>
          <span className="inline-flex items-center gap-1.5">
            <span className="w-3 h-3 rounded-full" style={{ background: 'var(--accent)' }} /> accuracy
          </span>
          <span className="inline-flex items-center gap-1.5">
            <span className="w-3 h-3 rounded-sm" style={{ background: 'oklch(0.90 0.02 80)' }} /> attempts
          </span>
        </div>
      </div>
      <div ref={wrap} className="relative w-full" style={{ height: h + 12 }}>
        <svg width={w} height={h} viewBox={`0 0 ${w} ${h}`} className="block">
          {/* y-axis gridlines for accuracy */}
          {[0, 0.25, 0.5, 0.75, 1].map(g => {
            const y = padT + (1 - g) * innerH;
            return (
              <g key={g}>
                <line x1={padL} x2={w - padR} y1={y} y2={y}
                      stroke="oklch(0.94 0.006 85)" strokeWidth="1" strokeDasharray="2 3" />
                <text x={padL - 8} y={y + 3} textAnchor="end"
                      style={{ fontFamily: 'Geist Mono, monospace', fontSize: 10, fill: 'var(--ink-mute)' }}>
                  {Math.round(g * 100)}
                </text>
              </g>
            );
          })}
          {/* attempt bars */}
          {points.map((p, i) => {
            const bh = (p.attempts / maxAttempts) * (innerH * 0.55);
            return (
              <rect key={'bar-'+i}
                    x={xs[i] - barW/2}
                    y={padT + innerH - bh}
                    width={barW}
                    height={bh}
                    rx="2"
                    fill="oklch(0.90 0.02 80)"
                    opacity={hover === i ? 1 : 0.7} />
            );
          })}
          {/* area + line */}
          <path d={areaPath} fill="color-mix(in oklch, var(--accent) 12%, transparent)" />
          <path d={linePath} fill="none" stroke="var(--accent)" strokeWidth="2"
                strokeLinecap="round" strokeLinejoin="round" />
          {/* points */}
          {points.map((p, i) => (
            <circle key={'pt-'+i} cx={xs[i]} cy={ys[i]} r={hover === i ? 5 : 3}
                    fill="#fff" stroke="var(--accent)" strokeWidth="2" />
          ))}
          {/* x-axis day labels (every 2 days) */}
          {points.map((p, i) => {
            if (i % 2 !== 0 && i !== n - 1) return null;
            const d = new Date(p.dayUtc);
            const label = d.toLocaleDateString(undefined, { month: 'numeric', day: 'numeric' });
            return (
              <text key={'lbl-'+i} x={xs[i]} y={h - 8} textAnchor="middle"
                    style={{ fontFamily: 'Geist Mono, monospace', fontSize: 10, fill: 'var(--ink-mute)' }}>
                {label}
              </text>
            );
          })}
          {/* hit area */}
          {points.map((p, i) => (
            <rect key={'hit-'+i}
                  x={xs[i] - (innerW / n) / 2}
                  y={padT}
                  width={innerW / n}
                  height={innerH}
                  fill="transparent"
                  onMouseEnter={() => setHover(i)}
                  onMouseLeave={() => setHover(null)} />
          ))}
        </svg>
        {hover != null && (
          <div className="tip" style={{
            left: xs[hover],
            top: ys[hover],
          }}>
            {new Date(points[hover].dayUtc).toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' })} ·
            {' '}{Math.round(points[hover].accuracy * 100)}% · {points[hover].attempts}q
          </div>
        )}
      </div>
    </section>
  );
}

Object.assign(window, {
  Icon, TypeTile, PrioDots, ProgressRing,
  TodayCard, StatsTrio, MasteryGrid, MasteryRadial, WeakAreasCard, TrendChart,
  typeLabel, categoryLabel, difficultyLabel, fmtRelative,
});
