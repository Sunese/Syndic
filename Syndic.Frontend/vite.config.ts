import tailwindcss from '@tailwindcss/vite';
import { sveltekit } from '@sveltejs/kit/vite';
import viteBasicSslPlugin from '@vitejs/plugin-basic-ssl';
import { defineConfig, loadEnv } from 'vite';

export default ({ mode }: { mode: string }) => {

  return defineConfig({
    plugins: [
      tailwindcss(),
      sveltekit()
    ]
  });
}
