// challenge-app.jsx
// IDE-style coding + scenario challenge surface. Wires Challenge.html to the
// backend's CodingChallengeDto / ScenarioChallengeDto + submission endpoints.

const { useState: cS, useEffect: cE, useMemo: cM, useRef: cR } = React;

const CHAL_TWEAKS = /*EDITMODE-BEGIN*/{
  "accent": "emerald",
  "ideFont": "JetBrains Mono",
  "fontSize": 12.5,
  "apiBase": "http://localhost:5000",
  "demoMode": false
}/*EDITMODE-END*/;

const CHAL_PALETTES = {
  emerald: { accent: 'oklch(0.62 0.14 155)', accentInk: 'oklch(0.32 0.10 155)', accentTint: 'oklch(0.95 0.04 155)' },
  indigo:  { accent: 'oklch(0.55 0.18 270)', accentInk: 'oklch(0.30 0.14 270)', accentTint: 'oklch(0.95 0.04 270)' },
  amber:   { accent: 'oklch(0.66 0.16 60)',  accentInk: 'oklch(0.36 0.12 60)',  accentTint: 'oklch(0.96 0.05 60)' },
};

/* ─────────────── Icons ─────────────── */
const CI = {
  Close: (p) => (<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" {...p}><path d="M4 4l8 8M12 4l-8 8"/></svg>),
  Back: (p) => (<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M13 8H3M7 4L3 8l4 4"/></svg>),
  Arrow: (p) => (<svg viewBox="0 0 16 16" width="13" height="13" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M3 8h10M9 4l4 4-4 4"/></svg>),
  Play: (p) => (<svg viewBox="0 0 16 16" width="13" height="13" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M5 3l8 5-8 5V3z" fill="currentColor"/></svg>),
  Check: (p) => (<svg viewBox="0 0 14 14" width="11" height="11" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M2.5 7.5l3 3L11.5 4"/></svg>),
  X: (p) => (<svg viewBox="0 0 14 14" width="11" height="11" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" {...p}><path d="M3.5 3.5l7 7M10.5 3.5l-7 7"/></svg>),
  Twist: (p) => (<svg viewBox="0 0 16 16" width="11" height="11" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M6 4l4 4-4 4"/></svg>),
  Spin: (p) => (<svg viewBox="0 0 16 16" width="13" height="13" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" {...p}><circle cx="8" cy="8" r="6" opacity=".25"/><path d="M14 8a6 6 0 0 0-6-6"/></svg>),
  Spark: (p) => (<svg viewBox="0 0 14 14" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M7 1v3M7 10v3M1 7h3M10 7h3M3 3l2 2M9 9l2 2M11 3l-2 2M3 11l2-2"/></svg>),
  Reset: (p) => (<svg viewBox="0 0 16 16" width="13" height="13" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M13 3v3.5h-3.5"/><path d="M13 6.5A5 5 0 0 0 4 5.5M3 9.5A5 5 0 0 0 12 10.5"/></svg>),
};

/* ─────────────── C# syntax highlighter ─────────────── */
const CSHARP_KEYWORDS = [
  'abstract','as','async','await','base','bool','break','byte','case','catch','char',
  'checked','class','const','continue','decimal','default','delegate','do','double','else',
  'enum','event','explicit','extern','false','finally','fixed','float','for','foreach',
  'get','goto','if','implicit','in','init','int','interface','internal','is','lock','long',
  'namespace','new','null','object','operator','out','override','params','partial','private',
  'protected','public','readonly','record','ref','return','sbyte','sealed','set','short',
  'sizeof','stackalloc','static','string','struct','switch','this','throw','true','try',
  'typeof','uint','ulong','unchecked','unsafe','ushort','using','var','virtual','void',
  'volatile','when','where','while','yield','nameof','sealed',
];

function highlightCSharp(src) {
  const specs = [
    ['comment', /\/\/[^\n]*/y],
    ['comment', /\/\*[\s\S]*?\*\//y],
    ['string',  /@?\$?"(?:\\.|[^"\\\n])*"?/y],
    ['string',  /'(?:\\.|[^'\\\n])'/y],
    ['attr',    /\[[A-Z][\w.]*(?:\([^\)\n]*\))?\]/y],
    ['keyword', new RegExp('\\b(?:' + CSHARP_KEYWORDS.join('|') + ')\\b', 'y')],
    ['number',  /\b\d+(?:\.\d+)?[fFdDmMlLuU]?\b/y],
    ['type',    /\b[A-Z][A-Za-z0-9_]*\b/y],
    ['fn',      /\b[a-zA-Z_]\w*(?=\s*\()/y],
    ['ident',   /\b[a-zA-Z_]\w*\b/y],
    ['ws',      /\s+/y],
    ['punct',   /[\(\)\[\]\{\}<>.,;:=+\-*\/%&|^!?@~]+/y],
  ];
  const out = [];
  let i = 0;
  while (i < src.length) {
    let matched = false;
    for (const [type, re] of specs) {
      re.lastIndex = i;
      const m = re.exec(src);
      if (m && m.index === i) {
        out.push({ type, text: m[0] });
        i += m[0].length;
        matched = true;
        break;
      }
    }
    if (!matched) {
      out.push({ type: 'misc', text: src[i] });
      i++;
    }
  }
  return out;
}

const COLORED = new Set(['comment', 'string', 'keyword', 'type', 'number', 'fn', 'attr']);

/* ─────────────── CodeEditor ─────────────── */
function CodeEditor({ value, onChange, readOnly = false }) {
  const taRef = cR(null);
  const tokens = cM(() => highlightCSharp(value), [value]);

  cE(() => {
    if (!taRef.current) return;
    taRef.current.style.height = 'auto';
    taRef.current.style.height = taRef.current.scrollHeight + 'px';
  }, [value]);

  const lines = value.split('\n');
  const lineNumbers = lines.map((_, i) => String(i + 1)).join('\n');

  const handleKey = (e) => {
    if (e.key === 'Tab') {
      e.preventDefault();
      const ta = e.target;
      const s = ta.selectionStart, end = ta.selectionEnd;
      const next = value.slice(0, s) + '    ' + value.slice(end);
      onChange(next);
      requestAnimationFrame(() => {
        ta.selectionStart = ta.selectionEnd = s + 4;
      });
    }
  };

  return (
    <div className="code-grid">
      <div className="gutter">{lineNumbers}</div>
      <div className="code-area">
        <pre aria-hidden="true">
          {tokens.map((t, i) =>
            COLORED.has(t.type)
              ? <span key={i} className={`tk-${t.type}`}>{t.text}</span>
              : <span key={i}>{t.text}</span>
          )}
          {/* trailing newline so caret-on-blank-line is visible */}{'\n'}
        </pre>
        {!readOnly && (
          <textarea
            ref={taRef}
            value={value}
            onChange={(e) => onChange(e.target.value)}
            onKeyDown={handleKey}
            spellCheck={false}
            autoComplete="off"
            autoCorrect="off"
            autoCapitalize="off"
            rows={Math.max(8, lines.length)}
          />
        )}
      </div>
    </div>
  );
}

/* ─────────────── Top bar ─────────────── */
function ChallengeTopBar({ kind, challenge, onExit, elapsedSec, outcome }) {
  return (
    <header className="topbar">
      <div className="container-x flex items-center gap-4" style={{ height: 60 }}>
        <button className="btn-icon" onClick={onExit} title="Çık" aria-label="Çık">
          <CI.Close />
        </button>
        <a href="Dashboard.html" className="hidden md:flex items-center gap-2.5"
           style={{ textDecoration: 'none' }}>
          <div className="tp-mark">T</div>
          <span className="font-semibold tracking-tight text-[14.5px]" style={{ color: 'var(--ink)' }}>
            Training Platform
          </span>
        </a>

        <div className="flex items-center gap-2 flex-1 min-w-0 pl-4"
             style={{ borderLeft: '1px solid var(--line)' }}>
          <span className="topic-chip">
            {kind === 'coding' ? 'Kod görevi' : 'Senaryo görevi'}
          </span>
          {challenge && (
            <span className="text-[13.5px] font-medium truncate"
                  style={{ color: 'var(--ink)', letterSpacing: '-0.01em', maxWidth: 600 }}>
              {challenge.title}
            </span>
          )}
        </div>

        <div className="flex items-center gap-3">
          {challenge && (
            <span className="font-mono text-[11.5px]" style={{ color: 'var(--ink-mute)' }}>
              ~{challenge.estimatedMinutes}m · {difficultyLabel(challenge.difficulty)}
            </span>
          )}
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full border bg-white"
                style={{ borderColor: 'var(--line)' }}>
            <Icon.Clock />
            <span className="font-mono text-[11.5px] tabular-nums" style={{ color: 'var(--ink-soft)' }}>
              {fmtTime(elapsedSec)}
            </span>
          </span>
          {outcome && (
            <span className={
              outcome.outcome === ChallengeOutcome.Passed   ? 'ribbon ribbon-passed' :
              outcome.outcome === ChallengeOutcome.NeedsWork ? 'ribbon ribbon-needs' :
              'ribbon ribbon-pending'
            }>
              {outcome.outcome === ChallengeOutcome.Passed   ? <><CI.Check /> passed</> :
               outcome.outcome === ChallengeOutcome.NeedsWork ? <>needs work</> :
               <>pending review</>}
              <span style={{ opacity: 0.6, marginLeft: 4 }}>{outcome.score}/100</span>
            </span>
          )}
        </div>
      </div>
    </header>
  );
}

function fmtTime(s) {
  const m = Math.floor(s / 60), sec = s % 60;
  return `${String(m).padStart(2, '0')}:${String(sec).padStart(2, '0')}`;
}

/* ─────────────── Markdown-ish renderer (lightweight) ─────────────── */
function renderMd(text) {
  if (!text) return null;
  const blocks = text.split(/\n{2,}/);
  return blocks.map((block, i) => {
    // Bold via **...**
    let html = block
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
      .replace(/`([^`]+)`/g, '<code>$1</code>')
      .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
      .replace(/\\(.)/g, '$1')
      .replace(/\n/g, '<br/>');
    return <p key={i} dangerouslySetInnerHTML={{ __html: html }} />;
  });
}

/* ─────────────── Problem panel (coding) ─────────────── */
function CodingProblemPanel({ challenge, mode, setMode, criteriaResults }) {
  return (
    <section className="card" style={{ minHeight: 'calc(100vh - 100px)' }}>
      <div className="px-5 pt-5 pb-4">
        <div className="flex items-center justify-between">
          <div className="eyebrow">// problem</div>
          <span className="font-mono text-[10.5px]" style={{ color: 'var(--ink-mute)' }}>
            {challenge.evaluationCriteria.length} kriter
          </span>
        </div>
        <h1 className="mt-2 font-semibold tracking-tight"
            style={{ fontSize: 20, letterSpacing: '-0.022em', color: 'var(--ink)', lineHeight: 1.25 }}>
          {challenge.title}
        </h1>
        <div className="mt-3 flex flex-wrap items-center gap-2">
          <span className="topic-chip">{TOPIC_NAMES[challenge.topicId]}</span>
          <span className="topic-chip">
            <span className="difpip">
              {[1,2,3,4].map(n => <i key={n} data-on={n <= challenge.difficulty ? 'true' : 'false'} />)}
            </span>
            <span style={{ color: 'var(--ink-mute)', marginLeft: 4 }}>{difficultyLabel(challenge.difficulty)}</span>
          </span>
          <span className="topic-chip">~{challenge.estimatedMinutes} min</span>
        </div>
      </div>

      <div className="px-5">
        <div className="tabs">
          {['problem', 'criteria', 'hints'].map(m => (
            <button key={m} data-active={mode === m} onClick={() => setMode(m)}>
              {m === 'problem' ? 'Problem' : m === 'criteria' ? `Kriterler (${challenge.evaluationCriteria.length})` : `İpuçları (${challenge.hints?.length || 0})`}
            </button>
          ))}
        </div>
      </div>

      <div className="px-5 pt-5 pb-6">
        {mode === 'problem' && (
          <>
            <div className="prose-md">{renderMd(challenge.description)}</div>
            <div className="mt-5 p-4 rounded-xl" style={{ background: 'oklch(0.985 0.005 88)', border: '1px solid var(--line)' }}>
              <div className="eyebrow" style={{ fontSize: 10.5 }}>beklenen sonuç</div>
              <p className="prose-md mt-2">{challenge.expectedOutcome}</p>
            </div>
          </>
        )}
        {mode === 'criteria' && (
          <div>
            {challenge.evaluationCriteria.map((c, i) => {
              const met = criteriaResults?.[i]?.met;
              const state = met == null ? 'open' : met ? 'true' : 'false';
              return (
                <div key={i} className="crit-row">
                  <span className="crit-check" data-met={state}>
                    {state === 'true' ? <CI.Check /> : state === 'false' ? <CI.X /> : i + 1}
                  </span>
                  <div>
                    <div className="text-[13.5px]" style={{ color: 'var(--ink)', letterSpacing: '-0.005em', lineHeight: 1.5 }}>
                      {c}
                    </div>
                    {state !== 'open' && (
                      <div className="text-[12px] mt-1" style={{ color: state === 'true' ? 'var(--accent-ink)' : 'oklch(0.46 0.16 25)' }}>
                        {state === 'true' ? 'Karşılandı' : 'Gönderimde karşılanmadı'}
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
        {mode === 'hints' && (
          <div className="flex flex-col gap-2.5">
            {(challenge.hints || []).map((h, i) => (
              <details key={i} className="hint">
                <summary>
                  <span className="twist"><CI.Twist /></span>
                  <span className="font-mono text-[11px]" style={{ color: 'var(--ink-mute)' }}>İPUCU {i + 1}</span>
                  <span>Göster</span>
                </summary>
                <div className="body">{h}</div>
              </details>
            ))}
            <div className="mt-2 text-[12px]" style={{ color: 'var(--ink-mute)' }}>
              Her ipucu en yüksek puanını 10 azaltır. Önce ipuçsuz dene.
            </div>
          </div>
        )}
      </div>
    </section>
  );
}

/* ─────────────── IDE panel (coding) ─────────────── */
function IdePanel({ challenge, code, setCode, notes, setNotes, onRun, onSubmit, submitting, runState, runResult, running, fileTab, setFileTab }) {
  const tabs = [
    { id: 'solution', label: 'Solution.cs', dirty: code !== challenge.starterCode },
    { id: 'tests',    label: 'Tests.cs',    dirty: false, readOnly: true },
    { id: 'notes',    label: 'Notes.md',    dirty: notes.length > 0 },
  ];
  const active = tabs.find(t => t.id === fileTab) || tabs[0];

  return (
    <section className="ide" style={{ minHeight: 'calc(100vh - 100px)' }}>
      {/* Tab strip */}
      <div className="ide-tabs">
        {tabs.map(t => (
          <button key={t.id} className="ide-tab"
                  data-active={fileTab === t.id}
                  onClick={() => setFileTab(t.id)}>
            <span className={`dot ${t.dirty ? 'dirty' : ''}`} />
            {t.label}
          </button>
        ))}
        <div className="flex-1" />
        <div className="flex items-center gap-2 px-2">
          <button className="ide-tab" onClick={() => setCode(challenge.starterCode)}>
            <CI.Reset /> Sıfırla
          </button>
        </div>
      </div>

      {/* Editor content */}
      <div className="code-wrap">
        {fileTab === 'solution' && (
          <CodeEditor value={code} onChange={setCode} />
        )}
        {fileTab === 'tests' && (
          <TestsView tests={challenge.tests || []} runState={runState}
                     testCode={challenge.testCode} runResult={runResult} running={running} />
        )}
        {fileTab === 'notes' && (
          <NotesView value={notes} onChange={setNotes} />
        )}
      </div>

      {/* Toolbar */}
      <div className="ide-toolbar">
        <div className="crumbs">
          <span>TrainingPlatform.Api</span>
          <span className="sep">/</span>
          <span>Controllers</span>
          <span className="sep">/</span>
          <span style={{ color: 'var(--ide-fg)' }}>{active.label}</span>
        </div>
        <div className="flex items-center gap-2">
          <span>net8.0 · csharp 12</span>
          <span className="sep">·</span>
          <span>UTF-8</span>
          <span className="sep">·</span>
          <span>{code.split('\n').length} lines</span>
        </div>
      </div>

      {/* Action bar (light) */}
      <div className="px-4 py-3 flex items-center justify-between"
           style={{ background: '#fff', borderTop: '1px solid var(--line)' }}>
        <div className="flex items-center gap-3">
          <button className="btn btn-ghost" onClick={onRun} disabled={submitting || running}>
            {running ? <><CI.Spin className="spin" /> Koşuluyor…</> : <><CI.Play /> Testleri çalıştır</>}
          </button>
          {runResult && !running && (
            <span className="font-mono text-[11.5px]"
                  style={{ color: runResult.compiled && runResult.failedTests === 0 && runResult.totalTests > 0
                    ? 'var(--accent-ink)' : 'oklch(0.46 0.16 25)' }}>
              {runResult.evaluated === false
                ? 'değerlendirilemedi'
                : runResult.compiled
                  ? `${runResult.passedTests}/${runResult.totalTests} test geçti`
                  : 'derleme hatası'}
            </span>
          )}
          <span className="font-mono text-[11px]" style={{ color: 'var(--ink-mute)' }}>
            Göndermek için <kbd>Cmd</kbd> + <kbd>Enter</kbd>
          </span>
        </div>
        <button className="btn btn-primary btn-lg" onClick={onSubmit} disabled={submitting || code === challenge.starterCode}>
          {submitting ? <><CI.Spin className="spin" /> Gönderiliyor…</> : <>Değerlendirmeye gönder <CI.Arrow /></>}
        </button>
      </div>
    </section>
  );
}

function TestsView({ tests, runState, testCode, runResult, running }) {
  // Real challenges carry the actual xUnit source in testCode; mock challenges
  // carry a display-only test list in `tests`.
  if (tests.length === 0) {
    return (
      <div style={{ padding: 16 }}>
        <div className="eyebrow" style={{ color: 'var(--ide-mute)', fontSize: 10.5 }}>
          // xunit test paketi — kodun bu dosyayla birlikte derlenir
        </div>
        {running && (
          <div className="mt-3 font-mono text-[12px]" style={{ color: 'var(--ide-mute)' }}>
            Derleniyor ve testler koşuluyor…
          </div>
        )}
        {runResult && !running && (
          <pre className="mt-3 font-mono text-[12px]"
               style={{
                 whiteSpace: 'pre-wrap',
                 color: runResult.compiled && runResult.failedTests === 0 && runResult.totalTests > 0
                   ? 'var(--accent)' : 'var(--danger)',
                 background: 'color-mix(in oklch, var(--ide-line) 30%, transparent)',
                 padding: '10px 12px', borderRadius: 8,
               }}>
            {runResult.output || (runResult.compiled ? `${runResult.passedTests}/${runResult.totalTests} test geçti` : 'Derleme başarısız.')}
          </pre>
        )}
        {testCode ? (
          <pre className="mt-3 font-mono text-[12px]" style={{ whiteSpace: 'pre-wrap', color: 'var(--ide-fg)', lineHeight: 1.6 }}>
            {testCode}
          </pre>
        ) : (
          <div className="mt-3 text-[12.5px]" style={{ color: 'var(--ide-mute)' }}>
            Bu görevin otomatik test paketi yok — gönderimin değerlendirme
            kriterlerine göre puanlanır ve incelenir.
          </div>
        )}
      </div>
    );
  }

  return (
    <div style={{ padding: 16 }}>
      <div className="eyebrow" style={{ color: 'var(--ide-mute)', fontSize: 10.5 }}>
        // public test suite ({tests.length} cases)
      </div>
      <div className="mt-3 flex flex-col">
        {tests.map((t, i) => {
          const state = runState
            ? (runState[i] || 'pending')
            : 'idle';
          return (
            <div key={i} className="run-line" data-state={state}>
              <span>
                {state === 'pass' && <span className="dot" />}
                {state === 'fail' && <span className="dot" />}
                {state === 'running' && <span className="gen-spinner-dot" style={{ width: 14, height: 14, border: '2px solid var(--ide-line)', borderTopColor: 'var(--ide-fg)' }} />}
                {state === 'pending' && <span className="dot" style={{ background: 'color-mix(in oklch, var(--ide-mute) 60%, transparent)' }} />}
                {state === 'idle' && <span className="font-mono text-[10px]" style={{ color: 'var(--ide-mute)' }}>{String(i+1).padStart(2,'0')}</span>}
              </span>
              <span style={{ color: state === 'pending' || state === 'idle' ? 'color-mix(in oklch, var(--ide-fg) 60%, transparent)' : 'var(--ide-fg)' }}>
                {t.name}
              </span>
              <span className="font-mono text-[10.5px]" style={{ color: state === 'pass' ? 'var(--accent)' : state === 'fail' ? 'var(--danger)' : 'var(--ide-mute)' }}>
                {state === 'pass' ? 'OK' :
                 state === 'fail' ? 'FAIL' :
                 state === 'running' ? '...' :
                 state === 'pending' ? 'queued' : '—'}
              </span>
            </div>
          );
        })}
      </div>
      <div className="mt-4 font-mono text-[10.5px]" style={{ color: 'var(--ide-mute)' }}>
        Gizli durumlar gönderimden sonra koşulur.
      </div>
    </div>
  );
}

function NotesView({ value, onChange }) {
  return (
    <div style={{ padding: 16 }}>
      <textarea
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder="// jot down your approach, edge cases, things you'd refactor with more time…"
        rows={20}
        style={{
          width: '100%',
          background: 'transparent',
          color: 'var(--ide-fg)',
          border: 0,
          outline: 'none',
          resize: 'none',
          fontFamily: 'JetBrains Mono, monospace',
          fontSize: 12.5,
          lineHeight: 1.65,
        }}
      />
    </div>
  );
}

/* ─────────────── Scenario layout ─────────────── */
function ScenarioPanel({ challenge, criteriaResults }) {
  return (
    <section className="card" style={{ minHeight: 'calc(100vh - 100px)' }}>
      <div className="px-5 pt-5 pb-4">
        <div className="eyebrow">// scenario</div>
        <h1 className="mt-2 font-semibold tracking-tight"
            style={{ fontSize: 22, letterSpacing: '-0.022em', color: 'var(--ink)', lineHeight: 1.25 }}>
          {challenge.title}
        </h1>
        <div className="mt-3 flex flex-wrap items-center gap-2">
          <span className="topic-chip">{TOPIC_NAMES[challenge.topicId]}</span>
          <span className="topic-chip">
            <span className="difpip">
              {[1,2,3,4].map(n => <i key={n} data-on={n <= challenge.difficulty ? 'true' : 'false'} />)}
            </span>
            <span style={{ color: 'var(--ink-mute)', marginLeft: 4 }}>{difficultyLabel(challenge.difficulty)}</span>
          </span>
          <span className="topic-chip">~{challenge.estimatedMinutes} min</span>
        </div>
      </div>

      <div className="px-5 pb-5">
        <div className="prose-md">{renderMd(challenge.scenario)}</div>
      </div>

      <div className="px-5 pb-5">
        <div className="eyebrow flex items-center gap-2" style={{ marginBottom: 10 }}>
          // evaluation criteria
        </div>
        <div>
          {challenge.evaluationCriteria.map((c, i) => {
            const met = criteriaResults?.[i]?.met;
            const state = met == null ? 'open' : met ? 'true' : 'false';
            return (
              <div key={i} className="crit-row">
                <span className="crit-check" data-met={state}>
                  {state === 'true' ? <CI.Check /> : state === 'false' ? <CI.X /> : i + 1}
                </span>
                <div className="text-[13.5px]" style={{ color: 'var(--ink)', letterSpacing: '-0.005em', lineHeight: 1.5 }}>
                  {c}
                </div>
              </div>
            );
          })}
        </div>
      </div>

      <div className="px-5 pb-5">
        <details className="hint">
          <summary>
            <span className="twist"><CI.Twist /></span>
            <span className="font-mono text-[11px]" style={{ color: 'var(--ink-mute)' }}>HINTS</span>
            <span>Show 3 hints (lowers max score)</span>
          </summary>
          <div className="body">
            {(challenge.hints || []).map((h, i) => (
              <div key={i} className="mb-2">
                <span className="font-mono text-[11px]" style={{ color: 'var(--ink-mute)' }}>#{i + 1} </span>
                {h}
              </div>
            ))}
          </div>
        </details>
      </div>
    </section>
  );
}

function ScenarioEditor({ challenge, text, setText, onSubmit, submitting }) {
  const words = text.trim() ? text.trim().split(/\s+/).length : 0;
  const minWords = 80;
  const canSubmit = words >= minWords;

  return (
    <section className="card" style={{ minHeight: 'calc(100vh - 100px)', display: 'flex', flexDirection: 'column' }}>
      <div className="px-5 pt-5 pb-3 flex items-center justify-between">
        <div>
          <div className="eyebrow">// your response</div>
          <div className="mt-1 text-[13px]" style={{ color: 'var(--ink-soft)' }}>
            Walk through your reasoning. Show the trade-offs.
          </div>
        </div>
        <div className="font-mono text-[11.5px] tabular-nums" style={{ color: 'var(--ink-mute)' }}>
          {words} words {words < minWords && <span style={{ color: 'var(--warn)' }}>· needs ≥ {minWords}</span>}
        </div>
      </div>

      <div className="px-5 pb-3 flex-1">
        <textarea
          className="scenario-text"
          style={{ minHeight: 'calc(100vh - 280px)' }}
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder="Start with your overall approach in 1–2 sentences, then go through each criterion. Don't worry about polish — clarity over prose."
        />
      </div>

      <div className="px-5 py-3 flex items-center justify-between"
           style={{ borderTop: '1px solid var(--line)' }}>
        <span className="font-mono text-[11px]" style={{ color: 'var(--ink-mute)' }}>
          <kbd>Cmd</kbd> + <kbd>Enter</kbd> to submit
        </span>
        <button className="btn btn-primary btn-lg" onClick={onSubmit} disabled={submitting || !canSubmit}>
          {submitting ? <><CI.Spin className="spin" /> Submitting…</> : <>Submit response <CI.Arrow /></>}
        </button>
      </div>
    </section>
  );
}

/* ─────────────── Evaluation panel (slides up after submit) ─────────────── */
function EvaluationPanel({ kind, challenge, result, onClose, onNext }) {
  return (
    <div className="fixed inset-x-0 bottom-0 z-30 slide-up"
         style={{
           background: '#fff',
           borderTop: '1px solid var(--line)',
           boxShadow: '0 -20px 60px -20px rgba(0,0,0,.18)',
         }}>
      <div className="container-x py-5">
        <div className="grid gap-5" style={{ gridTemplateColumns: 'minmax(0, 1fr) minmax(0, 1.4fr)' }}>
          {/* Left: outcome + score */}
          <div>
            <div className={
              result.outcome === ChallengeOutcome.Passed   ? 'banner banner-passed' :
              result.outcome === ChallengeOutcome.NeedsWork ? 'banner banner-needswork' :
              'banner banner-pending'
            }>
              <span className="w-9 h-9 rounded-full flex-shrink-0 inline-flex items-center justify-center"
                    style={{
                      background: result.outcome === ChallengeOutcome.Passed ? 'var(--accent)' : result.outcome === ChallengeOutcome.NeedsWork ? 'var(--warn)' : 'var(--ink-mute)',
                      color: '#fff'
                    }}>
                {result.outcome === ChallengeOutcome.Passed ? <CI.Check /> : <CI.X />}
              </span>
              <div className="min-w-0 flex-1">
                <div className="flex items-baseline justify-between gap-3">
                  <div className="font-semibold tracking-tight" style={{ fontSize: 18, letterSpacing: '-0.02em' }}>
                    {result.outcome === ChallengeOutcome.Passed ? 'Passed.' :
                     result.outcome === ChallengeOutcome.NeedsWork ? 'Needs work.' :
                     'Pending human review.'}
                  </div>
                  <div className="font-mono tabular-nums" style={{ fontSize: 13 }}>
                    score <b style={{ fontWeight: 600 }}>{result.score}</b> / 100
                  </div>
                </div>
                {Number.isFinite(result.testsTotal) && result.testsTotal > 0 && (
                  <div className="mt-1 font-mono text-[12px] tabular-nums">
                    {result.testsPassed}/{result.testsTotal} tests passed
                  </div>
                )}
                <p className="mt-1 text-[13.5px]" style={{ lineHeight: 1.55, textWrap: 'pretty', whiteSpace: 'pre-wrap' }}>
                  {result._aiFeedback}
                </p>
              </div>
            </div>

            <div className="mt-3 flex items-center gap-2">
              <button className="btn btn-ghost" onClick={onClose}>
                <CI.Back /> Keep editing
              </button>
              <button className="btn btn-primary btn-lg flex-1" onClick={onNext}>
                Back to today's plan <CI.Arrow />
              </button>
            </div>
          </div>

          {/* Right: criteria results */}
          <div>
            <div className="eyebrow flex items-center gap-2"><CI.Spark /> criteria breakdown</div>
            <div className="mt-2 card" style={{ padding: '6px 16px 4px' }}>
              {result._criteriaResults?.map((c, i) => (
                <div key={i} className="crit-row" style={{ borderBottomStyle: 'solid' }}>
                  <span className="crit-check" data-met={c.met ? 'true' : 'false'}>
                    {c.met ? <CI.Check /> : <CI.X />}
                  </span>
                  <div>
                    <div className="text-[13.5px]" style={{ color: 'var(--ink)' }}>
                      {c.criterion}
                    </div>
                    <div className="text-[12px] mt-0.5"
                         style={{ color: c.met ? 'var(--accent-ink)' : 'oklch(0.46 0.16 25)' }}>
                      {c.met ? 'Addressed' : 'Missing or only partially addressed'}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

/* ─────────────── Main App ─────────────── */
function App() {
  const [t, setTweak] = useTweaks(CHAL_TWEAKS);

  const kind = cM(() => new URLSearchParams(location.search).get('kind') || 'coding', []);
  const id   = cM(() => new URLSearchParams(location.search).get('id'),   []);
  const planId = cM(() => new URLSearchParams(location.search).get('plan'), []);

  const [challenge, setChallenge] = cS(null);
  const [loading, setLoading] = cS(true);
  const [errMsg, setErrMsg] = cS(null);

  const [code, setCode] = cS('');
  const [notes, setNotes] = cS('');
  const [scenarioText, setScenarioText] = cS('');
  const [fileTab, setFileTab] = cS('solution');
  const [problemMode, setProblemMode] = cS('problem');
  const [runState, setRunState] = cS(null);
  const [runResult, setRunResult] = cS(null);
  const [running, setRunning] = cS(false);

  const [submitting, setSubmitting] = cS(false);
  const [result, setResult] = cS(null);
  const [elapsed, setElapsed] = cS(0);
  const startRef = cR(Date.now());

  // Accent
  cE(() => {
    const p = CHAL_PALETTES[t.accent] || CHAL_PALETTES.emerald;
    document.documentElement.style.setProperty('--accent', p.accent);
    document.documentElement.style.setProperty('--accent-ink', p.accentInk);
    document.documentElement.style.setProperty('--accent-tint', p.accentTint);
  }, [t.accent]);

  // No valid user means we landed here without auth — go back to sign in.
  cE(() => {
    if (!localStorage.getItem('training_user')) {
      window.location.replace('Auth.html');
    }
  }, []);

  // Load challenge
  cE(() => {
    if (!id) {
      setErrMsg('No challenge id in URL — open one from your plan or topic page.');
      setLoading(false);
      return;
    }
    setLoading(true);
    fetchChallenge({ apiBase: t.apiBase, demoMode: t.demoMode }, kind, id)
      .then((c) => {
        setChallenge(c);
        if (kind === 'coding') setCode(c.starterCode || '');
        startRef.current = Date.now();
        setLoading(false);
      })
      .catch((err) => { setErrMsg(err.message || 'Failed to load'); setLoading(false); });
  }, [kind, id, t.apiBase, t.demoMode]);

  // Timer
  cE(() => {
    if (result || loading) return;
    const tid = setInterval(() => setElapsed(Math.floor((Date.now() - startRef.current) / 1000)), 1000);
    return () => clearInterval(tid);
  }, [result, loading]);

  // Keyboard shortcut: Cmd/Ctrl+Enter submits
  cE(() => {
    const onKey = (e) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
        e.preventDefault();
        handleSubmit();
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  });

  async function handleRunTests() {
    if (!challenge || kind !== 'coding' || running) return;
    setFileTab('tests');

    if (t.demoMode && (challenge.tests || []).length > 0) {
      // Demo mode: fake a test run with sequential reveal over the mock list.
      const tests = challenge.tests || [];
      setRunState(tests.map(() => 'pending'));
      for (let i = 0; i < tests.length; i++) {
        setRunState(prev => prev.map((s, idx) => idx === i ? 'running' : s));
        await new Promise(r => setTimeout(r, 450 + Math.random() * 300));
        const passed = tests[i].expected === 'pass' && (code.length > challenge.starterCode.length + 40);
        setRunState(prev => prev.map((s, idx) => idx === i ? (passed ? 'pass' : 'fail') : s));
      }
      return;
    }

    // Real mode: the judge runner compiles the submission with the challenge's
    // xUnit suite and reports pass counts — nothing is persisted.
    setRunning(true);
    setRunResult(null);
    try {
      const res = await runCodingTests({ apiBase: t.apiBase, demoMode: t.demoMode }, challenge.id, code);
      setRunResult(res);
    } catch (err) {
      setRunResult({ evaluated: false, compiled: false, output: err.message || 'Test run failed.' });
    } finally {
      setRunning(false);
    }
  }

  async function handleSubmit() {
    if (submitting || !challenge) return;
    setSubmitting(true);
    try {
      const body = kind === 'coding'
        ? { codingChallengeId: challenge.id, dailyStudyPlanId: planId || null, submittedCode: code, notes }
        : { scenarioChallengeId: challenge.id, dailyStudyPlanId: planId || null, responseText: scenarioText };
      const res = await submitChallenge({ apiBase: t.apiBase, demoMode: t.demoMode }, kind, body);
      setResult(res);
      // Reveal criteria tab on coding so user sees the breakdown alongside the panel
      if (kind === 'coding') setProblemMode('criteria');
    } catch (err) {
      setErrMsg(err.message || 'Submission failed');
    } finally {
      setSubmitting(false);
    }
  }

  const handleExit = () => { window.location.href = 'Dashboard.html'; };

  if (loading) {
    return (
      <>
        <ChallengeTopBar kind={kind} onExit={handleExit} elapsedSec={0} />
        <main className="container-x py-6">
          <div className="grid gap-5" style={{ gridTemplateColumns: '1fr 1.2fr' }}>
            <div className="skel" style={{ height: 'calc(100vh - 140px)' }} />
            <div className="skel" style={{ height: 'calc(100vh - 140px)' }} />
          </div>
        </main>
      </>
    );
  }

  if (errMsg && !challenge) {
    return (
      <>
        <ChallengeTopBar kind={kind} onExit={handleExit} elapsedSec={0} />
        <main className="container-x py-12">
          <div className="card p-10 text-center max-w-[480px] mx-auto">
            <h2 className="font-semibold tracking-tight" style={{ fontSize: 22, letterSpacing: '-0.022em' }}>
              Couldn't load the challenge.
            </h2>
            <p className="mt-2 text-[14.5px]" style={{ color: 'var(--ink-soft)' }}>{errMsg}</p>
            <button className="btn btn-primary mt-5" onClick={handleExit}>Back to plan</button>
          </div>
        </main>
      </>
    );
  }

  return (
    <>
      <ChallengeTopBar kind={kind} challenge={challenge} onExit={handleExit}
                       elapsedSec={elapsed} outcome={result} />

      <main className="container-x py-5 rise" style={{ paddingBottom: result ? 280 : 24 }}>
        <div className="grid gap-5"
             style={{ gridTemplateColumns: kind === 'coding'
               ? 'minmax(0, 1fr) minmax(0, 1.35fr)'
               : 'minmax(0, 1fr) minmax(0, 1.2fr)' }}>
          {kind === 'coding' ? (
            <>
              <CodingProblemPanel
                challenge={challenge}
                mode={problemMode}
                setMode={setProblemMode}
                criteriaResults={result?._criteriaResults}
              />
              <IdePanel
                challenge={challenge}
                code={code} setCode={setCode}
                notes={notes} setNotes={setNotes}
                fileTab={fileTab} setFileTab={setFileTab}
                onRun={handleRunTests} onSubmit={handleSubmit}
                submitting={submitting}
                runState={runState} runResult={runResult} running={running}
              />
            </>
          ) : (
            <>
              <ScenarioPanel
                challenge={challenge}
                criteriaResults={result?._criteriaResults}
              />
              <ScenarioEditor
                challenge={challenge}
                text={scenarioText} setText={setScenarioText}
                onSubmit={handleSubmit}
                submitting={submitting}
              />
            </>
          )}
        </div>
      </main>

      {result && (
        <EvaluationPanel
          kind={kind}
          challenge={challenge}
          result={result}
          onClose={() => setResult(null)}
          onNext={handleExit}
        />
      )}

      <TweaksPanel>
        <TweakSection label="Visual" />
        <TweakRadio label="Accent" value={t.accent}
                    options={['emerald', 'indigo', 'amber']}
                    onChange={(v) => setTweak('accent', v)} />

        <TweakSection label="API" />
        <TweakToggle label="Demo mode" value={t.demoMode}
                     onChange={(v) => setTweak('demoMode', v)} />
        <TweakText label="API base URL" value={t.apiBase}
                   onChange={(v) => setTweak('apiBase', v)}
                   placeholder="http://localhost:5000" />

        <TweakSection label="Jump to challenge" />
        <TweakButton label="c-1 · Refresh-token rotation" onClick={() => location.href = 'Challenge.html?kind=coding&id=c-1'} />
        <TweakButton label="c-2 · Idempotent pipeline" onClick={() => location.href = 'Challenge.html?kind=coding&id=c-2'} />
        <TweakButton label="s-1 · Indexing without downtime" onClick={() => location.href = 'Challenge.html?kind=scenario&id=s-1'} />
      </TweaksPanel>
    </>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<App />);
