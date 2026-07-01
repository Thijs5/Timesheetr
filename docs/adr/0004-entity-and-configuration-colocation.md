# ADR-0004: Co-locate entities and EF Core configuration

- Status: Accepted
- Date: 2026-06-17

## Context

Timesheetr's data model evolves across multiple files and folders. In practice, an entity and its EF Core mapping are often changed together, but are not always physically close in the codebase.

This creates friction during development:

- Related behaviour is split across files, which slows navigation and review.
- Mapping changes can be missed when entity properties change.
- Team conventions are implicit instead of explicit.

Following Vertical Slice Architecture (VSA) principles, code that changes together should live together. We want stronger functional cohesion.

## Decision

For each database entity, keep the entity type and its EF Core configuration in the same file.

- Co-locate the entity class and its `IEntityTypeConfiguration<TEntity>` implementation.
- Prefer one file per entity aggregate root or table-mapped entity, with the configuration in the same file.
- Keep this convention for new entities and when touching existing entities.

## Consequences

### Positive

- Faster understanding: model shape and mapping are visible in one place.
- Better change safety: property and mapping updates happen together.
- Stronger VSA alignment through functional cohesion.
- Easier code reviews with less cross-file context switching.

### Trade-offs

- Some files become longer.
- Shared/common configuration patterns must be applied carefully to avoid duplication.

## Implementation notes

- Apply this rule to all new entities immediately.
- Migrate existing entities opportunistically when they are modified.
- Keep configuration class naming explicit, for example `<EntityName>Configuration`.
