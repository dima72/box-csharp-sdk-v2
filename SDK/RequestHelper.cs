using System.Collections.Generic;
using System.Linq;
using BoxApi.V2.SDK.Model;
using BoxApi.V2.SDK.Serialization;
using RestSharp;
using RestSharp.Deserializers;

namespace BoxApi.V2
{
    public class RequestHelper
    {
        public const string JsonMimeType = "application/json";

        public IRestRequest Get(Type resourceType, string id)
        {

            var request = JsonRequest(resourceType, "{id}");
            request.AddUrlSegment("id", id);
            return request;
        }

        public IRestRequest GetItems(string id)
        {
            var request = JsonRequest(Type.Folder, "{id}/items");
            request.AddUrlSegment("id", id);
            return request;
        }

        public IRestRequest CreateFolder(string parentId, string name)
        {
            var request = JsonRequest(Type.Folder, "{parentId}", Method.POST);
            request.AddUrlSegment("parentId", parentId);
            request.AddBody(new {name});
            return request;
        }

        public IRestRequest CreateFile(string parentId, string name, byte[] content)
        {
            var request = JsonRequest(Type.File, "data", Method.POST);
            request.AddFile("filename1", content, name);
            request.AddParameter("folder_id", parentId);
            return request;
        }

        public IRestRequest DeleteFolder(string id, bool recursive)
        {
            var request = GetDeleteRequest(Type.Folder, id);
            request.AddParameter("recursive", recursive.ToString().ToLower());
            return request;
        }

        public IRestRequest DeleteFile(string id, string etag)
        {
            var request = GetDeleteRequest(Type.File, id);
            request.AddHeader("If-Match", etag ?? string.Empty);
            return request;
        }

        public IRestRequest DeleteComment(string id)
        {
            var request = GetDeleteRequest(Type.Comment, id);
            return request;
        }

        private IRestRequest GetDeleteRequest(Type resourceType, string id)
        {
            var request = JsonRequest(resourceType, "{id}", Method.DELETE);
            request.AddUrlSegment("id", id);
            return request;
        }

        public IRestRequest Copy(Type resourceType, string id, string newParentId, string name)
        {
            var request = JsonRequest(resourceType, "{id}/copy", Method.POST);
            request.AddUrlSegment("id", id);
            request.AddBody(new {parent = new {id = newParentId}, name});
            return request;
        }

        public IRestRequest Update(Type resourceType, string id, string parentId = null, string name = null, string description = null, SharedLink sharedLink = null, string message = null)
        {
            var request = JsonRequest(resourceType, "{id}", Method.PUT);
            request.AddUrlSegment("id", id);
            var body = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(parentId))
            {
                body.Add("parent", new{id = parentId});
            }
            if (!string.IsNullOrEmpty(name))
            {
                body.Add("name", name);
            }
            if (!string.IsNullOrEmpty(description))
            {
                body.Add("description", description);
            }
            if (sharedLink != null)
            {
                body.Add("shared_link", sharedLink);
            }
            if (!string.IsNullOrEmpty(message))
            {
                body.Add("message", message);
            }

            request.AddBody(body);
            return request;
        }

        public IRestRequest Read(string id)
        {
            var request = RawRequest(Type.File, "{id}/data");
            request.AddUrlSegment("id", id);
            return request;
        }

        public IRestRequest Write(string id, string name, byte[] content)
        {
            var request = JsonRequest(Type.File, "{id}/data", Method.POST);
            request.AddUrlSegment("id", id);
            request.AddFile("filename", content, name);
            return request;
        }

        public IRestRequest AddComment(string id, string message)
        {
            var request = JsonRequest(Type.File, "{id}/comments", Method.POST);
            request.AddUrlSegment("id", id);
            request.AddBody(new {message});
            return request;
        }

        public IRestRequest GetComments(Type resourceType, string id)
        {
            var request = JsonRequest(resourceType, "{id}/comments");
            request.AddUrlSegment("id", id);
            return request;
        }

        public IRestRequest GetTicket(string apiKey)
        {
            var restRequest = new RestRequest("1.0/rest");
            restRequest.AddParameter("action", "get_ticket");
            restRequest.AddParameter("api_key", apiKey);
            return restRequest;
        }

        public IRestRequest SwapTicketForToken(string apiKey, string ticket)
        {
            var restRequest = new RestRequest("1.0/rest");
            restRequest.AddParameter("action", "get_auth_token");
            restRequest.AddParameter("api_key", apiKey);
            restRequest.AddParameter("ticket", ticket);
            return restRequest;
        }

        private IRestRequest RawRequest(Type resourceType, string resource, Method method = Method.GET)
        {
            string path = "{version}/{type}" + (string.IsNullOrEmpty(resource) ? string.Empty : string.Format("/{0}", resource));
            var request = new RestRequest(path, method);
            request.AddUrlSegment("version", "2.0");
            request.AddUrlSegment("type", resourceType.Description());
            return request;
        }

        public bool WasSuccessful(IRestResponse restResponse, out Error error)
        {
            error = null;
            bool success = true;
            if (restResponse == null)
            {
                success = false;
            }

            else if (restResponse.ContentType.Equals(JsonMimeType) && restResponse.Content.Contains(@"""type"":""error"""))
            {
                success = false;
                var jsonDeserializer = new JsonDeserializer();
                error = jsonDeserializer.Deserialize<Error>(restResponse);
                if (error.Type == null)
                {
                    var errorCollection = jsonDeserializer.Deserialize<ErrorCollection>(restResponse);
                    if (!string.IsNullOrEmpty(errorCollection.TotalCount))
                    {
                        error = errorCollection.Entries.First();
                    }
                }
            }
            return success;
 
        }

        private IRestRequest JsonRequest(Type resourceType, string resource = null, Method method = Method.GET)
        {
            var request = RawRequest(resourceType, resource, method);
            request.RequestFormat = DataFormat.Json;
            request.JsonSerializer = new AttributableJsonSerializer();
            return request;
        }
    }
}