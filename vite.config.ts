import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig, loadEnv } from 'vite';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'VITE_');

  return {
    plugins: [react(), tailwindcss()],
    build: {
      manifest: true,
    },
    envPrefix: ['VITE_', 'WISH_'],
    server: {
      port: Number(env.VITE_PORT || 5173), // It broke
      strictPort: true,
    },
  };
});
