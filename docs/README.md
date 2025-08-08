# Documentation Index

## Clean Architecture .NET 8 - Complete Documentation

Welcome to the comprehensive documentation for the Clean Architecture .NET 8 project. This documentation covers everything from getting started to advanced deployment scenarios.

## Documentation Overview

### Getting Started
- **[Main README](../README.md)** - Project overview, quick start, and feature highlights
- **[Development Guide](development.md)** - Complete developer onboarding and best practices
- **[Architecture Guide](architecture.md)** - In-depth architectural patterns and design decisions

### Operations & Deployment
- **[Deployment Guide](deployment.md)** - Local, Docker, and AWS ECS Fargate deployment
- **[Scripts Documentation](../scripts/README.md)** - Automation scripts and deployment tools

### Comprehensive Patterns Library
- **[/patterns/](patterns/)** - Complete collection of .NET 8 implementation patterns and best practices

## Quick Navigation

### For New Developers
1. Start with **[Main README](../README.md)** for project overview
2. Follow **[Development Guide](development.md)** for environment setup
3. Review **[Architecture Guide](architecture.md)** to understand the design

### For DevOps/Operations
1. Review **[Deployment Guide](deployment.md)** for infrastructure setup
2. Check **[Scripts Documentation](../scripts/README.md)** for automation tools
3. Reference **[Main README](../README.md)** for Docker and AWS commands

### For Architects/Technical Leads
1. Study **[Architecture Guide](architecture.md)** for design patterns
2. Explore **[Patterns Library](patterns/)** for comprehensive implementation guides
3. Check **[Development Guide](development.md)** for coding standards

## Document Purposes

### [Main README](../README.md)
**Purpose**: Project introduction and quick start guide
**Audience**: Everyone (developers, stakeholders, users)
**Contents**:
- Project overview and features
- Quick start instructions
- API endpoint documentation
- Technology stack overview
- Example usage and commands
- Contributing guidelines

### [Architecture Guide](architecture.md)
**Purpose**: Deep dive into architectural patterns and design decisions
**Audience**: Senior developers, architects, technical leads
**Contents**:
- Clean Architecture layer breakdown
- Design patterns and implementations
- Domain-driven design concepts
- CQRS and MediatR usage
- Data flow and dependency management
- Performance and security considerations
- References to comprehensive patterns library

### [Patterns Library](patterns/)
**Purpose**: Comprehensive implementation patterns and best practices
**Audience**: All developers, architects, technical leads
**Contents**:
- **[Clean Architecture Patterns](patterns/clean-architecture.md)** - Complete .NET 8 implementation guide
- **[Domain-Driven Design](patterns/domain-driven-design.md)** - DDD patterns with C# examples
- **[CQRS Patterns](patterns/cqrs-patterns.md)** - MediatR command/query implementation
- **[Testing Strategy](patterns/testing-strategy.md)** - Comprehensive testing approach
- **[Coding Standards](patterns/coding-standards.md)** - .NET 8 conventions and best practices
- **[AWS Logging Patterns](patterns/aws-logging-patterns.md)** - Environment-specific logging
- **[AWS JWT Security](patterns/aws-jwt-security.md)** - Cognito integration patterns
- **[Security Patterns](patterns/security-patterns.md)** - Authentication and data protection
- **[Shared Kernel Guide](patterns/shared-kernel-guide.md)** - Common utilities and base classes
- **[Implementation Roadmap](patterns/implementation-roadmap.md)** - Step-by-step development plan

### [Development Guide](development.md)
**Purpose**: Complete developer onboarding and daily workflow
**Audience**: Developers (all levels)
**Contents**:
- Environment setup and prerequisites
- Development workflow and commands
- Coding standards and best practices
- Testing guidelines and examples
- Common pitfalls and troubleshooting
- Performance and security guidelines

### [Deployment Guide](deployment.md)
**Purpose**: Comprehensive deployment instructions for all environments
**Audience**: DevOps engineers, developers, deployment teams
**Contents**:
- Local development deployment
- Docker containerization
- AWS ECS Fargate production deployment
- CI/CD pipeline examples
- Configuration management
- Monitoring and troubleshooting

### [Scripts Documentation](../scripts/README.md)
**Purpose**: Automation tools and deployment scripts
**Audience**: DevOps engineers, developers
**Contents**:
- Script descriptions and usage
- Environment setup automation
- AWS deployment scripts
- Local development tools
- Make target explanations

## Documentation Matrix

| Document | Audience | Setup | Development | Architecture | Deployment | Troubleshooting |
|----------|----------|-------|-------------|--------------|------------|-----------------|
| **Main README** | All | High | Medium | Low | Medium | Low |
| **Architecture Guide** | Technical | Low | Medium | High | Low | Medium |
| **Development Guide** | Developers | High | High | Medium | Low | High |
| **Deployment Guide** | DevOps | Medium | Low | Low | High | High |
| **Scripts README** | DevOps | High | Low | - | High | Medium |
| **Patterns Library** | All Technical | Medium | High | High | Medium | Medium |

## Document Relationships

```
Main README (Entry Point)
├── Development Guide (Developer Onboarding)
│   ├── Architecture Guide (Deep Technical Details)
│   └── Patterns Library (Implementation Guides)
├── Deployment Guide (Operations & Infrastructure)
│   ├── Scripts README (Automation Tools)
│   └── Patterns Library (AWS Logging & Security)
└── Patterns Library (Comprehensive Best Practices)
    ├── Core Architecture Patterns
    ├── Development & Testing Patterns
    └── Infrastructure & Security Patterns
```

## Getting Started Paths

### New Developer Path
```
1. Main README (overview) 
   → 2. Development Guide (setup) 
   → 3. Architecture Guide (understanding)
   → 4. Patterns Library (implementation)
   → 5. Start coding!
```

### DevOps Engineer Path  
```
1. Main README (overview) 
   → 2. Deployment Guide (infrastructure) 
   → 3. Patterns/AWS Logging & Security
   → 4. Scripts README (automation) 
   → 5. Deploy!
```

### Technical Lead Path
```
1. Architecture Guide (design patterns) 
   → 2. Patterns Library (comprehensive guides)
   → 3. Development Guide (standards) 
   → 4. Plan implementation!
```

### Troubleshooting Path
```
1. Main README (quick reference) 
   → 2. Development Guide (dev issues) 
   → 3. Deployment Guide (ops issues) 
   → 4. Scripts README (automation issues)
```

## Documentation Standards

### Consistency Guidelines
- **Consistent Structure**: All docs follow similar heading patterns
- **Cross-References**: Documents link to related sections
- **Code Examples**: Real, tested code snippets throughout
- **Command Examples**: Copy-paste ready commands

### Maintenance Guidelines
- **Keep Updated**: Documentation updated with code changes
- **Version Alignment**: Docs reflect current .NET 8 implementation
- **Regular Review**: Quarterly documentation review cycles
- **User Feedback**: Incorporate feedback from developers and ops teams

## Quick Reference Commands

### Development
```bash
make setup-dev             # Complete development setup
make run-watch              # Start with hot reload
make test                   # Run all tests
make pre-commit             # Quality checks before commit
```

### Deployment  
```bash
make setup-dev              # Configure local development
make deploy                 # Full AWS deployment
make docker-run            # Run in Docker
make troubleshoot          # Environment diagnostics
```

### Documentation
```bash
# Key file locations
docs/README.md             # This index file
docs/architecture.md       # Technical architecture
docs/development.md        # Developer guide
docs/deployment.md         # Operations guide
docs/patterns/             # Comprehensive patterns library
README.md                  # Main project readme
scripts/README.md          # Automation scripts
```

## Support & Contributing

### Getting Help
1. **Check Documentation**: Review relevant guide above
2. **Run Diagnostics**: Use `make troubleshoot` for environment issues  
3. **Search Issues**: Check existing GitHub issues
4. **Ask Questions**: Create new issue with detailed context

### Contributing to Documentation
1. **Fork Repository**: Create your fork for changes
2. **Update Docs**: Make documentation improvements
3. **Test Examples**: Verify all code examples work
4. **Submit PR**: Create pull request with clear description
5. **Review Process**: Collaborate on feedback and improvements

### Documentation Checklist
When updating documentation:
- [ ] Code examples are tested and working
- [ ] Commands are copy-paste ready
- [ ] Cross-references are updated
- [ ] Consistent formatting and style
- [ ] Appropriate audience level
- [ ] Clear value proposition for readers

---

This documentation provides everything needed to understand, develop, deploy, and maintain the Clean Architecture .NET 8 project successfully.