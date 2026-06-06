# Migration Plan

## Shared Persistence

- Keep `ECommerceDbContext` in shared persistence until the database split phase.
- Keep service EF configuration in shared persistence while `ECommerceDbContext` is shared.
- Keep shared outbox and background job registration in `ECommerce.Infrastructure.BackgroundJobs` until worker composition is fully split.

## Service Infrastructure

- Move service-specific cache, storage, security, persistence, outbox, inbox, and messaging registrations into each service Infrastructure project when service hosts are independently composed.
- Keep blob storage, Redis cache, security, email, Kafka, RabbitMQ, and payment provider registrations shared until they are safely owned by service hosts.

## Future Runtime Work

- Move cart cleanup jobs into Cart Worker after worker composition is split.
- Add analytics-specific producers/consumers once tracking events are defined.
- Add notification email/SMS/push providers and message consumers when notification use cases are introduced.
- Add inventory inbox/consumer registrations as inventory reservation messaging matures.
- Add payment provider registration to Payment Infrastructure when Payment WebApi/Worker becomes independently composed.
