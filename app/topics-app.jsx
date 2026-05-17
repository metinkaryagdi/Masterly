// topics-app.jsx
// Topic catalogue with master-detail layout. Lists every topic with mastery,
// drills into a detail pane that exposes description, dependencies, sample
// questions, and a "start practice" CTA into Practice.html.

const { useState: tS, useEffect: tE, useMemo: tM } = React;

const TOPICS_TWEAKS = /*EDITMODE-BEGIN*/{
  "accent": "emerald",
  "sort": "weak-first",
  "apiBase": "http://localhost:5000",
  "demoMode": true
}/*EDITMODE-END*/;

const TOPICS_PALETTES = {
  emerald: { accent: 'oklch(0.62 0.14 155)', accentInk: 'oklch(0.32 0.10 155)', accentTint: 'oklch(0.95 0.04 155)' },
  indigo:  { accent: 'oklch(0.55 0.18 270)', accentInk: 'oklch(0.30 0.14 270)', accentTint: 'oklch(0.95 0.04 270)' },
  amber:   { accent: 'oklch(0.66 0.16 60)',  accentInk: 'oklch(0.36 0.12 60)',  accentTint: 'oklch(0.96 0.05 60)' },
};

/* ─────────────── Helpers ─────────────── */
function DifficultyPip({ value }) {
  const filled = Math.max(1, Math.min(4, value || 1));
  return (
    <span className="difpip" title={difficultyLabel(filled)}>
      {[1,2,3,4].map(i => <i key={i} data-on={i <= filled ? 'true' : 'false'} />)}
    </span>
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

function TypeTileSm({ type }) {
  if (type === 'CodingChallenge') return <span className="type-tile-sm coding"><Icon.Code /></span>;
  if (type === 'ScenarioChallenge') return <span className="type-tile-sm scenario"><Icon.Scenario /></span>;
  return <span className="type-tile-sm question"><Icon.Question /></span>;
}

/* ─────────────── Topic list item ─────────────── */
function TopicListItem({ topic, mastery, idx, selected, onClick }) {
  const score = mastery?.masteryScore ?? 0;
  return (
    <button className="tlist-item" data-selected={selected} onClick={onClick} type="button">
      <span className="badge-num">{String(idx + 1).padStart(2, '0')}</span>
      <div className="min-w-0">
        <div className="text-[14px] font-medium truncate"
             style={{ color: 'var(--ink)', letterSpacing: '-0.005em' }}>
          {topic.name}
        </div>
        <div className="flex items-center gap-2 mt-1">
          <DifficultyPip value={topic.difficulty} />
          <span className="font-mono text-[10.5px]" style={{ color: 'var(--ink-mute)' }}>
            {topic.questionCount}q · {topic.codingChallengeCount}c · {topic.scenarioCount}s
          </span>
        </div>
        <div className="mbar mt-2">
          <i style={{ width: `${score}%` }} />
        </div>
      </div>
      <div className="flex flex-col items-end justify-between min-w-[44px]" style={{ height: '100%' }}>
        <span className="font-semibold tabular-nums" style={{ fontSize: 17, letterSpacing: '-0.025em' }}>
          {score}
        </span>
        {mastery && <RiskBadge value={mastery.forgettingRisk} />}
      </div>
    </button>
  );
}

/* ─────────────── Sample question row ─────────────── */
function SampleQuestionRow({ q, onClick }) {
  return (
    <button type="button" className="sq-row" onClick={onClick}>
      <TypeTileSm type={q.type} />
      <div className="min-w-0">
        <div className="text-[13.5px] font-medium leading-snug truncate"
             style={{ color: 'var(--ink)', letterSpacing: '-0.005em' }}>
          {q.title}
        </div>
        <div className="flex items-center gap-2 mt-1">
          <DifficultyPip value={q.difficulty} />
          <span className="font-mono text-[10.5px]" style={{ color: 'var(--ink-mute)' }}>
            {q.type === 'Question' ? 'Question' : q.type === 'CodingChallenge' ? 'Coding' : 'Scenario'}
          </span>
        </div>
      </div>
      <span className="font-mono text-[11px]" style={{ color: 'var(--ink-mute)' }}>
        ~{q.minutes}m
      </span>
      <span className="text-[var(--ink-mute)]"><Icon.Arrow /></span>
    </button>
  );
}

/* ─────────────── Topic detail panel ─────────────── */
function TopicDetail({ topic, mastery, allTopics, onSelectTopic, onStartPractice, onStartQuestion }) {
  if (!topic) {
    return (
      <div className="card p-10 text-center">
        <p style={{ color: 'var(--ink-mute)' }}>Select a topic to see its mastery, dependencies, and sample questions.</p>
      </div>
    );
  }

  const deps = (topic.dependencyIds || [])
    .map(id => allTopics.find(t => t.id === id))
    .filter(Boolean);
  const dependents = allTopics.filter(t => (t.dependencyIds || []).includes(topic.id));
  const score = mastery?.masteryScore ?? 0;
  const accuracy = mastery?.accuracy ?? 0;
  const risk = mastery?.forgettingRisk ?? 0;

  return (
    <section className="card overflow-hidden">
      {/* Header */}
      <div className="px-7 pt-6 pb-5">
        <div className="flex items-start justify-between gap-6">
          <div className="min-w-0 flex-1">
            <div className="eyebrow flex items-center gap-2">// topic</div>
            <h1 className="mt-2 font-semibold tracking-tight"
                style={{ fontSize: 30, letterSpacing: '-0.028em', color: 'var(--ink)', lineHeight: 1.15 }}>
              {topic.name}
            </h1>
            <div className="mt-3 flex flex-wrap items-center gap-2">
              <span className="topic-chip">
                <span className="w-1.5 h-1.5 rounded-full" style={{ background: 'var(--accent)' }} />
                {topic.slug}
              </span>
              <span className="topic-chip">
                <DifficultyPip value={topic.difficulty} />
                <span style={{ color: 'var(--ink-mute)', marginLeft: 4 }}>
                  {difficultyLabel(topic.difficulty)}
                </span>
              </span>
              <span className="topic-chip">
                {topic.questionCount + topic.codingChallengeCount + topic.scenarioCount} items
              </span>
              {mastery && <RiskBadge value={risk} />}
            </div>
          </div>

          {/* Mastery ring */}
          <ProgressRing value={score / 100} size={108} stroke={8}>
            <div className="flex flex-col items-center">
              <span className="font-mono text-[10px] uppercase tracking-widest" style={{ color: 'var(--ink-mute)' }}>
                mastery
              </span>
              <span className="font-semibold tabular-nums leading-none mt-1"
                    style={{ fontSize: 32, letterSpacing: '-0.03em', color: 'var(--ink)' }}>
                {score}
              </span>
            </div>
          </ProgressRing>
        </div>

        <p className="mt-5 text-[14.5px]"
           style={{ color: 'var(--ink-soft)', lineHeight: 1.6, textWrap: 'pretty', maxWidth: 640 }}>
          {topic.description}
        </p>
      </div>

      <div className="hr" />

      {/* Stat tiles */}
      <div className="grid grid-cols-3 gap-3 p-5"
           style={{ background: 'oklch(0.985 0.006 88)' }}>
        <StatTile label="Accuracy" value={Math.round(accuracy * 100)} suffix="%" />
        <StatTile label="Decay rate" value={(topic.decayRate * 100).toFixed(0)} suffix="% / day"
                  sub={topic.decayRate > 0.3 ? 'fast forgetting' : topic.decayRate > 0.2 ? 'normal' : 'slow forgetting'} />
        <StatTile label="Forgetting risk" value={Math.round(risk * 100)} suffix="%"
                  sub={risk > 0.5 ? 'review soon' : risk > 0.25 ? 'review this week' : 'on track'} />
      </div>

      <div className="hr" />

      {/* Dependencies */}
      {(deps.length > 0 || dependents.length > 0) && (
        <>
          <div className="px-7 py-5">
            <div className="eyebrow flex items-center gap-2">// prerequisites &amp; unlocks</div>
            <div className="mt-3 grid gap-4" style={{ gridTemplateColumns: 'auto 1fr' }}>
              {deps.length > 0 && (
                <>
                  <span className="text-[12px] pt-1.5" style={{ color: 'var(--ink-mute)' }}>
                    Builds on
                  </span>
                  <div className="flex flex-wrap gap-2 items-center">
                    {deps.map((d, i) => (
                      <React.Fragment key={d.id}>
                        <button className="dep-chip" onClick={() => onSelectTopic(d)}>
                          {d.name}
                          <DifficultyPip value={d.difficulty} />
                        </button>
                        {i < deps.length - 1 && <span className="dep-arrow">+</span>}
                      </React.Fragment>
                    ))}
                  </div>
                </>
              )}
              {dependents.length > 0 && (
                <>
                  <span className="text-[12px] pt-1.5" style={{ color: 'var(--ink-mute)' }}>
                    Unlocks
                  </span>
                  <div className="flex flex-wrap gap-2 items-center">
                    {dependents.map((d, i) => (
                      <React.Fragment key={d.id}>
                        <button className="dep-chip" onClick={() => onSelectTopic(d)}>
                          <span className="dep-arrow">→</span> {d.name}
                        </button>
                      </React.Fragment>
                    ))}
                  </div>
                </>
              )}
            </div>
          </div>
          <div className="hr" />
        </>
      )}

      {/* Sample questions */}
      <div className="px-7 py-5">
        <div className="flex items-center justify-between mb-3">
          <div>
            <div className="eyebrow">// sample items</div>
            <div className="mt-1 text-[14px] font-medium"
                 style={{ color: 'var(--ink)', letterSpacing: '-0.015em' }}>
              Try a question right now
            </div>
          </div>
          <span className="font-mono text-[11px]" style={{ color: 'var(--ink-mute)' }}>
            showing {topic.sampleQuestions.length} of {topic.questionCount + topic.codingChallengeCount + topic.scenarioCount}
          </span>
        </div>
        <div className="flex flex-col gap-2">
          {topic.sampleQuestions.length === 0 ? (
            <div className="text-[13px] py-6 text-center" style={{ color: 'var(--ink-mute)' }}>
              No previews loaded for this topic yet.
            </div>
          ) : (
            topic.sampleQuestions.map((q) => (
              <SampleQuestionRow key={q.id} q={q} onClick={() => onStartQuestion(q)} />
            ))
          )}
        </div>
      </div>

      {/* CTA bar */}
      <div className="hr" />
      <div className="px-7 py-4 flex flex-wrap items-center justify-between gap-3"
           style={{ background: 'oklch(0.985 0.006 88)' }}>
        <div className="text-[12.5px]" style={{ color: 'var(--ink-soft)' }}>
          We'll prioritize {risk > 0.4 ? 'revision' : 'new material'} based on your current mastery.
        </div>
        <div className="flex items-center gap-2">
          <button className="btn btn-ghost" onClick={() => alert('Adds this topic\'s weakest items to today\'s plan.')}>
            Add to today's plan
          </button>
          <button className="btn btn-primary btn-lg" onClick={() => onStartPractice(topic)}>
            Start focused practice <Icon.Arrow />
          </button>
        </div>
      </div>
    </section>
  );
}

function StatTile({ label, value, suffix, sub }) {
  return (
    <div className="card" style={{ padding: '14px 16px', background: '#fff' }}>
      <div className="eyebrow" style={{ fontSize: 10.5 }}>{label}</div>
      <div className="mt-1.5 flex items-baseline gap-1">
        <span className="font-semibold tabular-nums"
              style={{ fontSize: 22, letterSpacing: '-0.025em', color: 'var(--ink)' }}>
          {value}
        </span>
        {suffix && <span className="text-[12px]" style={{ color: 'var(--ink-mute)' }}>{suffix}</span>}
      </div>
      {sub && <div className="text-[11.5px] mt-0.5" style={{ color: 'var(--ink-soft)' }}>{sub}</div>}
    </div>
  );
}

/* ─────────────── Sort control ─────────────── */
function SortControl({ value, onChange }) {
  const options = [
    { id: 'weak-first',   label: 'Weak first' },
    { id: 'strong-first', label: 'Strong first' },
    { id: 'difficulty',   label: 'Difficulty' },
    { id: 'name',         label: 'Name' },
  ];
  return (
    <div className="seg">
      {options.map(o => (
        <button key={o.id} type="button"
                data-active={value === o.id}
                onClick={() => onChange(o.id)}>
          {o.label}
        </button>
      ))}
    </div>
  );
}

/* ─────────────── Main App ─────────────── */
function App() {
  const [t, setTweak] = useTweaks(TOPICS_TWEAKS);
  const [topics, setTopics] = tS([]);
  const [mastery, setMastery] = tS([]); // from dashboard endpoint
  const [loading, setLoading] = tS(true);
  const [selectedId, setSelectedId] = tS(null);
  const [apiOnline, setApiOnline] = tS(t.demoMode ? 'demo' : null);

  // No valid user means we landed here without auth — go back to sign in.
  tE(() => {
    if (!localStorage.getItem('training_user')) {
      window.location.replace('Auth.html');
    }
  }, []);

  // Apply accent
  tE(() => {
    const p = TOPICS_PALETTES[t.accent] || TOPICS_PALETTES.emerald;
    document.documentElement.style.setProperty('--accent', p.accent);
    document.documentElement.style.setProperty('--accent-ink', p.accentInk);
    document.documentElement.style.setProperty('--accent-tint', p.accentTint);
  }, [t.accent]);

  // Load topics + dashboard mastery
  tE(() => {
    let cancelled = false;
    setLoading(true);
    setApiOnline(t.demoMode ? 'demo' : null);

    Promise.allSettled([
      fetchTopics({ apiBase: t.apiBase, demoMode: t.demoMode }),
      fetchDashboard({ apiBase: t.apiBase, demoMode: t.demoMode }),
    ]).then(([tp, dr]) => {
      if (cancelled) return;
      if (tp.status === 'fulfilled') setTopics(tp.value);
      else setTopics([]);
      if (dr.status === 'fulfilled') setMastery(dr.value.topicMastery);
      else setMastery([]);
      if (!t.demoMode) {
        setApiOnline(tp.status === 'fulfilled' && dr.status === 'fulfilled');
      }
      setLoading(false);

      // Preselect from URL ?id= or first weak
      const urlId = new URLSearchParams(window.location.search).get('id');
      const initial = urlId
        || tp.value?.[0]?.id
        || null;
      setSelectedId(initial);
    });
    return () => { cancelled = true; };
  }, [t.apiBase, t.demoMode]);

  const masteryById = tM(() => {
    const m = {};
    for (const item of mastery) m[item.topicId] = item;
    return m;
  }, [mastery]);

  const ordered = tM(() => {
    if (!topics.length) return [];
    const list = [...topics];
    if (t.sort === 'weak-first') {
      list.sort((a, b) => (masteryById[a.id]?.masteryScore ?? 0) - (masteryById[b.id]?.masteryScore ?? 0));
    } else if (t.sort === 'strong-first') {
      list.sort((a, b) => (masteryById[b.id]?.masteryScore ?? 0) - (masteryById[a.id]?.masteryScore ?? 0));
    } else if (t.sort === 'difficulty') {
      list.sort((a, b) => a.difficulty - b.difficulty);
    } else if (t.sort === 'name') {
      list.sort((a, b) => a.name.localeCompare(b.name));
    }
    return list;
  }, [topics, masteryById, t.sort]);

  const selected = topics.find(x => x.id === selectedId) || ordered[0];

  // Aggregate stats for the hero
  const aggregate = tM(() => {
    if (!mastery.length) return { avg: 0, atRisk: 0 };
    const avg = Math.round(mastery.reduce((a, b) => a + b.masteryScore, 0) / mastery.length);
    const atRisk = mastery.filter(m => m.forgettingRisk > 0.4).length;
    return { avg, atRisk };
  }, [mastery]);

  const handleSelectTopic = (topic) => {
    setSelectedId(topic.id);
    // Update URL without reload so deep links work
    const url = new URL(window.location.href);
    url.searchParams.set('id', topic.id);
    window.history.replaceState({}, '', url);
    // Scroll detail back to top on small viewports
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const handleStartPractice = (topic) => {
    const q = topic.sampleQuestions[0];
    if (!q) return window.location.href = 'Dashboard.html';
    handleStartQuestion(q);
  };

  const handleStartQuestion = (q) => {
    if (q.type === 'CodingChallenge') {
      window.location.href = `Challenge.html?kind=coding&id=${encodeURIComponent(q.id)}`;
    } else if (q.type === 'ScenarioChallenge') {
      window.location.href = `Challenge.html?kind=scenario&id=${encodeURIComponent(q.id)}`;
    } else {
      window.location.href = `Practice.html?question=${encodeURIComponent(q.id)}`;
    }
  };

  return (
    <>
      <TopNav activeTab="topics" apiOnline={apiOnline} />

      <main className="container-x py-8">
        {/* Hero */}
        <div className="flex items-end justify-between gap-4 mb-7 rise">
          <div>
            <div className="eyebrow">// catalogue</div>
            <h1 className="mt-2 font-semibold tracking-tight"
                style={{ fontSize: 30, letterSpacing: '-0.028em', color: 'var(--ink)', lineHeight: 1.15 }}>
              Topics
            </h1>
            <p className="mt-1.5 text-[14.5px]" style={{ color: 'var(--ink-soft)' }}>
              {loading
                ? <span className="skel inline-block h-[18px] w-[260px] align-middle" />
                : <>{topics.length} topics · avg mastery <span style={{ color: 'var(--ink)', fontWeight: 500 }}>{aggregate.avg}</span> · <span style={{ color: 'var(--ink)', fontWeight: 500 }}>{aggregate.atRisk}</span> at risk of decay</>
              }
            </p>
          </div>
          <div className="flex items-center gap-3">
            <SortControl value={t.sort} onChange={(v) => setTweak('sort', v)} />
          </div>
        </div>

        {/* Master-detail */}
        <div className="grid gap-5"
             style={{ gridTemplateColumns: 'minmax(0, 380px) minmax(0, 1fr)' }}>
          {/* List */}
          <div className="flex flex-col gap-2.5 rise" style={{ animationDelay: '.05s' }}>
            {loading
              ? Array(6).fill(0).map((_, i) => <div key={i} className="skel" style={{ height: 76 }} />)
              : ordered.map((tp, i) => (
                  <TopicListItem
                    key={tp.id}
                    topic={tp}
                    mastery={masteryById[tp.id]}
                    idx={i}
                    selected={tp.id === selected?.id}
                    onClick={() => handleSelectTopic(tp)}
                  />
                ))
            }
          </div>

          {/* Detail */}
          <div className="rise" style={{ animationDelay: '.12s' }}>
            {loading ? (
              <div className="card p-8 flex flex-col gap-4">
                <div className="skel h-[36px] w-[60%]" />
                <div className="skel h-[18px] w-[40%]" />
                <div className="skel h-[100px]" />
                <div className="skel h-[60px]" />
                <div className="skel h-[60px]" />
              </div>
            ) : (
              <TopicDetail
                key={selected?.id} /* re-mount to retrigger anims */
                topic={selected}
                mastery={masteryById[selected?.id]}
                allTopics={topics}
                onSelectTopic={handleSelectTopic}
                onStartPractice={handleStartPractice}
                onStartQuestion={handleStartQuestion}
              />
            )}
          </div>
        </div>

        {/* Footer */}
        <footer className="mt-10 pb-12 flex items-center justify-between text-[11px] font-mono"
                style={{ color: 'var(--ink-mute)' }}>
          <span>Training Platform · catalogue</span>
          <span>GET /api/topics</span>
        </footer>
      </main>

      <TweaksPanel>
        <TweakSection label="Visual" />
        <TweakRadio label="Accent" value={t.accent}
                    options={['emerald', 'indigo', 'amber']}
                    onChange={(v) => setTweak('accent', v)} />
        <TweakSelect label="Default sort" value={t.sort}
                     options={['weak-first', 'strong-first', 'difficulty', 'name']}
                     onChange={(v) => setTweak('sort', v)} />

        <TweakSection label="API" />
        <TweakToggle label="Demo mode" value={t.demoMode}
                     onChange={(v) => setTweak('demoMode', v)} />
        <TweakText label="API base URL" value={t.apiBase}
                   onChange={(v) => setTweak('apiBase', v)}
                   placeholder="http://localhost:5000" />
      </TweaksPanel>
    </>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<App />);
