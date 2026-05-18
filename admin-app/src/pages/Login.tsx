import { useState, FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { login } from '../api/client';
import { setAuth, extractRoleFromToken, isAdminRole, getAuth } from '../store/auth';

export default function Login() {
  const navigate = useNavigate();
  const { token, role } = getAuth();

  // Already logged in → redirect
  if (token && isAdminRole(role)) {
    navigate('/admin/oblasts', { replace: true });
    return null;
  }

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const { token: jwt } = await login(username, password);
      const role = extractRoleFromToken(jwt);
      if (!isAdminRole(role)) {
        setError('Access denied — Admin or SuperAdmin role required.');
        return;
      }
      setAuth(jwt, role!);
      navigate('/admin/oblasts', { replace: true });
    } catch (err: unknown) {
      setError('Invalid username or password.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      minHeight: '100vh', background: 'var(--gray-bg)'
    }}>
      <div className="card" style={{ width: 360, padding: '2rem' }}>
        <h2 style={{ marginBottom: '1.5rem', textAlign: 'center' }}>InteractiveMap Admin</h2>
        <form onSubmit={handleSubmit}>
          <div className="form-row">
            <label>Username</label>
            <input
              className="input"
              type="text"
              value={username}
              onChange={e => setUsername(e.target.value)}
              required
              autoFocus
            />
          </div>
          <div className="form-row">
            <label>Password</label>
            <input
              className="input"
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
            />
          </div>
          {error && <div className="error-msg">{error}</div>}
          <button
            className="btn primary"
            type="submit"
            disabled={loading}
            style={{ width: '100%', marginTop: '1rem', justifyContent: 'center' }}
          >
            {loading ? <span className="spinner" /> : 'Sign in'}
          </button>
        </form>
      </div>
    </div>
  );
}
