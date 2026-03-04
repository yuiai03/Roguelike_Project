#if !DISABLE_PLAYFABENTITY_API
using System;
using System.Collections.Generic;
using PlayFab.SharedModels;

namespace PlayFab.EventsModels
{
    [Serializable]
    public class CreateTelemetryKeyRequest : PlayFabRequestCommon
    {

        public Dictionary<string,string> CustomTags;

        public EntityKey Entity;

        public string KeyName;
    }

    [Serializable]
    public class CreateTelemetryKeyResponse : PlayFabResultCommon
    {

        public TelemetryKeyDetails NewKeyDetails;
    }

    [Serializable]
    public class DataConnectionAzureBlobSettings : PlayFabBaseModel
    {

        public string AccountName;

        public string ContainerName;

        public string TenantId;
    }

    [Serializable]
    public class DataConnectionAzureDataExplorerSettings : PlayFabBaseModel
    {

        public string ClusterUri;

        public string Database;

        public string Table;
    }

    [Serializable]
    public class DataConnectionDetails : PlayFabBaseModel
    {

        public DataConnectionSettings ConnectionSettings;

        public bool IsActive;

        public string Name;

        public DataConnectionStatusDetails Status;

        public DataConnectionType Type;
    }

    public enum DataConnectionErrorState
    {
        OK,
        Error
    }

    [Serializable]
    public class DataConnectionFabricKQLSettings : PlayFabBaseModel
    {

        public string ClusterUri;

        public string Database;

        public string Table;
    }

    [Serializable]
    public class DataConnectionSettings : PlayFabBaseModel
    {

        public DataConnectionAzureBlobSettings AzureBlobSettings;

        public DataConnectionAzureDataExplorerSettings AzureDataExplorerSettings;

        public DataConnectionFabricKQLSettings AzureFabricKQLSettings;
    }

    [Serializable]
    public class DataConnectionStatusDetails : PlayFabBaseModel
    {

        public string Error;

        public string ErrorMessage;

        public DateTime? MostRecentErrorTime;

        public DataConnectionErrorState? State;
    }

    public enum DataConnectionType
    {
        AzureBlobStorage,
        AzureDataExplorer,
        FabricKQL
    }

    [Serializable]
    public class DeleteDataConnectionRequest : PlayFabRequestCommon
    {

        public Dictionary<string,string> CustomTags;

        public string Name;
    }

    [Serializable]
    public class DeleteDataConnectionResponse : PlayFabResultCommon
    {

        public bool WasDeleted;
    }

    [Serializable]
    public class DeleteTelemetryKeyRequest : PlayFabRequestCommon
    {

        public Dictionary<string,string> CustomTags;

        public EntityKey Entity;

        public string KeyName;
    }

    [Serializable]
    public class DeleteTelemetryKeyResponse : PlayFabResultCommon
    {

        public bool WasKeyDeleted;
    }

    [Serializable]
    public class EntityKey : PlayFabBaseModel
    {

        public string Id;

        public string Type;
    }

    [Serializable]
    public class EventContents : PlayFabBaseModel
    {

        public Dictionary<string,string> CustomTags;

        public EntityKey Entity;

        public string EventNamespace;

        public string Name;

        public string OriginalId;

        public DateTime? OriginalTimestamp;

        public object Payload;

        public string PayloadJSON;
    }

    [Serializable]
    public class GetDataConnectionRequest : PlayFabRequestCommon
    {

        public Dictionary<string,string> CustomTags;

        public string Name;
    }

    [Serializable]
    public class GetDataConnectionResponse : PlayFabResultCommon
    {

        public DataConnectionDetails DataConnection;
    }

    [Serializable]
    public class GetTelemetryKeyRequest : PlayFabRequestCommon
    {

        public Dictionary<string,string> CustomTags;

        public EntityKey Entity;

        public string KeyName;
    }

    [Serializable]
    public class GetTelemetryKeyResponse : PlayFabResultCommon
    {

        public TelemetryKeyDetails KeyDetails;
    }

    [Serializable]
    public class ListDataConnectionsRequest : PlayFabRequestCommon
    {

        public Dictionary<string,string> CustomTags;
    }

    [Serializable]
    public class ListDataConnectionsResponse : PlayFabResultCommon
    {

        public List<DataConnectionDetails> DataConnections;
    }

    [Serializable]
    public class ListTelemetryKeysRequest : PlayFabRequestCommon
    {

        public Dictionary<string,string> CustomTags;

        public EntityKey Entity;
    }

    [Serializable]
    public class ListTelemetryKeysResponse : PlayFabResultCommon
    {

        public List<TelemetryKeyDetails> KeyDetails;
    }

    [Serializable]
    public class SetDataConnectionActiveRequest : PlayFabRequestCommon
    {

        public bool Active;

        public Dictionary<string,string> CustomTags;

        public string Name;
    }

    [Serializable]
    public class SetDataConnectionActiveResponse : PlayFabResultCommon
    {

        public DataConnectionDetails DataConnection;

        public bool WasUpdated;
    }

    [Serializable]
    public class SetDataConnectionRequest : PlayFabRequestCommon
    {

        public DataConnectionSettings ConnectionSettings;

        public Dictionary<string,string> CustomTags;

        public bool IsActive;

        public string Name;

        public DataConnectionType Type;
    }

    [Serializable]
    public class SetDataConnectionResponse : PlayFabResultCommon
    {

        public DataConnectionDetails DataConnection;
    }

    [Serializable]
    public class SetTelemetryKeyActiveRequest : PlayFabRequestCommon
    {

        public bool Active;

        public Dictionary<string,string> CustomTags;

        public EntityKey Entity;

        public string KeyName;
    }

    [Serializable]
    public class SetTelemetryKeyActiveResponse : PlayFabResultCommon
    {

        public TelemetryKeyDetails KeyDetails;

        public bool WasKeyUpdated;
    }

    [Serializable]
    public class TelemetryKeyDetails : PlayFabBaseModel
    {

        public DateTime CreateTime;

        public bool IsActive;

        public string KeyValue;

        public DateTime LastUpdateTime;

        public string Name;
    }

    [Serializable]
    public class WriteEventsRequest : PlayFabRequestCommon
    {

        public Dictionary<string,string> CustomTags;

        public List<EventContents> Events;
    }

    [Serializable]
    public class WriteEventsResponse : PlayFabResultCommon
    {

        public List<string> AssignedEventIds;
    }
}
#endif
