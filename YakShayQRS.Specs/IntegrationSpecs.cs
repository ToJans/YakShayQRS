using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace YakShayQRS.Specs
{
    [TestClass]
    public class IntegrationSpecs
    {
        public class Account
        {
            protected virtual string AccountId { get; set; }

            bool IsRegistered = false;
            private Decimal Balance = 0;

            public void RegisterAccount(string OwnerName)
            {
                if (IsRegistered)
                    return;
                AccountRegistered(OwnerName);
            }

            public void DepositAmount(decimal Amount)
            {
                if (Amount <= 0)
                    TransactionCancelled("Deposit", "Your amount has to be positive");
                AmountDeposited(Amount);
            }

            public void WithdrawAmount(decimal Amount)
            {
                if (Amount <= 0)
                    TransactionCancelled("Withdraw", "Your amount has to be positive");
                if (Amount > Balance)
                    TransactionCancelled("Withdraw", "Your amount has to be smaller then the balance");
                AmountWithdrawn(Amount);
            }

            public void Transfer(string TargetAccountId, decimal Amount)
            {
                if (Amount <= 0)
                    TransactionCancelled("Transfer", "Your amount has to be positive");
                if (Amount > Balance)
                    TransactionCancelled("Transfer", "Your amount has to be smaller then the balance");
                TransferProcessedOnSource(TargetAccountId, Amount);
            }

            public void ProcessTransferOnTarget(string SourceAccountId, decimal Amount)
            {
                if (IsRegistered)
                    TransferProcessedOnTarget(SourceAccountId, Amount);
                else
                    TransferFailedOnTarget(SourceAccountId, Amount);
            }

            public void CancelTransferOnSource(string TargetAccountId, decimal Amount)
            {
                TransferCancelledOnSource(TargetAccountId, Amount);
            }

            protected virtual void AccountRegistered(string OwnerName) { IsRegistered = true; }

            protected virtual void AmountDeposited(decimal Amount) { Balance += Amount; }

            protected virtual void AmountWithdrawn(decimal Amount) { Balance -= Amount; }

            protected virtual void TransferProcessedOnSource(string TargetAccountId, decimal Amount) { Balance -= Amount; }

            protected virtual void TransferCancelledOnSource(string TargetAccountId, decimal Amount) { Balance += Amount; }

            protected virtual void TransferProcessedOnTarget(string SourceAccountId, decimal Amount) { Balance += Amount; }

            protected virtual void TransferFailedOnTarget(string SourceAccountId, decimal Amount) { Balance += Amount; }

            protected virtual void TransactionCancelled(string what, string reason) { }
        }

        public class AccountTransferSaga
        {
            public void TransferProcessedOnSource(string AccountId, string TargetAccountId, decimal Amount)
            {
                ProcessTransferOnTarget(TargetAccountId, AccountId, Amount);
            }

            public void TransferFailedOnTarget(string AccountId, string SourceAccountId, decimal Amount)
            {
                CancelTransferOnSource(SourceAccountId, AccountId, Amount);
            }

            protected virtual void ProcessTransferOnTarget(string AccountId, string SourceAccountId, decimal Amount) { }

            protected virtual void CancelTransferOnSource(string AccountId, string TargetAccountId, decimal Amount) { }
        }

        public class AccountBalances
        {
            public Dictionary<string, Decimal> Balances = new Dictionary<string, decimal>();

            public void AccountRegistered(string AccountId) { Balances.Add(AccountId, 0); }

            public void AmountDeposited(string AccountId, decimal Amount) { Balances[AccountId] += Amount; }

            public void AmountWithdrawn(string AccountId, decimal Amount) { Balances[AccountId] -= Amount; }

            public void TransferProcessedOnTarget(string AccountId, string SourceAccountId, decimal Amount)
            {
                Balances[AccountId] += Amount;
                Balances[SourceAccountId] -= Amount;
            }
        }

        [TestMethod]
        public void Deposits_and_withdraws_should_not_interfere_with_each_other()
        {
            var SUT = new YakShayBus();
            SUT.RegisterType<Account>();
            SUT.RegisterType<AccountTransferSaga>();
            SUT.RegisterType<AccountBalances>();

            var es = new EventQueue();

            SUT.HandleUntilAllConsumed(Message.FromAction(x => x.RegisterAccount(AccountId: "account/1", OwnerName: "Tom")), es.Add, es.Filter);
            SUT.HandleUntilAllConsumed(Message.FromAction(x => x.RegisterAccount(AccountId: "account/2", OwnerName: "Ben")), es.Add, es.Filter);
            SUT.HandleUntilAllConsumed(Message.FromAction(x => x.DepositAmount(AccountId: "account/1", Amount: 126m)), es.Add, es.Filter);
            SUT.HandleUntilAllConsumed(Message.FromAction(x => x.DepositAmount(AccountId: "account/2", Amount: 10m)), es.Add, es.Filter);
            SUT.HandleUntilAllConsumed(Message.FromAction(x => x.Transfer(AccountId: "account/1", TargetAccountId: "account/2", Amount: 26m)), es.Add, es.Filter);

            var bal = new AccountBalances();
            SUT.ApplyHistory(bal, es.Filter);
            bal.Balances.Count.ShouldBe(2);
            bal.Balances["account/1"].ShouldBe(100m);
            bal.Balances["account/2"].ShouldBe(36m);
        }
    }
}