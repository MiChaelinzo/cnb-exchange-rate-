/// <reference types="vitest" />
import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    include: ['src/**/*.{test,spec}.{js,mjs,cjs,ts,mts,cts,jsx,tsx}'],
    // Configure for property-based testing with fast-check
    testTimeout: 10000, // Increased timeout for property tests
    setupFiles: ['src/test-setup.ts'],
  },
});