using Microsoft.Build.Framework;

namespace RepoTasks
{
    public class CreateBatches : Microsoft.Build.Utilities.Task
    {
        [Required]
        [Output]
        public ITaskItem[] Items { get; set; }

        [Required]
        public int MaxBatchSize { get; set; }

        public override bool Execute()
        {
            var bucket = MaxBatchSize;
            var batchCount = 0;
            for (var i = 0; i < Items.Length; i++)
            {
                Items[i].SetMetadata("Batch", batchCount.ToString());
                if (--bucket == 0)
                {
                    bucket = MaxBatchSize;
                    batchCount++;
                }
            }

            return true;
        }
    }
}
