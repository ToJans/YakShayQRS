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
                OnAccountRegistered(OwnerName);
            }

            public void DepositAmount(decimal Amount)
            {
                if (Amount <= 0)
                    OnTransactionCancelled("Deposit", "Your amount has to be positive");
                OnAmountDeposited(Amount);
            }

            public void WithdrawAmount(decimal Amount)
            {
                if (Amount <= 0)
                    OnTransactionCancelled("Withdraw", "Your amount has to be positive");
                if (Amount > Balance)
                    OnTransactionCancelled("Withdraw", "Your amount has to be smaller then the balance");
                OnAmountWithdrawn(Amount);
            }

            public void Transfer(string TargetAccountId, decimal Amount)
            {
                if (Amount <= 0)
                    OnTransactionCancelled("Transfer", "Your amount has to be positive");
                if (Amount > Balance)
                    OnTransactionCancelled("Transfer", "Your amount has to be smaller then the balance");
                OnTransferProcessedOnSource(TargetAccountId, Amount);
            }

            public void ProcessTransferOnTarget(string SourceAccountId, decimal Amount)
            {
                if (IsRegistered)
                    OnTransferProcessedOnTarget(SourceAccountId, Amount);
                else
                    OnTransferFailedOnTarget(SourceAccountId, Amount);
            }

            public void CancelTransferOnSource(string TargetAccountId, decimal Amount)
            {
                OnTransferCancelledOnSource(TargetAccountId, Amount);
            }

            protected virtual void OnAccountRegistered(string OwnerName) { IsRegistered = true; }

            protected virtual void OnAmountDeposited(decimal Amount) { Balance += Amount; }

            protected virtual void OnAmountWithdrawn(decimal Amount) { Balance -= Amount; }

            protected virtual void OnTransferProcessedOnSource(string TargetAccountId, decimal Amount) { Balance -= Amount; }

            protected virtual void OnTransferCancelledOnSource(string TargetAccountId, decimal Amount) { Balance += Amount; }

            protected virtual void OnTransferProcessedOnTarget(string SourceAccountId, decimal Amount) { Balance += Amount; }

            protected virtual void OnTransferFailedOnTarget(string SourceAccountId, decimal Amount) { Balance += Amount; }

            protected virtual void OnTransactionCancelled(string what, string reason) { }
        }

        public class AccountTransferSaga
        {
            public void OnTransferProcessedOnSource(string AccountId, string TargetAccountId, decimal Amount)
            {
                ProcessTransferOnTarget(TargetAccountId, AccountId, Amount);
            }

            public void OnTransferFailedOnTarget(string AccountId, string SourceAccountId, decimal Amount)
            {
                CancelTransferOnSource(SourceAccountId, AccountId, Amount);
            }

            protected virtual void ProcessTransferOnTarget(string AccountId, string SourceAccountId, decimal Amount) { }

            protected virtual void CancelTransferOnSource(string AccountId, string TargetAccountId, decimal Amount) { }
        }

        public class AccountBalances
        {
            public AccountBalances() { }

            public Dictionary<string, Decimal> Balances = new Dictionary<string, decimal>();

            public virtual void OnAccountRegistered(string AccountId) { Balances.Add(AccountId, 0); }

            public virtual void OnAmountDeposited(string AccountId, decimal Amount) { Balances[AccountId] += Amount; }

            public virtual void OnAmountWithdrawn(string AccountId, decimal Amount) { Balances[AccountId] -= Amount; }

            public virtual void OnTransferProcessedOnTarget(string AccountId, string SourceAccountId, decimal Amount)
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