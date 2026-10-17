import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Served by QDMS.Host under /ui. In dev (npm run dev) the API calls are proxied to a running QDMS on 5559.
const qdms = process.env.QDMS_URL ?? 'http://localhost:5559';
const apiPaths = ['/api', '/instruments', '/exchanges', '/sessiontemplates', '/tags', '/underlyingsymbols', '/datasources', '/jobs',
  '/dividends', '/earningsannouncements', '/economicreleases'];

export default defineConfig({
  base: '/ui/',
  plugins: [react()],
  build: {
    outDir: '../QDMS.Host/wwwroot/ui',
    emptyOutDir: true,
    chunkSizeWarningLimit: 2000,
  },
  server: {
    port: 5173,
    proxy: Object.fromEntries(apiPaths.map((p) => [p, { target: qdms, changeOrigin: true }])),
  },
});
