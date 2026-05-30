import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Build output goes straight into the API's wwwroot so the .NET backend serves the SPA same-origin
// (one container, one URL, no CORS). `npm run dev` proxies API calls to the locally running backend.
export default defineConfig({
  plugins: [react()],
  build: {
    outDir: '../engine/SeniorSharp.Api/wwwroot',
    emptyOutDir: true,
  },
  server: {
    proxy: {
      '/sessions': 'http://localhost:5000',
      '/voice': 'http://localhost:5000',
      '/health': 'http://localhost:5000',
    },
  },
})
