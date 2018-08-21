using Newtonsoft.Json;
using System;

namespace Microsoft.Build.OOB.ESRP
{
    public class Config
    {
        public string Version
        {
            get;
            set;
        } = "1.0.0";

        public string EsrpAPiBaseUri
        {
            get;
            set;
        }

        public int EsrpSessionTimeoutInSec
        {
            get;
            set;
        } = 3600;

        public int MinThreadPoolThreads
        {
            get;
            set;
        } = -1;

        public int MaxDegreeOfParellelism
        {
            get;
            set;
        } = -1;

        public bool ExponentialFirstFastRetry
        {
            get;
            set;
        } = true;

        public int ExponentialRetryCount
        {
            get;
            set;
        } = 5;

        public TimeSpan ExponentialRetryMinBackOff
        {
            get;
            set;
        } = new TimeSpan(0, 0, 3);

        public TimeSpan ExponentionalRetryMaxBackOff
        {
            get;
            set;
        } = new TimeSpan(0, 1, 0);

        public TimeSpan ExponentialRetryDeltaBackOff
        {
            get;
            set;
        } = new TimeSpan(0, 0, 5);

        public bool ExitOnFlaggedFile
        {
            get;
            set;
        } = false;

        public TimeSpan FlaggedFileClientWaitTime
        {
            get;
            set;
        } = new TimeSpan(23, 59, 59);

        public int ServicePointManagerDefaultConnectionLimit
        {
            get;
            set;
        } = -1;

        public string AppDataFolder
        {
            get;
            set;
        }
    }
}
