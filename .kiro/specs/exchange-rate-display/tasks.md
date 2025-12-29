# Implementation Plan: Exchange Rate Display

## Overview

This implementation plan breaks down the fullstack exchange rate application into discrete, manageable coding tasks. Each task builds incrementally toward a complete solution that fetches CNB exchange rates, exposes them via .NET API, and displays them in an Angular frontend.

## Tasks

- [x] 1. Set up project structure and dependencies
  - Create .NET Web API project with required NuGet packages (Microsoft.AspNetCore.Cors, Microsoft.Extensions.Http)
  - Create Angular project with HttpClient and required dependencies
  - Set up solution structure with backend and frontend folders
  - Configure development environment settings
  - _Requirements: 2.6, 3.6_

- [x] 2. Implement CNB data integration
  - [x] 2.1 Create ExchangeRate and ExchangeRateResponse data models
    - Define C# models with proper data annotations
    - Include validation attributes for required fields
    - _Requirements: 1.5_

  - [x] 2.2 Add FsCheck property-based testing framework to backend
    - Add FsCheck NuGet package to test project
    - Configure property-based testing infrastructure
    - _Requirements: Testing framework setup_

  - [x] 2.3 Implement CnbClient for HTTP communication
    - Create HTTP client wrapper for CNB API calls
    - Configure timeout and retry policies
    - Use IHttpClientFactory for proper HTTP client management
    - _Requirements: 1.1, 1.2_

  - [x] 2.4 Implement ExchangeRateProvider with CNB data parsing
    - Parse CNB TXT format into structured objects
    - Handle date parsing and sequence number extraction
    - Implement data validation for required fields
    - _Requirements: 1.3, 1.5_

  - [x] 2.5 Write property test for CNB data parsing
    - **Property 2: CNB Data Parsing Round Trip**
    - **Validates: Requirements 1.3**

- [x] 3. Implement backend API layer
  - [x] 3.1 Create ExchangeRateController with REST endpoint
    - Implement GET /api/exchangerates endpoint
    - Configure proper HTTP status codes and response formats
    - Add API documentation attributes
    - _Requirements: 2.1, 2.2_

  - [x] 3.2 Implement ExchangeRateService business logic layer
    - Orchestrate data retrieval from ExchangeRateProvider
    - Handle business logic and data transformation
    - Implement dependency injection configuration
    - _Requirements: 2.3_

  - [x] 3.3 Write unit tests for API response format (Property 4 implemented)
    - **Property 4: API Response Format**
    - **Validates: Requirements 2.1, 2.2**

  - [x] 3.4 Configure CORS for Angular frontend access
    - Add CORS middleware configuration
    - Configure allowed origins, methods, and headers
    - _Requirements: 2.5_

  - [ ]* 3.5 Write property test for CORS configuration
    - **Property 5: CORS Configuration**
    - **Validates: Requirements 2.5**

- [x] 4. Implement comprehensive error handling
  - [x] 4.1 Add error handling to ExchangeRateProvider
    - Handle CNB API unavailability with graceful degradation
    - Implement proper exception types and logging
    - _Requirements: 1.4, 5.2_

  - [x] 4.2 Add error handling to API controller
    - Return appropriate HTTP status codes (503 for CNB unavailability)
    - Implement global exception handling middleware
    - _Requirements: 2.4, 5.1, 5.4_

  - [ ]* 4.3 Write property test for error handling
    - **Property 3: Error Handling Consistency**
    - **Validates: Requirements 1.4, 2.4, 5.1, 5.2, 5.4**

- [x] 5. Checkpoint - Backend API complete
  - Ensure all backend tests pass, verify API endpoints work correctly

- [x] 6. Implement Angular frontend structure
  - [x] 6.1 Create ExchangeRate interface and models
    - Define TypeScript interfaces matching backend models
    - Create proper type definitions for API responses
    - _Requirements: 3.3_

  - [x] 6.2 Implement ExchangeRateService for HTTP communication
    - Create Angular service with HttpClient integration
    - Configure API base URL from environment configuration
    - Implement error handling and retry logic
    - _Requirements: 3.2, 4.2_

  - [x] 6.3 Add fast-check property-based testing framework to frontend
    - Add fast-check npm package to Angular project
    - Configure property-based testing with Vitest
    - _Requirements: Testing framework setup_

  - [ ]* 6.4 Write property test for HTTP integration
    - **Property 7: Frontend HTTP Integration**
    - **Validates: Requirements 3.2**

- [x] 7. Implement frontend UI components
  - [x] 7.1 Create ExchangeRateComponent with table display
    - Implement clean, readable table layout for exchange rates
    - Display all required fields (country, currency, amount, code, rate)
    - Add responsive design for mobile compatibility
    - _Requirements: 3.1, 3.3_

  - [ ]* 7.2 Write property test for data display
    - **Property 6: Frontend Data Display**
    - **Validates: Requirements 3.1, 3.3**

  - [x] 7.3 Implement loading states and error handling UI
    - Add loading spinner during API calls
    - Display user-friendly error messages for different failure scenarios
    - Implement retry functionality for failed requests
    - _Requirements: 3.4, 3.5, 5.3, 5.5_

  - [ ]* 7.4 Write property test for UI state management
    - **Property 8: UI State Management**
    - **Validates: Requirements 3.4, 3.5, 5.3, 5.5**

- [x] 8. Implement configuration management
  - [x] 8.1 Configure backend environment settings
    - Set up appsettings.json for different environments
    - Use IConfiguration for CNB API URL configuration
    - Remove any hardcoded URLs from source code
    - _Requirements: 4.1, 4.4_

  - [x] 8.2 Configure frontend environment settings
    - Set up environment.ts files for different environments
    - Configure API base URL through environment variables
    - _Requirements: 4.2, 4.3_

  - [ ]* 8.3 Write property test for configuration management
    - **Property 9: Configuration Management**
    - **Validates: Requirements 4.1, 4.2, 4.3, 4.4**

- [x] 9. Implement REST API conventions
  - [x] 9.1 Ensure proper REST endpoint design
    - Verify HTTP verbs, resource naming, and status codes
    - Add proper API versioning if needed
    - Implement consistent response formats
    - _Requirements: 6.2_

  - [ ]* 9.2 Write property test for REST conventions
    - **Property 10: REST API Conventions**
    - **Validates: Requirements 6.2**

- [x] 10. Integration and final testing
  - [x] 10.1 Wire frontend and backend together
    - Test end-to-end functionality with real CNB data
    - Verify CORS configuration works correctly
    - Test error scenarios and recovery
    - _Requirements: All integration requirements_

  - [ ]* 10.2 Write integration tests
    - Test complete data flow from CNB to frontend display
    - Verify error handling across the entire stack
    - _Requirements: All requirements_

- [x] 11. Documentation and deployment preparation
  - [x] 11.1 Create comprehensive README
    - Document build and run instructions for both projects
    - Include prerequisites and dependencies
    - Document configuration options and environment variables
    - Include troubleshooting guide
    - _Requirements: 7.1, 7.2, 7.3, 7.5_

  - [x] 11.2 Prepare for deployment
    - Ensure projects build and run in clean environments
    - Test deployment scripts and procedures
    - Verify all configuration is externalized
    - _Requirements: 7.4_

- [x] 12. Final checkpoint - Complete system verification
  - Ensure all tests pass, verify complete functionality, ask user if questions arise

## Remaining Optional Tasks

The following property-based testing tasks remain optional and can be implemented for enhanced test coverage:

- [ ]* 2.5 Write property test for CNB data parsing (Property 2)
- [ ]* 3.5 Write property test for CORS configuration (Property 5)
- [ ]* 4.3 Write property test for error handling (Property 3)
- [ ]* 6.4 Write property test for HTTP integration (Property 7)
- [ ]* 7.2 Write property test for data display (Property 6)
- [ ]* 7.4 Write property test for UI state management (Property 8)
- [ ]* 8.3 Write property test for configuration management (Property 9)
- [ ]* 9.2 Write property test for REST conventions (Property 10)
- [ ]* 10.2 Write integration tests

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Property tests validate universal correctness properties with minimum 100 iterations
- Unit tests validate specific examples and edge cases
- Integration tests verify end-to-end functionality
- Configuration management ensures no hardcoded values in production code
- The core application is fully functional - remaining tasks focus on enhanced testing coverage