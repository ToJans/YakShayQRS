using MinimalisticCQRS.Infrastructure;

namespace MinimalisticCQRS.Domain
{
    public class Account : AccountState
    {
        public Account() : base(null) { } // dirty hack required for Castle.DynamicProxy

        public Account(string AccountId) : base(AccountId) { }

        public void RegisterAccount(string OwnerName, string AccountNumber)
        {
            if (IsEnabled) return;
            AccountRegistered(OwnerName, AccountNumber);
        }

        public void DepositCash(decimal Amount)
        {
            Guard.Against(IsEnabled, "You can not deposit into an unregistered account");
            Guard.Against(Amount < 0, "You can not deposit an amount < 0");
            AmountDeposited(Amount);
        }

        public void WithdrawCash(decimal Amount)
        {
            Guard.Against(IsEnabled, "You can not withdraw from an unregistered account");
            Guard.Against(Amount < 0, "You can not withdraw an amount < 0");
            Guard.Against(Amount > Balance, "You can not withdraw an amount larger then the current balance");
            AmountWithdrawn(Amount);
        }

        public void TransferAmount(decimal Amount, string TargetAccountId)
        {
            Guard.Against(IsEnabled == false, "You can not transfer from an unregistered account");
            Guard.Against(Amount < 0, "You can not transfer an amount < 0");
            Guard.Against(Amount > Balance, "You can not transfer an amount larger then the current balance");
            AmountWithdrawn(Amount);
            TransferProcessedOnSource(Amount, TargetAccountId);
        }

        public void ProcessTransferOnTarget(decimal Amount, string SourceAccountId)
        {
            if (IsEnabled)
            {
                AmountDeposited(Amount);
                TransferCompleted(Amount, SourceAccountId);
            }
            else
            {
                TransferFailedOnTarget("You can not transfer to an unregistered account", Amount, SourceAccountId);
            }
        }

        public void CancelTransfer(string Reason, decimal Amount, string TargetAccountId)
        {
            AmountDeposited(Amount);
            TransferCanceled(Reason, Amount, TargetAccountId);
        }
    }

    public class AccountState
    {
        public string Id { get; private set; }

        public decimal Balance { get; private set; }

        public bool IsEnabled { get; private set; }

        public AccountState(string AccountId)
        {
            this.Id = AccountId;
        }

        public virtual void AccountRegistered(string OwnerName, string AccountNumber) { Balance = 0; IsEnabled = true; }

        public virtual void AmountDeposited(decimal Amount) { Balance += Amount; }

        public virtual void AmountWithdrawn(decimal Amount) { Balance -= Amount; }

        public virtual void TransferProcessedOnSource(decimal Amount, string TargetAccountId) { }

        public virtual void TransferCompleted(decimal Amount, string SourceAccountId) { }

        public virtual void TransferFailedOnTarget(string p, decimal Amount, string SourceAccountId) { }

        public virtual void TransferCanceled(string Reason, decimal Amount, string TargetAccountId) { }
    }
}