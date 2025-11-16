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
    <AuthProvider onSigninCallback={signinCallback}>
      <App />
    </AuthProvider>
  </StrictMode>,
);
