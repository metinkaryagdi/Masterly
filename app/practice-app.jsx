// practice-app.jsx
// Single-question practice surface: load → solve → submit → evaluate → next.

const { useState: uS, useEffect: uE, useMemo: uM, useRef: uR } = React;

const PRACTICE_TWEAKS = /*EDITMODE-BEGIN*/{
  "accent": "emerald",
  "showTimer": true,
  "apiBase": "http://localhost:5000",
  "demoMode": false
}/*EDITMODE-END*/;

const PRACTICE_PALETTES = {
  emerald: { accent: 'oklch(0.62 0.14 155)', accentInk: 'oklch(0.32 0.10 155)', accentTint: 'oklch(0.95 0.04 155)' },
  indigo:  { accent: 'oklch(0.55 0.18 270)', accentInk: 'oklch(0.30 0.14 270)', accentTint: 'oklch(0.95 0.04 270)' },
  amber:   { accent: 'oklch(0.66 0.16 60)',  accentInk: 'oklch(0.36 0.12 60)',  accentTint: 'oklch(0.96 0.05 60)' },
};

/* ─────────────── Local icons (additional) ─────────────── */
const PIcon = {
  Back: (p) => (<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M13 8H3M7 4L3 8l4 4"/></svg>),
  Close: (p) => (<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" {...p}><path d="M4 4l8 8M12 4l-8 8"/></svg>),
  Send: (p) => (<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M2 8h11M9 4l4 4-4 4"/></svg>),
  Sparkle: (p) => (<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M8 2v3M8 11v3M2 8h3M11 8h3M4 4l2 2M10 10l2 2M12 4l-2 2M4 12l2-2"/></svg>),
};

/* ─────────────── URL helpers ─────────────── */
function getParam(name) {
  return new URLSearchParams(window.location.search).get(name);
}

/* ─────────────── Difficulty pips ─────────────── */
function DifficultyPips({ value }) {
  const order = { Fundamental: 1, Intermediate: 2, Advanced: 3, Expert: 4 };
  const filled = typeof value === 'string' ? (order[value] || 1) : Math.max(1, Math.min(4, value || 1));
  const label = ['', 'Temel', 'Orta', 'İleri', 'Uzman'][filled];
  return (
    <span className="inline-flex items-center gap-2">
      <span className="difpip">
        {[1,2,3,4].map(i => <i key={i} data-on={i <= filled ? 'true' : 'false'} />)}
      </span>
      <span className="font-mono text-[11px]" style={{ color: 'var(--ink-mute)' }}>
        {label}
      </span>
    </span>
  );
}

/* ─────────────── Top bar ─────────────── */
function PracticeTop({ planItems, currentIndex, onExit, elapsedSec, showTimer }) {
  return (
    <header className="topbar">
      <div className="max-w-[860px] mx-auto px-7 flex items-center gap-4" style={{ height: 60 }}>
        <button className="btn-icon" onClick={onExit} title="Antrenmandan çık" aria-label="Çık">
          <PIcon.Close />
        </button>

        <div className="stepper">
          {(planItems || []).map((it, i) => (
            <i key={it.id}
               data-state={
                 i < currentIndex || it.isCompleted ? 'done' :
                 i === currentIndex ? 'active' : 'pending'
               } />
          ))}
        </div>

        <span className="font-mono text-[11.5px] tabular-nums" style={{ color: 'var(--ink-mute)' }}>
          {planItems
            ? <>{String(currentIndex + 1).padStart(2, '0')} <span style={{ color: 'var(--ink-mute)' }}>/ {String(planItems.length).padStart(2, '0')}</span></>
            : '—'}
        </span>

        {showTimer && (
          <span className="hidden sm:inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full border bg-white"
                style={{ borderColor: 'var(--line)' }}>
            <Icon.Clock />
            <span className="font-mono text-[11.5px] tabular-nums" style={{ color: 'var(--ink-soft)' }}>
              {fmtTime(elapsedSec)}
            </span>
          </span>
        )}
      </div>
    </header>
  );
}

function fmtTime(s) {
  const m = Math.floor(s / 60);
  const sec = s % 60;
  return `${String(m).padStart(2, '0')}:${String(sec).padStart(2, '0')}`;
}

/* ─────────────── Question header ─────────────── */
function QuestionHeader({ question, item }) {
  const typeLabel = question.questionType === QuestionType.MultipleChoice ? 'Çoktan seçmeli'
                  : question.questionType === QuestionType.ShortAnswer ? 'Kısa cevap'
                  : 'Senaryo';
  const isChallenge = item && item.itemType !== StudyPlanItemType.Question;

  return (
    <div className="mb-6">
      <div className="flex flex-wrap items-center gap-2 mb-4">
        <span className="topic-chip">
          <span className="w-1.5 h-1.5 rounded-full" style={{ background: 'var(--accent)' }} />
          {TOPIC_NAMES[question.topicId] || question.topicId}
        </span>
        <span className="topic-chip">
          {isChallenge && (item.itemType === StudyPlanItemType.CodingChallenge ? 'Kod görevi' : 'Senaryo görevi')}
          {!isChallenge && typeLabel}
        </span>
        <span className="topic-chip">
          <DifficultyPips value={question.difficulty} />
        </span>
        <span className="font-mono text-[11px] ml-auto" style={{ color: 'var(--ink-mute)' }}>
          ~{Math.round((question.estimatedSolvingTimeSeconds || 300) / 60)} dk · geçme ≥ {question.minimumPassingScore}
        </span>
      </div>

      <h1 className="font-semibold tracking-tight"
          style={{ fontSize: 24, letterSpacing: '-0.022em', color: 'var(--ink)', lineHeight: 1.3, textWrap: 'pretty' }}>
        {question.prompt}
      </h1>

      {question.tags && question.tags.length > 0 && (
        <div className="mt-4 flex flex-wrap gap-1.5">
          {question.tags.map(tag => (
            <span key={tag} className="font-mono text-[10.5px] px-2 py-0.5 rounded-md"
                  style={{ background: 'oklch(0.965 0.006 88)', color: 'var(--ink-mute)' }}>
              #{tag}
            </span>
          ))}
        </div>
      )}
    </div>
  );
}

/* ─────────────── Multiple-choice ─────────────── */
function MultipleChoice({ question, selected, onSelect, locked, evaluation }) {
  // After submit, the server reveals the correct option id on the evaluation
  // result — the question payload no longer carries the answer key. Fall back to
  // any inline flag so offline demo data keeps working.
  const correctId = evaluation?.correctOptionId ?? (question.options.find(o => o.isCorrect) || {}).id;
  return (
    <div className="flex flex-col gap-2.5">
      {question.options.map((opt, idx) => {
        const letter = String.fromCharCode(65 + idx); // A, B, C, D
        let state = null;
        if (evaluation) {
          if (opt.id === correctId) state = 'correct';
          else if (opt.id === selected) state = 'incorrect';
          if (!selected && opt.id === correctId) state = 'missed';
        }
        return (
          <button
            type="button"
            key={opt.id}
            className="opt"
            data-selected={selected === opt.id}
            data-state={state}
            disabled={locked}
            onClick={() => !locked && onSelect(opt.id)}
          >
            <span className="opt-letter">{letter}</span>
            <span className="text-[14.5px] leading-snug" style={{ letterSpacing: '-0.005em', textWrap: 'pretty' }}>
              {opt.text}
            </span>
            <span className="flex justify-end">
              {state === 'correct' && (
                <span className="w-6 h-6 rounded-full inline-flex items-center justify-center burst"
                      style={{ background: 'var(--accent)', color: '#fff' }}>
                  <Icon.Check />
                </span>
              )}
              {state === 'incorrect' && (
                <span className="w-6 h-6 rounded-full inline-flex items-center justify-center"
                      style={{ background: 'var(--danger)', color: '#fff' }}>
                  <PIcon.Close />
                </span>
              )}
            </span>
          </button>
        );
      })}
    </div>
  );
}

/* ─────────────── Short answer / Scenario ─────────────── */
function FreeText({ question, value, onChange, locked, isLong }) {
  const ref = uR(null);
  uE(() => { if (!locked && ref.current) ref.current.focus(); }, [locked]);
  const minLen = isLong ? 60 : 3;
  const length = value.trim().length;
  return (
    <div>
      <textarea
        ref={ref}
        className="tp-text"
        rows={isLong ? 8 : 2}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        disabled={locked}
        placeholder={
          isLong
            ? 'Yaklaşımını adım adım anlat — veri modeli, uçlar, sınır durumlar. Yanıt süresi ölçülüyor.'
            : 'Cevabını buraya yaz.'
        }
      />
      <div className="mt-2 flex items-center justify-between text-[11.5px] font-mono"
           style={{ color: 'var(--ink-mute)' }}>
        <span>{length} karakter{length < minLen ? <> · <span style={{ color: 'var(--warn)' }}>en az {minLen} gerekli</span></> : null}</span>
        <span>Göndermek için <kbd>Ctrl</kbd> + <kbd>Enter</kbd></span>
      </div>
    </div>
  );
}

/* ─────────────── Evaluation result ─────────────── */
function EvaluationCard({ result, question, masteryBefore, onNext, onBack, hasNext, backLabel, nextLabel, doneLabel }) {
  const correct = result.wasCorrect;
  return (
    <section className="mt-7 slide-up flex flex-col gap-4">
      {/* Banner */}
      <div className={`banner ${correct ? 'banner-correct' : 'banner-incorrect'}`}>
        <span className="w-9 h-9 rounded-full flex-shrink-0 inline-flex items-center justify-center burst"
              style={{ background: correct ? 'var(--accent)' : 'var(--danger)', color: '#fff' }}>
          {correct ? <Icon.Check /> : <PIcon.Close />}
        </span>
        <div className="min-w-0 flex-1">
          <div className="flex items-baseline justify-between gap-3">
            <div className="font-semibold tracking-tight"
                 style={{ fontSize: 18, letterSpacing: '-0.02em' }}>
              {correct ? 'Doğru.' : 'Tam değil.'}
            </div>
            <div className="font-mono tabular-nums" style={{ fontSize: 13 }}>
              puan <b style={{ fontWeight: 600 }}>{result.score}</b> / 100
            </div>
          </div>
          <p className="mt-1 text-[13.5px]" style={{ lineHeight: 1.55, textWrap: 'pretty' }}>
            {result.evaluationSummary}
          </p>
        </div>
      </div>

      {/* Mastery delta */}
      <div className="card p-5">
        <div className="eyebrow flex items-center gap-2"><PIcon.Sparkle /> Ustalık güncellemesi</div>
        <div className="mt-3 flex items-center gap-5">
          <div className="flex-1">
            <div className="flex items-baseline justify-between mb-1.5">
              <span className="text-[13px]" style={{ color: 'var(--ink-soft)' }}>
                {TOPIC_NAMES[question.topicId]}
              </span>
              <span className="font-mono text-[12.5px] tabular-nums" style={{ color: 'var(--ink-soft)' }}>
                {masteryBefore} → <b style={{ color: 'var(--ink)', fontWeight: 600 }}>{result.masteryScore}</b>
                <span style={{
                  marginLeft: 6,
                  color: result.masteryScore > masteryBefore ? 'var(--accent-ink)' :
                         result.masteryScore < masteryBefore ? 'oklch(0.46 0.16 25)' : 'var(--ink-mute)',
                  fontWeight: 500,
                }}>
                  {result.masteryScore === masteryBefore ? '±0' : (result.masteryScore > masteryBefore ? '+' : '') + (result.masteryScore - masteryBefore)}
                </span>
              </span>
            </div>
            <div className="mbar relative">
              {/* "before" ghost */}
              <div className="ghost"><i style={{ width: `${masteryBefore}%` }} /></div>
              {/* "after" */}
              <i style={{
                width: `${result.masteryScore}%`,
                background: result.masteryScore >= masteryBefore ? 'var(--accent)' : 'oklch(0.66 0.13 25)',
              }} />
            </div>
          </div>
        </div>

        <div className="mt-4 grid grid-cols-2 gap-3">
          <div className="rounded-xl border p-3" style={{ borderColor: 'var(--line)' }}>
            <div className="eyebrow" style={{ fontSize: 10 }}>Sonraki tekrar</div>
            <div className="mt-1.5 text-[14px] font-medium" style={{ letterSpacing: '-0.01em' }}>
              {fmtNextReview(result.nextReviewAtUtc)}
            </div>
            <div className="font-mono text-[11px] mt-0.5" style={{ color: 'var(--ink-mute)' }}>
              {new Date(result.nextReviewAtUtc).toLocaleDateString('tr-TR', { weekday: 'short', month: 'short', day: 'numeric' })}
            </div>
          </div>
          <div className="rounded-xl border p-3" style={{ borderColor: 'var(--line)' }}>
            <div className="eyebrow" style={{ fontSize: 10 }}>Unutma riski</div>
            <div className="mt-1.5 flex items-center gap-2">
              <span className="font-semibold tabular-nums" style={{ fontSize: 16 }}>
                {Math.round(result.forgettingRisk * 100)}%
              </span>
              <span className="font-mono text-[10.5px] px-1.5 py-0.5 rounded-md"
                    style={{
                      background: result.forgettingRisk > 0.5 ? 'oklch(0.96 0.05 25)' : result.forgettingRisk > 0.25 ? 'oklch(0.96 0.05 70)' : 'oklch(0.95 0.04 155)',
                      color:      result.forgettingRisk > 0.5 ? 'oklch(0.46 0.16 25)' : result.forgettingRisk > 0.25 ? 'oklch(0.46 0.14 70)' : 'oklch(0.38 0.10 155)',
                    }}>
                {result.forgettingRisk > 0.5 ? 'yüksek' : result.forgettingRisk > 0.25 ? 'artıyor' : 'stabil'}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Explanation */}
      <div className="card p-5">
        <div className="eyebrow flex items-center gap-2">// açıklama</div>
        <p className="mt-3 text-[14px]"
           style={{ color: 'var(--ink-soft)', lineHeight: 1.7, textWrap: 'pretty' }}>
          {result.explanation}
        </p>
      </div>

      {/* CTAs */}
      <div className="flex flex-wrap items-center justify-between gap-3 pt-1">
        <button className="btn btn-ghost btn-lg" onClick={onBack}>
          <PIcon.Back /> {backLabel || 'Bugünün planına dön'}
        </button>
        <button className="btn btn-primary btn-lg" onClick={onNext}>
          {hasNext ? <>{nextLabel || 'Sonraki soru'} <Icon.Arrow /></> : <>{doneLabel || 'Plan tamamlandı'} <Icon.Check /></>}
        </button>
      </div>
    </section>
  );
}

function fmtNextReview(iso) {
  const diff = new Date(iso).getTime() - Date.now();
  const days = Math.max(0, Math.round(diff / (24 * 3600 * 1000)));
  if (days === 0) return 'Bugün içinde';
  if (days === 1) return 'Yarın';
  if (days < 7) return `${days} gün sonra`;
  if (days < 14) return `1 hafta sonra`;
  return `${Math.round(days / 7)} hafta sonra`;
}

/* ─────────────── Empty / Error states ─────────────── */
function EmptyState({ title, body, action }) {
  return (
    <div className="card p-10 text-center mt-8">
      <h2 className="font-semibold tracking-tight" style={{ fontSize: 22, letterSpacing: '-0.022em' }}>
        {title}
      </h2>
      <p className="mt-2 text-[14.5px] max-w-[440px] mx-auto" style={{ color: 'var(--ink-soft)', lineHeight: 1.55 }}>
        {body}
      </p>
      {action && <div className="mt-5 flex justify-center">{action}</div>}
    </div>
  );
}

/* ─────────────── Mod seçimi (Test / Yazılı / Lab) ─────────────── */
const PRACTICE_MODES = [
  { id: 'plan',   title: 'Günün Planı', hint: 'karma',           desc: 'Bugünün planından kaldığın yerden devam et — soru ve görev karışık.' },
  { id: 'test',   title: 'Test',        hint: 'çoktan seçmeli',  desc: 'Havuzdan çoktan seçmeli sorularla hızlı bir tur at.' },
  { id: 'yazili', title: 'Yazılı',      hint: 'açık uçlu',       desc: 'Kısa cevap ve senaryo soruları — kendi cümlelerinle anlat.' },
  { id: 'lab',    title: 'Lab',         hint: 'kod editörü',     desc: 'Gerçek bir kod görevi: çöz, testleri koştur, puanını al.' },
];

function ModePicker({ onPick, busyMode }) {
  return (
    <main className="container-x py-10">
      <div className="eyebrow">// antrenman</div>
      <h1 className="mt-2 font-semibold tracking-tight"
          style={{ fontSize: 28, letterSpacing: '-0.025em', color: 'var(--ink)' }}>
        Bugün nasıl çalışmak istersin?
      </h1>
      <p className="mt-1.5 text-[14.5px]" style={{ color: 'var(--ink-soft)' }}>
        Test, yazılı ya da lab — hepsi ustalık puanını ve tekrar takvimini besler.
      </p>
      <div className="mt-6 grid gap-3" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))' }}>
        {PRACTICE_MODES.map((mode) => (
          <button key={mode.id} type="button" className="card p-5 text-left"
                  style={{ cursor: 'pointer', opacity: busyMode && busyMode !== mode.id ? 0.6 : 1 }}
                  disabled={!!busyMode}
                  onClick={() => onPick(mode.id)}>
            <div className="flex items-center justify-between">
              <span className="font-semibold tracking-tight" style={{ fontSize: 18, letterSpacing: '-0.02em', color: 'var(--ink)' }}>
                {mode.title}
              </span>
              <span className="font-mono text-[10.5px] px-2 py-0.5 rounded-md"
                    style={{ background: 'var(--accent-tint)', color: 'var(--accent-ink)' }}>
                {busyMode === mode.id ? 'hazırlanıyor…' : mode.hint}
              </span>
            </div>
            <p className="mt-2 text-[13.5px]" style={{ color: 'var(--ink-soft)', lineHeight: 1.55 }}>
              {mode.desc}
            </p>
          </button>
        ))}
      </div>
    </main>
  );
}

/* ─────────────── Main App ─────────────── */
function App() {
  const [t, setTweak] = useTweaks(PRACTICE_TWEAKS);

  // 1. Resolve current item from URL
  const itemIdParam = uM(() => getParam('item'), []);
  const questionIdParam = uM(() => getParam('question'), []);
  const modeParam = uM(() => getParam('mode'), []);

  const [plan, setPlan] = uS(null);
  const [item, setItem] = uS(null);
  const [question, setQuestion] = uS(null);
  const [dash, setDash] = uS(null);
  const [loading, setLoading] = uS(true);
  const [errMsg, setErrMsg] = uS(null);

  // Mode picker + typed sessions (Test / Yazılı). Lab redirects to Challenge.html.
  const [showPicker, setShowPicker] = uS(false);
  const [pickerBusy, setPickerBusy] = uS(null);
  const [session, setSession] = uS(null); // { questions: [...], index }

  const [selectedOptionId, setSelected] = uS(null);
  const [textAnswer, setTextAnswer] = uS('');

  const [submitting, setSubmitting] = uS(false);
  const [evaluation, setEvaluation] = uS(null);
  const [masteryBefore, setMasteryBefore] = uS(0);

  const [elapsed, setElapsed] = uS(0);
  const startRef = uR(Date.now());

  // Apply accent
  uE(() => {
    const p = PRACTICE_PALETTES[t.accent] || PRACTICE_PALETTES.emerald;
    document.documentElement.style.setProperty('--accent', p.accent);
    document.documentElement.style.setProperty('--accent-ink', p.accentInk);
    document.documentElement.style.setProperty('--accent-tint', p.accentTint);
  }, [t.accent]);

  // No valid user means we landed here without auth — go back to sign in.
  uE(() => {
    if (!localStorage.getItem('training_user')) {
      window.location.replace('Auth.html');
    }
  }, []);

  const resetSolvingState = () => {
    setSelected(null);
    setTextAnswer('');
    setEvaluation(null);
    startRef.current = Date.now();
    setElapsed(0);
  };

  // Load: no URL params → mode picker; mode=test|yazili → typed pool session;
  // otherwise the classic plan/item flow.
  uE(() => {
    let cancelled = false;
    setLoading(true);
    setErrMsg(null);

    (async () => {
      try {
        // Mode picker: opened plain from the nav.
        if (!itemIdParam && !questionIdParam && !modeParam) {
          setShowPicker(true);
          setLoading(false);
          return;
        }

        // Typed session: draw from the whole pool, filtered by question type.
        if (modeParam === 'test' || modeParam === 'yazili') {
          const [allQuestions, dashRes] = await Promise.all([
            fetchAllQuestions({ apiBase: t.apiBase, demoMode: t.demoMode }),
            fetchDashboard({ apiBase: t.apiBase, demoMode: t.demoMode }),
          ]);
          if (cancelled) return;
          setDash(dashRes);

          const wanted = modeParam === 'test'
            ? [QuestionType.MultipleChoice]
            : [QuestionType.ShortAnswer, QuestionType.Scenario];
          const filtered = (allQuestions || []).filter(q => wanted.includes(q.questionType));
          if (filtered.length === 0) throw new Error('Bu mod için havuzda soru bulunamadı.');

          const shuffled = [...filtered].sort(() => Math.random() - 0.5).slice(0, 10);
          setSession({ questions: shuffled, index: 0 });

          const first = shuffled[0];
          setQuestion(first);
          const m = dashRes.topicMastery.find(x => x.topicId === first.topicId);
          setMasteryBefore(m ? m.masteryScore : 50);
          resetSolvingState();
          setLoading(false);
          return;
        }

        // Plan / single-question flow.
        const [planRes, dashRes] = await Promise.all([
          fetchTodayPlan({ apiBase: t.apiBase, demoMode: t.demoMode }),
          fetchDashboard({ apiBase: t.apiBase, demoMode: t.demoMode }),
        ]);
        if (cancelled) return;
        setPlan(planRes);
        setDash(dashRes);

        // Resolve item — planRes is null when no plan exists for today yet.
        const planItems = planRes?.items || [];
        let resolvedItem = null;
        if (itemIdParam) {
          resolvedItem = planItems.find(i => i.id === itemIdParam);
        }
        if (!resolvedItem && !questionIdParam) {
          resolvedItem = planItems.find(i => !i.isCompleted) || planItems[0];
        }
        setItem(resolvedItem);

        // Resolve question
        const qid = questionIdParam || resolvedItem?.referenceId;
        if (!qid) throw new Error('Bugün için plan yok — önce panelden bir plan oluştur.');
        const q = await fetchQuestion({ apiBase: t.apiBase, demoMode: t.demoMode }, qid);
        if (cancelled) return;
        setQuestion(q);

        // Snapshot mastery
        const m = dashRes.topicMastery.find(x => x.topicId === q.topicId);
        setMasteryBefore(m ? m.masteryScore : 50);

        resetSolvingState();
        setLoading(false);
      } catch (err) {
        if (cancelled) return;
        setErrMsg(err.message || 'Yükleme başarısız oldu.');
        setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [itemIdParam, questionIdParam, modeParam, t.apiBase, t.demoMode]);

  // Mode picker actions. Lab needs a coding challenge id; the plan card
  // resumes today's plan when one exists.
  async function handlePickMode(modeId) {
    if (modeId === 'test' || modeId === 'yazili') {
      window.location.href = `Practice.html?mode=${modeId}`;
      return;
    }

    setPickerBusy(modeId);
    try {
      if (modeId === 'plan') {
        const planRes = await fetchTodayPlan({ apiBase: t.apiBase, demoMode: t.demoMode });
        const next = planRes?.items?.find(i => !i.isCompleted) || planRes?.items?.[0];
        window.location.href = next ? `Practice.html?item=${encodeURIComponent(next.id)}` : 'Dashboard.html';
        return;
      }

      // Lab: today's plan challenge first, otherwise a sample from the catalogue.
      const planRes = await fetchTodayPlan({ apiBase: t.apiBase, demoMode: t.demoMode }).catch(() => null);
      const planLab = planRes?.items?.find(i => i.itemType === StudyPlanItemType.CodingChallenge && !i.isCompleted);
      if (planLab) {
        window.location.href = `Challenge.html?kind=coding&id=${encodeURIComponent(planLab.referenceId)}&plan=${encodeURIComponent(planRes.id)}`;
        return;
      }
      const topics = await fetchTopics({ apiBase: t.apiBase, demoMode: t.demoMode });
      const samples = (topics || []).flatMap(topic => topic.sampleQuestions || []);
      const lab = samples.filter(s => s.type === 'CodingChallenge');
      if (lab.length === 0) throw new Error('Kod görevi bulunamadı.');
      const picked = lab[Math.floor(Math.random() * lab.length)];
      window.location.href = `Challenge.html?kind=coding&id=${encodeURIComponent(picked.id)}`;
    } catch (err) {
      setPickerBusy(null);
      setErrMsg(err.message || 'Mod açılamadı.');
      setShowPicker(false);
    }
  }

  // Timer tick
  uE(() => {
    if (evaluation || loading) return;
    const id = setInterval(() => setElapsed(Math.floor((Date.now() - startRef.current) / 1000)), 1000);
    return () => clearInterval(id);
  }, [evaluation, loading]);

  // Cmd/Ctrl+Enter to submit
  uE(() => {
    function onKey(e) {
      if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
        e.preventDefault();
        if (canSubmit() && !submitting && !evaluation) handleSubmit();
      }
      if (e.key === 'Escape' && evaluation) {
        handleNext();
      }
    }
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }); // re-bind each render is fine; captures latest closures

  function canSubmit() {
    if (!question) return false;
    if (question.questionType === QuestionType.MultipleChoice) return !!selectedOptionId;
    const minLen = question.questionType === QuestionType.Scenario ? 60 : 3;
    return textAnswer.trim().length >= minLen;
  }

  async function handleSubmit() {
    if (!canSubmit()) return;
    setSubmitting(true);
    try {
      const res = await submitAnswer(
        { apiBase: t.apiBase, demoMode: t.demoMode },
        {
          questionId: question.id,
          selectedOptionId: question.questionType === QuestionType.MultipleChoice ? selectedOptionId : null,
          submittedAnswer: question.questionType === QuestionType.MultipleChoice ? null : textAnswer,
          responseTimeSeconds: Math.max(1, elapsed),
          dailyStudyPlanId: plan?.id || null,
        }
      );
      // Use the _masteryBefore from the demo helper if present
      if (typeof res._masteryBefore === 'number') setMasteryBefore(res._masteryBefore);

      // Optimistically mark the plan item complete locally
      if (item) {
        setPlan(prev => prev && {
          ...prev,
          items: prev.items.map(i => i.id === item.id ? { ...i, isCompleted: true } : i),
        });
      }
      setEvaluation(res);
    } catch (err) {
      setErrMsg(err.message || 'Submission failed. Try again.');
    } finally {
      setSubmitting(false);
    }
  }

  function findNextItem() {
    if (!plan || !item) return null;
    const idx = plan.items.findIndex(i => i.id === item.id);
    const next = plan.items.slice(idx + 1).find(i => !i.isCompleted)
              || plan.items.find(i => !i.isCompleted && i.id !== item.id);
    return next;
  }

  function handleNext() {
    if (session) {
      const nextIdx = session.index + 1;
      if (nextIdx < session.questions.length) {
        const nextQ = session.questions[nextIdx];
        setSession({ ...session, index: nextIdx });
        setQuestion(nextQ);
        const m = dash?.topicMastery?.find(x => x.topicId === nextQ.topicId);
        setMasteryBefore(m ? m.masteryScore : 50);
        resetSolvingState();
        window.scrollTo({ top: 0 });
      } else {
        window.location.href = 'Practice.html';
      }
      return;
    }

    const nxt = findNextItem();
    if (nxt) window.location.href = `Practice.html?item=${encodeURIComponent(nxt.id)}`;
    else window.location.href = 'Dashboard.html';
  }

  function handleExit() {
    window.location.href = 'Dashboard.html';
  }

  const currentIndex = uM(() => {
    if (session) return session.index;
    if (!plan || !item) return 0;
    return plan.items.findIndex(i => i.id === item.id);
  }, [plan, item, session]);

  const stepperItems = session
    ? session.questions.map((q, i) => ({ id: q.id, isCompleted: i < session.index }))
    : plan?.items;

  const hasNext = session
    ? session.index + 1 < session.questions.length
    : !!findNextItem();
  const isScenario = question && question.questionType !== QuestionType.MultipleChoice;
  const isLongAnswer = question && question.questionType === QuestionType.Scenario;

  return (
    <>
      <PracticeTop
        planItems={stepperItems}
        currentIndex={currentIndex}
        onExit={handleExit}
        elapsedSec={elapsed}
        showTimer={t.showTimer}
      />

      {!loading && showPicker && <ModePicker onPick={handlePickMode} busyMode={pickerBusy} />}

      {!showPicker && <main className="container-x py-10">
        {loading && (
          <div className="flex flex-col gap-3 mt-4">
            <div className="skel h-[18px] w-[120px]" />
            <div className="skel h-[32px] w-[80%]" />
            <div className="skel h-[32px] w-[60%]" />
            <div className="skel h-[60px] mt-4" />
            <div className="skel h-[60px]" />
            <div className="skel h-[60px]" />
            <div className="skel h-[60px]" />
          </div>
        )}

        {!loading && errMsg && (
          <EmptyState
            title="Soru yüklenemedi."
            body={errMsg}
            action={<button className="btn btn-primary" onClick={handleExit}>Panele dön</button>}
          />
        )}

        {!loading && !errMsg && question && (
          <div className="rise">
            <QuestionHeader question={question} item={item} />

            {question.questionType === QuestionType.MultipleChoice ? (
              <MultipleChoice
                question={question}
                selected={selectedOptionId}
                onSelect={setSelected}
                locked={!!evaluation || submitting}
                evaluation={evaluation}
              />
            ) : (
              <FreeText
                question={question}
                value={textAnswer}
                onChange={setTextAnswer}
                locked={!!evaluation || submitting}
                isLong={isLongAnswer}
              />
            )}

            {!evaluation && (
              <div className="mt-6 flex items-center justify-between gap-3">
                <span className="text-[12.5px]" style={{ color: 'var(--ink-mute)' }}>
                  Acele etme — hız da hesaba katılır ama doğruluk daha ağır basar.
                </span>
                <button
                  className="btn btn-primary btn-lg"
                  onClick={handleSubmit}
                  disabled={!canSubmit() || submitting}
                >
                  {submitting
                    ? <><Icon.Spinner /> Gönderiliyor…</>
                    : <>Cevabı gönder <PIcon.Send /></>
                  }
                </button>
              </div>
            )}

            {evaluation && (
              <EvaluationCard
                result={evaluation}
                question={question}
                masteryBefore={masteryBefore}
                onNext={handleNext}
                onBack={session ? () => { window.location.href = 'Practice.html'; } : handleExit}
                hasNext={hasNext}
                backLabel={session ? 'Mod seçimine dön' : undefined}
                doneLabel={session ? 'Tur tamamlandı' : undefined}
              />
            )}
          </div>
        )}

        {/* Empty plan fallback */}
        {!loading && !errMsg && !question && (
          <EmptyState
            title="Çalışılacak plan yok."
            body="Panele dönüp bugünün planını oluştur — en zayıf konularına göre ayarlayacağız."
            action={<button className="btn btn-primary" onClick={handleExit}>Paneli aç</button>}
          />
        )}
      </main>}

      <TweaksPanel>
        <TweakSection label="Visual" />
        <TweakRadio label="Accent" value={t.accent}
                    options={['emerald', 'indigo', 'amber']}
                    onChange={(v) => setTweak('accent', v)} />
        <TweakToggle label="Show timer" value={t.showTimer}
                     onChange={(v) => setTweak('showTimer', v)} />

        <TweakSection label="API" />
        <TweakToggle label="Demo mode" value={t.demoMode}
                     onChange={(v) => setTweak('demoMode', v)} />
        <TweakText label="API base URL" value={t.apiBase}
                   onChange={(v) => setTweak('apiBase', v)}
                   placeholder="http://localhost:5000" />

        <TweakSection label="Jump to item" />
        {plan?.items?.map((it, idx) => (
          <button key={it.id}
                  className="text-left px-2 py-1.5 rounded-md hover:bg-black/5 text-[11px]"
                  style={{
                    color: it.id === item?.id ? 'var(--ink)' : 'var(--ink-soft)',
                    fontWeight: it.id === item?.id ? 600 : 400,
                    fontFamily: 'Geist Mono, monospace',
                  }}
                  onClick={() => window.location.href = `Practice.html?item=${it.id}`}>
            <span style={{ color: 'var(--ink-mute)' }}>{String(idx + 1).padStart(2, '0')}</span>{' '}
            {it.isCompleted ? '✓' : '·'} {it.title.slice(0, 36)}{it.title.length > 36 ? '…' : ''}
          </button>
        ))}
      </TweaksPanel>
    </>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<App />);
