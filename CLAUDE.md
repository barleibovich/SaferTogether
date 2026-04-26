# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**SaferTogether** is a university team project (Ben-Gurion University, Computer Science Department). The project focuses on [details to be filled in as the project develops].

**Current Status:** Documentation and planning phase. The repository currently contains project documentation files. Actual implementation will follow.

## Repository Structure (To Be Established)

As the project develops, the team should establish a clear structure. Recommended approach:

- `/src` or `/app` — Main application source code
- `/tests` — Test files (unit, integration, etc.)
- `/docs` — Additional documentation
- `/.github/workflows` — CI/CD pipelines (if using GitHub Actions)
- `package.json` / `requirements.txt` / equivalent — Dependency management
- `.env.example` — Example environment variables

## Development Commands (To Be Added)

Once the tech stack is chosen, document:
- How to install dependencies
- How to run the application
- How to run tests (all tests, single test)
- How to lint/format code
- How to build for production
- How to run the development server

## Architecture and Design Decisions

As development begins:
- Document the technology stack (language, framework, database, etc.)
- Explain the high-level architecture (frontend/backend split, API design, data flow)
- Note any major design decisions and their rationale
- List any external APIs or services used

## Team Information

- **Team Members:** Avner, Shimon, Bar, Leibovich
- **Advisor:** PhD. Moshe Sulami
- **University:** Ben-Gurion University

## Git Workflow

- Default branch: `main`
- Create feature branches for new work: `git checkout -b feature/description`
- Commit messages should be clear and descriptive
- Keep the repository clean — avoid committing build artifacts, node_modules, or local config files

## Notes for Future Claude Instances

When substantial code is added to this repository:
1. Update this file with actual build/test/lint commands
2. Document the chosen tech stack and why
3. Explain the code organization and architecture
4. Add any special setup steps or dependencies
5. Note any common gotchas or important architectural patterns
