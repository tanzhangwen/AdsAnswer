//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="BuildResults.cs">
//      Created by: tomtan at 7/10/2015 10:38:17 AM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer
{
    public enum BuildResults
    {
        Success = 0,
        Canceled,

        PrepareBuild_Succeed,
        PrepareBuild_Failed,

        Crawler_Succeed,
        Crawler_NoNewFileFound,
        Crawler_CannotFindDownloadedFile,
        Crawler_InvalidXmlFile,
        Crawler_ZeroLength,
        Crawler_RemoteServerNoResponse,
        Crawler_UnexpectedError,
        Crawler_CannotCopyRemoteFile,
        Crawler_ValidSourceFileFailed,
        Crawler_WrongUriFormat,
        Crawler_404NotFound,
        Crawler_ProtocolError,

        Formatter_Succeed,
        Formatter_PreProcessFailed,
        Formatter_NormalizeFailed,
        Formatter_DedupFailed,
        Formatter_MergeFailed,
        Formatter_ExtractKeyFailed,

        OdysseyTable_EntityFileMissed,
        OdysseyTable_PatternFileMissed,
        OdysseyTable_RecordFileMissed,
        OdysseyTable_Succeed,

        CreateBinaryKif_Succeed,
        CreateBinaryKif_Failed,

        PublishPointData_Succeed,
        PublishPointData_Failed,

        OisConvert_Succeed,
        OisConvert_Failed,

        Market_MergeDataFailed,
        Aggregator_CreateQasDataFailed,
        Aggregator_CreateSuggestionDataFailed,

        FeedLevelProcess_Succeed,
        DomainLevelProcess_Succeed,
        DomainLevelProcess_NotCompleted,
        MarketLevelProcess_Succeed,
        OSTriggeringDataBuildProcess_Succeed,
        QASDataBuildProcess_Succeed,
        QASDataBuildProcess_NoData,

        Translate_Succeed,
        Translate_PreProcessFailed,
        Translate_Failed,
        Translate_PostProcessFailed,

        Validate_SourceFailed,
        Validate_KifXmlFailed,
        Validate_TargetFailed,  
        Validate_FileSizeFailed,

        Image_Succeed,
        Image_ExtractImageFailed,
        Image_MergeImageFailed,
        Image_DownloadImageFailed,
        Image_ScaleImageFailed,
        Image_IngestImageFailed,
        Image_ProcessImageFailed,

        TriggeringMining_NoNewFileFound,
        TriggeringMining_ErrorOccur,
        TriggeringMining_Successed,
        TriggeringMining_NoSlapiDataFailed,

        CosmosUploadSucceed,
        CosmosUploadFailed,

        QEP_Failed,
        FailedWithException,

        ManualRuleConvertion_Succeeded,
        ManualRuleConvertion_Failed,
    }
}
