//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="OSContext.cs">
//      Created by: tomtan at 7/14/2015 10:27:06 AM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.AnswerData.Image
{
    using System.Collections.Generic;
    using Microsoft.ObjectStore;
    using AdsAnswer.Config;

    public class OSContext
    {
        public List<ITableLocation> Locations = new List<ITableLocation>();
        public DataLoadConfiguration Configuration;

        private static OSContext instance = null;

        public static OSContext Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new OSContext();
                }
                return instance;
            }
        }

        private OSContext()
        {
            //foreach (var env in ConfigStore.Instance.OsClientConfig.OSEnv)
            //{
            //    this.Locations.Add(new Microsoft.ObjectStore.VIP(env));
            //}
            Locations.Add(new Microsoft.ObjectStore.VIP("objectstoremulti.int.co.playmsn.com:83"));
            //this.Configuration = new DataLoadConfiguration(
            //    this.Locations,
            //    ConfigStore.Instance.OsClientConfig.OSNamespace,
            //    ConfigStore.Instance.OsClientConfig.OSTable,
            //    ConfigStore.Instance.OsClientConfig.MaxObjectsPerRequest,
            //    ConfigStore.Instance.OsClientConfig.MaxSimultaneousRequests,
            //    ConfigStore.Instance.OsClientConfig.RetriesPerRequest,
            //    ConfigStore.Instance.OsClientConfig.HttpTimeOutInMs,
            //    ConfigStore.Instance.OsClientConfig.MaxKeysPerSecond,
            //    true
            //    );
            Configuration = new DataLoadConfiguration(
                Locations,
                "RaaObjectStore",
                "RaaImageTest",
                5,
                5,
                3,
                10000,
                6,
                true);
        }
    }
}
