import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  getHromada, HromadaDetail as HromadaDetailType, DataSource,
  approveSource, rejectSource, addSource, runDiscovery, DiscoveryRun
} from '../api/client';
import Topbar from '../components/Topbar';

type Tab = 'active' | 'pending' | 'rejected';

function statusLabel(s: number) {
  if (s === 1) return <span className="badge active">Active</span>;
  if (s === 2) return <span className="badge rejected">Rejected</span>;
  return <span className="badge pending">Pending</span>;
}

function originLabel(o: number) {
  return o === 1
    ? <span className="badge ai">AI</span>
    : <span style={{ fontSize: 12, color: 'var(--muted)' }}>Manual</span>;
}

export default function HromadaDetail() {
  const { id } = useParams<{ id: string }>();
  const [hromada, setHromada] = useState<HromadaDetailType | null>(null);
  const [tab, setTab] = useState<Tab>('pending');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [discovering, setDiscovering] = useState(false);
  const [discoveryResult, setDiscoveryResult] = useState<DiscoveryRun | null>(null);
  const [discoveryError, setDiscoveryError] = useState('');
  const [addUrl, setAddUrl] = useState('');
  const [addDesc, setAddDesc] = useState('');
  const [addError, setAddError] = useState('');
  const [addSuccess, setAddSuccess] = useState('');
  const [adding, setAdding] = useState(false);

  async function load() {
    if (!id) return;
    setLoading(true);
    setError('');
    try {
      setHromada(await getHromada(id));
    } catch {
      setError('Failed to load hromada');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { load(); }, [id]);

  async function handleDiscover() {
    if (!id) return;
    setDiscovering(true);
    setDiscoveryError('');
    setDiscoveryResult(null);
    try {
      const run = await runDiscovery(id);
      setDiscoveryResult(run);
      await load(); // refresh sources
      setTab('pending');
    } catch (e: unknown) {
      setDiscoveryError('Discovery failed. Check the server logs.');
    } finally {
      setDiscovering(false);
    }
  }

  async function handleApprove(source: DataSource) {
    try {
      await approveSource(source.id, source.rowVersion);
      await load();
    } catch {
      alert('Failed to approve — refresh and try again.');
    }
  }

  async function handleReject(source: DataSource) {
    try {
      await rejectSource(source.id, source.rowVersion);
      await load();
    } catch {
      alert('Failed to reject — refresh and try again.');
    }
  }

  async function handleAddUrl(e: React.FormEvent) {
    e.preventDefault();
    if (!id) return;
    setAdding(true);
    setAddError('');
    setAddSuccess('');
    try {
      await addSource(id, addUrl, addDesc);
      setAddSuccess(`Source added: ${addUrl}`);
      setAddUrl('');
      setAddDesc('');
      await load();
      setTab('active');
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { errors?: Record<string, string[]> } } })?.response?.data?.errors;
      setAddError(msg ? JSON.stringify(msg) : 'Failed to add URL. Make sure it is a valid .gov.ua address.');
    } finally {
      setAdding(false);
    }
  }

  const filtered = (hromada?.sources ?? []).filter(s => {
    if (tab === 'active') return s.status === 1;
    if (tab === 'pending') return s.status === 0;
    return s.status === 2;
  });

  const counts = {
    active: hromada?.sources.filter(s => s.status === 1).length ?? 0,
    pending: hromada?.sources.filter(s => s.status === 0).length ?? 0,
    rejected: hromada?.sources.filter(s => s.status === 2).length ?? 0,
  };

  return (
    <>
      <Topbar />
      <div className="layout">
        <div className="breadcrumb">
          <Link to="/admin/oblasts">Oblasts</Link>
          <span>›</span>
          {hromada && <Link to={`/admin/oblasts/${hromada.oblastId}`}>{hromada.oblastName}</Link>}
          <span>›</span>
          <span>{hromada?.name ?? id}</span>
        </div>

        {loading && <div className="spinner" style={{ marginTop: '2rem' }} />}
        {error && <div className="error-msg">{error}</div>}

        {hromada && (
          <>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1.25rem' }}>
              <h2 style={{ margin: 0 }}>{hromada.name}</h2>
              <span style={{ color: 'var(--muted)', fontSize: 15 }}>{hromada.nameUk}</span>
            </div>

            {/* AI Discovery */}
            <div className="card">
              <h3 style={{ marginBottom: '0.75rem' }}>AI Source Discovery</h3>
              <p style={{ color: 'var(--muted)', margin: '0 0 0.75rem' }}>
                Asks Claude to search for official .gov.ua pages publishing shelter data for this hromada.
              </p>
              <button
                className="btn primary"
                onClick={handleDiscover}
                disabled={discovering}
              >
                {discovering ? <><span className="spinner" style={{ borderTopColor: '#fff' }} /> Searching…</> : '🔍 Find sources with AI'}
              </button>
              {discoveryError && <div className="error-msg" style={{ marginTop: '0.5rem' }}>{discoveryError}</div>}
              {discoveryResult && (
                <div className="success-msg" style={{ marginTop: '0.5rem' }}>
                  Discovery complete — found {discoveryResult.candidatesFound} candidates,
                  added {discoveryResult.candidatesInserted} new pending sources.
                </div>
              )}
            </div>

            {/* Add URL manually */}
            <div className="card">
              <h3 style={{ marginBottom: '0.75rem' }}>Add URL manually</h3>
              <form onSubmit={handleAddUrl}>
                <div className="grid-2">
                  <div className="form-row" style={{ margin: 0 }}>
                    <label>URL (.gov.ua only)</label>
                    <input
                      className="input"
                      type="url"
                      placeholder="https://example.gov.ua/shelters"
                      value={addUrl}
                      onChange={e => setAddUrl(e.target.value)}
                      required
                    />
                  </div>
                  <div className="form-row" style={{ margin: 0 }}>
                    <label>Description (optional)</label>
                    <input
                      className="input"
                      type="text"
                      placeholder="Shelter list page"
                      value={addDesc}
                      onChange={e => setAddDesc(e.target.value)}
                    />
                  </div>
                </div>
                <div style={{ marginTop: '0.75rem' }}>
                  <button className="btn primary" type="submit" disabled={adding}>
                    {adding ? <span className="spinner" style={{ borderTopColor: '#fff' }} /> : 'Add'}
                  </button>
                </div>
                {addError && <div className="error-msg">{addError}</div>}
                {addSuccess && <div className="success-msg">{addSuccess}</div>}
              </form>
            </div>

            {/* Sources table */}
            <div className="card" style={{ padding: 0 }}>
              <div style={{ padding: '0.75rem 1rem 0' }}>
                <div className="tab-bar">
                  {(['pending', 'active', 'rejected'] as Tab[]).map(t => (
                    <div
                      key={t}
                      className={`tab ${tab === t ? 'active' : ''}`}
                      onClick={() => setTab(t)}
                    >
                      {t.charAt(0).toUpperCase() + t.slice(1)} ({counts[t]})
                    </div>
                  ))}
                </div>
              </div>
              <table>
                <thead>
                  <tr>
                    <th style={{ width: '40%' }}>URL</th>
                    <th>Description</th>
                    <th>Origin</th>
                    <th>Status</th>
                    {(tab === 'pending') && <th>Actions</th>}
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(s => (
                    <tr key={s.id}>
                      <td style={{ wordBreak: 'break-all', maxWidth: 320 }}>
                        <a href={s.url} target="_blank" rel="noopener noreferrer">
                          {s.url}
                        </a>
                      </td>
                      <td style={{ color: 'var(--muted)', fontSize: 13 }}>{s.description || '—'}</td>
                      <td>{originLabel(s.origin)}</td>
                      <td>{statusLabel(s.status)}</td>
                      {tab === 'pending' && (
                        <td>
                          <div style={{ display: 'flex', gap: '0.4rem' }}>
                            <button className="btn success" onClick={() => handleApprove(s)}>✓ Approve</button>
                            <button className="btn danger" onClick={() => handleReject(s)}>✗ Reject</button>
                          </div>
                        </td>
                      )}
                    </tr>
                  ))}
                  {filtered.length === 0 && (
                    <tr>
                      <td colSpan={tab === 'pending' ? 5 : 4} style={{ color: 'var(--muted)', textAlign: 'center', padding: '1.5rem' }}>
                        No {tab} sources
                      </td>
                    </tr>
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
