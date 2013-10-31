using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SheerID;
using System.Linq;

namespace SheerIDTests
{
    [TestClass]
    public class APITests
    {
        static API api;
        static string accessCode="";
        static API.TokenResponse assetToken;

        private API TestAPI
        {
            get
            {
                if (api == null)
                {
                    ReadAccessCode();
                    Constructor();
                }
                return api;
            }
        }

        [TestMethod]
        public void ReadAccessCode()
        {
            //Place an access code into a file (c:\users\YOU\accesscode)
            //without the access code all remote tests will fail
            //access codes can be generated at https://services-sandbox.sheerid.com/home/tokens.html
            accessCode = System.IO.File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\accesscode");
        }

        [TestMethod]
        public void Constructor()
        {
            api = new API(accessCode);
        }

        [TestMethod]
        public void AffiliationTypes()
        {
            api.Verification.affiliationTypes = new System.Collections.Generic.List<AffiliationType>() { AffiliationType.STUDENT_FULL_TIME };
        }

        [TestMethod]
        public void AssetTypes()
        {
            api.Verification.assetTypes = new System.Collections.Generic.List<AssetType>() { AssetType.DATED_ID_CARD };
        }

        [TestMethod]
        public void PersonalInfo()
        {
            api.Verification.PersonalInfo = new API.PostVerification.UserInfo() { FIRST_NAME = "Test", LAST_NAME = "Tester" };
        }

        [TestMethod]
        public void MatchName()
        {
            api.Verification.matchName = false;
        }

        [TestMethod]
        public void OrganizationId()
        {
            api.Verification.organizationId = 3640;
        }

        [TestMethod]
        public void OrganizationName()
        {
            api.Verification.organizationName = "UNIVERSITY OF OREGON";
        }

        [TestMethod]
        public void OrganizationType()
        {
            api.Verification.organizationType  = SheerID.OrganizationType.UNIVERSITY;
        }

        [TestMethod]
        public void RewardIds()
        {
            api.Verification.rewardIds = new System.Collections.Generic.List<string>() { "asdf", "asdf" };
        }

        [TestMethod]
        public void RequestId()
        {
            api.Verification.requestId = "";
        }

        [TestMethod]
        public void VerificationTypes()
        {
            api.Verification.verificationTypes = new System.Collections.Generic.List<VerificationType>() { VerificationType.AUTHORITATIVE, VerificationType.ASSET_REVIEW };
        }

        [TestMethod]
        public void Ping()
        {
            api.Ping();
        }

        [TestMethod]
        public void GetFields()
        {
            api.GetFields(api.Verification.affiliationTypes);
        }

        [TestMethod]
        public void GetOrganizationType()
        {
            api.Verification.organizationType = api.GetOrganizationType(api.Verification.affiliationTypes).GetResponse();
        }

        [TestMethod]
        public void ListAffiliationTypes()
        {
            api.ListAffiliationTypes(api.Verification.organizationType);
        }

        [TestMethod]
        public void ListAssetTypes()
        {
            api.ListAssetTypes(api.Verification.organizationType);
        }

        [TestMethod]
        public void ListFields()
        {
            api.ListFields();
        }

        [TestMethod]
        public void ListOrganizations()
        {
            api.ListOrganizations(api.Verification.organizationType, api.Verification.organizationName);
        }

        [TestMethod]
        public void ListOrganizationTypes()
        {
            api.ListOrganizationTypes();
        }

        [TestMethod]
        public void VerificationPost_Failure()
        {
            api.Verification.PersonalInfo.BIRTH_DATE = "1981-02-28";
            using (var serviceResponse = api.Verification.PostRequest())
            {
                Assert.IsTrue(serviceResponse.GetResponse().Result == false);
            }
        }

        [TestMethod]
        public void VerificationPost_Success()
        {
            api.Verification.PersonalInfo.BIRTH_DATE = "1982-02-28";
            using (var serviceResponse = api.Verification.PostRequest())
            {
                Assert.IsTrue(serviceResponse.GetResponse().Result == true);
            }
        }

        [TestMethod]
        public void VerificationPost_AssetsRequired()
        {
            api.Verification.PersonalInfo.BIRTH_DATE = null;
            using (var serviceResponse = api.Verification.PostRequest())
            {
                Assert.IsTrue(serviceResponse.GetResponse().Errors[0].Code == 39);
                Assert.IsTrue(serviceResponse.GetResponse().Errors[0].Message == "Awaiting documentation upload");
                api.Verification.requestId = serviceResponse.GetResponse().RequestId; //needed for asset upload
                Assert.IsNull(serviceResponse.GetResponse().Result);
            }
        }

        [TestMethod]
        public void UpdateMetadata()
        {
            Assert.IsTrue(api.UpdateMetadata(api.Verification.requestId, new System.Collections.Generic.Dictionary<string, string>() { { "meta", "data" } }));
        }

        [TestMethod]
        public void UpdateOrderId()
        {
            Assert.IsTrue(api.UpdateOrderId(api.Verification.requestId, "testOrder"));
        }

        [TestMethod]
        public void UpdateVerification()
        {
            Assert.IsNotNull(api.Verification.requestId);
            api.Verification.PersonalInfo.LAST_NAME = "Tester2";
            using (var serviceResponse = api.Verification.PostRequest())
            {
                Assert.IsNull(serviceResponse.GetResponse().Result);
            }
        }

        [TestMethod]
        public void PostAssetToken()
        {
            assetToken = api.GetAssetToken(api.Verification.requestId).GetResponse();
        }

        [TestMethod]
        public void PostAsset()
        {
            Assert.IsNotNull(assetToken);
            
            var files = new System.Collections.Generic.List<SheerID.API.UploadableFile>();
            var file = new API.UploadableFile();
            file.fileContentType = "image/bmp";
            file.fileName = "TestAsset.bmp";
            var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("SheerIDTests.TestAsset.bmp");
            file.data = new byte[stream.Length];
            stream.Read(file.data, 0, (int)stream.Length);
            files.Add(file);

            Assert.IsTrue(api.PostAsset(assetToken, files).GetResponse()[0].Status == AssetStatus.PENDING_REVIEW);
        }

        [TestMethod]
        public void ListAssets()
        {
            using (var serviceResponse = api.ListAssets(api.Verification.requestId))
            {
                Assert.IsTrue(serviceResponse.GetResponse()[0].Name == "TestAsset.bmp");
            }
        }

        [TestMethod]
        public void Inquire()
        {
            using (var serviceResponse = api.Inquire(api.Verification.requestId))
            {
                Assert.IsTrue(serviceResponse.GetResponse().Status == VerificationStatus.PENDING);
            }
        }

        [TestMethod]
        public void RevokeToken()
        {
            Assert.IsTrue(api.RevokeToken(assetToken));
        }

        private const string NAMESPACE_PREFIX = "SheerID-cs-NameSpaceTest-";
        API.Namespace _namespace;
        private API.Namespace TestNamespace
        {
            get
            {
                if (_namespace == null)
                {
                    _namespace = new API.Namespace() { Name = NAMESPACE_PREFIX + Guid.NewGuid().ToString() };
                }
                return _namespace;
            }
        }

        [TestMethod]
        public void GetNameSpace_Failure()
        {
            Assert.IsTrue(TestAPI.GetNamespace(TestNamespace).Status == System.Net.HttpStatusCode.NotFound);
        }

        [TestMethod]
        public void MapNameSpace()
        {
            //TODO: replace with real template
            Assert.IsTrue(TestAPI.MapNamespace(TestNamespace, new API.VerificationRequestTemplate()).Status == System.Net.HttpStatusCode.NoContent);
        }

        [TestMethod]
        public void ReMapNameSpace()
        {
            //TODO: replace with real template, but different than in MapNameSpace
            Assert.IsTrue(TestAPI.MapNamespace(TestNamespace, new API.VerificationRequestTemplate()).Status == System.Net.HttpStatusCode.NoContent);
        }

        [TestMethod]
        public void ListNameSpace()
        {
            Assert.IsTrue(TestAPI.ListNamespaces().GetResponse().Any(o => o.Name == TestNamespace.Name));
        }

        [TestMethod]
        public void DeleteAllTestingNameSpaces()
        {
            foreach (var remoteNamespace in TestAPI.ListNamespaces().GetResponse())
            {//Delete any namespace that starts with NAMESPACE_PREFIX followe by a GUID
                Guid tempGuid;
                if (remoteNamespace.Name.StartsWith(NAMESPACE_PREFIX)
                    && Guid.TryParse(remoteNamespace.Name.Substring(NAMESPACE_PREFIX.Length), out tempGuid))
                {//NAMESPACE_PREFIX forced just in case
                    Assert.IsTrue(TestAPI.DeleteNamespace(new API.Namespace() { Name = NAMESPACE_PREFIX + remoteNamespace.Name.Replace(NAMESPACE_PREFIX, "") }));
                }
            }
        }
    }
}
