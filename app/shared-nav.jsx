// shared-nav.jsx
// Sticky top navigation used across Dashboard, Topics, and any other "shell"
// page. Tabs are real navigations (not in-page state) so refresh/deep-link
// behaviour matches user expectation.

function TopNav({ activeTab, apiOnline }) {
  const user = (() => {
    try { return JSON.parse(localStorage.getItem('training_user') || 'null'); } catch { return null; }
  })();
  const initials = (user?.displayName || 'You')
    .split(' ').map(s => s[0]).slice(0, 2).join('').toUpperCase();

  const tabs = [
    { id: 'today',    label: 'Bugün',     href: 'Dashboard.html' },
    { id: 'topics',   label: 'Konular',   href: 'Topics.html' },
    { id: 'practice', label: 'Antrenman', href: 'Practice.html' },
  ];

  const onSignOut = () => {
    localStorage.removeItem('training_token');
    localStorage.removeItem('training_user');
    window.location.href = 'Auth.html';
  };

  const dotColor = apiOnline === false ? 'var(--danger)'
                : apiOnline === 'demo' ? 'var(--warn)'
                : 'var(--accent)';
  const dotLabel = apiOnline === false ? 'API kapalı'
                : apiOnline === 'demo' ? 'Demo'
                : 'Canlı';

  return (
    <header className="topbar">
      <div className="container-x flex items-center justify-between" style={{ height: 60 }}>
        <div className="flex items-center gap-7">
          <a href="Dashboard.html" className="flex items-center gap-2.5"
             style={{ textDecoration: 'none' }}>
            <div className="tp-mark">M</div>
            <div className="font-semibold tracking-tight text-[14.5px]"
                 style={{ color: 'var(--ink)', letterSpacing: '-0.015em' }}>
              Masterly
            </div>
          </a>
          <nav className="hidden md:flex items-center gap-1">
            {tabs.map((t) => (
              <a key={t.id} href={t.href}
                 className="nav-tab"
                 data-active={activeTab === t.id}
                 style={{ textDecoration: 'none' }}>
                {t.label}
              </a>
            ))}
          </nav>
        </div>

        <div className="flex items-center gap-3">
          <span className="inline-flex items-center gap-1.5 text-[11px] font-mono"
                style={{ color: 'var(--ink-mute)' }}>
            <span className="relative flex h-1.5 w-1.5">
              <span className="absolute inset-0 rounded-full animate-ping opacity-40"
                    style={{ background: dotColor }} />
              <span className="relative rounded-full h-1.5 w-1.5"
                    style={{ background: dotColor }} />
            </span>
            {dotLabel}
          </span>

          <div className="flex items-center gap-2 pl-3"
               style={{ borderLeft: '1px solid var(--line)' }}>
            <a href="Settings.html" className="avatar"
               title="Ayarlar ve profil"
               style={{ textDecoration: 'none' }}>{initials}</a>
            <div className="hidden sm:flex flex-col leading-tight">
              <span className="text-[12.5px] font-medium" style={{ color: 'var(--ink)' }}>
                {user?.displayName || 'You'}
              </span>
              <span className="text-[10.5px] font-mono" style={{ color: 'var(--ink-mute)' }}>
                {user?.email || 'guest@local'}
              </span>
            </div>
            <a href="Settings.html" className="btn-icon ml-1"
               title="Ayarlar" aria-label="Ayarlar"
               style={{ textDecoration: 'none' }}>
              <svg viewBox="0 0 16 16" width="13" height="13" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
                <circle cx="8" cy="8" r="2"/>
                <path d="M13.5 8a5.5 5.5 0 0 0-.1-1.1l1.4-1.1-1.5-2.6-1.7.6a5.5 5.5 0 0 0-1.9-1.1L9.5 1h-3l-.2 1.7a5.5 5.5 0 0 0-1.9 1.1l-1.7-.6L1.2 5.8 2.6 6.9A5.5 5.5 0 0 0 2.5 8a5.5 5.5 0 0 0 .1 1.1L1.2 10.2l1.5 2.6 1.7-.6a5.5 5.5 0 0 0 1.9 1.1L6.5 15h3l.2-1.7a5.5 5.5 0 0 0 1.9-1.1l1.7.6 1.5-2.6-1.4-1.1A5.5 5.5 0 0 0 13.5 8z"/>
              </svg>
            </a>
            <button className="btn-icon ml-1" onClick={onSignOut} title="Çıkış yap" aria-label="Çıkış yap">
              <Icon.Caret />
            </button>
          </div>
        </div>
      </div>
    </header>
  );
}

window.TopNav = TopNav;
