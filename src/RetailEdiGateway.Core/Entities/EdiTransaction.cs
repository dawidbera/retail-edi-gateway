using System;

namespace RetailEdiGateway.Core.Entities
{
    /// <summary>
    /// Represents an electronic data interchange (EDI) transaction log for outbound/inbound payloads.
    /// </summary>
    public class EdiTransaction
    {
        /// <summary>
        /// Unique identifier for the EDI transaction log.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Optional reference to the associated purchase order.
        /// </summary>
        public Guid? PurchaseOrderId { get; set; }

        /// <summary>
        /// Navigation property to the associated purchase order.
        /// </summary>
        public PurchaseOrder? PurchaseOrder { get; set; }

        /// <summary>
        /// Type of message exchanged.
        /// </summary>
        public EdiMessageType MessageType { get; set; }

        /// <summary>
        /// Transaction direction.
        /// </summary>
        public EdiDirection Direction { get; set; }

        /// <summary>
        /// Raw message payload content (EDIFACT text or XML).
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// Transaction processing status.
        /// </summary>
        public EdiTransactionStatus Status { get; set; } = EdiTransactionStatus.Pending;

        /// <summary>
        /// Timestamp when the message was processed.
        /// </summary>
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The number of transmission attempts made for outbound transactions.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Optional error message stored if processing or transmission failed.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Represents the types of EDI messages supported by the gateway.
    /// </summary>
    public enum EdiMessageType
    {
        /// <summary>
        /// Purchase Order message sent to a supplier.
        /// </summary>
        Orders,

        /// <summary>
        /// Order Response message received from a supplier.
        /// </summary>
        Ordrsp,

        /// <summary>
        /// Despatch Advice (Advanced Shipping Notice) received from a supplier.
        /// </summary>
        Desadv,

        /// <summary>
        /// Unknown or unsupported message type.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Represents the flow direction of an EDI transaction.
    /// </summary>
    public enum EdiDirection
    {
        /// <summary>
        /// Message received from an external partner.
        /// </summary>
        Inbound,

        /// <summary>
        /// Message sent to an external partner.
        /// </summary>
        Outbound
    }

    /// <summary>
    /// Represents the processing status of an EDI transaction.
    /// </summary>
    public enum EdiTransactionStatus
    {
        /// <summary>
        /// The transaction is pending processing or transmission.
        /// </summary>
        Pending,

        /// <summary>
        /// The transaction was processed or transmitted successfully.
        /// </summary>
        Success,

        /// <summary>
        /// The transaction processing or transmission failed.
        /// </summary>
        Failed
    }
}
