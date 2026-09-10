import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import { defaultClient } from './api';
import App from './App.tsx';
import { ApiProvider } from './hooks';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ApiProvider value={defaultClient}>
      <App />
    </ApiProvider>
  </StrictMode>,
);
