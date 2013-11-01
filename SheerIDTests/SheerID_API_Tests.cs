using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SheerID;
using System.Linq;
using System.Collections.Generic;

namespace SheerIDTests
{
    [TestClass]
    public class APITests
    {
        static API api;//TODO: specify private
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

        private const string NAMESPACE_PREFIX = "sheerid-cs-namespacetest-";
        private const string TEMPLATENAME1 = "sheerid-cs-TestingTemplate1";
        private const string TEMPLATENAME2 = "sheerid-cs-TestingTemplate2";

        [TestMethod]
        public void PutTestingTemplates()
        {//Because Templates cannot be deleted, the creation of a template will happen only once per account
            var templates = ListTemplates().GetResponse();
            if (!templates.Any(o => o.Name == TEMPLATENAME1))
            {
                Assert.IsTrue(CreateTestTemplate(TEMPLATENAME1).GetResponse().Name == TEMPLATENAME1);
            }
            if (!templates.Any(o => o.Name == TEMPLATENAME2))
            {
                Assert.IsTrue(CreateTestTemplate(TEMPLATENAME2).GetResponse().Name == TEMPLATENAME2);
            }
            ListTemplatesTest();
        }
        private API.ServiceResponse<API.VerificationRequestTemplate> CreateTestTemplate(string name)
        {
            return TestAPI.CreateTemplate(
            new System.Collections.Generic.List<AffiliationType>() { AffiliationType.STUDENT_FULL_TIME, AffiliationType.STUDENT_PART_TIME },
            new System.Collections.Generic.List<AssetType>() { AssetType.DATED_ID_CARD, AssetType.TRANSCRIPT },
            new System.Collections.Generic.List<string>() { name + "-TestingReward1", name + "-TestingReward2" },
            new System.Collections.Generic.List<VerificationType>() { VerificationType.AUTHORITATIVE, VerificationType.ASSET_REVIEW },
            name: name);
        }

        [TestMethod]
        public void GetTemplateTest()
        {
            Assert.IsTrue(GetTemplateByName(TEMPLATENAME1).GetResponse().Name == TEMPLATENAME1);
        }
        private SheerID.API.ServiceResponse<API.VerificationRequestTemplate> GetTemplateByName(string name)
        {
            return TestAPI.GetTemplate(
                ListTemplates().GetResponse().First(o => o.Name == name).Id
                );
        }

        [TestMethod]
        public void ListTemplatesTest()
        {
            Assert.IsTrue(ListTemplates().GetResponse().Any(o => o.Name == TEMPLATENAME1 || o.Name == TEMPLATENAME2));
        }
        private SheerID.API.ServiceResponse<List<API.VerificationRequestTemplate>> ListTemplates()
        {
            return TestAPI.ListTemplates();
        }

        private static string _namespace = NAMESPACE_PREFIX + Guid.NewGuid().ToString();

        [TestMethod]
        public void GetNamespace_Failure()
        {
            Assert.IsTrue(TestAPI.GetNamespace(_namespace).Status == System.Net.HttpStatusCode.NotFound);
        }

        [TestMethod]
        public void MapNamespace()
        {
            Assert.IsTrue(TestAPI.MapNamespace(_namespace, GetTemplateByName(TEMPLATENAME1).GetResponse()).GetResponse().Name == _namespace);
        }

        [TestMethod]
        public void ReMapNamespace()
        {
            Assert.IsTrue(TestAPI.MapNamespace(_namespace, GetTemplateByName(TEMPLATENAME2).GetResponse()).GetResponse().Name == _namespace);
        }

        [TestMethod]
        public void ListNamespace()
        {
            MapNamespace();
            Assert.IsTrue(TestAPI.ListNamespaces().GetResponse().Any(o => o.Name.StartsWith(NAMESPACE_PREFIX)));
        }

        [TestMethod]
        public void DeleteAllTestingNamespaces()
        {
            foreach (var remoteNamespace in TestAPI.ListNamespaces().GetResponse())
            {//Delete any namespace that starts with NAMESPACE_PREFIX followe by a GUID
                Guid tempGuid;
                if (remoteNamespace.Name.StartsWith(NAMESPACE_PREFIX) && Guid.TryParse(remoteNamespace.Name.Substring(NAMESPACE_PREFIX.Length), out tempGuid))
                {//NAMESPACE_PREFIX forced just in case
                    Assert.IsTrue(TestAPI.DeleteNamespace(NAMESPACE_PREFIX + remoteNamespace.Name.Replace(NAMESPACE_PREFIX, "")));
                }
            }
        }

    }
}
