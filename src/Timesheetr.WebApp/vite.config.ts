import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    proxy: {
      '/api': process.env['services__backend__http__0'] ?? 'http://localhost:5000',
      '/hubs': {
        target: process.env['services__backend__http__0'] ?? 'http://localhost:5000',
        ws: true,
      },
    },
  },
  define: {
    'import.meta.env.VITE_RABBITMQ_MANAGEMENT_URL': JSON.stringify(
      process.env['services__messaging__management__0'] ?? 'http://localhost:15672'
    ),
    'import.meta.env.VITE_RABBITMQ_USERNAME': JSON.stringify(
      process.env['VITE_RABBITMQ_USERNAME'] ?? 'admin'
    ),
    'import.meta.env.VITE_RABBITMQ_PASSWORD': JSON.stringify(
      process.env['VITE_RABBITMQ_PASSWORD'] ?? 'admin'
    ),
  },
})
