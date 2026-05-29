using System.Text.Json.Serialization;

namespace LaptopTracker.Models.Agent
{
    public class ApiResult<T>
    {
        [JsonPropertyName("ok")]    public bool Ok    { get; set; }
        [JsonPropertyName("data")]  public T?   Data  { get; set; }
        [JsonPropertyName("error")] public ApiError? Error { get; set; }

        public static ApiResult<T> Success(T data) =>
            new() { Ok = true, Data = data };

        public static ApiResult<T> Fail(string code, string message) =>
            new() { Ok = false, Error = new ApiError { Code = code, Message = message } };
    }

    public class ApiError
    {
        [JsonPropertyName("code")]    public string Code    { get; set; } = string.Empty;
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    }

    public class DeviceDto
    {
        public string   Source         { get; set; } = string.Empty;
        public int      Id             { get; set; }
        public string   SerialNumber   { get; set; } = string.Empty;
        public string?  AssetTag       { get; set; }
        public string   DeviceType     { get; set; } = string.Empty;
        public string   DeviceStateType { get; set; } = string.Empty;
        public string   RITM           { get; set; } = string.Empty;
        public DateTime Date           { get; set; }
        public string   DeviceLocation { get; set; } = string.Empty;
        public string   Status         { get; set; } = string.Empty;
        public bool     IsDeleted      { get; set; }
        // ReturnDevices-specific
        public string?  WorkOrder          { get; set; }
        public string?  PickupStatus       { get; set; }
        public string?  Location           { get; set; }
        public bool?    ChargerReturned    { get; set; }
        public bool?    PowerCableReturned { get; set; }
        // LoanerDevices-specific
        public string?   KidHandedTo    { get; set; }
        public DateTime? DateHandedOver { get; set; }
        public string?   WIC            { get; set; }
        public string?   KID            { get; set; }
        // WicStock-specific
        public string? SwapRITM { get; set; }
    }

    public class InconsistencyDto
    {
        public string       SerialNumber    { get; set; } = string.Empty;
        public List<string> FoundInSources  { get; set; } = new();
        public string       InconsistencyType { get; set; } = string.Empty;
        public string?      Detail          { get; set; }
    }

    public class PrepareResult
    {
        public string                    Action          { get; set; } = string.Empty;
        public DeviceDto?                CurrentState    { get; set; }
        public Dictionary<string,string?> ProposedChanges { get; set; } = new();
        public string?                   Warning         { get; set; }
    }

    public class SuggestUpdateRequest
    {
        public string                    Source { get; set; } = string.Empty;
        public int                       Id     { get; set; }
        public Dictionary<string,string?> Fields { get; set; } = new();
    }

    public class PrepareStatusUpdateRequest
    {
        public string Source { get; set; } = string.Empty;
        public int    Id     { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PrepareLocationUpdateRequest
    {
        public string Source         { get; set; } = string.Empty;
        public int    Id             { get; set; }
        public string DeviceLocation { get; set; } = string.Empty;
    }

    public class PrepareAssignmentUpdateRequest
    {
        public string    Source         { get; set; } = string.Empty;
        public int       Id             { get; set; }
        public string?   KidHandedTo    { get; set; }
        public string?   KID            { get; set; }
        public DateTime? DateHandedOver { get; set; }
    }

    public class PrepareReturnMarkRequest
    {
        public string Source { get; set; } = string.Empty;
        public int    Id     { get; set; }
    }

    public class CallStatusUpdateRequest     : PrepareStatusUpdateRequest     { }
    public class CallLocationUpdateRequest   : PrepareLocationUpdateRequest   { }
    public class CallAssignmentUpdateRequest : PrepareAssignmentUpdateRequest { }
    public class CallReturnRequest           : PrepareReturnMarkRequest       { }
}
