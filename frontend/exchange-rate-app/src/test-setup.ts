import 'zone.js/testing';
import { getTestBed } from '@angular/core/testing';
import {
  BrowserDynamicTestingModule,
  platformBrowserDynamicTesting,
} from '@angular/platform-browser-dynamic/testing';
import * as fc from 'fast-check';

// Initialize the Angular testing environment.
getTestBed().initTestEnvironment(
  BrowserDynamicTestingModule,
  platformBrowserDynamicTesting(),
);

// Configure fast-check for property-based testing
// Set default number of runs for property tests (minimum 100 as per design requirements)
fc.configureGlobal({
  numRuns: 100, // Minimum 100 iterations per property test as specified in design
  verbose: true, // Enable verbose output for debugging
});