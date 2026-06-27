// TODO IDEMPOTENCY:
// Any future payment command consumer must treat RabbitMQ/Kafka delivery as at-least-once.
// Use InboxMessage or a payment idempotency table plus unique constraints before acknowledging/committing messages.
//
// TODO RECONCILIATION:
// Add dead-letter/retry monitoring and provider reconciliation before payment events drive order status.
