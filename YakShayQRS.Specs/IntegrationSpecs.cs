using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace YakShayQRS.Specs
{
    [TestClass]
    // This class contains **ALL** the code required to get the system up & running
    public class IntegrationSpecs
    {
        public class Account
        {
            // virtual props define the unique id, are used as a message filter
            // and initialized upon loading the instance
            protected virtual string AccountId { get; set; }

            bool IsRegistered = false;
            private Decimal Balance = 0;

            // public non-virtual methods define the external interface (i.e. the commands);
            // these methods are **NEVER** invoked when rebuilding current state from past messages
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

            // virtual methods emit messages before they get called &
            // the virtual props are added to the emitted messages.
            // When building the current object's state based on past messages,
            // only the virtual methods get invoked.
            protected virtual void OnAccountRegistered(string OwnerName) { IsRegistered = true; }

            protected virtual void OnAmountDeposited(decimal Amount) { Balance += Amount; }

            protected virtual void OnAmountWithdrawn(decimal Amount) { Balance -= Amount; }

            protected virtual void OnTransferProcessedOnSource(string TargetAccountId, decimal Amount) { Balance -= Amount; }

            protected virtual void OnTransferCancelledOnSource(string TargetAccountId, decimal Amount) { Balance += Amount; }

            protected virtual void OnTransferProcessedOnTarget(string SourceAccountId, decimal Amount) { Balance += Amount; }

            protected virtual void OnTransferFailedOnTarget(string SourceAccountId, decimal Amount) { }

            protected virtual void OnTransactionCancelled(string what, string reason) { }
        }

        // simply xlat events from one account to commands for another account
        public class AccountTransferSaga
        {
            // no virtual props = no unique Id

            // Processing these events emits commands (messages)
            public void OnTransferProcessedOnSource(string AccountId, string TargetAccountId, decimal Amount)
            {
                ProcessTransferOnTarget(TargetAccountId, AccountId, Amount);
            }

            public void OnTransferFailedOnTarget(string AccountId, string SourceAccountId, decimal Amount)
            {
                CancelTransferOnSource(SourceAccountId, AccountId, Amount);
            }

            // these commands get emitted
            protected virtual void ProcessTransferOnTarget(string AccountId, string SourceAccountId, decimal Amount) { }

            protected virtual void CancelTransferOnSource(string AccountId, string TargetAccountId, decimal Amount) { }
        }

        // build a viewmodel to verify proper state
        public class AccountBalances
        {
            public AccountBalances() { }

            public Dictionary<string, Decimal> Balances = new Dictionary<string, decimal>();

            // no non-virtual commands, as this class only processes messages, it does not emit any

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
            // register all types under test
            SUT.RegisterType<Account>();
            SUT.RegisterType<AccountTransferSaga>();
            SUT.RegisterType<AccountBalances>();

            var ms = new MessageStore();

            SUT.HandleUntilAllConsumed(Message.FromAction(x => x.RegisterAccount(AccountId: "account/1", OwnerName: "Tom")), ms.Add, ms.Filter);
            SUT.HandleUntilAllConsumed(Message.FromAction(x => x.RegisterAccount(AccountId: "account/2", OwnerName: "Ben")), ms.Add, ms.Filter);
            SUT.HandleUntilAllConsumed(Message.FromAction(x => x.DepositAmount(AccountId: "account/1", Amount: 126m)), ms.Add, ms.Filter);
            SUT.HandleUntilAllConsumed(Message.FromAction(x => x.DepositAmount(AccountId: "account/2", Amount: 10m)), ms.Add, ms.Filter);
            SUT.HandleUntilAllConsumed(Message.FromAction(x => x.Transfer(AccountId: "account/1", TargetAccountId: "account/2", Amount: 26m)), ms.Add, ms.Filter);
            SUT.HandleUntilAllConsumed(Message.FromAction(x => x.WithdrawAmount(AccountId: "account/2", Amount: 10m)), ms.Add, ms.Filter);

            var bal = new AccountBalances();
            SUT.ApplyHistory(bal, ms.Filter);
            bal.Balances.Count.ShouldBe(2);
            bal.Balances["account/1"].ShouldBe(100m);
            bal.Balances["account/2"].ShouldBe(26m);
        }
    }
}