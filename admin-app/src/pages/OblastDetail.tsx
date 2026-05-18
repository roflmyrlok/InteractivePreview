import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { getOblast, OblastDetail as OblastDetailType } from '../api/client';
import Topbar from '../components/Topbar';

export default function OblastDetail() {
  const { code } = useParams<{ code: string }>();
  const [oblast, setOblast] = useState<OblastDetailType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');

  useEffect(() => {
    if (!code) return;
    getOblast(code)
      .then(setOblast)
      .catch(() => setError('Failed to load oblast'))
      .finally(() => setLoading(false));
  }, [code]);

  const filtered = oblast?.hromadas.filter(h =>
    h.name.toLowerCase().includes(search.toLowerCase()) ||
    h.nameUk.toLowerCase().includes(search.toLowerCase())
  ) ?? [];

  return (
    <>
      <Topbar />
      <div className="layout">
        <div className="breadcrumb">
          <Link to="/admin/oblasts">Oblasts</Link>
          <span>›</span>
          <span>{oblast?.name ?? code}</span>
        </div>

        {loading && <div className="spinner" style={{ marginTop: '2rem' }} />}
        {error && <div className="error-msg">{error}</div>}

        {oblast && (
          <>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1rem' }}>
              <h2 style={{ margin: 0 }}>{oblast.name}</h2>
              <span style={{ color: 'var(--muted)', fontSize: 15 }}>{oblast.nameUk}</span>
              {oblast.isOccupied && <span className="badge occupied">Occupied</span>}
            </div>

            <div className="card">
              <input
                className="input"
                placeholder="Search hromadas…"
                value={search}
                onChange={e => setSearch(e.target.value)}
                style={{ maxWidth: 320 }}
              />
            </div>

            <div className="card" style={{ padding: 0 }}>
              <table>
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Slug</th>
                    <th>Active</th>
                    <th>Pending</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(h => (
                    <tr key={h.id}>
                      <td>
                        <Link to={`/admin/hromadas/${h.id}`}>{h.name}</Link>
                        <div style={{ fontSize: 12, color: 'var(--muted)' }}>{h.nameUk}</div>
                      </td>
                      <td style={{ fontFamily: 'monospace', fontSize: 12 }}>{h.slug}</td>
                      <td>
                        {h.activeSourceCount > 0
                          ? <span className="badge active">{h.activeSourceCount}</span>
                          : <span style={{ color: 'var(--muted)' }}>0</span>}
                      </td>
                      <td>
                        {h.pendingSourceCount > 0
                          ? <span className="badge pending">{h.pendingSourceCount}</span>
                          : <span style={{ color: 'var(--muted)' }}>0</span>}
                      </td>
                    </tr>
                  ))}
                  {filtered.length === 0 && (
                    <tr><td colSpan={4} style={{ color: 'var(--muted)', textAlign: 'center' }}>No hromadas found</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>
    </>
  );
}
