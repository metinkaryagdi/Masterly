// dashboard-app.jsx
// Top-level Dashboard composition: TopNav + grid + tweaks wiring.

const { useState: useSt, useEffect: useEf, useMemo: useMm } = React;

const DASH_TWEAKS = /*EDITMODE-BEGIN*/{
  "accent": "emerald",
  "masteryView": "grid",
  "showTrend": true,
  "density": "regular",
  "apiBase": "http://localhost:5000",
  "demoMode": true
}/*EDITMODE-END*/;

const DASH_PALETTES = {
  emerald: { accent: 'oklch(0.62 0.14 155)', accentInk: 'oklch(0.32 0.10 155)', accentTint: 'oklch(0.95 0.04 155)' },
  indigo:  { accent: 'oklch(0.55 0.18 270)', accentInk: 'oklch(0.30 0.14 270)', accentTint: 'oklch(0.95 0.04 270)' },
  amber:   { accent: 'oklch(0.66 0.16 60)',  accentInk: 'oklch(0.36 0.12 60)',  accentTint: 'oklch(0.96 0.05 60)' },
};

/* ─────────────── Section heading ─────────────── */
function SectionHead({ eyebrow, title, action }) {
  return (
    <div className="flex items-end justify-between gap-3 mb-3">
      <div>
        <div className="eyebrow">{eyebrow}</div>
        <h2 className="mt-1 font-semibold tracking-tight"
            style={{ fontSize: 19, letterSpacing: '-0.022em', color: 'var(--ink)' }}>
          {title}
        </h2>
      </div>
      {action}
    </div>
  );
}

/* ─────────────── Greeting ─────────────── */
function Greeting({ user, plan }) {
  const hour = new Date().getHours();
  const greet = hour < 5 ? 'Burning the midnight oil,'
              : hour < 12 ? 'Good morning,'
              : hour < 18 ? 'Good afternoon,'
              : 'Good evening,';
  const name = (user?.displayName || 'learner').split(' ')[0];
  const remaining = plan ? plan.items.filter(i => !i.isCompleted).length : null;

  return (
    <div className="flex items-end justify-between gap-4 mb-7">
      <div>
        <div className="eyebrow flex items-center gap-2"><Icon.Calendar /> {new Date().toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' })}</div>
        <h1 className="mt-2 font-semibold tracking-tight"
            style={{ fontSize: 30, letterSpacing: '-0.028em', color: 'var(--ink)', lineHeight: 1.15 }}>
          {greet} {name}.
        </h1>
        <p className="mt-1.5 text-[14.5px]" style={{ color: 'var(--ink-soft)' }}>
          {plan == null
            ? "Let's calibrate today's plan."
            : remaining === 0
              ? "Today's plan is complete — that's a wrap. ✓"
              : <>You have <span style={{ color: 'var(--ink)', fontWeight: 500 }}>{remaining} items</span> left in today's plan.</>
          }
        </p>
      </div>
    </div>
  );
}

/* ─────────────── Main App ─────────────── */
function App() {
  const [t, setTweak] = useTweaks(DASH_TWEAKS);
  const [user, setUser] = useSt(getCurrentUser);

  const [plan, setPlan] = useSt(null);
  const [dash, setDash] = useSt(null);
  const [loadingPlan, setLoadingPlan] = useSt(true);
  const [loadingDash, setLoadingDash] = useSt(true);
  const [generating, setGenerating] = useSt(false);
  const [apiOnline, setApiOnline] = useSt(t.demoMode ? 'demo' : null);
  const [toast, setToast] = useSt(null);

  // Stub a user if there's nothing in localStorage (demo path).
  useEf(() => {
    if (!user) {
      const stub = { userId: 'demo', displayName: 'Ada Lovelace', email: 'ada@training.dev' };
      localStorage.setItem('training_user', JSON.stringify(stub));
      localStorage.setItem('training_token', 'demo.eyJzdWIiOiJkZW1vIn0.signature');
      setUser(stub);
    }
  }, []);

  // Accent palette
  useEf(() => {
    const p = DASH_PALETTES[t.accent] || DASH_PALETTES.emerald;
    document.documentElement.style.setProperty('--accent', p.accent);
    document.documentElement.style.setProperty('--accent-ink', p.accentInk);
    document.documentElement.style.setProperty('--accent-tint', p.accentTint);
  }, [t.accent]);

  // Load plan + dash
  useEf(() => {
    let cancelled = false;
    setLoadingPlan(true);
    setLoadingDash(true);
    setApiOnline(t.demoMode ? 'demo' : null);

    Promise.allSettled([
      fetchTodayPlan({ apiBase: t.apiBase, demoMode: t.demoMode }),
      fetchDashboard({ apiBase: t.apiBase, demoMode: t.demoMode }),
    ]).then(([p, d]) => {
      if (cancelled) return;
      if (p.status === 'fulfilled') setPlan(p.value);
      else { setPlan(null); }
      if (d.status === 'fulfilled') setDash(d.value);
      else { setDash(null); }

      const anyFail = p.status === 'rejected' || d.status === 'rejected';
      if (t.demoMode) setApiOnline('demo');
      else setApiOnline(!anyFail);

      if (anyFail && !t.demoMode) {
        setToast({ kind: 'error', msg: `Can't reach API at ${t.apiBase}. Flip on Demo mode in Tweaks.` });
      }

      setLoadingPlan(false);
      setLoadingDash(false);
    });
    return () => { cancelled = true; };
  }, [t.apiBase, t.demoMode]);

  // Toast auto-dismiss
  useEf(() => {
    if (!toast) return;
    const id = setTimeout(() => setToast(null), 5000);
    return () => clearTimeout(id);
  }, [toast]);

  const handleSignOut = () => {
    localStorage.removeItem('training_token');
    localStorage.removeItem('training_user');
    window.location.href = 'Auth.html';
  };

  const handleGenerate = async () => {
    setGenerating(true);
    try {
      const p = await generatePlan({ apiBase: t.apiBase, demoMode: t.demoMode });
      setPlan(p);
      setToast({ kind: 'success', msg: 'Plan refreshed for today.' });
    } catch (err) {
      setToast({ kind: 'error', msg: err.message || 'Generation failed.' });
    } finally {
      setGenerating(false);
    }
  };

  const handleItemClick = (item) => {
    if (item.isCompleted) return;
    // Route by item type: Questions → Practice; Coding/Scenario challenges → Challenge.
    if (item.itemType === StudyPlanItemType.CodingChallenge) {
      window.location.href = `Challenge.html?kind=coding&id=${encodeURIComponent(item.referenceId)}&plan=${encodeURIComponent(plan?.id || '')}`;
    } else if (item.itemType === StudyPlanItemType.ScenarioChallenge) {
      window.location.href = `Challenge.html?kind=scenario&id=${encodeURIComponent(item.referenceId)}&plan=${encodeURIComponent(plan?.id || '')}`;
    } else {
      window.location.href = `Practice.html?item=${encodeURIComponent(item.id)}`;
    }
  };

  return (
    <>
      <TopNav activeTab="today" apiOnline={apiOnline} />

      <main className="container-x py-8">
        <Greeting user={user} plan={plan} />

        {/* Row 1: Today's plan (2/3) + Stats (1/3) */}
        <div className="grid gap-5" style={{ gridTemplateColumns: 'minmax(0, 1.8fr) minmax(0, 1fr)' }}>
          <div className="rise">
            <TodayCard
              plan={plan}
              loading={loadingPlan}
              onGenerate={handleGenerate}
              generating={generating}
              onItemClick={handleItemClick}
            />
          </div>
          <div className="flex flex-col gap-4 rise" style={{ animationDelay: '.08s' }}>
            <StatsTrioVertical dash={dash} loading={loadingDash} />
          </div>
        </div>

        {/* Row 2: Mastery (2/3) + Weak areas (1/3) */}
        <div className="grid gap-5 mt-6" style={{ gridTemplateColumns: 'minmax(0, 1.8fr) minmax(0, 1fr)' }}>
          <section className="card card-pad rise" style={{ animationDelay: '.16s' }}>
            <SectionHead
              eyebrow="// topic mastery"
              title={`Mastery across ${dash?.topicMastery?.length ?? 8} topics`}
              action={
                <div className="flex items-center gap-1.5">
                  <button className={`nav-tab text-[12.5px]`} data-active={t.masteryView === 'grid'}
                          onClick={() => setTweak('masteryView', 'grid')}>Grid</button>
                  <button className={`nav-tab text-[12.5px]`} data-active={t.masteryView === 'radial'}
                          onClick={() => setTweak('masteryView', 'radial')}>Radial</button>
                </div>
              }
            />
            {t.masteryView === 'radial'
              ? <MasteryRadial topics={dash?.topicMastery} loading={loadingDash} />
              : <MasteryGrid    topics={dash?.topicMastery} loading={loadingDash} />}
          </section>
          <div className="rise" style={{ animationDelay: '.22s' }}>
            <WeakAreasCard areas={dash?.weakAreas || []} loading={loadingDash}
                           onPractice={() => setToast({ kind: 'info', msg: 'Targeted practice will use the weakest 5 topics.' })} />
          </div>
        </div>

        {/* Row 3: Trend chart */}
        {t.showTrend && (
          <div className="mt-6 rise" style={{ animationDelay: '.28s' }}>
            <TrendChart points={dash?.learningTrend} loading={loadingDash} />
          </div>
        )}

        {/* Footer */}
        <footer className="mt-10 pb-12 flex items-center justify-between text-[11px] font-mono"
                style={{ color: 'var(--ink-mute)' }}>
          <span>Training Platform · v0.4 beta</span>
          <span>GET /api/study-plans/today · GET /api/analytics/dashboard</span>
        </footer>
      </main>

      {/* Toast */}
      {toast && (
        <div className="fixed bottom-6 left-1/2 -translate-x-1/2 z-50 rise"
             style={{
               background: toast.kind === 'error' ? 'oklch(0.42 0.15 25)' :
                           toast.kind === 'success' ? 'var(--accent-ink)' :
                           'var(--ink)',
               color: '#fff',
               padding: '10px 16px',
               borderRadius: 10,
               fontSize: 13,
               boxShadow: '0 10px 30px rgba(0,0,0,.18)',
               maxWidth: 'calc(100vw - 32px)',
             }}>
          {toast.msg}
        </div>
      )}

      <TweaksPanel>
        <TweakSection label="Visual" />
        <TweakRadio label="Accent" value={t.accent}
                    options={['emerald', 'indigo', 'amber']}
                    onChange={(v) => setTweak('accent', v)} />
        <TweakRadio label="Mastery view" value={t.masteryView}
                    options={['grid', 'radial']}
                    onChange={(v) => setTweak('masteryView', v)} />
        <TweakToggle label="Show trend chart" value={t.showTrend}
                     onChange={(v) => setTweak('showTrend', v)} />

        <TweakSection label="API" />
        <TweakToggle label="Demo mode" value={t.demoMode}
                     onChange={(v) => setTweak('demoMode', v)} />
        <TweakText label="API base URL" value={t.apiBase}
                   onChange={(v) => setTweak('apiBase', v)}
                   placeholder="http://localhost:5000" />

        <TweakSection label="Actions" />
        <TweakButton label="Regenerate plan" onClick={handleGenerate} />
        <TweakButton label="Sign out" onClick={handleSignOut} />
      </TweaksPanel>
    </>
  );
}

/* ─────────────── Stats trio rendered vertically next to plan ─────────────── */
function StatsTrioVertical({ dash, loading }) {
  if (loading || !dash) {
    return (
      <>
        {[1,2,3].map(i => <div key={i} className="card skel" style={{ height: 95 }} />)}
      </>
    );
  }
  const accuracyTrend = dash.learningTrend.slice(-7).map(p => p.accuracy);
  const attemptsTrend = dash.learningTrend.slice(-7).map(p => p.attempts);
  return (
    <>
      <StatCardWide
        icon={<Icon.Flame />}
        label="Study streak"
        value={dash.consistencyDays}
        suffix="days in a row"
        accent
        trend={attemptsTrend}
        sub={<>Your longest yet — keep going.</>}
      />
      <StatCardWide
        icon={<Icon.Clock />}
        label="Avg response time"
        value={dash.averageResponseTimeSeconds.toFixed(0)}
        suffix="seconds"
        trend={[...accuracyTrend].reverse()}
        sub={<>Faster than last week.</>}
      />
      <StatCardWide
        icon={<Icon.Trophy />}
        label="Challenge success"
        value={Math.round(dash.challengeSuccessRate * 100)}
        suffix="% scored ≥ 70"
        trend={accuracyTrend}
        sub={<>Across coding &amp; scenarios.</>}
      />
    </>
  );
}

function StatCardWide({ icon, label, value, suffix, accent, sub, trend }) {
  return (
    <div className="card" style={{ padding: '16px 20px' }}>
      <div className="flex items-center justify-between">
        <span className="eyebrow flex items-center gap-2">
          <span className="w-5 h-5 rounded-md inline-flex items-center justify-center"
                style={{
                  background: accent ? 'var(--accent-tint)' : 'oklch(0.96 0.006 88)',
                  color: accent ? 'var(--accent-ink)' : 'var(--ink-soft)',
                }}>
            {icon}
          </span>
          {label}
        </span>
        {trend && (
          <svg width="60" height="22" viewBox="0 0 60 22">
            <polyline
              points={trend.map((v, i) => {
                const min = Math.min(...trend), max = Math.max(...trend), range = max - min || 1;
                const x = (i / (trend.length - 1)) * 60;
                const y = 22 - ((v - min) / range) * 18 - 2;
                return `${x},${y}`;
              }).join(' ')}
              fill="none" stroke={accent ? 'var(--accent)' : 'oklch(0.66 0.04 80)'} strokeWidth="1.5"
              strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        )}
      </div>
      <div className="mt-1.5 flex items-baseline gap-1.5">
        <span className="font-semibold tabular-nums"
              style={{ fontSize: 28, letterSpacing: '-0.03em', color: 'var(--ink)' }}>
          {value}
        </span>
        <span className="text-[13px]" style={{ color: 'var(--ink-mute)' }}>{suffix}</span>
      </div>
      <div className="text-[12px]" style={{ color: 'var(--ink-soft)' }}>{sub}</div>
    </div>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<App />);
