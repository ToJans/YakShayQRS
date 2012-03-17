namespace MinimalisticCQRS.Domain
{
    public class AccountTransferSaga
    {
        public void TransferProcessedOnSource(decimal Amount, string TargetAccountId, string AccountId)
        {
            ProcessTransferOnTarget(Amount, SourceAccountId: AccountId, AccountId: TargetAccountId);
        }

        public virtual void ProcessTransferOnTarget(decimal Amount, string SourceAccountId, string AccountId)
        {
        }

        public void TransferFailedOnTarget(string Reason, decimal Amount, string SourceAccountId, string AccountId)
        {
            CancelTransfer(Reason, Amount, TargetAccountId: AccountId, AccountId: SourceAccountId);
        }

        public virtual void CancelTransfer(string Reason, decimal Amount, string TargetAccountId, string AccountId)
        {
        }
    }
}