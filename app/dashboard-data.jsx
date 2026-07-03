// dashboard-data.jsx
// Mock data shaped exactly like the backend DTOs, plus a thin API client
// that falls back to mocks in demo mode.

// --- Enums (mirroring backend; serialized as strings via JsonStringEnumConverter) ---
const StudyPlanItemType = { Question: 'Question', CodingChallenge: 'CodingChallenge', ScenarioChallenge: 'ScenarioChallenge' };
const DailyStudyPlanStatus = { Draft: 'Draft', Active: 'Active', Completed: 'Completed' };
const TopicDifficulty = { Fundamental: 'Fundamental', Intermediate: 'Intermediate', Advanced: 'Advanced', Expert: 'Expert' };
const QuestionType = { MultipleChoice: 'MultipleChoice', ShortAnswer: 'ShortAnswer', Scenario: 'Scenario' };

const TOPIC_NAMES = {
  't-cs':   'C# Language',
  't-asp':  'ASP.NET Core',
  't-ef':   'EF Core 8',
  't-clean':'Clean Architecture',
  't-cqrs': 'CQRS + Mediator',
  't-jwt':  'JWT & Identity',
  't-cache':'Caching (Redis)',
  't-pg':   'PostgreSQL',
};

// 14-day trend (oldest → newest)
function makeTrend() {
  const today = new Date();
  today.setUTCHours(0,0,0,0);
  const points = [];
  // Slight upward trend with noise
  for (let i = 13; i >= 0; i--) {
    const d = new Date(today);
    d.setUTCDate(d.getUTCDate() - i);
    const base = 0.58 + (13 - i) * 0.022;
    const noise = (Math.sin(i * 1.7) * 0.06) + (Math.cos(i * 0.9) * 0.04);
    const acc = Math.max(0.32, Math.min(0.96, base + noise));
    const attempts = Math.max(4, Math.round(10 + Math.sin(i * 1.3) * 5 + (13 - i) * 0.35));
    points.push({ dayUtc: d.toISOString(), accuracy: acc, attempts });
  }
  return points;
}

const MOCK_DASHBOARD = {
  topicMastery: [
    { topicId: 't-clean', topicName: TOPIC_NAMES['t-clean'], masteryScore: 88, forgettingRisk: 0.12, accuracy: 0.91 },
    { topicId: 't-cqrs',  topicName: TOPIC_NAMES['t-cqrs'],  masteryScore: 81, forgettingRisk: 0.21, accuracy: 0.86 },
    { topicId: 't-cs',    topicName: TOPIC_NAMES['t-cs'],    masteryScore: 79, forgettingRisk: 0.18, accuracy: 0.84 },
    { topicId: 't-ef',    topicName: TOPIC_NAMES['t-ef'],    masteryScore: 74, forgettingRisk: 0.34, accuracy: 0.78 },
    { topicId: 't-asp',   topicName: TOPIC_NAMES['t-asp'],   masteryScore: 71, forgettingRisk: 0.22, accuracy: 0.79 },
    { topicId: 't-jwt',   topicName: TOPIC_NAMES['t-jwt'],   masteryScore: 64, forgettingRisk: 0.41, accuracy: 0.71 },
    { topicId: 't-pg',    topicName: TOPIC_NAMES['t-pg'],    masteryScore: 58, forgettingRisk: 0.36, accuracy: 0.68 },
    { topicId: 't-cache', topicName: TOPIC_NAMES['t-cache'], masteryScore: 47, forgettingRisk: 0.62, accuracy: 0.58 },
  ],
  get weakAreas() {
    return [...this.topicMastery]
      .sort((a, b) => a.masteryScore - b.masteryScore || b.forgettingRisk - a.forgettingRisk)
      .slice(0, 5);
  },
  learningTrend: makeTrend(),
  averageResponseTimeSeconds: 42.6,
  consistencyDays: 12,
  challengeSuccessRate: 0.74,
};

const MOCK_PLAN = {
  id: 'plan-1',
  userId: 'user-1',
  studyDateUtc: new Date().toISOString(),
  generatedAtUtc: new Date(Date.now() - 1000 * 60 * 12).toISOString(),
  status: DailyStudyPlanStatus.Active,
  items: [
    {
      id: 'i-1', itemType: StudyPlanItemType.Question, referenceId: 'q-1',
      topicId: 't-cache', sourceCategory: 'WeakArea', sequence: 1, priority: 0.92,
      title: 'Cache-aside vs. write-through — pick the right strategy',
      isCompleted: true,
      meta: { difficulty: TopicDifficulty.Intermediate, estimatedMinutes: 4 },
    },
    {
      id: 'i-2', itemType: StudyPlanItemType.Question, referenceId: 'q-2',
      topicId: 't-cache', sourceCategory: 'WeakArea', sequence: 2, priority: 0.88,
      title: 'Stale cache after concurrent writes — what failed?',
      isCompleted: true,
      meta: { difficulty: TopicDifficulty.Advanced, estimatedMinutes: 6 },
    },
    {
      id: 'i-3', itemType: StudyPlanItemType.CodingChallenge, referenceId: 'c-1',
      topicId: 't-jwt', sourceCategory: 'Revision', sequence: 3, priority: 0.81,
      title: 'Implement refresh-token rotation with rolling invalidation',
      isCompleted: true,
      meta: { difficulty: TopicDifficulty.Advanced, estimatedMinutes: 18 },
    },
    {
      id: 'i-4', itemType: StudyPlanItemType.Question, referenceId: 'q-3',
      topicId: 't-jwt', sourceCategory: 'Revision', sequence: 4, priority: 0.74,
      title: 'Where should JWT signing keys live in a multi-instance API?',
      isCompleted: false,
      meta: { difficulty: TopicDifficulty.Intermediate, estimatedMinutes: 5 },
    },
    {
      id: 'i-5', itemType: StudyPlanItemType.ScenarioChallenge, referenceId: 's-1',
      topicId: 't-pg', sourceCategory: 'NewMaterial', sequence: 5, priority: 0.69,
      title: 'Design indexes for a 50M-row reporting table without locking writes',
      isCompleted: false,
      meta: { difficulty: TopicDifficulty.Advanced, estimatedMinutes: 12 },
    },
    {
      id: 'i-6', itemType: StudyPlanItemType.Question, referenceId: 'q-4',
      topicId: 't-ef', sourceCategory: 'NewMaterial', sequence: 6, priority: 0.62,
      title: 'EF Core: split queries vs. single query — when do you switch?',
      isCompleted: false,
      meta: { difficulty: TopicDifficulty.Intermediate, estimatedMinutes: 5 },
    },
    {
      id: 'i-7', itemType: StudyPlanItemType.CodingChallenge, referenceId: 'c-2',
      topicId: 't-cqrs', sourceCategory: 'Stretch', sequence: 7, priority: 0.55,
      title: 'Add an idempotent command pipeline behaviour (MediatR)',
      isCompleted: false,
      meta: { difficulty: TopicDifficulty.Expert, estimatedMinutes: 22 },
    },
  ],
};

// --- API client ---
function getAuthToken() {
  return localStorage.getItem('training_token');
}

function getCurrentUser() {
  try {
    return JSON.parse(localStorage.getItem('training_user') || 'null');
  } catch { return null; }
}

async function fetchJson(url, init = {}) {
  const res = await fetch(url, {
    ...init,
    headers: {
      'Accept': 'application/json',
      'Content-Type': 'application/json',
      ...(getAuthToken() ? { 'Authorization': `Bearer ${getAuthToken()}` } : {}),
      ...(init.headers || {}),
    },
  });
  if (!res.ok) {
    let msg = res.statusText;
    try { const d = await res.json(); msg = d.detail || d.message || d.title || msg; } catch {}
    const err = new Error(msg); err.status = res.status;

    // Global auth handler: an expired/invalid token kicks the user back to sign in,
    // unless we're already on the auth page (otherwise the redirect would loop).
    if (res.status === 401 && getAuthToken() && !/Auth\.html(?:$|[?#])/i.test(window.location.pathname)) {
      localStorage.removeItem('training_token');
      localStorage.removeItem('training_user');
      window.location.href = 'Auth.html';
    }

    throw err;
  }
  return res.json();
}

// The learner's local calendar date. Sent with plan requests so "today"
// follows the user's day instead of flipping at UTC midnight.
function localStudyDate() {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

async function fetchTodayPlan({ apiBase, demoMode }) {
  if (demoMode) {
    await new Promise(r => setTimeout(r, 350));
    return MOCK_PLAN;
  }
  try {
    return await fetchJson(`${apiBase.replace(/\/$/, '')}/api/study-plans/today?studyDateUtc=${localStudyDate()}`);
  } catch (err) {
    // 404 just means no plan exists for today yet — a normal state, not an
    // API outage. Callers render the "generate one" empty state for null.
    if (err.status === 404) return null;
    throw err;
  }
}

async function fetchDashboard({ apiBase, demoMode }) {
  if (demoMode) {
    await new Promise(r => setTimeout(r, 450));
    return MOCK_DASHBOARD;
  }
  return fetchJson(`${apiBase.replace(/\/$/, '')}/api/analytics/dashboard`);
}

async function generatePlan({ apiBase, demoMode }) {
  if (demoMode) {
    await new Promise(r => setTimeout(r, 900));
    return MOCK_PLAN;
  }
  return fetchJson(`${apiBase.replace(/\/$/, '')}/api/study-plans/generate`, {
    method: 'POST',
    body: JSON.stringify({ studyDateUtc: localStudyDate() }),
  });
}

/* ─────────────── Mock questions keyed by referenceId ─────────────── */
const MOCK_QUESTIONS = {
  'q-1': {
    id: 'q-1', topicId: 't-cache',
    questionType: QuestionType.MultipleChoice,
    prompt: "You're caching read-heavy product data with sporadic writes. Cache-aside or write-through — which is the better default and why?",
    explanation: "Cache-aside (lazy loading) is the default for most read-heavy workloads: the cache stays uncluttered with rarely-read data, application code stays explicit about when to read/write, and a cache outage only degrades performance (not correctness). Write-through couples writes to the cache layer, which adds latency to writes and risks cache writes failing while the DB succeeds — only worth it when reads must be consistent with writes at sub-millisecond latency.",
    difficulty: TopicDifficulty.Intermediate,
    estimatedSolvingTimeSeconds: 240,
    minimumPassingScore: 60,
    tags: ['caching', 'redis', 'patterns'],
    acceptedAnswers: [],
    options: [
      { id: 'o-1a', text: 'Write-through — keeps cache always consistent with DB so reads are always fast.',     isCorrect: false, order: 1 },
      { id: 'o-1b', text: 'Cache-aside — lazy-load on read miss; the cache only holds actually-read items.',     isCorrect: true,  order: 2 },
      { id: 'o-1c', text: 'Write-back — buffer writes in cache and flush async to reduce DB load.',              isCorrect: false, order: 3 },
      { id: 'o-1d', text: 'Refresh-ahead — proactively refresh expiring entries on a background schedule.',     isCorrect: false, order: 4 },
    ],
  },
  'q-2': {
    id: 'q-2', topicId: 't-cache',
    questionType: QuestionType.MultipleChoice,
    prompt: "Two API replicas serve the same user. Replica A invalidates a cache key right before Replica B writes a fresh value to the DB. The next read returns the *old* value. Where's the bug?",
    explanation: "The classic invalidate-then-write race: A's invalidation races B's write. A reader between A's invalidate and B's commit re-populates the cache from DB with the still-old value, and B's commit lands but never updates the cache. Solutions: invalidate AFTER the write commits (write-through-invalidate), use versioned keys, or guard with a short-lived 'in-progress' marker. Bumping TTL doesn't fix it — it just shortens the staleness window.",
    difficulty: TopicDifficulty.Advanced,
    estimatedSolvingTimeSeconds: 360,
    minimumPassingScore: 60,
    tags: ['caching', 'race-conditions'],
    acceptedAnswers: [],
    options: [
      { id: 'o-2a', text: 'A invalidated the key before B committed, then a reader re-populated it with stale data.', isCorrect: true,  order: 1 },
      { id: 'o-2b', text: 'Redis dropped the SET command silently — should retry with WAIT.',                          isCorrect: false, order: 2 },
      { id: 'o-2c', text: 'TTL was too long; shorter TTLs prevent this entirely.',                                     isCorrect: false, order: 3 },
      { id: 'o-2d', text: 'Replica B used a different cache key namespace.',                                           isCorrect: false, order: 4 },
    ],
  },
  'c-1': {
    id: 'c-1', topicId: 't-jwt',
    questionType: QuestionType.Scenario, // coding challenge — treat as free-form
    prompt: 'Implement refresh-token rotation with rolling invalidation. Sketch the data model + endpoints. Mention how you detect token reuse (stolen-token detection).',
    explanation: "Solid rotation needs: (1) a refresh_tokens table with id, user_id, jti, parent_jti, expires_at, revoked_at, replaced_by. (2) /auth/refresh accepts a refresh token, verifies it's not revoked, mints a new pair, marks the old one replaced_by=new. (3) If a revoked-but-not-expired token is presented, an attacker is likely replaying — invalidate the whole family (by parent_jti chain) and force re-login. (4) Always require Secure+HttpOnly cookies or a hardened device-bound storage.",
    difficulty: TopicDifficulty.Advanced,
    estimatedSolvingTimeSeconds: 1080,
    minimumPassingScore: 70,
    tags: ['jwt', 'auth', 'security'],
    acceptedAnswers: [],
    options: [],
  },
  'q-3': {
    id: 'q-3', topicId: 't-jwt',
    questionType: QuestionType.MultipleChoice,
    prompt: 'You\'re running ASP.NET Core behind a load balancer with 6 stateless instances. Where should the JWT signing key live so all of them validate the same tokens consistently?',
    explanation: "Multi-instance APIs need a shared, externally-provisioned signing key — either a secret pulled from a managed secret store (Azure Key Vault, AWS SSM/Secrets Manager, HashiCorp Vault) or an asymmetric key whose public part is published as a JWKS endpoint. Per-instance secrets cause random validation failures; appsettings.json secrets leak into source control; DataProtection keys default to a local folder that isn't shared.",
    difficulty: TopicDifficulty.Intermediate,
    estimatedSolvingTimeSeconds: 300,
    minimumPassingScore: 60,
    tags: ['jwt', 'auth', 'infra'],
    acceptedAnswers: [],
    options: [
      { id: 'o-3a', text: 'Per-instance — let each instance generate its own and trust the load balancer.',           isCorrect: false, order: 1 },
      { id: 'o-3b', text: 'In appsettings.json, committed to the repo for reproducibility.',                          isCorrect: false, order: 2 },
      { id: 'o-3c', text: 'In a managed secret store (Key Vault / Secrets Manager), pulled at startup.',              isCorrect: true,  order: 3 },
      { id: 'o-3d', text: 'In the default DataProtection key ring on each pod\'s local disk.',                        isCorrect: false, order: 4 },
    ],
  },
  's-1': {
    id: 's-1', topicId: 't-pg',
    questionType: QuestionType.Scenario,
    prompt: 'A 50M-row PostgreSQL reporting table is hot — analysts run heavy aggregations while ETL writes ~200 rows/sec. Indexing it for 3 new aggregation columns will lock writers for ~hours. Design the rollout so neither readers nor writers see downtime.',
    explanation: "Use CREATE INDEX CONCURRENTLY for each new index — it scans without an ACCESS EXCLUSIVE lock, so writes continue. Watch for: long-running transactions can starve it; failed CONCURRENTLY indexes leave INVALID indexes you must DROP; on partitioned tables you create on each partition then attach. For very large tables, consider a partial index (WHERE clause) instead of a global one, or BRIN if the data is naturally ordered. Schedule during a low-ETL window to minimize bloat from HOT updates.",
    difficulty: TopicDifficulty.Advanced,
    estimatedSolvingTimeSeconds: 720,
    minimumPassingScore: 70,
    tags: ['postgres', 'performance', 'ops'],
    acceptedAnswers: [],
    options: [],
  },
  'q-4': {
    id: 'q-4', topicId: 't-ef',
    questionType: QuestionType.MultipleChoice,
    prompt: 'EF Core defaults to single-query for `.Include()` chains. When should you switch to `AsSplitQuery()`?',
    explanation: "Single-query is fine for narrow includes, but cartesian explosion is the killer: includes on multiple collections multiply row counts and inflate transferred data dramatically. Split-query issues one query per include, eliminating the explosion at the cost of N round-trips. Switch when you see slow .Include chains on multiple collections; keep single-query for one-to-one or single-collection includes. It's not about lazy loading and it's not the default.",
    difficulty: TopicDifficulty.Intermediate,
    estimatedSolvingTimeSeconds: 300,
    minimumPassingScore: 60,
    tags: ['ef-core', 'performance'],
    acceptedAnswers: [],
    options: [
      { id: 'o-4a', text: 'Always — split queries are strictly better.',                                          isCorrect: false, order: 1 },
      { id: 'o-4b', text: 'When you Include multiple collections and see cartesian explosion in row counts.',     isCorrect: true,  order: 2 },
      { id: 'o-4c', text: 'When you want lazy loading without proxies.',                                          isCorrect: false, order: 3 },
      { id: 'o-4d', text: 'When the database is sharded across regions.',                                         isCorrect: false, order: 4 },
    ],
  },
  'c-2': {
    id: 'c-2', topicId: 't-cqrs',
    questionType: QuestionType.Scenario,
    prompt: 'Add an idempotent-command pipeline behaviour to a MediatR-style dispatcher. Same `Idempotency-Key` arriving twice within the dedup window must return the original result without re-executing the handler. Outline the behaviour, the store, and how you key it.',
    explanation: "An IdempotencyBehavior<TRequest, TResponse> wrapping the handler checks an idempotency_store table keyed on (key, request-hash) with status (in-progress / completed) and a serialized response. On hit, return the stored response; on miss, insert in-progress, run the handler, write the response, mark completed. Use a unique constraint on the key so two concurrent requests serialize. Hash the request body so retries with the SAME key but DIFFERENT bodies fail loudly instead of silently returning the wrong cached result.",
    difficulty: TopicDifficulty.Expert,
    estimatedSolvingTimeSeconds: 1320,
    minimumPassingScore: 70,
    tags: ['cqrs', 'mediatr', 'idempotency'],
    acceptedAnswers: [],
    options: [],
  },
};

/* ─────────────── Submit answer (simulated for demo) ─────────────── */
async function submitAnswer({ apiBase, demoMode }, body) {
  if (demoMode) {
    await new Promise(r => setTimeout(r, 600));
    const q = Object.values(MOCK_QUESTIONS).find(x => x.id === body.questionId);
    if (!q) throw new Error('Question not found');

    let wasCorrect = false;
    let score = 0;
    let summary = '';

    if (q.questionType === QuestionType.MultipleChoice) {
      const opt = q.options.find(o => o.id === body.selectedOptionId);
      wasCorrect = !!opt?.isCorrect;
      score = wasCorrect ? 100 : 0;
      summary = wasCorrect
        ? 'Correct option — nailed it.'
        : `You picked "${opt?.text.slice(0,50) || '—'}". The correct answer was option ${q.options.findIndex(o => o.isCorrect) + 1}.`;
    } else {
      const text = (body.submittedAnswer || '').trim();
      const length = text.length;
      // Toy heuristic: longer + mentions key terms scores higher
      const keywords = q.tags.concat(['index', 'lock', 'cache', 'jwt', 'mediator', 'pipeline', 'idempotent', 'rotation']);
      const hits = keywords.filter(k => text.toLowerCase().includes(k.toLowerCase())).length;
      score = Math.min(100, Math.round(length / 8) + hits * 8);
      wasCorrect = score >= q.minimumPassingScore;
      summary = wasCorrect
        ? `Strong answer — covered ${hits} key concepts, ${length} chars.`
        : `Partial credit — ${hits} key concepts mentioned, but missing depth.`;
    }

    // Simulated mastery delta + risk
    const masteryBefore = MOCK_DASHBOARD.topicMastery.find(t => t.topicId === q.topicId)?.masteryScore || 50;
    const delta = wasCorrect ? Math.round(2 + Math.random() * 4) : -Math.round(1 + Math.random() * 3);
    const masteryAfter = Math.max(0, Math.min(100, masteryBefore + delta));
    const forgettingRisk = Math.max(0.05, Math.min(0.95, 1 - masteryAfter / 100 + (wasCorrect ? -0.05 : 0.1)));
    const nextReviewDays = wasCorrect ? Math.round(2 + masteryAfter / 12) : 1;
    const nextReviewAtUtc = new Date(Date.now() + nextReviewDays * 24 * 3600 * 1000).toISOString();

    // Mutate mock so dashboard reflects on next visit
    const t = MOCK_DASHBOARD.topicMastery.find(t => t.topicId === q.topicId);
    if (t) {
      t.masteryScore = masteryAfter;
      t.forgettingRisk = forgettingRisk;
      t.accuracy = Math.max(0, Math.min(1, t.accuracy + (wasCorrect ? 0.01 : -0.02)));
    }

    return {
      userAnswerId: 'ua-' + Math.random().toString(36).slice(2, 10),
      wasCorrect,
      score,
      evaluationSummary: summary,
      explanation: q.explanation,
      masteryScore: masteryAfter,
      _masteryBefore: masteryBefore, // extra for UI delta viz (not in real DTO)
      nextReviewAtUtc,
      forgettingRisk,
    };
  }
  return fetchJson(`${apiBase.replace(/\/$/, '')}/api/quiz/answers`, {
    method: 'POST',
    body: JSON.stringify(body),
  });
}

/* ─────────────── Mock topic detail catalogue ─────────────── */
const MOCK_TOPICS = [
  {
    id: 't-cs', name: 'C# Language', slug: 'csharp',
    description: 'The foundation of every .NET 8 service you ship — from value types and nullable reference types to pattern matching, records, primary constructors, and the async state machine. Master the language and the rest of the stack stops fighting you.',
    difficulty: TopicDifficulty.Fundamental,
    decayRate: 0.18, dependencyIds: [],
    questionCount: 142, codingChallengeCount: 18, scenarioCount: 6,
    sampleQuestions: [
      { id: 'tq-cs-1', type: 'Question',         title: 'Records vs. classes — when does immutability matter most?', difficulty: TopicDifficulty.Intermediate, minutes: 4 },
      { id: 'tq-cs-2', type: 'Question',         title: 'Pattern matching: what does `is { } x` actually check?',     difficulty: TopicDifficulty.Intermediate, minutes: 3 },
      { id: 'tq-cs-3', type: 'CodingChallenge',  title: 'Implement a thread-safe LRU cache with TTL',                  difficulty: TopicDifficulty.Advanced,    minutes: 25 },
      { id: 'tq-cs-4', type: 'Question',         title: 'Why is `ConfigureAwait(false)` rarely needed in ASP.NET Core?', difficulty: TopicDifficulty.Advanced,  minutes: 5 },
      { id: 'tq-cs-5', type: 'Question',         title: 'Span<T> vs. Memory<T> — when do you reach for each?',        difficulty: TopicDifficulty.Advanced,    minutes: 6 },
    ],
  },
  {
    id: 't-asp', name: 'ASP.NET Core', slug: 'aspnet-core',
    description: 'Minimal APIs, controllers, dependency injection, middleware order, model binding, content negotiation. The boring parts that crash your service at 3 AM when you get them subtly wrong.',
    difficulty: TopicDifficulty.Intermediate,
    decayRate: 0.22, dependencyIds: ['t-cs'],
    questionCount: 96, codingChallengeCount: 14, scenarioCount: 8,
    sampleQuestions: [
      { id: 'tq-asp-1', type: 'Question',         title: 'Middleware order: UseAuthentication vs UseAuthorization',     difficulty: TopicDifficulty.Intermediate, minutes: 3 },
      { id: 'tq-asp-2', type: 'Question',         title: 'When does model binding silently swallow an error?',          difficulty: TopicDifficulty.Advanced,    minutes: 5 },
      { id: 'tq-asp-3', type: 'ScenarioChallenge',title: 'Design a tenant-aware request pipeline',                      difficulty: TopicDifficulty.Advanced,    minutes: 18 },
      { id: 'tq-asp-4', type: 'Question',         title: 'IHostedService vs BackgroundService — when each one fits',    difficulty: TopicDifficulty.Intermediate, minutes: 4 },
    ],
  },
  {
    id: 't-ef', name: 'EF Core 8', slug: 'ef-core',
    description: 'Tracking, change tracking, transactions, migrations, query splitting, JSON columns, compiled queries, interceptors. The line between a snappy API and a query-of-doom is often one bad `Include`.',
    difficulty: TopicDifficulty.Intermediate,
    decayRate: 0.26, dependencyIds: ['t-cs', 't-pg'],
    questionCount: 118, codingChallengeCount: 12, scenarioCount: 9,
    sampleQuestions: MOCK_QUESTIONS['q-4']
      ? [{ id: 'q-4', type: 'Question', title: MOCK_QUESTIONS['q-4'].prompt, difficulty: TopicDifficulty.Intermediate, minutes: 5 }]
      : [],
  },
  {
    id: 't-clean', name: 'Clean Architecture', slug: 'clean-architecture',
    description: 'Onion / hex / clean — the labels matter less than the dependency rule. Keep your domain free of EF, ASP.NET, and infrastructure leakage so you can test it, refactor it, and explain it to the next person on call.',
    difficulty: TopicDifficulty.Intermediate,
    decayRate: 0.14, dependencyIds: ['t-cs', 't-asp'],
    questionCount: 84, codingChallengeCount: 9, scenarioCount: 12,
    sampleQuestions: [
      { id: 'tq-cln-1', type: 'Question',          title: 'Why shouldn\'t your Domain reference Microsoft.EntityFrameworkCore?', difficulty: TopicDifficulty.Intermediate, minutes: 3 },
      { id: 'tq-cln-2', type: 'ScenarioChallenge', title: 'Refactor a Service-Locator-heavy module to clean boundaries',          difficulty: TopicDifficulty.Advanced,     minutes: 22 },
      { id: 'tq-cln-3', type: 'Question',          title: 'Where do mappers (entity ↔ DTO) belong?',                              difficulty: TopicDifficulty.Intermediate, minutes: 4 },
    ],
  },
  {
    id: 't-cqrs', name: 'CQRS + Mediator', slug: 'cqrs',
    description: 'Splitting reads from writes, command handlers, query handlers, pipeline behaviours, transactional outboxes. The right pattern when your write model and read model genuinely diverge — wrong if they don\'t.',
    difficulty: TopicDifficulty.Advanced,
    decayRate: 0.30, dependencyIds: ['t-clean'],
    questionCount: 72, codingChallengeCount: 11, scenarioCount: 7,
    sampleQuestions: [
      ...(MOCK_QUESTIONS['c-2'] ? [{ id: 'c-2', type: 'CodingChallenge', title: MOCK_QUESTIONS['c-2'].prompt.slice(0, 80) + '…', difficulty: TopicDifficulty.Expert, minutes: 22 }] : []),
      { id: 'tq-cqrs-1', type: 'Question',          title: 'When is CQRS over-engineering for a small service?',  difficulty: TopicDifficulty.Intermediate, minutes: 4 },
      { id: 'tq-cqrs-2', type: 'Question',          title: 'How does a pipeline behaviour differ from middleware?', difficulty: TopicDifficulty.Advanced,    minutes: 5 },
      { id: 'tq-cqrs-3', type: 'ScenarioChallenge', title: 'Design a transactional outbox with idempotent publish', difficulty: TopicDifficulty.Expert,      minutes: 28 },
    ],
  },
  {
    id: 't-jwt', name: 'JWT & Identity', slug: 'jwt',
    description: 'Bearer tokens, refresh rotation, key management across instances, signing algorithm choices, claim shape, revocation strategies. Get this wrong and everyone has the wrong permissions until you redeploy.',
    difficulty: TopicDifficulty.Intermediate,
    decayRate: 0.28, dependencyIds: ['t-asp'],
    questionCount: 64, codingChallengeCount: 8, scenarioCount: 5,
    sampleQuestions: [
      ...(MOCK_QUESTIONS['q-3'] ? [{ id: 'q-3', type: 'Question', title: MOCK_QUESTIONS['q-3'].prompt, difficulty: TopicDifficulty.Intermediate, minutes: 5 }] : []),
      ...(MOCK_QUESTIONS['c-1'] ? [{ id: 'c-1', type: 'CodingChallenge', title: MOCK_QUESTIONS['c-1'].prompt.slice(0, 80) + '…', difficulty: TopicDifficulty.Advanced, minutes: 18 }] : []),
      { id: 'tq-jwt-1', type: 'Question', title: 'HS256 vs RS256 — which is right for multi-service auth?', difficulty: TopicDifficulty.Advanced, minutes: 4 },
    ],
  },
  {
    id: 't-cache', name: 'Caching (Redis)', slug: 'caching',
    description: 'Cache-aside, write-through, write-back, refresh-ahead. The patterns are simple. The race conditions, the invalidation strategy, and the failure mode when Redis blips — those are where real engineers earn their pay.',
    difficulty: TopicDifficulty.Intermediate,
    decayRate: 0.34, dependencyIds: ['t-cs'],
    questionCount: 58, codingChallengeCount: 7, scenarioCount: 6,
    sampleQuestions: [
      ...(MOCK_QUESTIONS['q-1'] ? [{ id: 'q-1', type: 'Question', title: MOCK_QUESTIONS['q-1'].prompt, difficulty: TopicDifficulty.Intermediate, minutes: 4 }] : []),
      ...(MOCK_QUESTIONS['q-2'] ? [{ id: 'q-2', type: 'Question', title: MOCK_QUESTIONS['q-2'].prompt, difficulty: TopicDifficulty.Advanced, minutes: 6 }] : []),
      { id: 'tq-ch-1', type: 'Question', title: 'TTL vs explicit invalidation — when to mix the two?', difficulty: TopicDifficulty.Intermediate, minutes: 3 },
    ],
  },
  {
    id: 't-pg', name: 'PostgreSQL', slug: 'postgres',
    description: 'Indexing, EXPLAIN ANALYZE, MVCC, advisory locks, JSONB, CTEs, partitioning. The store underneath nearly every meaningful service you\'ll build. Treat it as a tool, not a black box.',
    difficulty: TopicDifficulty.Fundamental,
    decayRate: 0.22, dependencyIds: [],
    questionCount: 91, codingChallengeCount: 6, scenarioCount: 10,
    sampleQuestions: [
      ...(MOCK_QUESTIONS['s-1'] ? [{ id: 's-1', type: 'ScenarioChallenge', title: MOCK_QUESTIONS['s-1'].prompt.slice(0, 80) + '…', difficulty: TopicDifficulty.Advanced, minutes: 12 }] : []),
      { id: 'tq-pg-1', type: 'Question',         title: 'Read the EXPLAIN: which join algorithm just blew up?',     difficulty: TopicDifficulty.Advanced,    minutes: 5 },
      { id: 'tq-pg-2', type: 'Question',         title: 'Partial vs covering indexes — pick the right one',          difficulty: TopicDifficulty.Intermediate, minutes: 4 },
      { id: 'tq-pg-3', type: 'ScenarioChallenge',title: 'Migrate a serial column to bigint on a hot table',          difficulty: TopicDifficulty.Expert,       minutes: 24 },
    ],
  },
];

async function fetchTopics({ apiBase, demoMode }) {
  if (demoMode) {
    await new Promise(r => setTimeout(r, 250));
    return MOCK_TOPICS;
  }
  return fetchJson(`${apiBase.replace(/\/$/, '')}/api/topics`);
}

/* ─────────────── Mock challenges (Coding + Scenario) ─────────────── */
const ChallengeOutcome = { PendingReview: 0, Passed: 1, NeedsWork: 2 };

const MOCK_CODING_CHALLENGES = {
  'c-1': {
    id: 'c-1', topicId: 't-jwt',
    title: 'Refresh-token rotation with rolling invalidation',
    description: `Build the endpoint that accepts a refresh token, mints a fresh access+refresh pair, and invalidates the chain when reuse is detected.

The token store is already wired up via \`IRefreshTokenService\`. Your job is the rotation logic.

**Rules**
- A presented refresh token must be currently valid (not expired, not revoked).
- On valid presentation, mint a new pair and mark the presented token \`replaced_by\` the new one.
- If a revoked (but not expired) refresh token is presented, an attacker is likely replaying. Invalidate the **entire family** (parent_jti chain) and force re-auth.
- Always return cookies, never raw tokens.`,
    difficulty: 3,
    estimatedMinutes: 25,
    evaluationCriteria: [
      'Rotates the refresh token on every successful refresh',
      'Marks the old token as replaced (audit trail preserved)',
      'Detects token reuse and invalidates the family',
      'Returns Secure + HttpOnly cookies, never raw tokens',
      'Handles concurrent refresh requests safely',
    ],
    starterCode: `using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TrainingPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IRefreshTokenService _tokens;
    private readonly ITokenIssuer _issuer;

    public AuthController(IRefreshTokenService tokens, ITokenIssuer issuer)
    {
        _tokens = tokens;
        _issuer = issuer;
    }

    // TODO: implement rotation with rolling invalidation.
    // Reuse detection MUST invalidate the full token family.
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var presented = Request.Cookies["rt"];
        if (string.IsNullOrEmpty(presented))
        {
            return Unauthorized();
        }

        // var record = await _tokens.FindAsync(presented);
        // ...

        throw new NotImplementedException();
    }
}`,
    expectedOutcome: `Endpoint reads refresh cookie, validates the record, marks the old token replaced, mints a new access+refresh pair, sets fresh Secure+HttpOnly cookies. Reuse detection finds the parent chain and revokes all descendants.`,
    hints: [
      'Start with the happy path: find token by jti, validate, mint new pair, mark old replaced_by=new.id, set cookies.',
      'For reuse detection: if you find a record whose revoked_at IS NOT NULL but expires_at hasn\'t passed, walk parent_jti up the chain and revoke everything that hangs off it.',
      'Concurrency: use a unique constraint on (parent_jti, replaced_by) or a row-level lock so two refreshes don\'t both succeed.',
    ],
    tests: [
      { name: 'Happy refresh issues new pair and revokes old',           expected: 'pass' },
      { name: 'Expired token returns 401 without minting',                expected: 'pass' },
      { name: 'Replay of revoked token invalidates family',               expected: 'pass' },
      { name: 'Concurrent refreshes both succeed without family revoke',  expected: 'fail' },
    ],
  },
  'c-2': {
    id: 'c-2', topicId: 't-cqrs',
    title: 'Idempotent command pipeline (MediatR)',
    description: `Implement \`IdempotencyBehavior<TRequest, TResponse>\` so that two requests carrying the same \`Idempotency-Key\` inside the dedup window return the original result without re-executing the handler.

The pipeline is registered globally. The store interface (\`IIdempotencyStore\`) lets you read+write the (key → response) tuple.

**Rules**
- Same key + same request body within the window → return cached response, do NOT re-execute.
- Same key + DIFFERENT body → fail loudly (HTTP 409 Conflict).
- Two concurrent requests with the same key must serialize; the second waits for the first to finish, then returns its result.`,
    difficulty: 4,
    estimatedMinutes: 30,
    evaluationCriteria: [
      'Cached response returned without re-running the handler',
      'Hash mismatch on same key throws Conflict',
      'Concurrent requests serialize on a unique constraint',
      'In-progress markers cleaned up on handler failure',
      'Behaviour registered in DI without breaking other pipeline order',
    ],
    starterCode: `using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace TrainingPlatform.Application.Common.Behaviors;

public sealed class IdempotencyBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IIdempotencyStore _store;
    private readonly IIdempotencyKeyAccessor _key;

    public IdempotencyBehavior(IIdempotencyStore store, IIdempotencyKeyAccessor key)
    {
        _store = store;
        _key = key;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // TODO: implement
        // 1. read idempotency key from accessor; if absent, pass through.
        // 2. hash request body.
        // 3. lookup (key, hash). on hit, return cached. on conflict, throw.
        // 4. insert in-progress row (unique constraint on key).
        // 5. call next().
        // 6. write response, mark completed.
        return await next();
    }
}`,
    expectedOutcome: `Pipeline behaviour transparently dedupes commands by key+hash. Replay returns the original response; mismatched body raises Conflict; concurrent calls serialize.`,
    hints: [
      'Compute a content hash (SHA256 of the JSON body) before any DB call.',
      'Wrap the insert + handler + update in a single transaction so a crash mid-handler leaves no dangling "in-progress" row.',
      'Use a UNIQUE constraint on key alone — the second concurrent insert fails fast, then re-reads the first one\'s result.',
    ],
    tests: [
      { name: 'Replay with same key+body returns cached response',     expected: 'pass' },
      { name: 'Same key, different body → Conflict (409)',              expected: 'pass' },
      { name: 'Concurrent replays serialize correctly',                  expected: 'pass' },
      { name: 'Handler failure rolls back the in-progress marker',      expected: 'pass' },
    ],
  },
};

const MOCK_SCENARIO_CHALLENGES = {
  's-1': {
    id: 's-1', topicId: 't-pg',
    title: 'Indexing a 50M-row reporting table without downtime',
    scenario: `A 50M-row PostgreSQL reporting table is hot — analysts run heavy aggregations while ETL writes ~200 rows/sec around the clock.

Product wants three new aggregations on dimensions you haven't indexed yet. A naive \`CREATE INDEX\` will lock writers for hours.

Design the rollout so neither readers nor writers see downtime. Explain the migration plan, the rollback if any step fails, and how you'll know it's safe to drop the old query plan.

Address:
- The index build itself
- What happens to in-flight ETL during the build
- How you measure success before flipping queries to the new indexes
- A rollback path if the build leaves invalid indexes`,
    difficulty: 3,
    estimatedMinutes: 18,
    evaluationCriteria: [
      'Uses CREATE INDEX CONCURRENTLY (not blocking)',
      'Identifies INVALID-index failure mode and cleanup',
      'Has a measurement plan (EXPLAIN ANALYZE before/after)',
      'Mentions HOT updates / bloat from concurrent writes',
      'Plans the rollout window vs. ETL load',
    ],
    referenceSolution: `Use CREATE INDEX CONCURRENTLY for each new index. It scans without an ACCESS EXCLUSIVE lock — writers continue. Watch for long-running transactions starving the build, and failed CONCURRENTLY indexes that leave INVALID indexes (must DROP). Measure with EXPLAIN ANALYZE on representative queries before/after; only flip queries to use the new indexes after vacuum settles. Schedule during a low-ETL window to minimize HOT-update churn. Rollback: DROP INDEX CONCURRENTLY on the failed/replaced indexes — safe online.`,
    hints: [
      'The magic word is CONCURRENTLY. Once you have that, the rest is operational hygiene.',
      'A failed CONCURRENTLY build leaves an INVALID index. Always have a cleanup step.',
      'You\'re not "done" when the build finishes — you\'re done when EXPLAIN ANALYZE on the target query shows the new index is picked.',
    ],
  },
};

async function fetchChallenge({ apiBase, demoMode }, kind, challengeId) {
  if (demoMode) {
    await new Promise(r => setTimeout(r, 250));
    const map = kind === 'coding' ? MOCK_CODING_CHALLENGES : MOCK_SCENARIO_CHALLENGES;
    const c = map[challengeId];
    if (!c) throw new Error('Challenge not found');
    return c;
  }
  const path = kind === 'coding' ? 'coding' : 'scenario';
  return fetchJson(`${apiBase.replace(/\/$/, '')}/api/challenges/${path}/${encodeURIComponent(challengeId)}`);
}

async function submitChallenge({ apiBase, demoMode }, kind, body) {
  if (demoMode) {
    await new Promise(r => setTimeout(r, 1100));
    // Toy grading
    const challenge = (kind === 'coding'
      ? MOCK_CODING_CHALLENGES[body.codingChallengeId]
      : MOCK_SCENARIO_CHALLENGES[body.scenarioChallengeId]);
    const text = (kind === 'coding' ? body.submittedCode : body.responseText) || '';
    const length = text.length;
    const hits = (challenge?.evaluationCriteria || []).map((c) => {
      const tokens = c.toLowerCase().split(/[^a-z0-9]+/).filter(t => t.length > 3);
      const found = tokens.filter(t => text.toLowerCase().includes(t)).length;
      return { criterion: c, met: found >= Math.max(1, Math.floor(tokens.length / 3)) };
    });
    const metCount = hits.filter(h => h.met).length;
    const score = Math.min(100, Math.round((metCount / hits.length) * 80 + Math.min(20, length / 50)));
    const outcome = score >= 70 ? ChallengeOutcome.Passed
                  : score >= 40 ? ChallengeOutcome.NeedsWork
                  : ChallengeOutcome.PendingReview;
    const aiFeedback = score >= 70
      ? `Solid attempt — you covered ${metCount} of ${hits.length} criteria with depth. Watch out for the edge case around ${hits.find(h => !h.met)?.criterion || 'concurrency'} which is partially addressed.`
      : score >= 40
        ? `Workable skeleton but missing key pieces. Specifically: ${hits.filter(h => !h.met).map(h => h.criterion).slice(0, 2).join('; ')}.`
        : `Not enough substance yet. Re-read the rules and address each evaluation criterion explicitly.`;
    return {
      id: 'sub-' + Math.random().toString(36).slice(2, 10),
      score,
      outcome,
      createdAtUtc: new Date().toISOString(),
      _criteriaResults: hits,
      _aiFeedback: aiFeedback,
    };
  }
  const url = kind === 'coding'
    ? `${apiBase.replace(/\/$/, '')}/api/challenges/coding/submissions`
    : `${apiBase.replace(/\/$/, '')}/api/challenges/scenario/submissions`;
  const res = await fetchJson(url, { method: 'POST', body: JSON.stringify(body) });
  // The API serializes enums as strings and carries feedback in `feedback`;
  // normalise to the shape the evaluation panel expects.
  if (res && typeof res.outcome === 'string') {
    res.outcome = ({ PendingReview: 0, Passed: 1, NeedsWork: 2 })[res.outcome] ?? 0;
  }
  if (res && res.feedback && !res._aiFeedback) res._aiFeedback = res.feedback;
  return res;
}

// Runs the learner's code against the challenge's test suite WITHOUT recording
// a submission — the editor's "Run tests" button.
async function runCodingTests({ apiBase, demoMode }, challengeId, code) {
  if (demoMode) {
    await new Promise(r => setTimeout(r, 900));
    const plausible = (code || '').length > 120;
    return {
      evaluated: true, compiled: true,
      totalTests: 4, passedTests: plausible ? 4 : 1, failedTests: plausible ? 0 : 3,
      output: plausible ? 'All 4 tests passed.' : '1 of 4 tests passed.\n\nFAILED Demo_test\n  Expected true, got false.',
    };
  }
  return fetchJson(`${apiBase.replace(/\/$/, '')}/api/challenges/coding/${encodeURIComponent(challengeId)}/run`, {
    method: 'POST',
    body: JSON.stringify({ submittedCode: code }),
  });
}

async function fetchQuestion({ apiBase, demoMode }, questionId) {
  if (demoMode) {
    await new Promise(r => setTimeout(r, 200));
    const q = MOCK_QUESTIONS[questionId];
    if (!q) throw new Error('Question not found');
    return q;
  }
  return fetchJson(`${apiBase.replace(/\/$/, '')}/api/questions/${encodeURIComponent(questionId)}`);
}

/* ─────────────── Health probe ─────────────── */
async function probeHealth({ apiBase }) {
  const ctrl = new AbortController();
  const timer = setTimeout(() => ctrl.abort(), 2500);
  try {
    const res = await fetch(`${apiBase.replace(/\/$/, '')}/api/health`, { signal: ctrl.signal, mode: 'cors' });
    return res.ok;
  } catch {
    return false;
  } finally {
    clearTimeout(timer);
  }
}

/* ─────────────── User preferences ─────────────── */
const PREFERENCE_DEFAULTS = {
  dailyQuestionTarget: 8,
  dailyStudyMinutes: 20,
  dailyCodingChallengeTarget: 1,
  dailyScenarioChallengeTarget: 1,
  includeWeekends: true,
  goals: [],
};

async function fetchPreferences({ apiBase, demoMode }) {
  if (demoMode) {
    await new Promise(r => setTimeout(r, 120));
    try {
      const cached = JSON.parse(localStorage.getItem('training_preferences_demo') || 'null');
      return cached || { ...PREFERENCE_DEFAULTS };
    } catch { return { ...PREFERENCE_DEFAULTS }; }
  }
  return fetchJson(`${apiBase.replace(/\/$/, '')}/api/me/preferences`);
}

async function updatePreferences({ apiBase, demoMode }, body) {
  const payload = { ...PREFERENCE_DEFAULTS, ...body };
  if (demoMode) {
    await new Promise(r => setTimeout(r, 220));
    localStorage.setItem('training_preferences_demo', JSON.stringify(payload));
    return payload;
  }
  return fetchJson(`${apiBase.replace(/\/$/, '')}/api/me/preferences`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  });
}

async function completeOnboarding({ apiBase, demoMode }, body) {
  const payload = {
    dailyQuestionTarget: body.dailyQuestionTarget ?? PREFERENCE_DEFAULTS.dailyQuestionTarget,
    dailyStudyMinutes: body.dailyStudyMinutes ?? PREFERENCE_DEFAULTS.dailyStudyMinutes,
    dailyCodingChallengeTarget: body.dailyCodingChallengeTarget ?? PREFERENCE_DEFAULTS.dailyCodingChallengeTarget,
    dailyScenarioChallengeTarget: body.dailyScenarioChallengeTarget ?? PREFERENCE_DEFAULTS.dailyScenarioChallengeTarget,
    includeWeekends: body.includeWeekends ?? PREFERENCE_DEFAULTS.includeWeekends,
    goals: body.goals ?? [],
    assessments: body.assessments ?? [],
  };
  if (demoMode) {
    await new Promise(r => setTimeout(r, 220));
    const stored = { ...payload };
    delete stored.assessments;
    localStorage.setItem('training_preferences_demo', JSON.stringify(stored));
    return stored;
  }
  return fetchJson(`${apiBase.replace(/\/$/, '')}/api/me/onboarding`, {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}

Object.assign(window, {
  StudyPlanItemType, DailyStudyPlanStatus, TopicDifficulty, QuestionType, ChallengeOutcome,
  TOPIC_NAMES,
  PREFERENCE_DEFAULTS,
  MOCK_DASHBOARD, MOCK_PLAN, MOCK_QUESTIONS, MOCK_TOPICS,
  MOCK_CODING_CHALLENGES, MOCK_SCENARIO_CHALLENGES,
  fetchTodayPlan, fetchDashboard, generatePlan, fetchQuestion, submitAnswer,
  fetchTopics, fetchChallenge, submitChallenge, runCodingTests,
  fetchPreferences, updatePreferences, completeOnboarding, probeHealth,
  getCurrentUser, getAuthToken,
});
