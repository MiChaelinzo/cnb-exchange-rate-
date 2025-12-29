# Requirements Document

## Introduction

A fullstack application that retrieves exchange rate data from the Czech National Bank (CNB), exposes it through a .NET API, and displays it in an Angular web application. The system demonstrates real-world API integration, clean architecture, and modern web development practices.

## Glossary

- **CNB**: Czech National Bank - the central bank providing public exchange rate data
- **ExchangeRateProvider**: .NET class responsible for fetching and parsing CNB exchange rate data
- **Backend_API**: .NET REST API that exposes exchange rate endpoints
- **Frontend_App**: Angular web application that consumes the Backend_API
- **Exchange_Rate**: Currency conversion rate data including currency code, rate value, and amount

## Requirements

### Requirement 1: CNB Data Integration

**User Story:** As a system, I want to retrieve real exchange rate data from CNB's public API, so that I can provide accurate and up-to-date currency information.

#### Acceptance Criteria

1. THE ExchangeRateProvider SHALL identify and connect to a valid CNB public data source
2. WHEN the ExchangeRateProvider requests data, THE CNB_API SHALL return current exchange rates
3. THE ExchangeRateProvider SHALL parse the CNB response data into structured exchange rate objects
4. WHEN CNB data is unavailable, THE ExchangeRateProvider SHALL handle the error gracefully
5. THE ExchangeRateProvider SHALL validate that received data contains required fields (currency code, rate, amount)

### Requirement 2: Backend API Implementation

**User Story:** As a frontend developer, I want a .NET REST API endpoint for exchange rates, so that I can retrieve currency data for display.

#### Acceptance Criteria

1. THE Backend_API SHALL expose a REST endpoint that returns exchange rate data
2. WHEN a client requests exchange rates, THE Backend_API SHALL return data in JSON format
3. THE Backend_API SHALL use the ExchangeRateProvider to fetch current CNB data
4. WHEN the ExchangeRateProvider fails, THE Backend_API SHALL return appropriate HTTP error codes
5. THE Backend_API SHALL include CORS configuration to allow Angular frontend access
6. THE Backend_API SHALL be buildable and runnable in a standard .NET environment

### Requirement 3: Angular Frontend Implementation

**User Story:** As a user, I want to view current exchange rates in a web browser, so that I can see currency conversion information.

#### Acceptance Criteria

1. THE Frontend_App SHALL display exchange rates in a clean, readable table format
2. WHEN the Frontend_App loads, THE HttpClient SHALL fetch data from the Backend_API
3. THE Frontend_App SHALL display currency codes, exchange rates, and amounts clearly
4. WHEN data is loading, THE Frontend_App SHALL show a loading indicator
5. WHEN the Backend_API is unavailable, THE Frontend_App SHALL display an error message
6. THE Frontend_App SHALL be buildable and runnable using standard Angular CLI commands

### Requirement 4: Configuration Management

**User Story:** As a developer, I want configurable endpoints and settings, so that I can deploy the application in different environments.

#### Acceptance Criteria

1. THE Backend_API SHALL use configuration files or environment variables for CNB API URLs
2. THE Frontend_App SHALL use environment configuration for Backend_API URLs
3. WHERE different environments are used, THE system SHALL support environment-specific configurations
4. THE system SHALL NOT contain hardcoded URLs in the source code
5. THE configuration SHALL be documented in the README file

### Requirement 5: Error Handling and Resilience

**User Story:** As a user, I want the application to handle errors gracefully, so that I receive meaningful feedback when issues occur.

#### Acceptance Criteria

1. WHEN CNB API is unavailable, THE Backend_API SHALL return a 503 Service Unavailable status
2. WHEN invalid data is received, THE ExchangeRateProvider SHALL log the error and throw an appropriate exception
3. WHEN the Backend_API returns an error, THE Frontend_App SHALL display a user-friendly error message
4. THE Backend_API SHALL implement proper exception handling with appropriate HTTP status codes
5. THE Frontend_App SHALL handle network timeouts and connection errors gracefully

### Requirement 6: Code Quality and Architecture

**User Story:** As a developer, I want clean, maintainable code with proper separation of concerns, so that the application is easy to understand and extend.

#### Acceptance Criteria

1. THE ExchangeRateProvider SHALL be implemented as a separate class with single responsibility
2. THE Backend_API SHALL follow REST conventions for endpoint design
3. THE Frontend_App SHALL use Angular best practices including services for HTTP communication
4. THE codebase SHALL demonstrate clean architecture principles with proper layering
5. WHERE possible, THE system SHALL include dependency injection for loose coupling

### Requirement 7: Documentation and Deployment

**User Story:** As a developer, I want clear build and run instructions, so that I can set up and run the application locally.

#### Acceptance Criteria

1. THE repository SHALL include a README file with build instructions for both backend and frontend
2. THE README SHALL document any prerequisites and dependencies
3. THE README SHALL include step-by-step instructions to run the application locally
4. THE system SHALL be deployable using standard .NET and Angular tooling
5. THE documentation SHALL include any assumptions or design decisions made during development

### Requirement 8: Testing Strategy

**User Story:** As a developer, I want automated tests to verify functionality, so that I can ensure code quality and catch regressions.

#### Acceptance Criteria

1. WHERE time permits, THE Backend_API SHALL include unit tests for the ExchangeRateProvider
2. WHERE time permits, THE Frontend_App SHALL include unit tests for key components
3. THE tests SHALL verify error handling scenarios
4. THE tests SHALL validate data parsing and transformation logic
5. THE test suite SHALL be runnable using standard testing frameworks (.NET Test, Jasmine/Karma)