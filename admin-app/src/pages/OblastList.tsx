import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getOblasts, OblastSummary } from '../api/client';
import Topbar from '../components/Topbar';

export default function OblastList() {
  const [oblasts, setOblasts] = useState<OblastSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    getOblasts()
      .then(setOblasts)
      .catch(() => setError('Failed to load oblasts'))
      .finally(() => setLoading(false));
  }, []);

  return (
    <>
      <Topbar />
      <div className="layout">
        <h2>Oblasts</h2>
        {loading && <div className="spinner" style={{ marginTop: '2rem' }} />}
        {error && <div className="error-msg">{error}</div>}
        {!loading && !error && (
          <div className="card" style={{ padding: 0 }}>
            <table>
              <thead>
                <tr>
                  <th>Code</th>
                  <th>Name</th>
                  <th>Status</th>
                  <th>Hromadas</th>
                  <th>Active Sources</th>
                </tr>
              </thead>
              <tbody>
                {oblasts.map(o => (
                  <tr key={o.id}>
                    <td style={{ color: 'var(--muted)', fontFamily: 'monospace' }}>{o.code}</td>
                    <td>
                      <Link to={`/admin/oblasts/${o.code}`}>{o.name}</Link>
                      <div style={{ fontSize: 12, color: 'var(--muted)' }}>{o.nameUk}</div>
                    </td>
                    <td>
                      {o.isOccupied
                        ? <span className="badge occupied">Occupied</span>
                        : <span className="badge active">Free</span>}
                    </td>
                    <td>{o.hromadaCount}</td>
                    <td>{o.activeSourceCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </>
  );
}
