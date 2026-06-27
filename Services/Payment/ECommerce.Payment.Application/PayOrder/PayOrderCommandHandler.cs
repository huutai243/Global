// TODO IDEMPOTENCY:
// Implement PayOrder handling with durable idempotency before this flow is used in production.
// The handler should atomically persist Payment state, idempotency/inbox state, and any OutboxMessage.
//
// TODO LEDGER:
// Payment status alone is not a double-entry ledger.
// If this service manages money/balances, add LedgerAccount and LedgerEntry with debit/credit entries balanced per transaction.
//
// TODO RECONCILIATION:
// Add reconciliation for payment succeeded but order not paid, provider transaction exists but local payment is pending,
// and payment/outbox messages that are failed, dead-lettered, or never consumed.
