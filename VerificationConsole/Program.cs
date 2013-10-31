using ConsoleTools;
using SheerID;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerificationConsole
{
    class Program
    {
        static Output output = new Output(typeof(SheerID.API).Assembly);
        static void Main(string[] args)
        {

            var accessCode = "";
            var keypath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\accesscode";
            if (System.IO.File.Exists(keypath))
            {
                accessCode = System.IO.File.ReadAllText(keypath);
            }
            while (accessCode == "")
            {
                Console.Write("Enter your access code: ");
                accessCode = Console.ReadLine();
                var r = new System.Text.RegularExpressions.Regex("[a-zA-Z0-9]*"); //block unwanted characters
                accessCode = r.Match(accessCode).Value;
            }
            var sheerid = new SheerID.API(accessCode);
            if (sheerid.Ping())
            {
                while (1 == 1)
                {
                    Console.Clear();
                    var choices = new[] { "Verify", "Namespace Management" };
                    var choice = output.PromptKeyMatrix<string>(choices, "Main Menu:");
                    if (choice == choices[0]) { VerifyConsole(sheerid); }
                }

            }
            else
            {
                Console.WriteLine("SheerID service is inaccessible.");
            }
            Console.ReadKey();
            
        }

        static void VerifyConsole(SheerID.API sheerid)
        {
            var verification = sheerid.Verification;

            Console.Clear();
            using (var serviceResponse = sheerid.ListOrganizationTypes())
            {
                if (IsError(serviceResponse)) { return; }
                verification.organizationType = output.PromptKeyMatrix<OrganizationType>(serviceResponse.GetResponse(), "Organization Types");
            }

            var organizationNameFilter = output.PromptFor<string>("Enter an organization search filter or hit enter for the complete list.");

            Console.Clear();

            using (var serviceResponse = sheerid.ListOrganizations(verification.organizationType, organizationNameFilter))
            {
                if (IsError(serviceResponse)) { return; }
                var organizations = serviceResponse.GetResponse();
                if (IsZeroCount(organizations)) { return; }
                var organization = output.PromptKeyMatrix<SheerID.API.Organization>(serviceResponse.GetResponse(), "Organizations");
                verification.organizationId = organization.ID;
                verification.organizationName = organization.Name;
            }

            Console.Clear();
            using (var serviceResponse = sheerid.ListAffiliationTypes(verification.organizationType))
            {
                if (IsError(serviceResponse)) { return; }
                foreach (var selection in output.PromptKeyMatrix<SheerID.AffiliationType>(serviceResponse.GetResponse(), "Affiliation Types", Output.MatrixSelectionMethod.SelectMany))
                    verification.affiliationTypes.Add(selection.Value);
            }

            Console.Clear();
            var infoCollection = verification.PersonalInfo.GetType().GetFields().Where(o => sheerid.GetFields(verification.affiliationTypes).GetResponse().Contains(o.Name)).ToArray();
            var infoMatrix = output.CreateKeyMatrix<System.Reflection.FieldInfo>(infoCollection);
            var infoType = verification.PersonalInfo.GetType();
            var infoRenameKey = new Dictionary<string, string>()
                    {
                        {"EMAIL", "Email"},
                        {"FIRST_NAME", "First Name"},
                        {"MIDDLE_NAME", "Middle Name"},
                        {"LAST_NAME", "Last Name"},
                        {"FULL_NAME", "Full Name"},
                        {"COMPANY_NAME", "Company Name"},
                        {"BIRTH_DATE", "Birth Date [ex. 1996-02-28]"},
                        {"ID_NUMBER", "ID Number"},
                        {"JOB_TITLE", "Job Title"},
                        {"USERNAME", "Username"},
                        {"POSTAL_CODE", "Postal Code"},
                        {"PHONE_NUMBER", "Phone Number"},
                        {"SSN", "SSN"},
                        {"SSN_LAST4", "SSN Last 4"},
                        {"STATUS_START_DATE", "Status Start Date"},
                        {"SUFFIX", "Suffix"},
                        {"RELATIONSHIP", "Relationship"}
                    };

            foreach (var f in infoMatrix)
                if (infoRenameKey.ContainsKey(f.Value.Name))
                    f.Key.DisplayName = infoRenameKey[f.Value.Name];

            while (1 == 1)
            {
                if (verification.requestId != null)
                {
                    Console.Clear();
                    var choices = new[] { "Inquire on status", "Update personal information", "Upload assets", "List assets", "Post a new verification", "Download asset to desktop" };
                    var choice = output.PromptKeyMatrix<string>(choices, "Request exists:");
                    if (choice == choices[0])//inquire
                    {
                        using (var serviceResponse = sheerid.Inquire(verification.requestId))
                        {
                            if (IsError(serviceResponse)) { continue; }
                            output.OutputObject(serviceResponse.GetResponse());
                        }
                        Console.ReadKey();
                        continue; //skip past verification post
                    }
                    else if (choice == choices[1])//update
                    {
                        //do nothing
                    }
                    else if (choice == choices[2])//upload
                    {
                        Console.Clear();
                        var files = new List<SheerID.API.UploadableFile>();
                        using (var serviceResponse = sheerid.ListAssetTypes(verification.organizationType))
                        {
                            if (IsError(serviceResponse)) { return; }
                            foreach (var selection in output.PromptKeyMatrix<SheerID.AssetType>(serviceResponse.GetResponse(), "Asset Types", Output.MatrixSelectionMethod.SelectMany))
                            {
                                verification.assetTypes.Add(selection.Value);
                                Console.Write(selection.Value.ToString() + " [TestImage.png]>");
                                var filePath = Console.ReadLine();

                                var file = new SheerID.API.UploadableFile();
                                if (filePath == "")
                                {
                                    file.fileContentType = "image/png";
                                    file.fileName = "TestImage.png";
                                    var stream = System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream("VerificationConsole.TestImage.png");
                                    file.data = new byte[stream.Length];
                                    stream.Read(file.data, 0, (int)stream.Length);
                                }
                                else
                                {
                                    file.fileContentType = System.Web.MimeMapping.GetMimeMapping(filePath);
                                    file.fileName = System.IO.Path.GetFileName(filePath);
                                    file.data = System.IO.File.ReadAllBytes(filePath);
                                }
                                files.Add(file);
                            }
                        }
                        var assetToken = sheerid.GetAssetToken(verification.requestId).GetResponse();

                        Console.WriteLine("Upload Response:");
                        output.OutputObject(sheerid.PostAsset(assetToken, files).GetResponse(), depth: 1);
                        Console.ReadKey();
                        continue; //skip past verification post
                    }
                    if (choice == choices[3])//list assets
                    {
                        using (var serviceResponse = sheerid.ListAssets(verification.requestId))
                        {
                            if (IsError(serviceResponse)) { continue; }
                            output.OutputObject(serviceResponse.GetResponse());
                        }
                        Console.ReadKey();
                        continue; //skip past verification post
                    }
                    else if (choice == choices[4])//new
                    {
                        verification.requestId = null;
                    }
                    else if (choice == choices[5])//new
                    {
                        Console.Write("AssetID: ");
                        Console.Clear();
                        using (var serviceResponse = sheerid.ListAssets(verification.requestId))
                        {
                            if (IsError(serviceResponse)) { continue; }
                            var assets = serviceResponse.GetResponse();
                            if (IsZeroCount(assets)) { continue; }
                            foreach (var selection in output.PromptKeyMatrix<SheerID.API.Asset>(serviceResponse.GetResponse(), "Assets", Output.MatrixSelectionMethod.SelectMany))
                            {
                                Console.WriteLine(selection.Value.Name + " download started...");
                                using (var dataResponse = sheerid.GetAssetData(selection.Value.ID))
                                {
                                    if (IsError(dataResponse)) { continue; }
                                    var fileData = dataResponse.GetResponse();
                                    var path = output.GetUniqueFilePath(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), selection.Value.Name));
                                    System.IO.File.WriteAllBytes(path, fileData);
                                    Console.WriteLine("Saved: " + path);
                                }
                            }
                        }
                        Console.ReadKey();
                        continue;
                    }
                }

                Console.Clear();
                foreach (var selection in output.PromptKeyMatrix<System.Reflection.FieldInfo>(infoMatrix, infoCollection, "Personal Info", Output.MatrixSelectionMethod.SelectMany))
                {
                    object oldValue = selection.Value.GetValue(verification.PersonalInfo);
                    Console.Write(infoMatrix.First(o => o.Value == infoType.GetField(selection.Value.Name)).Key.DisplayName + (oldValue != null ? " [" + oldValue + "]" : "") + ": ");
                    string newValue = Console.ReadLine();
                    selection.Value.SetValue(verification.PersonalInfo, newValue != "" ? newValue : oldValue);
                }

                Console.Clear();
                Console.WriteLine("Verification Response:");
                using (var verificationServiceResponse = verification.PostRequest())
                {
                    if (IsError(verificationServiceResponse)) { continue; }
                    var verificationResponse = verificationServiceResponse.GetResponse();
                    output.OutputObject(verificationResponse, depth: 1);

                    if (verificationServiceResponse.Status == System.Net.HttpStatusCode.OK)
                    {
                        verification.requestId = verificationResponse.RequestId; //if this object is posted again it will be an update
                        Console.WriteLine("Status: " + verificationResponse.Status + " Result: " + verificationResponse.Result);
                        foreach (var error in verificationResponse.Errors)
                        {
                            Console.WriteLine(error.Code + " " + error.Message);
                        }
                    }
                    Console.ReadKey();
                }
            }
        }

        static bool IsError<T>(SheerID.API.ServiceResponse<T> serviceResponse)
        {
            if (!serviceResponse.Success)
            {
                var error = serviceResponse.GetError();
                output.OutputObject(error);
                Console.ReadKey();
                return true;
            }
            return false;
        }
        static bool IsZeroCount(System.Collections.ICollection collection)
        {
            if (collection.Count == 0)
            {
                Console.WriteLine("0 matchs found.");
                Console.ReadKey();
                return true;
            }
            return false;
        }
    }
}
