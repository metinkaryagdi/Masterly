// settings-app.jsx
// Settings & profile — editable view of everything onboarding captured,
// plus appearance, notifications, data export, and a guarded delete-account.

const { useState: sS, useEffect: sE, useMemo: sM, useRef: sR } = React;

const SETTINGS_TWEAKS = /*EDITMODE-BEGIN*/{
  "accent": "emerald",
  "apiBase": "http://localhost:5000",
  "demoMode": false
}/*EDITMODE-END*/;

const SETTINGS_PALETTES = {
  emerald: { accent: 'oklch(0.62 0.14 155)', accentInk: 'oklch(0.32 0.10 155)', accentTint: 'oklch(0.95 0.04 155)' },
  indigo:  { accent: 'oklch(0.55 0.18 270)', accentInk: 'oklch(0.30 0.14 270)', accentTint: 'oklch(0.95 0.04 270)' },
  amber:   { accent: 'oklch(0.66 0.16 60)',  accentInk: 'oklch(0.36 0.12 60)',  accentTint: 'oklch(0.96 0.05 60)' },
};

const GOALS = [
  { id: 'interview', label: 'Sistem tasarımı mülakatına hazırlan' },
  { id: 'ef',        label: "EF Core 8'de üretim seviyesine gel" },
  { id: 'cqrs',      label: 'CQRS + Clean Architecture ustası ol' },
  { id: 'auth',      label: 'Güvenlik ve JWT temellerini güçlendir' },
  { id: 'perf',      label: 'Yavaş API ve sorguları hızlandır' },
  { id: 'pg',        label: "PostgreSQL iç yapısında derinleş" },
  { id: 'daily',     label: 'Sadece her gün düzenli çalış' },
];

const LEVELS = [
  { id: 'novice',   label: 'Acemi' },
  { id: 'familiar', label: 'Aşina' },
  { id: 'strong',   label: 'Güçlü' },
];

const SECTIONS = [
  { id: 'profile',        label: 'Profil' },
  { id: 'learning',       label: 'Öğrenme tercihleri' },
  { id: 'assessment',     label: 'Öz değerlendirme' },
  { id: 'appearance',     label: 'Görünüm' },
  { id: 'notifications',  label: 'Bildirimler' },
  { id: 'data',           label: 'Veri ve hesap' },
  { id: 'danger',         label: 'Tehlikeli bölge' },
];

/* ─────────────── Toggle ─────────────── */
function Toggle({ value, onChange }) {
  return (
    <div className="toggle" data-on={!!value}
         onClick={() => onChange(!value)}
         role="switch" aria-checked={!!value} tabIndex={0}
         onKeyDown={(e) => { if (e.key === ' ' || e.key === 'Enter') { e.preventDefault(); onChange(!value); } }} />
  );
}

/* ─────────────── Section card ─────────────── */
function Section({ id, eyebrow, title, description, children, saved }) {
  return (
    <section id={id} className="card scroll-mt-24">
      <div className="sec-head flex items-start justify-between gap-4">
        <div>
          <div className="eyebrow">// {eyebrow}</div>
          <h2 className="mt-1 font-semibold tracking-tight"
              style={{ fontSize: 20, letterSpacing: '-0.022em', color: 'var(--ink)' }}>
            {title}
          </h2>
          {description && (
            <p className="mt-1 text-[13.5px]" style={{ color: 'var(--ink-soft)', lineHeight: 1.55 }}>
              {description}
            </p>
          )}
        </div>
        {saved && <span className="saved">✓ Kaydedildi</span>}
      </div>
      <div className="hr" />
      <div className="sec-body">{children}</div>
    </section>
  );
}

/* ─────────────── Row ─────────────── */
function Row({ label, hint, children, alignRight }) {
  return (
    <div className="row" style={alignRight ? { gridTemplateColumns: 'minmax(0, 1.4fr) minmax(0, 1fr)' } : undefined}>
      <div>
        <div className="row-lbl">{label}</div>
        {hint && <div className="row-hint">{hint}</div>}
      </div>
      <div>{children}</div>
    </div>
  );
}

/* ─────────────── Main App ─────────────── */
function App() {
  const [t, setTweak] = useTweaks(SETTINGS_TWEAKS);
  const [user, setUser] = sS(getCurrentUser);

  // No valid user means we landed here without auth — go back to sign in.
  sE(() => {
    if (!user) {
      window.location.replace('Auth.html');
    }
  }, []);

  // Load prefs
  const initialPrefs = sM(() => {
    try {
      const p = JSON.parse(localStorage.getItem('training_prefs') || 'null');
      return p || { goals: [], minutesPerDay: 20, assessments: {} };
    } catch { return { goals: [], minutesPerDay: 20, assessments: {} }; }
  }, []);
  const [goals, setGoals] = sS(new Set(initialPrefs.goals || []));
  const [minutes, setMinutes] = sS(initialPrefs.minutesPerDay || 20);
  const [assessments, setAssessments] = sS(initialPrefs.assessments || {});

  // What the server currently holds for the fields we sync. The debounced
  // effect below only PUTs when local state actually differs from this, which
  // cleanly absorbs both the mount run and the hydration echo (debounce
  // coalescing makes run-counting unreliable). null = not hydrated yet.
  const serverSnapshot = sR(null);
  const snapshotOf = (goalsSet, mins) => JSON.stringify({ g: Array.from(goalsSet).sort(), m: mins });

  // Server preferences are the source of truth for goals + study minutes;
  // localStorage only provides the instant first paint.
  sE(() => {
    fetchPreferences({ apiBase: t.apiBase, demoMode: t.demoMode })
      .then((p) => {
        if (!p) return;
        const serverGoals = new Set(Array.isArray(p.goals) ? p.goals : []);
        const serverMinutes = p.dailyStudyMinutes || 20;
        serverSnapshot.current = snapshotOf(serverGoals, serverMinutes);
        setGoals(serverGoals);
        setMinutes(serverMinutes);
      })
      .catch(() => { /* offline — keep local values, no sync this session */ });
  }, [t.apiBase, t.demoMode]);

  // Profile
  const [name, setName] = sS(user?.displayName || '');
  sE(() => { setName(user?.displayName || ''); }, [user]);

  // Appearance
  const [accent, setAccent] = sS(() => localStorage.getItem('training_accent') || 'emerald');
  sE(() => {
    const p = SETTINGS_PALETTES[accent] || SETTINGS_PALETTES.emerald;
    document.documentElement.style.setProperty('--accent', p.accent);
    document.documentElement.style.setProperty('--accent-ink', p.accentInk);
    document.documentElement.style.setProperty('--accent-tint', p.accentTint);
    localStorage.setItem('training_accent', accent);
  }, [accent]);

  // Notifications
  const initialNotif = sM(() => {
    try {
      return JSON.parse(localStorage.getItem('training_notifications') || 'null')
        || { dailyReminder: true, reminderTime: '09:00', weeklySummary: true, streakAlerts: false };
    } catch { return { dailyReminder: true, reminderTime: '09:00', weeklySummary: true, streakAlerts: false }; }
  }, []);
  const [notif, setNotif] = sS(initialNotif);
  sE(() => { localStorage.setItem('training_notifications', JSON.stringify(notif)); }, [notif]);

  // Mastery for self-assessment comparison
  const [mastery, setMastery] = sS([]);
  sE(() => {
    fetchDashboard({ apiBase: t.apiBase, demoMode: t.demoMode })
      .then((d) => setMastery(d.topicMastery))
      .catch(() => setMastery([]));
  }, [t.apiBase, t.demoMode]);
  const masteryById = sM(() => {
    const m = {}; for (const x of mastery) m[x.topicId] = x; return m;
  }, [mastery]);

  // "Saved" indicators per section
  const [savedAt, setSavedAt] = sS({});
  const flashSaved = (sectionId) => {
    setSavedAt((s) => ({ ...s, [sectionId]: Date.now() }));
    setTimeout(() => setSavedAt((s) => {
      // Only clear if no newer save happened
      const ns = { ...s };
      delete ns[sectionId];
      return ns;
    }), 1800);
  };

  // Persist prefs (debounced).
  // - Goals and minutes are stored server-side (PUT /api/me/preferences);
  //   assessments stay local — the backend snapshots them at onboarding.
  // - Minutes maps to the backend's DailyStudyMinutes; we also derive
  //   DailyQuestionTarget from it so the daily plan respects the new budget.
  const prefsDebounce = sR(null);
  sE(() => {
    if (prefsDebounce.current) clearTimeout(prefsDebounce.current);
    prefsDebounce.current = setTimeout(() => {
      const prefs = {
        goals: Array.from(goals),
        minutesPerDay: minutes,
        assessments,
        completedAt: initialPrefs.completedAt || new Date().toISOString(),
      };
      localStorage.setItem('training_prefs', JSON.stringify(prefs));

      const snap = snapshotOf(goals, minutes);
      if (serverSnapshot.current === null || snap === serverSnapshot.current) {
        return; // not hydrated yet, or nothing the server cares about changed
      }
      updatePreferences({ apiBase: t.apiBase, demoMode: t.demoMode }, {
        dailyQuestionTarget: Math.max(1, Math.round(minutes / 2.5)),
        dailyStudyMinutes: minutes,
        dailyCodingChallengeTarget: 1,
        dailyScenarioChallengeTarget: 1,
        includeWeekends: true,
        goals: Array.from(goals),
      }).then(() => { serverSnapshot.current = snap; })
        .catch((err) => console.warn('Preferences sync failed:', err));
    }, 600);
    return () => prefsDebounce.current && clearTimeout(prefsDebounce.current);
  }, [goals, minutes, assessments, t.apiBase, t.demoMode]);

  // Persist profile
  sE(() => {
    if (!user) return;
    const trimmed = name.trim();
    if (trimmed && trimmed !== user.displayName) {
      const next = { ...user, displayName: trimmed };
      localStorage.setItem('training_user', JSON.stringify(next));
    }
  }, [name]);

  // Section observers (scrollspy)
  const [activeSec, setActiveSec] = sS(SECTIONS[0].id);
  sE(() => {
    const els = SECTIONS.map(s => document.getElementById(s.id)).filter(Boolean);
    if (els.length === 0) return;
    const io = new IntersectionObserver((entries) => {
      // Find topmost visible
      const visible = entries
        .filter(e => e.isIntersecting)
        .sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top);
      if (visible.length) setActiveSec(visible[0].target.id);
    }, { rootMargin: '-100px 0px -60% 0px' });
    els.forEach(el => io.observe(el));
    return () => io.disconnect();
  }, []);

  // Delete account modal
  const [deleting, setDeleting] = sS(false);
  const [confirmText, setConfirmText] = sS('');

  const handleDeleteAccount = () => {
    if (confirmText !== 'delete my account') return;
    localStorage.clear();
    window.location.href = 'Auth.html';
  };

  const handleSignOut = () => {
    localStorage.removeItem('training_token');
    localStorage.removeItem('training_user');
    window.location.href = 'Auth.html';
  };

  const handleExportData = () => {
    const blob = new Blob([JSON.stringify({
      user,
      prefs: { goals: Array.from(goals), minutesPerDay: minutes, assessments },
      notifications: notif,
      accent,
      exportedAt: new Date().toISOString(),
    }, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = 'training-platform-export.json';
    a.click();
    URL.revokeObjectURL(url);
    flashSaved('data');
  };

  const handleResetAssessment = () => {
    setAssessments({});
    flashSaved('assessment');
    // Bring user back to onboarding step 3
    setTimeout(() => {
      if (confirm('Run the onboarding flow again to recalibrate?')) {
        localStorage.removeItem('training_onboarded');
        window.location.href = 'Onboarding.html';
      }
    }, 300);
  };

  return (
    <>
      <TopNav activeTab="settings" apiOnline={t.demoMode ? 'demo' : null} />

      <main className="container-x py-8 rise">
        <div className="mb-7">
          <div className="eyebrow">// account</div>
          <h1 className="mt-2 font-semibold tracking-tight"
              style={{ fontSize: 30, letterSpacing: '-0.028em', color: 'var(--ink)', lineHeight: 1.15 }}>
            Settings
          </h1>
          <p className="mt-1.5 text-[14.5px]" style={{ color: 'var(--ink-soft)' }}>
            Manage your profile, learning calibration, and how the platform shows up.
          </p>
        </div>

        <div className="grid gap-10" style={{ gridTemplateColumns: 'minmax(0, 200px) minmax(0, 1fr)' }}>
          {/* Left rail */}
          <nav className="rail flex flex-col gap-1">
            {SECTIONS.map((s, i) => (
              <a key={s.id} href={`#${s.id}`}
                 data-active={activeSec === s.id}
                 onClick={(e) => {
                   e.preventDefault();
                   document.getElementById(s.id)?.scrollIntoView({ block: 'start' });
                 }}>
                <span className="lbl-num">{String(i + 1).padStart(2, '0')}</span>
                <span dangerouslySetInnerHTML={{ __html: s.label }} />
              </a>
            ))}
          </nav>

          {/* Main content */}
          <div className="flex flex-col gap-6 rise-stagger min-w-0">

            {/* ────── Profile ────── */}
            <Section id="profile" eyebrow="profile" title="Who you are"
                     description="What we'll use to address you and the email tied to your JWT."
                     saved={!!savedAt['profile']}>
              <Row label="Avatar" hint="Generated from your initials. Upload coming later.">
                <div className="flex items-center gap-4">
                  <div className="avatar avatar-lg">
                    {(name || 'You').split(' ').map(s => s[0]).slice(0,2).join('').toUpperCase()}
                  </div>
                  <button className="btn btn-ghost" disabled title="Not yet — backend has no avatar field.">
                    Upload photo (soon)
                  </button>
                </div>
              </Row>
              <Row label="Display name"
                   hint="Shown on the leaderboard and in your greeting. 3–120 characters.">
                <div className="flex items-center gap-2">
                  <input className="inline-input" value={name}
                         onChange={(e) => setName(e.target.value)}
                         onBlur={() => {
                           if (name.trim() && name.trim() !== user?.displayName) {
                             setUser({ ...user, displayName: name.trim() });
                             flashSaved('profile');
                           }
                         }}
                         minLength={3} maxLength={120} />
                </div>
              </Row>
              <Row label="Email"
                   hint="Email changes require a verification flow we haven't built yet.">
                <input className="inline-input" value={user?.email || ''} readOnly />
              </Row>
              <Row label="User ID"
                   hint="From the JWT subject claim. Useful for support tickets.">
                <input className="inline-input font-mono"
                       style={{ fontSize: 12.5, fontFamily: 'Geist Mono, monospace' }}
                       value={user?.userId || ''} readOnly />
              </Row>
            </Section>

            {/* ────── Learning preferences ────── */}
            <Section id="learning" eyebrow="learning preferences"
                     title="What we're calibrating for"
                     description="Your daily plan is biased toward these. Change them anytime."
                     saved={!!savedAt['learning']}>
              <Row label="Hedefler" hint="Uyanların hepsini seç.">
                <div className="flex flex-wrap gap-2">
                  {GOALS.map((g) => {
                    const selected = goals.has(g.id);
                    return (
                      <button key={g.id} className="goal-pill" data-selected={selected}
                              onClick={() => {
                                setGoals((prev) => {
                                  const next = new Set(prev);
                                  if (next.has(g.id)) next.delete(g.id);
                                  else next.add(g.id);
                                  return next;
                                });
                                flashSaved('learning');
                              }}>
                        <span className="chk">{selected && <span style={{ fontSize: 9, lineHeight: 0 }}>✓</span>}</span>
                        {g.label}
                      </button>
                    );
                  })}
                </div>
              </Row>

              <Row label="Time budget"
                   hint="Used to size daily plans. Tighter budgets push deep challenges to weekends.">
                <div>
                  <div className="flex items-baseline justify-between mb-3">
                    <span className="font-mono text-[11.5px]" style={{ color: 'var(--ink-mute)' }}>5 – 60 min</span>
                    <span className="font-semibold tabular-nums" style={{ fontSize: 22, letterSpacing: '-0.025em' }}>
                      {minutes}<span className="text-[13px] font-normal" style={{ color: 'var(--ink-mute)', marginLeft: 4 }}>min / day</span>
                    </span>
                  </div>
                  <input type="range" min={5} max={60} step={5} value={minutes}
                         className="slider"
                         onChange={(e) => { setMinutes(parseInt(e.target.value, 10)); flashSaved('learning'); }} />
                  <div className="flex justify-between mt-1.5 font-mono text-[10.5px]" style={{ color: 'var(--ink-mute)' }}>
                    <span>5</span><span>15</span><span>30</span><span>45</span><span>60</span>
                  </div>
                </div>
              </Row>
            </Section>

            {/* ────── Self-assessment ────── */}
            <Section id="assessment" eyebrow="self-assessment"
                     title="Where you stand"
                     description="Your starting baseline per topic. We compare it against your actual mastery as you go."
                     saved={!!savedAt['assessment']}>

              <div className="flex flex-col gap-1">
                {MOCK_TOPICS.map((tp) => {
                  const actual = masteryById[tp.id]?.masteryScore;
                  return (
                    <div key={tp.id} className="sa-row">
                      <div className="min-w-0">
                        <div className="text-[14px] font-medium truncate" style={{ color: 'var(--ink)', letterSpacing: '-0.005em' }}>
                          {tp.name}
                        </div>
                        {actual != null && (
                          <div className="text-[11.5px] font-mono mt-1" style={{ color: 'var(--ink-mute)' }}>
                            actual mastery <span style={{ color: 'var(--ink)', fontWeight: 500 }}>{actual}</span>
                            {assessments[tp.id] && (
                              <> · self-rated <span style={{ color: 'var(--ink)', fontWeight: 500 }}>{assessments[tp.id]}</span></>
                            )}
                          </div>
                        )}
                      </div>
                      <div className="w-[150px] hidden md:block">
                        {actual != null && (
                          <div className="h-[5px] rounded-full" style={{ background: 'oklch(0.94 0.006 85)' }}>
                            <div className="h-full rounded-full" style={{
                              width: `${actual}%`,
                              background: 'var(--accent)',
                              transition: 'width 1s cubic-bezier(.2,.7,.2,1)',
                            }} />
                          </div>
                        )}
                      </div>
                      <div className="sa-seg">
                        {LEVELS.map((lv) => (
                          <button key={lv.id} type="button"
                                  data-active={assessments[tp.id] === lv.id}
                                  onClick={() => {
                                    setAssessments(prev => ({ ...prev, [tp.id]: lv.id }));
                                    flashSaved('assessment');
                                  }}>
                            {lv.label}
                          </button>
                        ))}
                      </div>
                    </div>
                  );
                })}
              </div>

              <div className="mt-5 flex items-center justify-between gap-3 pt-4"
                   style={{ borderTop: '1px solid var(--line)' }}>
                <span className="text-[12.5px]" style={{ color: 'var(--ink-soft)' }}>
                  Want to rerun the calibration flow from scratch?
                </span>
                <button className="btn btn-ghost" onClick={handleResetAssessment}>
                  Reset &amp; recalibrate
                </button>
              </div>
            </Section>

            {/* ────── Appearance ────── */}
            <Section id="appearance" eyebrow="appearance"
                     title="How the platform looks"
                     description="Accent picks the highlight color used on rings, badges, and CTAs."
                     saved={!!savedAt['appearance']}>
              <Row label="Accent" hint="Applies across every screen.">
                <div className="flex flex-wrap gap-2.5">
                  {[
                    { id: 'emerald', label: 'Emerald', color: 'oklch(0.62 0.14 155)' },
                    { id: 'indigo',  label: 'Indigo',  color: 'oklch(0.55 0.18 270)' },
                    { id: 'amber',   label: 'Amber',   color: 'oklch(0.66 0.16 60)'  },
                  ].map((s) => (
                    <button key={s.id} type="button"
                            className="swatch"
                            data-selected={accent === s.id}
                            onClick={() => { setAccent(s.id); flashSaved('appearance'); }}>
                      <span className="dot" style={{ background: s.color }} />
                      {s.label}
                    </button>
                  ))}
                </div>
              </Row>
              <Row label="Theme" hint="Dark mode coming next quarter — for now, light only.">
                <button className="btn btn-ghost" disabled>Light · default</button>
              </Row>
            </Section>

            {/* ────── Notifications ────── */}
            <Section id="notifications" eyebrow="notifications"
                     title="When we'll nudge you"
                     description="Reminders we can send to keep your streak alive. Email only — no push yet."
                     saved={!!savedAt['notifications']}>
              <Row label="Daily plan reminder"
                   hint="A morning email with today's plan link.">
                <div className="flex items-center gap-4">
                  <Toggle value={notif.dailyReminder}
                          onChange={(v) => { setNotif(n => ({ ...n, dailyReminder: v })); flashSaved('notifications'); }} />
                  <input type="time" value={notif.reminderTime}
                         disabled={!notif.dailyReminder}
                         onChange={(e) => { setNotif(n => ({ ...n, reminderTime: e.target.value })); flashSaved('notifications'); }}
                         className="inline-input" style={{ width: 120 }} />
                </div>
              </Row>
              <Row label="Weekly summary"
                   hint="Friday digest of mastery gains, weak areas, and what to focus on next week.">
                <Toggle value={notif.weeklySummary}
                        onChange={(v) => { setNotif(n => ({ ...n, weeklySummary: v })); flashSaved('notifications'); }} />
              </Row>
              <Row label="Streak-at-risk alerts"
                   hint="A late-day ping if you haven't started today's plan and your streak is on the line.">
                <Toggle value={notif.streakAlerts}
                        onChange={(v) => { setNotif(n => ({ ...n, streakAlerts: v })); flashSaved('notifications'); }} />
              </Row>
            </Section>

            {/* ────── Data & account ────── */}
            <Section id="data" eyebrow="data & account"
                     title="Take your data, leave whenever"
                     description="Export everything we know about your preferences. Sign out anytime."
                     saved={!!savedAt['data']}>
              <Row label="Export my data" hint="Profile + preferences + assessments as JSON.">
                <button className="btn btn-ghost" onClick={handleExportData}>
                  Download JSON
                </button>
              </Row>
              <Row label="Authentication" hint="Signs out on this device only. Other sessions keep their tokens.">
                <button className="btn btn-ghost" onClick={handleSignOut}>
                  Sign out
                </button>
              </Row>
              <Row label="Active token" hint="The JWT currently in localStorage. First 24 chars shown.">
                <input className="inline-input" readOnly
                       style={{ fontFamily: 'Geist Mono, monospace', fontSize: 12 }}
                       value={(localStorage.getItem('training_token') || '').slice(0, 24) + '…'} />
              </Row>
            </Section>

            {/* ────── Danger zone ────── */}
            <section id="danger" className="danger-card scroll-mt-24">
              <div className="sec-head">
                <div className="eyebrow" style={{ color: 'oklch(0.46 0.16 25)' }}>// danger zone</div>
                <h2 className="mt-1 font-semibold tracking-tight"
                    style={{ fontSize: 20, letterSpacing: '-0.022em', color: 'var(--ink)' }}>
                  Permanent stuff
                </h2>
                <p className="mt-1 text-[13.5px]" style={{ color: 'var(--ink-soft)' }}>
                  No undo. We mean it. Backed up nothing.
                </p>
              </div>
              <div className="sec-body" style={{ borderTop: '1px solid color-mix(in oklch, var(--danger) 18%, transparent)' }}>
                <Row label="Delete account"
                     hint="Removes your user record, answers, submissions, and progress. We keep aggregate stats only.">
                  <button className="btn btn-danger-ghost" onClick={() => { setDeleting(true); setConfirmText(''); }}>
                    Delete account…
                  </button>
                </Row>
              </div>
            </section>

            {/* Footer */}
            <footer className="pt-2 pb-8 flex items-center justify-between text-[11px] font-mono"
                    style={{ color: 'var(--ink-mute)' }}>
              <span>Training Platform · settings</span>
              <span>local-only · backend has no GET/PATCH /api/users/me yet</span>
            </footer>
          </div>
        </div>
      </main>

      {/* Delete confirmation modal */}
      {deleting && (
        <div className="modal-backdrop" onClick={(e) => { if (e.target === e.currentTarget) setDeleting(false); }}>
          <div className="modal">
            <div className="eyebrow" style={{ color: 'oklch(0.46 0.16 25)' }}>// delete account</div>
            <h3 className="mt-1.5 font-semibold tracking-tight" style={{ fontSize: 20, letterSpacing: '-0.022em' }}>
              Are you absolutely sure?
            </h3>
            <p className="mt-2 text-[14px]" style={{ color: 'var(--ink-soft)', lineHeight: 1.55 }}>
              This deletes your profile, all submissions, mastery scores, and revision schedules. There is no undo — you'll have to start from scratch if you come back.
            </p>
            <div className="mt-5">
              <label className="text-[12.5px]" style={{ color: 'var(--ink-soft)' }}>
                Type <span style={{ fontFamily: 'Geist Mono, monospace', color: 'var(--ink)', background: 'oklch(0.96 0.006 88)', padding: '1px 6px', borderRadius: 4 }}>delete my account</span> to confirm
              </label>
              <input type="text" className="inline-input mt-2"
                     placeholder="delete my account"
                     value={confirmText}
                     onChange={(e) => setConfirmText(e.target.value)}
                     autoFocus
                     style={{ fontFamily: 'Geist Mono, monospace', fontSize: 13 }} />
            </div>
            <div className="mt-6 flex items-center justify-end gap-2">
              <button className="btn btn-ghost" onClick={() => setDeleting(false)}>Cancel</button>
              <button className="btn btn-danger"
                      disabled={confirmText !== 'delete my account'}
                      onClick={handleDeleteAccount}>
                Yes, delete everything
              </button>
            </div>
          </div>
        </div>
      )}

      <TweaksPanel>
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
