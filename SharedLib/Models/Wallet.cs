using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Models
{
    public class UserWallet
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        public string UserId { get; set; }
        public DateTime Created { get; set; }
        public decimal Funds { get; set; } = 0;
        public decimal Credits { get; set; } = 0;
        public Currency Currency { get; set; }
        /// <summary>
        /// Hidden from user, when creating a payment money is issued to a new temporary wallet
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>
        /// When a temporary wallet, the payment ID of the wallet the money belongs to
        /// </summary>
        public string BillId { get; set; }

        public string JobId { get; set; }
        public WalletPurpose Purpose { get; set; }


        //EXTRA META
        public decimal AvailableFunds { get; set; }
        public decimal OutgoingFunds { get; set; }
        public decimal IncomingFunds { get; set; }
    }

    public enum WalletPurpose
    {
        General = 0,
        Job = 10,
        Hold = 2
    }

    public class WalletTransaction
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        public ushort Priority { get; set; } = 1;
        public string UserId { get; set; }
        public string SenderWalletId { get; set; }
        public string SenderUsername { get; set; }
        public string ReceiverId { get; set; }
        public string ReceiverWalletId { get; set; }
        public string ReceiverUsername { get; set; }

        public decimal Amount { get; set; }
        public float TransactionFeeP { get; set; } = 0.02f;
        public Currency Currency { get; set; }

        public bool Completed { get; set; }
        public DateTime DateInitiated { get; set; }
        public DateTime? DateCompleted { get; set; }
        public string FailReason { get; set; }
        public TransactionStatus Status { get; set; }
        public TransactionType Type { get; set; }
    }

    public class TransactionAction
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string TransactionId { get; set; }
        public string UserId { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateCompleted { get; set; }
        public bool Executed { get; set; } = false;
    }

    public class PayoutTransactionAction : TransactionAction
    {
        public string WalletId { get; set; }
        public string ConnectAccountId { get; set; }

        public decimal Amount { get; set; }
        public Currency Currency { get; set; }

        public long? GetCents()
        {
            throw new NotImplementedException();
        }
    }

    public class PayoutToBank : TransactionAction
    {
        public string ActionId { get; set; }

        public string WalletId { get; set; }
        public string ConnectAccountId { get; set; }
        public decimal Amount { get; set; }
        public Currency Currency { get; set; }

        //public DateTime DateCreated { get; set; }
        //public DateTime? DateCompleted { get; set; }
        //public bool Executed { get; set; } = false;
    }

    public enum TransactionType
    {
        Transfer,
        Recharge,
        Payout
    }

    public enum TransactionStatus
    {
        Waiting,
        Processed,
        Failed,
    }

}
