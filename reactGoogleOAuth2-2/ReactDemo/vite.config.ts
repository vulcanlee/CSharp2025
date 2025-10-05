import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import fs from 'fs'
import path from 'path'

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    server: {
        host: true,       // 讓外部/局域網也能連(可選)
        port: 49158,
        https: {
            key: fs.readFileSync(path.resolve('localhost-key.pem')),
            cert: fs.readFileSync(path.resolve('localhost.pem')),
        },
    }
    
})
