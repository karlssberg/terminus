# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-10-22

### Added
- Initial release of Terminus framework
- Core abstractions:
  - `IEndpoint` interface for marking endpoint classes
  - `EndpointAttribute` for decorating endpoint methods
  - `EndpointMetadata` for storing endpoint information
- Endpoint discovery system:
  - `EndpointDiscovery` static class for discovering endpoints in assemblies and types
  - Support for scanning assemblies and specific types
- Endpoint registry:
  - `EndpointRegistry` for managing discovered endpoints
  - Support for registering endpoints from assemblies, types, or metadata
  - Query endpoints by name or tag
- Endpoint invocation:
  - `EndpointInvoker` for invoking endpoint methods
  - Support for both synchronous and asynchronous endpoints
  - Automatic handling of Task and Task<T> return types
- Features:
  - Custom endpoint names
  - Tag-based endpoint categorization
  - Thread-safe endpoint registry
  - Full XML documentation
- Comprehensive test suite with 23+ tests
- Sample application demonstrating all features
- Detailed README with examples and use cases
- CI/CD workflow with GitHub Actions
