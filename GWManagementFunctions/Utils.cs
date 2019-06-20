using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace GWManagementFunctions
{
    public class Utils
    {

        public static string CheckResponse(string response, out dynamic jobject)
        {
            if (response == "Response did not arrive")
            {
                jobject = null;
                return response;
            }

            jobject = JsonConvert.DeserializeObject(response);

            if ((int)jobject.data.status != 0)
                return $"{jobject.data.statusStr}";

            //No error
            return "";
        }

        public static string CheckResponse(string response)
        {
            dynamic jobject;
            if (response == "Response did not arrive")
            {
                return response;
            }

            jobject = JsonConvert.DeserializeObject(response);

            if ((int)jobject.data.status != 0)
                return $"{jobject.data.statusStr}";

            //No error
            return "";
        }
    }

    public static class ResultMessages
    {

        public static string ErrorResult { get; } = "Error";
        public static string Success { get; } = "OK";
        public static string DiscoveryError { get; } = "DiscoveryError";

        public static string DiscoverySuccess { get; } = "DiscoverySuccess";
        public static string SmartConnectError { get; } = "SmartConnectError";

        public static string SmartConnectSuccess { get; } = "SmartConnectSuccess";
        public static string UnbondError { get; } = "UnbondError";
        public static string UnbondSuccess { get; } = "UnbondSuccess";
        public static string BondError { get; } = "BondError";
        public static string BondSuccess { get; } = "BondSuccess";

        public static string AddTaskSuccess { get; } = "AddTaskSuccess";
        public static string AddTaskError { get; } = "AddTaskError";

        public static string GetTaskSuccess { get; } = "GetTaskSuccess";
        public static string GetTaskError { get; } = "GetTaskError";

        public static string RemoveTaskSuccess { get; } = "RemoveTaskSuccess";
        public static string RemoveTaskError { get; } = "RemoveTaskError";
        public static string ListTasksSuccess { get; } = "ListTasksSuccess";
        public static string ListTasksError { get; } = "ListTasksError";

        public static string TaskRestoreError { get; } = "TaskRestoreError";
        public static string TaskRestoreSuccess{ get; } = "TaskRestoreSuccess";

        public static string EnumerateSuccess { get; } = "EnumerateSuccess";
        public static string EnumerateError { get; } = "EnumerateError";

        public static string DiscoveredDevicesSuccsess { get; } = "DiscoveredDevicesSuccess";
        public static string DiscoveredDevicesError { get; } = "EDiscoveredDevicesError";
    }

    public class ResponseObject
    {

        public ResponseObject(string result, string resultMessage, string gwLogMessage, string additionalInfo = "")
        {
            this.Result = result;
            this.ResultMessage = resultMessage;
            this.GWLogMessage = gwLogMessage;
            this.AdditionalInfo = additionalInfo;
        }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("resultMessage")]
        public string ResultMessage { get; set; }

        [JsonProperty("gwLogMessage")]
        public string GWLogMessage { get; set; }

        [JsonProperty("additionalInfoe")]
        public string AdditionalInfo { get; set; }
    }
}
