/// <reference types="vitest" />
import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    include: ['src/**/*.property.spec.ts'],
    // Configure for property-based testing with fast-check
    testTimeout: 15000, // Increased timeout for property tests
    // No setup files to avoid Zone.js issues
  },
});