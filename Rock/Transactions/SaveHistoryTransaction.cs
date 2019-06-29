using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Rock.Data;
using Rock.Model;

namespace Rock.Transactions
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Rock.Transactions.ITransaction" />
    public class SaveHistoryTransaction : ITransaction
    {
        /// <summary>
        /// Keep a list of all the historyRecords that have been queued up with each new SaveHistoryTransaction() then insert them all at once 
        /// </summary>
        private static ConcurrentQueue<History> HistoryRecordsToInsert = new ConcurrentQueue<History>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveHistoryTransaction" /> class.
        /// </summary>
        /// <param name="historyRecordsToInsert">The history records to insert.</param>
        /// <param name="currentPerson">The current person.</param>
        public SaveHistoryTransaction( List<History> historyRecordsToInsert )
        {
            foreach ( var historyRecord in historyRecordsToInsert )
            {
                HistoryRecordsToInsert.Enqueue( historyRecord );
            }
        }

        /// <summary>
        /// Executes this instance.
        /// </summary>
        public void Execute()
        {
            var historyRecordsToInsert = new List<History>();
            while ( HistoryRecordsToInsert.TryDequeue( out History historyRecord ) )
            {
                historyRecordsToInsert.Add( historyRecord );
            }

            if ( !historyRecordsToInsert.Any() )
            {
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                rockContext.BulkInsert( historyRecordsToInsert );
            }
        }
    }
}
