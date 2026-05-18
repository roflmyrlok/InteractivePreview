import { useNavigate } from 'react-router-dom';
import { clearAuth, getAuth } from '../store/auth';

export default function Topbar() {
  const navigate = useNavigate();
  const { role } = getAuth();

  function handleLogout() {
    clearAuth();
    navigate('/admin/');
  }

  return (
    <div className="topbar">
      <h1>
        <a href="/admin/oblasts" style={{ color: 'inherit', textDecoration: 'none' }}>
          InteractiveMap Admin
        </a>
      </h1>
      <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
        {role && <span style={{ color: 'var(--muted)', fontSize: 13 }}>{role}</span>}
        <button className="btn" onClick={handleLogout}>Logout</button>
      </div>
    </div>
  );
}
