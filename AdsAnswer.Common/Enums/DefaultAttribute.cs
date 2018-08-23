//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="DefaultAttribute.cs">
//      Created by: tomtan at 7/10/2015 11:05:18 AM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class DefaultAttribute : Attribute
    {
    }
}
