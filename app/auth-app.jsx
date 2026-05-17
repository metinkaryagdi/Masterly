// auth-app.jsx
// Top-level Auth screen: layout + tab toggle + tweaks wiring.

const { useState, useRef, useLayoutEffect, useEffect: useEffect2 } = React;

const TWEAK_DEFAULTS = /*EDITMODE-BEGIN*/{
  "accent": "emerald",
  "layout": "split",
  "brandVisual": "mastery",
  "apiBase": "http://localhost:5000",
  "demoMode": true
}/*EDITMODE-END*/;

const ACCENT_PALETTES = {
  emerald: { accent: 'oklch(0.62 0.14 155)', accentInk: 'oklch(0.32 0.10 155)', accentTint: 'oklch(0.95 0.04 155)' },
  indigo:  { accent: 'oklch(0.55 0.18 270)', accentInk: 'oklch(0.30 0.14 270)', accentTint: 'oklch(0.95 0.04 270)' },
  amber:   { accent: 'oklch(0.66 0.16 60)',  accentInk: 'oklch(0.36 0.12 60)',  accentTint: 'oklch(0.96 0.05 60)' },
};

function TabToggle({ mode, onChange }) {
  const wrapRef = useRef(null);
  const [thumb, setThumb] = useState({ left: 3, width: 0 });

  useLayoutEffect(() => {
    if (!wrapRef.current) return;
    const wrap = wrapRef.current;
    const active = wrap.querySelector(`[data-active="true"]`);
    if (!active) return;
    const wRect = wrap.getBoundingClientRect();
    const aRect = active.getBoundingClientRect();
    setThumb({ left: aRect.left - wRect.left, width: aRect.width });
  }, [mode]);

  return (
    <div ref={wrapRef} className="tp-tabs w-full">
      <span className="tp-tab-thumb" style={{ left: thumb.left, width: thumb.width }} />
      <button type="button"
              className="tp-tab"
              data-active={mode === 'login'}
              onClick={() => onChange('login')}>
        Sign in
      </button>
      <button type="button"
              className="tp-tab"
              data-active={mode === 'register'}
              onClick={() => onChange('register')}>
        Create account
      </button>
    </div>
  );
}

function ApiStatusBadge({ apiBase, demoMode }) {
  const [status, setStatus] = useState(demoMode ? 'demo' : 'checking'); // demo | checking | online | offline
  useEffect2(() => {
    if (demoMode) { setStatus('demo'); return; }
    let cancelled = false;
    setStatus('checking');
    const ctrl = new AbortController();
    const timer = setTimeout(() => ctrl.abort(), 2500);
    fetch(`${apiBase.replace(/\/$/, '')}/api/topics`, { signal: ctrl.signal, mode: 'cors' })
      .then(() => { if (!cancelled) setStatus('online'); })
      .catch(() => { if (!cancelled) setStatus('offline'); })
      .finally(() => clearTimeout(timer));
    return () => { cancelled = true; ctrl.abort(); };
  }, [apiBase, demoMode]);

  const map = {
    demo:     { dot: 'oklch(0.66 0.16 60)',  label: 'Demo mode',    hint: 'simulated' },
    checking: { dot: 'oklch(0.75 0.02 80)',  label: 'Checking API', hint: apiBase },
    online:   { dot: 'oklch(0.62 0.14 155)', label: 'API online',   hint: apiBase },
    offline:  { dot: 'oklch(0.58 0.18 25)',  label: 'API offline',  hint: apiBase },
  };
  const s = map[status];

  return (
    <div className="inline-flex items-center gap-2 px-2.5 py-1 rounded-full border bg-white"
         style={{ borderColor: 'var(--line)' }}>
      <span className="relative flex h-1.5 w-1.5">
        <span className="absolute inset-0 rounded-full animate-ping opacity-40" style={{ background: s.dot }} />
        <span className="relative rounded-full h-1.5 w-1.5" style={{ background: s.dot }} />
      </span>
      <span className="text-[11.5px] font-medium" style={{ color: 'var(--ink)' }}>{s.label}</span>
      <span className="font-mono text-[10.5px] truncate max-w-[180px]" style={{ color: 'var(--ink-mute)' }}>{s.hint}</span>
    </div>
  );
}

function FormPane({ mode, setMode, apiBase, demoMode, layout }) {
  const handleSuccess = (data, { isRegister } = {}) => {
    const onboarded = localStorage.getItem('training_onboarded') === 'true';
    // Registrations always onboard. Logins skip onboarding if it was done before.
    if (isRegister || !onboarded) {
      window.location.href = 'Onboarding.html';
    } else {
      window.location.href = 'Dashboard.html';
    }
  };

  const isCentered = layout === 'centered' || layout === 'minimal';
  const isMinimal = layout === 'minimal';

  return (
    <main className="tp-form-pane relative flex items-center justify-center"
          style={{
            minHeight: '100vh',
            padding: isCentered ? '48px 24px' : '48px 56px',
            background: isCentered && !isMinimal ? 'var(--bg-warm)' : '#fff',
          }}>

      {isCentered && !isMinimal && <div className="absolute inset-0 tp-grid-bg pointer-events-none" />}

      <div className="relative w-full" style={{ maxWidth: 420 }}>
        {/* Centered layout: brand mini-header */}
        {isCentered && (
          <div className="flex flex-col items-center mb-7">
            <div className="tp-mark mb-3" style={{ width: 36, height: 36, fontSize: 17, borderRadius: 10 }}>T</div>
            <div className="text-[14px] font-semibold tracking-tight" style={{ color: 'var(--ink)' }}>
              Training Platform
            </div>
          </div>
        )}

        {/* Card wrapper */}
        <div className={isCentered && !isMinimal ? 'p-7 rounded-2xl bg-white' : ''}
             style={isCentered && !isMinimal ? {
               border: '1px solid var(--line)',
               boxShadow: '0 1px 0 rgba(255,255,255,.5) inset, 0 30px 80px -40px rgba(0,0,0,.18), 0 2px 6px -3px rgba(0,0,0,.06)',
             } : {}}>

          {/* Heading */}
          <div className="mb-6">
            <div className="font-mono text-[11px] uppercase tracking-[0.14em] mb-2.5"
                 style={{ color: 'var(--ink-mute)' }}>
              {mode === 'login' ? '// returning learner' : '// new learner'}
            </div>
            <h2 className="font-semibold tracking-tight"
                style={{ fontSize: 26, letterSpacing: '-0.025em', lineHeight: 1.15, color: 'var(--ink)' }}>
              {mode === 'login' ? 'Welcome back.' : 'Create your account.'}
            </h2>
            <p className="text-[14px] mt-1.5" style={{ color: 'var(--ink-soft)', lineHeight: 1.5 }}>
              {mode === 'login'
                ? "Pick up where you left off — today's plan is waiting."
                : 'Get a personalized .NET 8 study plan calibrated to your level.'}
            </p>
          </div>

          {/* Tabs */}
          <div className="mb-5">
            <TabToggle mode={mode} onChange={setMode} />
          </div>

          {/* Form */}
          <AuthForm
            key={mode /* reset state across tab change */}
            mode={mode}
            onModeChange={setMode}
            onSuccess={handleSuccess}
            apiBase={apiBase}
            demoMode={demoMode}
          />

          {/* API status + footer */}
          <div className="mt-7 pt-5 flex items-center justify-between gap-3"
               style={{ borderTop: '1px solid var(--line)' }}>
            <ApiStatusBadge apiBase={apiBase} demoMode={demoMode} />
            <div className="text-[11px] font-mono" style={{ color: 'var(--ink-mute)' }}>
              POST&nbsp;/api/auth/{mode}
            </div>
          </div>
        </div>

        {/* Legal */}
        <p className="mt-6 text-center text-[11.5px]" style={{ color: 'var(--ink-mute)', lineHeight: 1.55 }}>
          By continuing, you agree to our{' '}
          <a href="#" className="tp-link" onClick={(e) => e.preventDefault()}>Terms</a>{' '}and{' '}
          <a href="#" className="tp-link" onClick={(e) => e.preventDefault()}>Privacy Policy</a>.
        </p>
      </div>
    </main>
  );
}

function App() {
  const [t, setTweak] = useTweaks(TWEAK_DEFAULTS);
  const [mode, setMode] = useState('login');

  // Apply accent palette as CSS vars on :root
  useEffect2(() => {
    const p = ACCENT_PALETTES[t.accent] || ACCENT_PALETTES.emerald;
    const root = document.documentElement;
    root.style.setProperty('--accent', p.accent);
    root.style.setProperty('--accent-ink', p.accentInk);
    root.style.setProperty('--accent-tint', p.accentTint);
  }, [t.accent]);

  const splitView = (
    <div className="grid min-h-screen" style={{ gridTemplateColumns: 'minmax(0, 1.05fr) minmax(0, 1fr)' }}>
      <BrandPanel variant={t.brandVisual} />
      <FormPane mode={mode} setMode={setMode} apiBase={t.apiBase} demoMode={t.demoMode} layout="split" />
    </div>
  );

  const centeredView = (
    <FormPane mode={mode} setMode={setMode} apiBase={t.apiBase} demoMode={t.demoMode} layout={t.layout} />
  );

  return (
    <>
      {t.layout === 'split' ? splitView : centeredView}

      <TweaksPanel>
        <TweakSection label="Visual" />
        <TweakRadio
          label="Layout"
          value={t.layout}
          options={['split', 'centered', 'minimal']}
          onChange={(v) => setTweak('layout', v)}
        />
        <TweakRadio
          label="Brand visual"
          value={t.brandVisual}
          options={['mastery', 'streak', 'topics']}
          onChange={(v) => setTweak('brandVisual', v)}
        />
        <TweakRadio
          label="Accent"
          value={t.accent}
          options={['emerald', 'indigo', 'amber']}
          onChange={(v) => setTweak('accent', v)}
        />

        <TweakSection label="API" />
        <TweakToggle
          label="Demo mode"
          value={t.demoMode}
          onChange={(v) => setTweak('demoMode', v)}
        />
        <TweakText
          label="API base URL"
          value={t.apiBase}
          onChange={(v) => setTweak('apiBase', v)}
          placeholder="http://localhost:5000"
        />
        <TweakSection label="Try it" />
        <div className="text-[10.5px] leading-[1.5]" style={{ color: 'rgba(41,38,27,.62)', fontFamily: 'Geist Mono, monospace' }}>
          Demo mode triggers fake responses:<br />
          · <b>taken@test.com</b> → 409 conflict<br />
          · <b>fail@test.com</b> → 401 unauthorized<br />
          · anything else → success
        </div>
      </TweaksPanel>
    </>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<App />);
