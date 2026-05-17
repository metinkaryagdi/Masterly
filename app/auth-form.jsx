// auth-form.jsx
// Login + Register form, with shared validation matching backend rules.

const { useState, useMemo, useEffect, useRef } = React;

// --- Icons (minimal, stroke-based) ---
function IconMail(props) {
  return (
    <svg viewBox="0 0 20 20" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="1.5" {...props}>
      <rect x="2.5" y="4" width="15" height="12" rx="2.5" />
      <path d="M3 5.5l7 5 7-5" strokeLinecap="round" />
    </svg>
  );
}
function IconLock(props) {
  return (
    <svg viewBox="0 0 20 20" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="1.5" {...props}>
      <rect x="4" y="8.5" width="12" height="9" rx="2" />
      <path d="M6.5 8.5V6a3.5 3.5 0 1 1 7 0v2.5" strokeLinecap="round" />
    </svg>
  );
}
function IconUser(props) {
  return (
    <svg viewBox="0 0 20 20" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="1.5" {...props}>
      <circle cx="10" cy="7.5" r="3" />
      <path d="M3.5 17c.8-3.4 3.5-5 6.5-5s5.7 1.6 6.5 5" strokeLinecap="round" />
    </svg>
  );
}
function IconEye({ off, ...rest }) {
  return (
    <svg viewBox="0 0 20 20" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" {...rest}>
      <path d="M2 10s2.8-5.5 8-5.5S18 10 18 10s-2.8 5.5-8 5.5S2 10 2 10z" />
      <circle cx="10" cy="10" r="2.4" />
      {off && <path d="M3 3l14 14" />}
    </svg>
  );
}
function IconArrow(props) {
  return (
    <svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...props}>
      <path d="M3 8h10M9 4l4 4-4 4" />
    </svg>
  );
}
function IconSpin(props) {
  return (
    <svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" {...props}>
      <circle cx="8" cy="8" r="6" opacity="0.25" />
      <path d="M14 8a6 6 0 0 0-6-6">
        <animateTransform attributeName="transform" type="rotate" from="0 8 8" to="360 8 8" dur="0.9s" repeatCount="indefinite" />
      </path>
    </svg>
  );
}
function IconCheck(props) {
  return (
    <svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
      <path d="M3 8.5l3.2 3.2L13 5" />
    </svg>
  );
}
function IconAlert(props) {
  return (
    <svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" {...props}>
      <circle cx="8" cy="8" r="6.5" />
      <path d="M8 4.5v4M8 11h.01" />
    </svg>
  );
}

// --- Validators (mirror backend FluentValidation rules) ---
const Validators = {
  email: (v) => {
    if (!v.trim()) return 'Email is required';
    // RFC-ish, intentionally loose — server is the source of truth
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v.trim())) return 'Enter a valid email address';
    return null;
  },
  password: (v, mode) => {
    if (!v) return 'Password is required';
    if (mode === 'register') {
      if (v.length < 8) return 'At least 8 characters';
      if (v.length > 100) return 'Too long (max 100)';
    }
    return null;
  },
  displayName: (v) => {
    if (!v.trim()) return 'Display name is required';
    if (v.trim().length < 3) return 'At least 3 characters';
    if (v.trim().length > 120) return 'Too long (max 120)';
    return null;
  },
};

function passwordStrength(pw) {
  if (!pw) return 0;
  let score = 0;
  if (pw.length >= 8) score++;
  if (pw.length >= 12) score++;
  if (/[A-Z]/.test(pw) && /[a-z]/.test(pw)) score++;
  if (/\d/.test(pw) && /[^A-Za-z0-9]/.test(pw)) score++;
  return Math.min(score, 4);
}

// --- Field wrapper ---
function Field({ label, hint, error, children, htmlFor, optional }) {
  return (
    <div>
      <label htmlFor={htmlFor} className="tp-label flex items-baseline justify-between">
        <span>{label}</span>
        {optional && <span className="font-mono text-[10.5px]" style={{ color: 'var(--ink-mute)' }}>optional</span>}
      </label>
      {children}
      {error
        ? <div className="tp-error flex items-center gap-1.5 mt-1.5"><IconAlert /> {error}</div>
        : hint ? <div className="tp-hint mt-1.5">{hint}</div> : null
      }
    </div>
  );
}

function TextInput({ id, type = 'text', value, onChange, placeholder, autoComplete, icon, error, trailing, ...rest }) {
  return (
    <div className="relative">
      {icon && (
        <span className="absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none"
              style={{ color: 'var(--ink-mute)' }}>
          {icon}
        </span>
      )}
      <input
        id={id}
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        autoComplete={autoComplete}
        className="tp-input"
        style={{ paddingLeft: icon ? 38 : 14, paddingRight: trailing ? 40 : 14 }}
        aria-invalid={error ? 'true' : 'false'}
        {...rest}
      />
      {trailing && (
        <div className="absolute right-2 top-1/2 -translate-y-1/2 flex items-center">
          {trailing}
        </div>
      )}
    </div>
  );
}

// --- API client ---
async function callAuth(endpoint, body, { apiBase, demoMode }) {
  if (demoMode) {
    // Simulate latency
    await new Promise((r) => setTimeout(r, 700));
    // Simulated errors for known emails
    if (endpoint === 'login' && body.email === 'fail@test.com') {
      const err = new Error('Invalid email or password.');
      err.status = 401;
      throw err;
    }
    if (endpoint === 'register' && body.email === 'taken@test.com') {
      const err = new Error('A user with the same email already exists.');
      err.status = 409;
      throw err;
    }
    return {
      userId: '00000000-0000-0000-0000-000000000001',
      displayName: body.displayName || body.email.split('@')[0],
      email: body.email,
      accessToken: 'demo.' + btoa(JSON.stringify({ sub: body.email, iat: Date.now() })) + '.signature',
    };
  }

  const res = await fetch(`${apiBase.replace(/\/$/, '')}/api/auth/${endpoint}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    let message = res.statusText;
    try {
      const data = await res.json();
      message = data.detail || data.message || data.title || message;
    } catch {}
    const err = new Error(message);
    err.status = res.status;
    throw err;
  }
  return res.json();
}

// --- Main form ---
function AuthForm({ mode, onModeChange, onSuccess, apiBase, demoMode }) {
  const isRegister = mode === 'register';

  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('');
  const [showPw, setShowPw] = useState(false);
  const [touched, setTouched] = useState({});
  const [submitting, setSubmitting] = useState(false);
  const [serverError, setServerError] = useState(null);
  const [success, setSuccess] = useState(false);

  // Clear server error when user edits
  useEffect(() => { setServerError(null); }, [email, password, displayName, mode]);

  const errors = useMemo(() => ({
    email: Validators.email(email),
    password: Validators.password(password, mode),
    displayName: isRegister ? Validators.displayName(displayName) : null,
  }), [email, password, displayName, mode, isRegister]);

  const visibleErrors = {
    email: touched.email ? errors.email : null,
    password: touched.password ? errors.password : null,
    displayName: isRegister && touched.displayName ? errors.displayName : null,
  };

  const formValid = !errors.email && !errors.password && (!isRegister || !errors.displayName);
  const strength = passwordStrength(password);

  async function handleSubmit(e) {
    e.preventDefault();
    setTouched({ email: true, password: true, displayName: true });
    if (!formValid || submitting) return;
    setSubmitting(true);
    setServerError(null);

    try {
      const body = isRegister
        ? { email: email.trim().toLowerCase(), displayName: displayName.trim(), password }
        : { email: email.trim().toLowerCase(), password };
      const data = await callAuth(isRegister ? 'register' : 'login', body, { apiBase, demoMode });

      // Persist JWT + user info
      localStorage.setItem('training_token', data.accessToken);
      localStorage.setItem('training_user', JSON.stringify({
        userId: data.userId,
        displayName: data.displayName,
        email: data.email,
      }));
      // Fresh registrations always go through onboarding; logins skip it
      // unless they've never been onboarded on this device.
      if (isRegister) {
        localStorage.removeItem('training_onboarded');
      }
      setSuccess(true);

      // Brief delay so success state is perceptible, then hand off
      setTimeout(() => onSuccess && onSuccess(data, { isRegister }), 650);
    } catch (err) {
      // Friendly messages for the canonical status codes
      let msg = err.message || 'Something went wrong.';
      if (err.status === 401) msg = 'Wrong email or password. Try again or create an account.';
      else if (err.status === 409) msg = 'An account with this email already exists. Try signing in.';
      else if (err.status === 400) msg = msg || 'Some fields look off — check them and retry.';
      else if (!err.status)        msg = `Can't reach the API at ${apiBase}. Is the backend running? You can flip on Demo mode in Tweaks.`;
      setServerError(msg);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
      {/* Server feedback banner */}
      {serverError && (
        <div className="tp-banner tp-rise">
          <span className="mt-[2px]"><IconAlert /></span>
          <span>{serverError}</span>
        </div>
      )}
      {success && (
        <div className="tp-banner tp-banner-success tp-rise">
          <span className="mt-[2px]"><IconCheck /></span>
          <span>Welcome{isRegister ? ' aboard' : ' back'}. Loading your dashboard…</span>
        </div>
      )}

      {/* Display name (register only) */}
      {isRegister && (
        <Field label="Display name" htmlFor="displayName"
               error={visibleErrors.displayName}
               hint="What we'll call you on the leaderboard.">
          <TextInput
            id="displayName"
            value={displayName}
            onChange={setDisplayName}
            onBlur={() => setTouched((t) => ({ ...t, displayName: true }))}
            placeholder="Ada Lovelace"
            autoComplete="name"
            icon={<IconUser />}
            error={visibleErrors.displayName}
          />
        </Field>
      )}

      {/* Email */}
      <Field label="Email" htmlFor="email" error={visibleErrors.email}>
        <TextInput
          id="email"
          type="email"
          value={email}
          onChange={setEmail}
          onBlur={() => setTouched((t) => ({ ...t, email: true }))}
          placeholder="you@company.dev"
          autoComplete="email"
          icon={<IconMail />}
          error={visibleErrors.email}
        />
      </Field>

      {/* Password */}
      <Field
        label="Password"
        htmlFor="password"
        error={visibleErrors.password}
        hint={isRegister ? 'At least 8 characters.' : null}
      >
        <TextInput
          id="password"
          type={showPw ? 'text' : 'password'}
          value={password}
          onChange={setPassword}
          onBlur={() => setTouched((t) => ({ ...t, password: true }))}
          placeholder={isRegister ? 'Choose something memorable' : 'Your password'}
          autoComplete={isRegister ? 'new-password' : 'current-password'}
          icon={<IconLock />}
          error={visibleErrors.password}
          trailing={
            <button
              type="button"
              onClick={() => setShowPw((v) => !v)}
              className="w-8 h-8 inline-flex items-center justify-center rounded-md hover:bg-black/5"
              style={{ color: 'var(--ink-mute)' }}
              aria-label={showPw ? 'Hide password' : 'Show password'}
              tabIndex={-1}
            >
              <IconEye off={showPw} />
            </button>
          }
        />
        {isRegister && password && (
          <div className="tp-strength">
            {[0,1,2,3].map((i) => (
              <i key={i} data-on={strength > i ? 'true' : 'false'} />
            ))}
          </div>
        )}
      </Field>

      {!isRegister && (
        <div className="flex justify-end -mt-1">
          <a href="#" className="text-[12.5px] tp-link" onClick={(e) => e.preventDefault()}>
            Forgot password?
          </a>
        </div>
      )}

      {/* Submit */}
      <button type="submit" className="tp-btn-primary mt-1" disabled={submitting || success}>
        {submitting
          ? <><IconSpin /> {isRegister ? 'Creating account…' : 'Signing in…'}</>
          : success
            ? <><IconCheck /> Done</>
            : <>{isRegister ? 'Create account' : 'Sign in'} <IconArrow /></>
        }
      </button>

      {/* Switch mode hint */}
      <div className="text-center text-[13px]" style={{ color: 'var(--ink-soft)' }}>
        {isRegister ? (
          <>Already have an account?{' '}
            <button type="button" onClick={() => onModeChange('login')} className="tp-link font-medium" style={{ color: 'var(--ink)' }}>
              Sign in
            </button>
          </>
        ) : (
          <>New here?{' '}
            <button type="button" onClick={() => onModeChange('register')} className="tp-link font-medium" style={{ color: 'var(--ink)' }}>
              Create an account
            </button>
          </>
        )}
      </div>
    </form>
  );
}

window.AuthForm = AuthForm;
