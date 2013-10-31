using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SheerID
{
    /*
    * Copyright 2013 SheerID, Inc. or its affiliates. All Rights Reserved.
    *
    * Licensed under the Apache License, Version 2.0 (the "License").
    * You may not use this file except in compliance with the License.
    * A copy of the License is located at:
    *
    *  http://www.apache.org/licenses/LICENSE-2.0.html
    *
    * This file is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
    * CONDITIONS OF ANY KIND, either express or implied. See the License for
    * the specific language governing permissions and limitations under the
    * License.
    * 
    * For more information, visit:
    *
    *  http://developer.sheerid.com
    *
    */

    public class API
    {
        #region Constructor
        public Queue<string> Log = new Queue<string>();
        Rest rest;
        const double SHEERID_API_VERSION = 0.5;
        public enum SHEERID_ENDPOINT { SANDBOX, PRODUCTION };
        const string SHEERID_ENDPOINT_SANDBOX = "https://services-sandbox.sheerid.com";
        const string SHEERID_ENDPOINT_PRODUCTION = "https://services.sheerid.com";

        public API(string accessToken, SHEERID_ENDPOINT endpoint = SHEERID_ENDPOINT.SANDBOX, bool verbose = false)
        {
            var endpointURL = endpoint == SHEERID_ENDPOINT.PRODUCTION ? SHEERID_ENDPOINT_PRODUCTION : SHEERID_ENDPOINT_SANDBOX;
            this.rest = new Rest(
                accessToken, 
                ref this.Log,
                endpointURL,
                verbose);
            this.Verification = new PostVerification(accessToken, ref this.Log, endpointURL, verbose);
        }
        #endregion

        #region Public Methods
        public ServiceResponse<List<Namespace>> ListNamespaces()
        {
            return this.rest.Get<List<Namespace>>("/namespace");
        }

        public ServiceResponse<Namespace> GetNamespace(string name)
        {
            return this.rest.Get<Namespace>(string.Format("/namespace/{0}", name));
        }

        public bool DeleteNamespace(string name)
        {
            return this.rest.Delete<Namespace>(string.Format("/namespace/{0}", name)).Status == HttpStatusCode.NoContent;
        }

        public ServiceResponse<Namespace> MapNamespace(string name, VerificationRequestTemplate template)
        {
            return this.rest.Put<Namespace>(string.Format("/namespace/{0}", name), new Dictionary<string, string>() { { "templateId", template.Id } });
        }

        public ServiceResponse<byte[]> GetAssetData(string assetId)
        {
            return this.rest.Get<byte[]>(string.Format("/asset/{0}/raw", assetId));
        }

        public ServiceResponse<Asset> GetAsset(string assetId)
        {
            return this.rest.Get<Asset>(string.Format("/asset/{0}", assetId));
        }

        public ServiceResponse<Asset> ReviewAsset(string assetId)
        {
            return this.rest.Get<Asset>(string.Format("/asset/{0}", assetId));
        }

        public bool Ping() 
        { 
            return this.rest.Get<object>("/ping").Status == HttpStatusCode.OK; 
        }

        public ServiceResponse<List<OrganizationType>> ListOrganizationTypes() 
        { 
            return this.rest.Get<List<OrganizationType>>("/organizationType");
        }

        public ServiceResponse<List<AssetType>> ListAssetTypes(OrganizationType organizationType)
        { 
            return this.rest.Get<List<AssetType>>("/assetType", new Dictionary<string, string> { { "organizationType", organizationType.ToString() }});
        }

        public ServiceResponse<List<Asset>> PostAsset(TokenResponse token, List<UploadableFile> files)
        {
            return this.rest.Post<List<Asset>>("/asset", new Dictionary<string, string>() { { "token", token.Token } }, files); 
        }

        public ServiceResponse<TokenResponse> GetAssetToken(string requestId)
        {
            return this.rest.Post<TokenResponse>("/asset/token", (requestId != null ? new Dictionary<string, string>() { { "requestId", requestId } } : null));
        }

        public ServiceResponse<List<string>> ListFields() 
        { 
            return this.rest.Get<List<string>>("/field");
        }

        public ServiceResponse<VerificationResponse> Inquire(string requestId)
        {
            return this.rest.Get<VerificationResponse>(string.Format("/verification/{0}", requestId));
        }

        //Replaced by the Verification object
        //public ServiceResponse<VerificationResponse> UpdateVerification(string requestId, Dictionary<string, string> data)
        //{
        //    Dictionary<string, string> post_data = new Dictionary<string, string>();
        //    foreach (KeyValuePair<string, string> kv in data)
        //        post_data.Add(kv.Key, kv.Value);
        //    return this.rest.Post<VerificationResponse>(string.Format("/verification/{0}", requestId), post_data);
        //}

        public ServiceResponse<List<Organization>> ListOrganizations(OrganizationType type, string name = null)
        {
            return ListOrganizations(type.ToString(), name);
        }

        public ServiceResponse<List<Organization>> ListOrganizations(string type = null, string name = null)
        {
            var parameters = new Dictionary<string, string>();
            if (type != null)
            {
                parameters.Add("type", type);
            }
            if (name != null)
            {
                parameters.Add("name", name);
            }
            return this.rest.Get<List<Organization>>("/organization", parameters);
        }

        public ServiceResponse<List<AffiliationType>> ListAffiliationTypes()
        {
            return this.rest.Get<List<AffiliationType>>("/affiliationType");
        }

        public ServiceResponse<List<AffiliationType>> ListAffiliationTypes(OrganizationType organizationType)
        {
            return this.rest.Get<List<AffiliationType>>("/affiliationType", new Dictionary<string, string>() { { "organizationType", organizationType.ToString() } });
        }

        public ServiceResponse<List<Asset>> ListAssets(string requestId)
        {
            return this.rest.Get<List<Asset>>(string.Format("/verification/{0}/assets", requestId));
        }

        public bool RevokeToken(TokenResponse token)
        {
            return RevokeToken(token.Token);
        }

        public bool RevokeToken(string token)
        {
            var serviceResponse = this.rest.Delete<object>(string.Format("/token/{0}", token));
            return serviceResponse.Status == HttpStatusCode.NoContent;
        }

        public bool UpdateMetadata(string requestId, Dictionary<string, string> meta)
        {
            var serviceResponse = this.rest.Post<object>(string.Format("/verification/{0}/metadata", requestId), meta);
            return serviceResponse.Status == HttpStatusCode.OK;
        }

        public bool UpdateOrderId(string requestId, string orderId)
        {
            return this.UpdateMetadata(requestId, new Dictionary<string, string>() { { "orderId", orderId } });
        }

        public ServiceResponse<List<string>> GetFields(List<AffiliationType> affiliation_types)
        {
            //TODO: use service, currently faking a service response so as not to create a breaking change in the future
            List<string> fields = new List<string>() { "FIRST_NAME", "LAST_NAME" };
            if ((new List<AffiliationType>() { AffiliationType.STUDENT_FULL_TIME, AffiliationType.STUDENT_PART_TIME, AffiliationType.ACTIVE_DUTY, AffiliationType.VETERAN, AffiliationType.MILITARY_RETIREE, AffiliationType.RESERVIST }).Any(s => affiliation_types.Contains(s)))
                fields.Add("BIRTH_DATE");
            if (affiliation_types.Contains(AffiliationType.FACULTY))
                fields.Add("POSTAL_CODE");
            if ((new List<AffiliationType>() { AffiliationType.VETERAN, AffiliationType.MILITARY_RETIREE, AffiliationType.RESERVIST }).Any(s => affiliation_types.Contains(s)))
                fields.Add("STATUS_START_DATE");
            if (affiliation_types.Contains(AffiliationType.NON_PROFIT))
                fields.Add("ID_NUMBER");
            if (affiliation_types.Contains(AffiliationType.MILITARY_FAMILY))
                fields.Add("RELATIONSHIP");
            var sr = new ServiceResponse<List<string>>();
            sr.Response = fields;
            return sr;
        }

        public ServiceResponse<OrganizationType> GetOrganizationType(List<AffiliationType> affiliation_types)
        {
            var sr = new ServiceResponse<OrganizationType>();

            //TODO: improve / use service, currently faking a service response so as not to create a breaking change in the future
            if ((new List<AffiliationType>() { AffiliationType.ACTIVE_DUTY, AffiliationType.VETERAN, AffiliationType.MILITARY_RETIREE, AffiliationType.RESERVIST, AffiliationType.MILITARY_FAMILY }).Any(s => affiliation_types.Contains(s)))
                sr.Response = OrganizationType.MILITARY;
            else if ((new List<AffiliationType>() { AffiliationType.STUDENT_FULL_TIME, AffiliationType.STUDENT_PART_TIME, AffiliationType.FACULTY }).Any(s => affiliation_types.Contains(s)))
                sr.Response = OrganizationType.UNIVERSITY;
            // TODO: map other types

            return sr;
        }

        public PostVerification Verification { get; protected set; }
        
        #endregion

        public class PostVerification
        {
            Rest rest;
            internal PostVerification(string accessToken, ref Queue<string> Log, string baseUrl = null, bool verbose = false)
            {
                this.rest = new Rest(
                    accessToken,
                    ref Log,
                    baseUrl != null ? baseUrl : SHEERID_ENDPOINT_SANDBOX,
                    verbose);
                this.PersonalInfo = new UserInfo();
            }

            public ServiceResponse<VerificationResponse> PostRequest()
            {
                var post_data = Rest.LoadFields(this.PersonalInfo, Rest.LoadFields(this));

                return rest.Post<VerificationResponse>("/verification" + (requestId!=null?"/"+requestId:""), post_data);
            }

            public string requestId = null;
            public List<AffiliationType> affiliationTypes = new List<AffiliationType>();
            public List<AssetType> assetTypes = new List<AssetType>();
            public List<string> rewardIds = new List<string>();
            public List<VerificationType> verificationTypes = new List<VerificationType>();
            public bool matchName;
            public long organizationId;
            public string organizationName;
            public OrganizationType organizationType;
            public UserInfo PersonalInfo {get; set;}
            public class UserInfo
            {
                public string EMAIL, FIRST_NAME, MIDDLE_NAME, LAST_NAME, FULL_NAME, COMPANY_NAME, BIRTH_DATE, ID_NUMBER, JOB_TITLE, USERNAME, POSTAL_CODE, PHONE_NUMBER, SSN, SSN_LAST4, STATUS_START_DATE, SUFFIX, RELATIONSHIP;
            }

        }

        protected class Rest
        {
            #region Constructors
            private string accessToken, baseUrl;
            private bool verbose;
            private Queue<string> Log;

            public Rest(string accessToken, ref Queue<string> Log, string baseUrl, bool verbose)
            {
                this.accessToken = accessToken;
                this.baseUrl = baseUrl;
                this.Log = Log;
                this.verbose = verbose;
            }
            #endregion

            #region Public Methods
            public ServiceResponse<T> Get<T>(string path, Dictionary<string, string> parameters = null)
            {
                SheerIDRequest req = new SheerIDRequest(this.accessToken, ref this.Log, "GET", this.Url(path), parameters, null, this.verbose);
                return req.Execute<T>();
            }

            public ServiceResponse<T> Post<T>(string path, Dictionary<string, string> parameters = null, List<UploadableFile> files = null)
            {
                SheerIDRequest req = new SheerIDRequest(this.accessToken, ref this.Log, "POST", this.Url(path), parameters, files, this.verbose);
                return req.Execute<T>();
            }

            public ServiceResponse<T> Put<T>(string path, Dictionary<string, string> parameters = null, List<UploadableFile> files = null)
            {
                SheerIDRequest req = new SheerIDRequest(this.accessToken, ref this.Log, "PUT", this.Url(path), parameters, files, this.verbose);
                return req.Execute<T>();
            }

            public ServiceResponse<T> Delete<T>(string path)
            {
                SheerIDRequest req = new SheerIDRequest(this.accessToken, ref this.Log, "DELETE", this.Url(path), null, null, this.verbose);
                return req.Execute<T>();
            }
            #endregion

            #region Helpers
            string Url(string path = "")
            {
	            return string.Format("{0}/rest/{1}{2}", this.baseUrl, SHEERID_API_VERSION, path);
            }
            public static Dictionary<string,string> LoadFields(object obj)
            {
                var data = new Dictionary<string,string>();
                return LoadFields(obj, data);
            }
            public static Dictionary<string,string> LoadFields(object obj, Dictionary<string, string> data)
            {
                foreach (var f in obj.GetType().GetFields())
                {
                    var value = f.GetValue(obj);
                    if (value is System.Collections.ICollection)
                    {
                        string listValue = "";
                        foreach (var s in (System.Collections.ICollection)value)
                            listValue += ", " + s;
                        if (listValue.StartsWith(", "))
                            listValue = listValue.Substring(2);
                        data.Add(f.Name, listValue);
                    }
                    else
                    {
                        if (value != null)
                            data.Add(f.Name, value.ToString());
                    }
                }
                return data;
            }
            #endregion
        }

        protected class SheerIDRequest
        {
            #region Constructor
            string method;
            string url;
            Dictionary<string, string> parameters;
            List<UploadableFile> files;
            string authHeader;
            bool verbose;
            Queue<string> Log;

            public SheerIDRequest(string accessToken, ref Queue<string> Log, string method, string url, Dictionary<string, string> parameters = null, List<UploadableFile> files = null, bool verbose = false)
            {
                this.method = method;
                this.url = url;
                this.parameters = parameters != null ? parameters : new Dictionary<string, string>();
                this.files = files;
                this.authHeader = string.Format("Bearer {0}", accessToken);
                this.Log = Log;
                this.verbose = verbose;
            }
            #endregion

            #region Public Methods
            public ServiceResponse<T> Execute<T>()
            {
                HttpWebRequest req;
                if (this.method == "POST")
                {
                    req = WebRequest.Create(this.url) as HttpWebRequest;


                    req.Method = "POST";
                    req.Headers.Add(System.Net.HttpRequestHeader.Authorization, this.authHeader);

                    byte[] bytes;
                    
                    if (this.files == null)
                    {
                        req.ContentType = "application/x-www-form-urlencoded";
                        bytes = Encoding.UTF8.GetBytes(this.QueryString);
                    }
                    else
                    {
                        var boundary = "----WebKitFormBoundary" + RandomString(16);
                        req.ContentType = "multipart/form-data; boundary=" + boundary;

                        var beginRequest = "";
                        foreach (var kv in this.parameters) //load all parameters into byte array
                        {
                            beginRequest += string.Format("{1}--{0}{1}Content-Disposition: form-data; name=\"{2}\"{1}{1}{3}", boundary, Environment.NewLine, kv.Key, kv.Value);
                        }

                        bytes = Encoding.UTF8.GetBytes(beginRequest);
                        foreach (var file in this.files) //load files into byte array
                        {
                            var uploadRequest = string.Format("{1}--{0}{1}Content-Disposition: form-data; name=\"{2}\"; filename=\"{3}\"{1}Content-Type: {4}{1}{1}", boundary, Environment.NewLine, file.fileID, file.fileName, file.fileContentType);
                            bytes = bytes.Concat(Encoding.UTF8.GetBytes(uploadRequest)).ToArray();
                            bytes = bytes.Concat(file.data).ToArray();
                        }
                        bytes = bytes.Concat(Encoding.UTF8.GetBytes(string.Format("{1}--{0}--{1}", boundary, Environment.NewLine))).ToArray();
                    }

                    req.ContentLength = bytes.Length;
                    using (var writer = req.GetRequestStream())
                    {
                        writer.Write(bytes, 0, bytes.Length);
                    }
                }
                else
                {
                    //GET and DELETE
                    string query = this.QueryString;
                    req = HttpWebRequest.Create(query.Length > 0 ? string.Format("{0}?{1}", this.url, query) : this.url) as HttpWebRequest;
                    req.Headers.Add(System.Net.HttpRequestHeader.Authorization, this.authHeader);
                    req.Method = this.method;
                }

                ServiceResponse<T> serviceResponse;
                try
                {
                    using (HttpWebResponse resp = ((HttpWebResponse)req.GetResponse()))
                    {
                        serviceResponse = ConsumeResponse<T>(resp);
                    }
                }
                catch (WebException e)
                {
                    using (HttpWebResponse resp = (HttpWebResponse)e.Response)
                    {
                        serviceResponse = ConsumeResponse<T>(resp);
                    }
                }

                if (this.verbose)
                {
                    this.Log.Enqueue(string.Format("[SheerID] Status: {1} {0}URL: {2} {0}Query: {3} {0}Files: {4}", 
                        Environment.NewLine, 
                        serviceResponse.Status, 
                        this.url, this.QueryString,
                        (this.files != null ? this.files.Count().ToString() : "0")
                        ));
                }

                return serviceResponse;

            }

            #endregion

            #region Private Helpers
            ServiceResponse<T> ConsumeResponse<T>(HttpWebResponse resp)
            {
                var serviceResponse = new ServiceResponse<T> { Status = resp.StatusCode, ContentType = resp.ContentType };
                var stream = resp.GetResponseStream();
                if (resp.ContentLength > 0)
                {
                    serviceResponse.Raw = new byte[resp.ContentLength];
                    stream.Read(serviceResponse.Raw, 0, (int)resp.ContentLength);
                }
                else
                {//if contentlength is unknown, API bug 413
                    if (resp.ContentType == "application/json")
                    {//if contenttype is known
                        using (var reader = new StreamReader(stream))
                        {
                            serviceResponse.Raw = Encoding.UTF8.GetBytes(reader.ReadToEnd());
                        }
                    }
                    else
                    {//if contenttype is anything else read as raw bytes
                        List<byte> lbytes = new List<byte>();
                        int i;
                        while ((i = stream.ReadByte()) > -1 )
                        {
                            lbytes.Add((byte)i);
                            System.Diagnostics.Debug.WriteLine(i + " -- " + lbytes.Count());
                        }
                        serviceResponse.Raw = lbytes.ToArray();
                    }
                }
                return serviceResponse;
            }

            string RandomString(int length)
            {
                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var random = new Random();
                var randomString = new string(
                    Enumerable.Repeat(chars, length)
                              .Select(s => s[random.Next(s.Length)])
                              .ToArray());
                return randomString;
            }

	        string QueryString {
                get
                {
                    if (this.parameters.Count() == 0)
                        return "";
                    string q = "";
                    foreach (var kv in this.parameters)
                    {
                        if (!string.IsNullOrEmpty(kv.Value))
                            q += string.Format("&{0}={1}", UrlEncode(kv.Key), UrlEncode(kv.Value));
                    }
                    if (q.Contains("&"))
                        return string.Format("{0}", q.Substring(1));
                    return "";
                }
	        }

            string UrlEncode(string urlPart)
            {
                return  System.Net.WebUtility.UrlEncode(urlPart);
            }
            #endregion

            public class UnixTimestampConverter : JavaScriptConverter
            {
                //Uses reflection to invoke and populate the target type
                //conditionally converts Unix Timestamps to DateTime objects which otherwise would have thrown exceptions
                public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
                {
                    object target = type.InvokeMember(null,
                        System.Reflection.BindingFlags.DeclaredOnly |
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.CreateInstance, null, null, null);

                    foreach (var kv in dictionary)
                    {
                        if (kv.Value != null)
                        {
                            var prop = type.GetProperties().First(o => o.Name.ToLower() == kv.Key.ToLower());
                            if (prop.PropertyType == typeof(DateTime) && kv.Value.GetType() == typeof(Int64))
                                prop.SetValue(target, new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds((Int64)kv.Value));
                            else if (prop.PropertyType.BaseType == typeof(Enum))
                            {
                                if (prop.PropertyType.GetFields().Any(o => o.Name == (string)kv.Value))
                                    prop.SetValue(target, serializer.ConvertToType(kv.Value, prop.PropertyType));
                                else
                                    throw new KeyNotFoundException("Invalid enum: " + kv.Value);
                            }
                            else
                                prop.SetValue(target, serializer.ConvertToType(kv.Value, prop.PropertyType));
                        }
                    }
                    return target;
                }

                public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
                {
                    throw new NotImplementedException();
                }

                public override IEnumerable<Type> SupportedTypes
                {
                    get { return GetType().Assembly.GetTypes(); }
                }
            }
        }
        public class UploadableFile
        {
            public string fileID = "file";
            public string fileName;
            public string fileContentType;
            public byte[] data;
        }
        public class ServiceResponse<T> : IDisposable
        {
            public HttpWebResponse httpWebResponse { get; set; }
            public bool Success { get { return Status == HttpStatusCode.OK || Status == HttpStatusCode.NoContent || Status == HttpStatusCode.Created; } }
            public HttpStatusCode Status { get; set; }
            public byte[] Raw { get; set; }
            internal T Response { get; set; }
            public T GetResponse()
            {
                if (Response == null)
                {
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    ser.RegisterConverters(new[] { new SheerIDRequest.UnixTimestampConverter() });
                    T typedObject;
                    if (this.GetType().GenericTypeArguments[0] == typeof(byte[]))
                    {
                        object o = Raw; //Can't just cast Raw to T
                        typedObject = (T)o;
                    }
                    else
                    {
                            typedObject = ser.Deserialize<T>(System.Text.Encoding.UTF8.GetString(Raw));
                    }
                    return typedObject;
                }
                return Response;
            }
            public HttpError GetError()
            {
                JavaScriptSerializer ser = new JavaScriptSerializer();
                try
                {
                    return ser.Deserialize<HttpError>(System.Text.Encoding.UTF8.GetString(Raw));
                }
                catch
                {
                    return new HttpError { Message = System.Text.Encoding.UTF8.GetString(Raw) };
                }
            }

            void IDisposable.Dispose()
            {
                Raw = null;
            }

            public string ContentType { get; set; }
        }

        #region SheerID API Objects
        public class VerificationRequestTemplate
        {
            public string Id;
            public string Name;
            public VerificationRequestConfig Config { get; set; }
            public Dictionary<string, string> Metadata { get; set; }    
        }
        public class Namespace
        {
            public string TemplateId;
            public string Name;
        }
        public class VerificationResponse : ResponseErrors
        {
            public List<Affiliation> Affiliations { get; set; }
            public List<Affiliation> InactiveAffiliations { get; set; }
            public VerificationStatus Status { get; set; }
            public bool? Result { get; set; }
            public string RequestId { get; set; }
            public DateTime Timestamp { get; set; }
            public VerificationRequest Request { get; set; }
            public Dictionary<string, string> Metadata { get; set; }
        }
        public class VerificationRequest
        {
            public Dictionary<string, string> Metadata { get; set; }
            public Organization Organization { get; set; }
            public DateTime Timestamp { get; set; }
            public string UserId { get; set; }
            public VerificationRequestConfig Config { get; set; }
        }
        public class Comment
        {
            public string UserId { get; set; }
            public string Text { get; set; }
            public DateTime Created { get; set; }
        }
        public class Asset
        {
            public string ID { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public long Size { get; set; }
            public DateTime Created { get; set; }
            public AssetStatus Status { get; set; }
            public AssetType AssetType { get; set; }
            public List<Comment> Comments { get; set; }
            public List<Error> Errors { get; set; }
            public DateTime Issued { get; set; }
            public DateTime Expires { get; set; }
            public override string ToString()
            {
                return Name;
            }
        }
        public class Organization
        {
            public long ID { get; set; }	
            public string Name { get; set; }
            public OrganizationType Type { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public override string ToString()
            {
                return Name;
            }
        }
        public class VerificationRequestConfig
        {
            public List<AffiliationType> AffiliationTypes { get; set; }
            public List<VerificationType> VerificationTypes { get; set; }
            public List<AssetType> AssetTypes { get; set; }
            public List<string> RewardIds { get; set; }
        }
        public class Affiliation
        {
            public AffiliationType Type { get; set; }
            public long OrganizationId { get; set; }
            public string OrganizationName { get; set; }
            public DateTime Updated { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public List<string> Attributes { get; set; }
        }
        public class TokenResponse : ResponseErrors
        {
            public string Token { get; set; }
            public DateTime Expires { get; set; }
        }
        public class Error
        {
            public string Message { get; set; }
            public int Code { get; set; }
        }
        public abstract class ResponseErrors
        {
            public List<Error> Errors { get; set; }
        }
        public class HttpError
        {
            public string Message { get; set; }
            public int Code { get; set; }
        }
        #endregion
    }

    public enum AssetStatus { NULL, ACCEPTED, PENDING_REVIEW, REJECTED };
    public enum VerificationStatus { NULL, NEW, OPEN, PENDING, COMPLETE };
    public enum OrganizationType { NULL, UNIVERSITY, MEMBERSHIP, MILITARY, FIRST_RESPONDER, MEDICAL, NON_PROFIT, CORPORATE, K12 };
    public enum AffiliationType { NULL, STUDENT_FULL_TIME, STUDENT_PART_TIME, FACULTY, EMPLOYEE, RESELLER, NON_PROFIT, ACTIVE_DUTY, VETERAN, RESERVIST, MILITARY_FAMILY, MILITARY_RETIREE, DISABLED_VETERAN, FIREFIGHTER, POLICE, EMT, NURSE, MEMBER };
    public enum VerificationType { NULL, AUTHORITATIVE, ASSET_REVIEW, HONOR_SYSTEM };
    public enum AssetType { NULL, ID_CARD, DATED_ID_CARD, OFFICIAL_LETTER, OTHER, PAY_STUB, CLASS_SCHEDULE, TRANSCRIPT, TUITION_RECEIPT, REGISTRATION_RECEIPT, INSURANCE_CARD, DD214, REPORT_CARD };

}
