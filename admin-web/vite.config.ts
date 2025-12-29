import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  // Must match Nginx location prefix: https://bocail.com/admin/
  base: '/admin/',
  build: {
    // Build output goes to the website directory served by Baota/Nginx.
    outDir: '../admin',
    emptyOutDir: true
  },
  server: {
    // During local dev on server, proxy API to backend.
    proxy: {
      '/api': 'http://127.0.0.1:3001'
    }
  }
})


