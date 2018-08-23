//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="EventType.cs">
//      Created by: tomtan at 7/10/2015 12:09:05 PM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.Logging
{
    // Summary:
    //     Identifies the type of event that has caused the trace.
    public enum EventType
    {
        // Summary:
        //     Reserved for feed reporting info
        Critical = 1,
        //
        // Summary:
        //     Recoverable error.
        Error = 2,
        //
        // Summary:
        //     Noncritical problem.
        Warning = 4,
        //
        // Summary:
        //     Informational message.
        Information = 8,
        //
        // Summary:
        //     Debugging trace.
        Verbose = 16,
        //
        // Summary:
        //     Starting of a logical operation.
        Start = 256,
        //
        // Summary:
        //     Stopping of a logical operation.
        Stop = 512,
        //
        // Summary:
        //     Suspension of a logical operation.
        Suspend = 1024,
        //
        // Summary:
        //     Resumption of a logical operation.
        Resume = 2048,
        //
        // Summary:
        //     Changing of correlation identity.
        Transfer = 4096,
    }
}
