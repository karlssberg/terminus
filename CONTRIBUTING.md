# Contributing to Terminus

Thank you for your interest in contributing to Terminus! This document provides guidelines and instructions for contributing.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/YOUR_USERNAME/terminus.git`
3. Create a feature branch: `git checkout -b feature/your-feature-name`
4. Make your changes
5. Submit a pull request

## Development Setup

### Prerequisites

- .NET 8.0 SDK or higher
- Git

### Building the Project

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Running the Sample

```bash
cd samples/Terminus.Sample
dotnet run
```

## Coding Guidelines

- Follow existing code style and conventions
- Write clear, descriptive commit messages
- Add XML documentation for public APIs
- Write tests for new features
- Ensure all tests pass before submitting a PR
- Keep changes focused and atomic

## Testing Guidelines

- Write unit tests for new functionality
- Ensure tests are deterministic and don't depend on external resources
- Use descriptive test names that explain what is being tested
- Follow the Arrange-Act-Assert pattern
- Aim for good code coverage

## Pull Request Process

1. Update documentation if you're changing functionality
2. Add tests for new features
3. Ensure all tests pass
4. Update CHANGELOG.md with your changes
5. Submit your pull request with a clear description of the changes

## Code of Conduct

- Be respectful and inclusive
- Welcome newcomers and help them get started
- Focus on constructive criticism
- Assume good intentions

## Questions?

Feel free to open an issue if you have questions or need clarification.
