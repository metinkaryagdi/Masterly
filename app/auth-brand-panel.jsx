// auth-brand-panel.jsx
// Left-side brand panel. Three variants: mastery (default), streak, topics.

const { useEffect, useState } = React;

function MasteryRing({ value = 84, size = 240 }) {
  // Concentric arcs: outer = overall mastery; inner segments = topic clusters.
  const cx = size / 2;
  const cy = size / 2;
  const outer = (size / 2) - 14;
  const inner = outer - 32;

  const outerCirc = 2 * Math.PI * outer;
  const innerCirc = 2 * Math.PI * inner;

  const [shown, setShown] = useState(0);
  useEffect(() => {
    let raf;
    const start = performance.now();
    const duration = 1300;
    const tick = (now) => {
      const t = Math.min(1, (now - start) / duration);
      const eased = 1 - Math.pow(1 - t, 3);
      setShown(Math.round(value * eased));
      if (t < 1) raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [value]);

  // Topic micro-arcs around the inner ring
  const segments = [
    { label: 'C#', frac: 0.92, hue: 155, start: 0 },
    { label: 'EF', frac: 0.76, hue: 195, start: 0.26 },
    { label: 'JWT', frac: 0.58, hue: 240, start: 0.52 },
    { label: 'CQRS', frac: 0.81, hue: 90, start: 0.78 },
  ];

  return (
    <div className="relative" style={{ width: size, height: size }}>
      <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`} className="block">
        {/* Outer track */}
        <circle
          cx={cx} cy={cy} r={outer}
          fill="none"
          stroke="oklch(0.93 0.006 85)"
          strokeWidth="10"
        />
        {/* Outer progress */}
        <circle
          cx={cx} cy={cy} r={outer}
          fill="none"
          stroke="var(--accent)"
          strokeWidth="10"
          strokeLinecap="round"
          strokeDasharray={outerCirc}
          strokeDashoffset={outerCirc * (1 - value / 100)}
          className="tp-ring-progress"
          style={{ '--from': outerCirc, '--to': outerCirc * (1 - value / 100) }}
        />
        {/* Inner track */}
        <circle
          cx={cx} cy={cy} r={inner}
          fill="none"
          stroke="oklch(0.95 0.005 85)"
          strokeWidth="6"
        />
        {/* Topic micro-arcs on inner ring */}
        {segments.map((s, i) => {
          const arcLen = innerCirc * 0.21;
          const gap = innerCirc * 0.04;
          const offset = innerCirc * (1 - s.start) - arcLen * s.frac;
          return (
            <circle
              key={i}
              cx={cx} cy={cy} r={inner}
              fill="none"
              stroke={`oklch(0.62 0.13 ${s.hue})`}
              strokeWidth="6"
              strokeLinecap="round"
              strokeDasharray={`${arcLen * s.frac} ${innerCirc}`}
              strokeDashoffset={offset}
              transform={`rotate(-90 ${cx} ${cy})`}
              opacity="0.85"
            />
          );
        })}
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center">
        <div className="font-mono text-[11px] tracking-widest uppercase" style={{ color: 'var(--ink-mute)' }}>
          Mastery
        </div>
        <div className="font-semibold leading-none mt-2" style={{ fontSize: 58, letterSpacing: '-0.04em', color: 'var(--ink)' }}>
          {shown}
        </div>
        <div className="mt-2 text-[12px]" style={{ color: 'var(--ink-soft)' }}>
          across <span className="font-medium" style={{ color: 'var(--ink)' }}>8 topics</span>
        </div>
      </div>
    </div>
  );
}

function StreakBars() {
  const days = [3, 5, 4, 7, 6, 8, 5, 9, 7, 10, 8, 11, 9, 12];
  const max = 12;
  return (
    <div className="w-full max-w-[360px]">
      <div className="flex items-end justify-between h-[180px] gap-2">
        {days.map((v, i) => (
          <div key={i} className="flex-1 flex flex-col items-center justify-end gap-1">
            <div
              className="w-full rounded-md"
              style={{
                height: `${(v / max) * 100}%`,
                background: i === days.length - 1
                  ? 'var(--accent)'
                  : `oklch(${0.72 + (i / days.length) * 0.05} 0.06 155 / ${0.4 + (i / days.length) * 0.5})`,
                transition: 'height .8s ease',
              }}
            />
          </div>
        ))}
      </div>
      <div className="mt-3 flex items-baseline justify-between font-mono text-[10.5px] tracking-wide uppercase" style={{ color: 'var(--ink-mute)' }}>
        <span>2 weeks</span>
        <span style={{ color: 'var(--accent-ink)', fontWeight: 600 }}>Today · 12 solved</span>
      </div>
    </div>
  );
}

function TopicStack() {
  const topics = [
    { name: 'Clean Architecture', mastery: 88, q: 142 },
    { name: 'CQRS + MediatR',     mastery: 81, q: 96 },
    { name: 'EF Core 8',          mastery: 74, q: 118 },
    { name: 'JWT & Identity',     mastery: 67, q: 64 },
    { name: 'Caching (Redis)',    mastery: 52, q: 38 },
  ];
  return (
    <div className="w-full max-w-[380px] flex flex-col gap-3">
      {topics.map((t, i) => (
        <div key={i} className="bg-white rounded-xl p-3.5 border" style={{ borderColor: 'var(--line)' }}>
          <div className="flex items-center justify-between mb-2">
            <div className="text-[13.5px] font-medium" style={{ color: 'var(--ink)', letterSpacing: '-0.01em' }}>
              {t.name}
            </div>
            <div className="font-mono text-[11px]" style={{ color: 'var(--ink-mute)' }}>
              {t.q}q
            </div>
          </div>
          <div className="flex items-center gap-2.5">
            <div className="flex-1 h-[5px] rounded-full" style={{ background: 'oklch(0.94 0.006 85)' }}>
              <div className="h-full rounded-full" style={{
                width: `${t.mastery}%`,
                background: 'var(--accent)',
                transition: 'width 1s cubic-bezier(.2,.7,.2,1)',
              }} />
            </div>
            <div className="font-mono text-[11.5px] tabular-nums w-[28px] text-right" style={{ color: 'var(--ink-soft)' }}>
              {t.mastery}
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

function BrandPanel({ variant = 'mastery' }) {
  return (
    <aside className="tp-brand-panel relative overflow-hidden flex flex-col justify-between"
           style={{ background: 'var(--bg-warm)', borderRight: '1px solid var(--line)', minHeight: '100vh' }}>
      {/* decorative layers */}
      <div className="absolute inset-0 tp-grid-bg" />
      <div className="absolute inset-0 tp-glow" />

      {/* top: wordmark */}
      <div className="relative z-10 px-12 pt-10 flex items-center gap-2.5">
        <div className="tp-mark">M</div>
        <div className="text-[15px] font-semibold tracking-tight" style={{ color: 'var(--ink)', letterSpacing: '-0.015em' }}>
          Masterly
        </div>
        <div className="ml-2 font-mono text-[10.5px] px-2 py-0.5 rounded-md"
             style={{
               background: 'color-mix(in oklch, var(--accent) 14%, transparent)',
               color: 'var(--accent-ink)',
               letterSpacing: '0.02em',
             }}>
          v0.4 · beta
        </div>
      </div>

      {/* center: visual */}
      <div className="relative z-10 px-12 flex flex-col items-center justify-center flex-1 py-12">
        <div className="text-[13px] font-mono uppercase tracking-[0.14em] mb-6" style={{ color: 'var(--ink-mute)' }}>
          {variant === 'mastery' && '// daily adaptive practice'}
          {variant === 'streak'  && '// 14-day study streak'}
          {variant === 'topics'  && '// .NET 8 backend track'}
        </div>

        {variant === 'mastery' && <MasteryRing value={84} size={260} />}
        {variant === 'streak'  && <StreakBars />}
        {variant === 'topics'  && <TopicStack />}

        <h1 className="mt-10 text-center font-semibold tracking-tight max-w-[440px]"
            style={{ fontSize: 30, lineHeight: 1.12, letterSpacing: '-0.022em', color: 'var(--ink)' }}>
          Build mastery in <span style={{ color: 'var(--accent-ink)' }}>.NET 8</span> backend, one daily plan at a time.
        </h1>
        <p className="mt-3 text-center max-w-[400px] text-[14.5px]" style={{ color: 'var(--ink-soft)', lineHeight: 1.55 }}>
          Adaptive questions, spaced revision, and topic-level mastery scoring — all calibrated to where you actually struggle.
        </p>
      </div>

      {/* bottom: topic chips */}
      <div className="relative z-10 px-12 pb-10">
        <div className="flex flex-wrap gap-2 justify-center">
          {[
            'C#', 'ASP.NET Core', 'EF Core', 'Clean Architecture',
            'CQRS', 'JWT', 'Caching', 'PostgreSQL',
          ].map((t) => (
            <span key={t} className="tp-chip">
              <span className="tp-chip-dot" />
              {t}
            </span>
          ))}
        </div>
      </div>
    </aside>
  );
}

window.BrandPanel = BrandPanel;
