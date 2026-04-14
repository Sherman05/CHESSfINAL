import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { viteSingleFile } from 'vite-plugin-singlefile'

// Build into a SINGLE HTML file — no server needed, just open in browser
export default defineConfig({
  plugins: [react(), viteSingleFile()],
  build: {
    outDir: '../dist_web',
  },
})
