import { defineConfig } from 'vite';

export default defineConfig({
  server: {
    host: '127.0.0.1',
    port: 5173,
    strictPort: true,
    allowedHosts: ['127.0.0.1'],
    proxy: { '/api': { target: 'http://127.0.0.1:5180', changeOrigin: true } },
  },
  build: { sourcemap: false },
});
