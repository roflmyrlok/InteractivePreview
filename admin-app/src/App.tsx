import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { getAuth, isAdminRole } from './store/auth';
import Login from './pages/Login';
import OblastList from './pages/OblastList';
import OblastDetail from './pages/OblastDetail';
import HromadaDetail from './pages/HromadaDetail';

function RequireAdmin({ children }: { children: React.ReactNode }) {
  const { token, role } = getAuth();
  if (!token || !isAdminRole(role)) return <Navigate to="/admin/" replace />;
  return <>{children}</>;
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/admin/" element={<Login />} />
        <Route path="/admin/oblasts" element={<RequireAdmin><OblastList /></RequireAdmin>} />
        <Route path="/admin/oblasts/:code" element={<RequireAdmin><OblastDetail /></RequireAdmin>} />
        <Route path="/admin/hromadas/:id" element={<RequireAdmin><HromadaDetail /></RequireAdmin>} />
        <Route path="*" element={<Navigate to="/admin/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
