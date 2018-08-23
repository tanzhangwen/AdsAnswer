//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="ImageIngester.cs">
//      Created by: tomtan at 7/14/2015 10:04:34 AM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.AnswerData.Image
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Bond;
    using Microsoft.ObjectStore;

    public class ImageIngester
    {
        public static void Ingest(string keyString, byte[] data, object context)
        {
            Ingest(keyString, data, context, OSContext.Instance.Configuration);
        }

        public static void Ingest(string keyString, byte[] data, object context, DataLoadConfiguration config)
        {
            using (DataLoader loader = new DataLoader(config))
            {
                Ingest(keyString, data, context, loader);
                loader.Flush();
                loader.Receive(true);
            }
        }

        public static List<IDataLoadResult> Ingest(string keyString, byte[] data, object context, DataLoader loader)
        {
            Multimedia.Key key = new Multimedia.Key();
            key.blobValue = new BondBlob(Encoding.UTF8.GetBytes(keyString));
            Multimedia.Value value = new Multimedia.Value();
            value.blobValue = new BondBlob(data);

            loader.Send(key, value, context);
            return loader.Receive(false);
        }

        public static void Delete(string keyString, object context)
        {
            Delete(keyString, context, OSContext.Instance.Configuration);
        }

        public static void Delete(string keyString, object context, DataLoadConfiguration config)
        {
            using (DataLoader loader = new DataLoader(config))
            {
                Delete(keyString, context, loader);
                loader.Flush();
                loader.Receive(true);
            }
        }

        public static List<IDataLoadResult> Delete(string keyString, object context, DataLoader loader)
        {
            Multimedia.Key key = new Multimedia.Key();
            key.blobValue = new BondBlob(Encoding.UTF8.GetBytes(keyString));

            loader.Delete(key, context);
            return loader.Receive(false);
        }
    }
}
