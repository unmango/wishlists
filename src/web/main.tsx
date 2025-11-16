import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { AuthProvider } from 'react-oidc-context';
import './index.css';
import App from './App.tsx';

const signinCallback = (): void => {
  window.history.replaceState({}, document.title, window.location.pathname);
};

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider
      authority={import.meta.env.VITE_OIDC_AUTHORITY || 'http://localhost:8080'}
      scope='openid offline_access api'
      client_id='spa'
      redirect_uri={window.location.origin + '/callback/login/github'}
      response_type='code'
      onSigninCallback={signinCallback}
    >
      <App />
    </AuthProvider>
  </StrictMode>,
);
